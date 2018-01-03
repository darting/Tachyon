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

[<Sealed>]
type internal Cell<'S,'M> (runtime: IActorRuntime, signals: BoundedQueue<ISignal>, messages: UnboundedQueue<'M>, init:Behavior<'S,'M>) as this =
    let mutable status = 0
    let mutable current = Unchecked.defaultof<Behavior<'S,'M>>
    do
        current <-
            match init with
            | Deferred (callback) -> callback(this)
            | other -> other
    interface ICell<'S,'M> with
        member this.Runtime = runtime
        member this.Self = raise (System.NotImplementedException())
        member this.Spawn(behavior) = raise (System.NotImplementedException())
    interface IAsyncDisposable with
        member this.Dispose(): unit = 
            raise (System.NotImplementedException())
        member this.DisposeAsync(token: CancellationToken): Task = 
            raise (System.NotImplementedException())

[<Sealed>]
type internal ActorRuntime() as this =
    let timer: ITimer = upcast new DefaultTimer()
    let actors = HashedConcurrentDictionary(null)
    member this.DisposeAsync(token: CancellationToken) =
        timer.Dispose()
        Tasks.Task.CompletedTask
    interface IActorRuntime with
        member this.Dispose() = this.DisposeAsync(CancellationToken.None).Wait 10000 |> ignore
        member this.DisposeAsync(token) = this.DisposeAsync(token)
        member this.Scheduler = Tasks.Task.Factory.Scheduler
        member this.Spawn(behavior) = failwith "NotImplemented"
        member this.Timer = timer
    