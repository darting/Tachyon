namespace Tachyon.Actors

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Tasks
open Tachyon.Core.Atomic
open Tachyon.Core
open Tachyon.Actors.Mailbox

type internal DefaultTimer() =
    let disposed = atomb false
    let lock = new ReaderWriterLockSlim()
    let disposables = HashSet<IDisposable>()
    let finish (d: IDisposable)= 
        d.Dispose()
        try
            lock.EnterWriteLock()
            disposables.Remove(d) |> ignore
        finally
            lock.ExitWriteLock()

    let register (disposable: IDisposable) (token: CancellationToken) = 
        if token.CanBeCanceled then token.Register(System.Action(fun () -> finish disposable)) |> ignore
        try
            lock.EnterWriteLock()
            disposables.Add disposable |> ignore
        finally
            lock.ExitWriteLock()

    interface ITimer with

        member this.Schedule(delay: System.TimeSpan, target: IRef<'M>, message: 'M, token: CancellationToken): unit = 
            if !disposed then raise (ObjectDisposedException "Cannot schedule message, because timer has been already disposed")
            if not token.IsCancellationRequested
            then
                // fire once jobs are registered with Task.Delay
                let delayedSend () = task {
                    do! Task.Delay(delay, token)
                    target <! message
                }
                let task = Task.Run<unit>(Func<Task<unit>>(delayedSend), token)
                register task token
        
        member this.Schedule(delay: System.TimeSpan, interval: System.TimeSpan, target: IRef<'M>, message: 'M, token: CancellationToken): unit = 
            if !disposed then raise (ObjectDisposedException "Cannot schedule message, because timer has been already disposed")
            if not token.IsCancellationRequested
            then
                // messages triggered repeatadelly are registered using Timers
                let timer = new Timer(TimerCallback(fun _ -> target <! message), null, delay, interval)
                register timer token

        member this.Dispose () =
            if disposed := true = false
            then 
                disposables |> Seq.iter (fun d -> d.Dispose())
                disposables.Clear()

[<RequireQualifiedAccess>]
module internal CellConstants =
    
    [<Literal>] 
    let Uninitialized = 0
    
    [<Literal>] 
    let Idle = 1
    
    [<Literal>] 
    let Busy = 2

    [<Literal>]
    let Throughtput = 128
    
[<Sealed>]
type internal LocalRef<'S, 'M > (pid: ActorId, cell: Cell<'S,'M>) =
    member this.ActorId = pid
    interface IRef<'M> with
        member this.Send(message: 'M): unit = cell.PostMessage (message)
        member this.Send(signal: ISignal): unit = cell.PostSignal (signal)

and [<Sealed>] internal Cell<'S,'M> (runtime: IActorRuntime, signals: BoundedQueue, messages: UnboundedQueue<'M>, pid: ActorId, init:Behavior<'S,'M>) as this =
    let mutable status = 0
    let mutable current = Unchecked.defaultof<Behavior<'S,'M>>
    let mutable self = LocalRef(pid, this)
    do
        Interlocked.Exchange(&status, CellConstants.Idle) |> ignore
        current <- Actor.undefer this init
    let handleNext next msgOrSig = 
        match next with
        | Unhandled -> runtime.DeadLetter msgOrSig
        | Same -> ()
        | Receive _ -> current <- next
        | Deferred _ -> failwith "Deferred can be used only at start"

    let executeSignal (signal: ISignal): Task = 
        let nextTask = 
            match current with
            | Receive(state, _, handle) -> handle this state signal
            | _ -> failwithf "Cannot receive messages using %s" (string current)
        if nextTask.IsCompletedSuccessfully
        then 
            let next = nextTask.Result
            handleNext next signal
            Task.CompletedTask
        else upcast task {
            let! next = nextTask.AsTask()
            handleNext next signal
        }        
        
    let executeMessage (msg: 'M): Task = 
        let nextTask = 
            match current with
            | Receive(state, handle, _) -> handle this state msg
            | _ -> failwithf "Cannot receive messages using %s" (string current)
        if nextTask.IsCompletedSuccessfully
        then 
            let next = nextTask.Result
            handleNext next msg
            Task.CompletedTask
        else upcast task {
            let! next = nextTask.AsTask()
            handleNext next msg
        }

    let schedule fn = 
        if Interlocked.CompareExchange(&status, CellConstants.Busy, CellConstants.Idle) = CellConstants.Idle
        then runtime.Scheduler.ScheduleTask fn

    let rec execute(): Task =
        let reshedule: Action<Task,obj> = Action<Task,obj>(fun t msgOrSig -> schedule execute)
        let rec exec i =
            match signals.TryPop() with
            | true, signal ->
                let t = executeSignal signal
                if not t.IsCompleted 
                then 
                    t.ContinueWith(reshedule, box signal) |> ignore
                    false
                else exec (i-1)
            | false, _ -> 
                match messages.TryPop() with
                | true, msg -> 
                    let t = executeMessage msg
                    if not t.IsCompleted 
                    then 
                        t.ContinueWith(reshedule, box msg) |> ignore
                        false
                    else exec (i-1)
                | false, _  -> true
        let finished = exec CellConstants.Throughtput
        if finished
        then
            Interlocked.Exchange(&status, CellConstants.Idle) |> ignore
            if signals.HasMessages && messages.HasMessages
            then
                schedule execute
        Task.CompletedTask

    member this.PostSignal(signal: ISignal): unit = 
        if signals.TryPush(signal) then schedule execute

    member this.PostMessage(message: 'M): unit = 
        if messages.TryPush(message) then schedule execute
    member this.Self: IRef<'M> = upcast self
    interface ICell<'S,'M> with
        member this.Runtime = runtime
        member this.Self = this.Self
        member this.Spawn(behavior, name) = runtime.Spawn(behavior, name)
    interface IAsyncDisposable with
        member this.Dispose(): unit = 
            raise (System.NotImplementedException())
        member this.DisposeAsync(token: CancellationToken): Task = 
            raise (System.NotImplementedException())
    interface ICell
            
[<Sealed>]
type ActorTaskScheduler() =
    let factory = Task.Factory
    interface IScheduler with
        member this.ScheduleTask(fn: unit -> Task): unit = Task.Run(Func<Task>(fn), CancellationToken.None) |> ignore

and [<Sealed>] ActorRuntime private() =
    let timer: ITimer = upcast new DefaultTimer()
    let scheduler: IScheduler = upcast ActorTaskScheduler()
    let actors = HashedConcurrentDictionary(null)
    static member Start(): IActorRuntime = upcast new ActorRuntime()
    member this.DisposeAsync(token: CancellationToken): Task = upcast task {
        let disposables =
            actors 
            |> Seq.map (fun p -> p.Value)
            |> Seq.map (disposeAsync token)
            |> Seq.toArray
        do! Task.WhenAll(disposables)
        timer.Dispose()
    }
    interface IActorRuntime with
        member this.Dispose() = this.DisposeAsync(CancellationToken.None).Wait 10000 |> ignore
        member this.DisposeAsync(token) = this.DisposeAsync(token)
        member this.Scheduler = scheduler
        member this.Timer = timer
        member this.DeadLetter(msg) = printfn "DeadLetter: %A" msg
        member this.Spawn<'S,'M>(behavior: Behavior<'S,'M>, name: string) = 
            let signals = BoundedQueue(CellConstants.Throughtput)
            let messages = UnboundedQueue()
            let pid = Murmur.Hash(name)
            let cell = new Cell<'S,'M>(this, signals, messages, pid, behavior)
            if actors.TryAdd(name, cell)
            then
                cell.Self.Send(Activated)
                cell.Self
            else failwith "Actor already exists"