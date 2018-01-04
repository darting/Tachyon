namespace Tachyon.Actors

open System
open System.Threading.Tasks
open Tachyon.Core
open System.Threading

type NodeId = int

[<AutoOpen>]
module ActorOperators =
    let inline (<!) (ref: IRef<'M>) (msg: 'M) = ref.Send(msg)

[<Struct>]
type ActorId = { NodeId: NodeId; ActorId: int }

type Receive<'S,'M> = ICell<'S,'M> -> 'S -> 'M -> System.Threading.Tasks.ValueTask<Behavior<'S,'M>>
and  Signalize<'S,'M> = ICell<'S,'M> -> 'S -> ISignal -> System.Threading.Tasks.ValueTask<Behavior<'S,'M>>

and Behavior<'S, 'M> =
    | Deferred of (ICell<'S,'M> -> Behavior<'S,'M>)
    | Unhandled
    | Same
    | Receive of state:'S * onMessage:Receive<'S,'M> * onSignal:Signalize<'S,'M>
    member x.OnSignal(onSignal: Signalize<'S,'M>) =
        match x with
        | Receive(state, onMessage,_) -> Receive(state, onMessage, onSignal)
        | _ -> invalidOp "Only receive behavior can define Signal receiver"

and [<Interface>] ICell<'S,'M> =
    abstract member Self: IRef<'M>
    abstract member Runtime: IActorRuntime
    abstract member Spawn: (Behavior<'S,'M>) -> IRef<'M>
    
and [<Interface>] IScheduler =
    abstract member ScheduleTask: (unit -> Task) -> unit      

and [<Interface>] IActorRuntime =
    inherit IAsyncDisposable
    abstract member Scheduler: IScheduler
    abstract member Timer: ITimer
    abstract member Spawn: (Behavior<'S,'M>) -> IRef<'M>
    abstract member DeadLetter: 'M -> unit

// SIGNALS
type Activated = Activated interface ISignal
type Deactivated = Deactivated of exn option interface ISignal
    
[<RequireQualifiedAccess>]
module Actor =

    let inline defaultSignal _ _ signal =
        match signal with
        | _ -> ValueTask(Same)
    
    let inline deferred<'S, 'M> (next: ICell<'S,'M> -> Behavior<'S,'M>) = Deferred(next)

    let immutable<'S,'M> (state: 'S) (onMessage: ICell<'S,'M> -> 'S -> 'M -> Behavior<'S,'M>): Behavior<'S,'M> =
        let onMsg c s m = onMessage c s m |> ValueTask
        Receive(state, onMsg, defaultSignal)
        
    let asyncImmutable<'S,'M>(state: 'S) (onMessage: ICell<'S,'M> -> 'S -> 'M -> Task<Behavior<'S,'M>>): Behavior<'S,'M> =
        let onMsg c s m = 
            let t = onMessage c s m
            ValueTask<Behavior<'S,'M>>(t)
        Receive(state, onMsg, defaultSignal)

    let stateless<'M> (onMessage: ICell<unit, 'M> -> 'M -> Behavior<unit, 'M>): Behavior<unit, 'M> =
        immutable<unit, 'M> () (fun c _ m -> onMessage c m)
        
    let asyncStateless<'M> (onMessage: ICell<unit, 'M> -> 'M -> Task<Behavior<unit, 'M>>): Behavior<unit, 'M> =
        asyncImmutable<unit, 'M> () (fun c _ m -> onMessage c m)

    let internal undefer cell =
        function
        | Deferred next -> next(cell)
        | other -> other

module Supervisor =
    
    type Strategy =
        | Restart of logging:bool * retries:int option * timeout:TimeSpan
        | Resume  of logging:bool
        | Backoff of logging:bool * min:TimeSpan * max:TimeSpan * factor:double * reset:TimeSpan

    /// Keeps actor alive without changing its state (just as if exception didn't happen).
    let resume = Resume(true)

    /// Upon error, restarts actor immediatelly without any limit of retries.
    let restart = Restart(true, None, TimeSpan.Zero)

    let restartWithLimit retries timeout = Restart(true, Some(retries), timeout)

    let restartWithBackoff min max factor = Backoff(true, min, max, factor, min)
    