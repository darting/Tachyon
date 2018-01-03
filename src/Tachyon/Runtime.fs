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
module internal CellStatus =
    
    [<Literal>] 
    let Uninitialized = 0
    
    [<Literal>] 
    let Idle = 1
    
    [<Literal>] 
    let Busy = 2


[<Sealed>]
type internal LocalRef<'S, 'M > (cell: Cell<'S,'M>) =
    interface IRef<'M> with
        member this.Send(message: 'M): unit = cell.PostMessage (message)
        member this.Send(signal: ISignal): unit = cell.PostSignal (signal)

and [<Sealed>] internal Cell<'S,'M> (runtime: IActorRuntime, signals: BoundedQueue<ISignal>, messages: UnboundedQueue<'M>, init:Behavior<'S,'M>) as this =
    let mutable status = 0
    let mutable current = Unchecked.defaultof<Behavior<'S,'M>>
    let mutable self = LocalRef(this)
    do
        Interlocked.Exchange(&status, CellStatus.Idle) |> ignore
        current <-
            match init with
            | Deferred (callback) -> callback(this)
            | other -> other
    member this.RunAsync (): Task = upcast Task.FromResult()
    member this.PostSignal(signal: ISignal): unit = 
        if signals.TryPush(&signal)
        then
            runtime.Scheduler.ScheduleTask(this.RunAsync)

    member this.PostMessage(message: 'M) = 
        if messages.TryPush(message)
        then
            runtime.Scheduler.ScheduleTask(this.RunAsync)
    interface ICell<'S,'M> with
        member this.Runtime = runtime
        member this.Self = upcast self
        member this.Spawn(behavior) = runtime.Spawn(behavior)
    interface IAsyncDisposable with
        member this.Dispose(): unit = 
            raise (System.NotImplementedException())
        member this.DisposeAsync(token: CancellationToken): Task = 
            raise (System.NotImplementedException())
            
[<Sealed>]
type ActorTaskScheduler() =
    let factory = Task.Factory
    interface IScheduler with
        member this.ScheduleTask(fn: unit -> Task): unit = Task.Run(Func<Task>(fn), CancellationToken.None) |> ignore

and [<Sealed>] internal ActorRuntime() as this =
    let timer: ITimer = upcast new DefaultTimer()
    let scheduler: IScheduler = upcast ActorTaskScheduler()
    let actors = HashedConcurrentDictionary(null)
    member this.DisposeAsync(token: CancellationToken) =
        timer.Dispose()
        Tasks.Task.CompletedTask
    interface IActorRuntime with
        member this.Dispose() = this.DisposeAsync(CancellationToken.None).Wait 10000 |> ignore
        member this.DisposeAsync(token) = this.DisposeAsync(token)
        member this.Scheduler = scheduler
        member this.Spawn(behavior) = failwith "NotImplemented"
        member this.Timer = timer
    