namespace Tachyon.Actors

open System
open System.Threading.Tasks

type NodeId = int

[<Struct>]
type ActorId = { NodeId: NodeId; ActorId: int }

[<Interface>]
type ISilient = interface end

[<Interface>]
type ISignal = 
    inherit ISilient

[<Interface>]
type IAddressable =
    abstract member Send: signal:ISignal -> unit

[<Interface>]
type IRef<'M> = 
    abstract member Send: message:'M -> unit
    inherit IAddressable

[<Interface>]
type IRuntime = 
    abstract member Main: IAddressable

[<Interface>]
type ICell<'S, 'M> = 
    abstract member Self: IRef<'M>
    abstract member Runtime: IRuntime

type Receive<'S,'M> = ICell<'S,'M> -> 'S -> 'M -> System.Threading.Tasks.ValueTask<Behavior<'S,'M>>
and  Signalize<'S,'M> = ICell<'S,'M> -> 'S -> ISignal -> System.Threading.Tasks.ValueTask<Behavior<'S,'M>>

and [<Struct>] Behavior<'S, 'M> =
    | Deferred of (unit -> Behavior<'S,'M>)
    | Unhandled
    | Same
    | Receive of state:'S * onMessage:Receive<'S,'M> * onSignal:Signalize<'S,'M>
    member x.OnSignal(onSignal: Signalize<'S,'M>) =
        match x with
        | Receive(state, onMessage,_) -> Receive(state, onMessage, onSignal)
        | _ -> invalidOp "Only receive behavior can define Signal receiver"

module Actor =

    let inline defaultSignal _ _ signal =
        match signal with
        | _ -> ValueTask(Same)
    
    let immutable<'S,'M> (state: 'S) (onMessage: ICell<'S,'M> -> 'S -> 'M -> Behavior<'S,'M>): Behavior<'S,'M> =
        let onMsg c s m = onMessage c s m |> ValueTask
        Receive(state, onMsg, defaultSignal)
        
    let asyncImmutable<'S,'M>(state: 'S) (onMessage: ICell<'S,'M> -> 'S -> 'M -> Async<Behavior<'S,'M>>): Behavior<'S,'M> =
        let onMsg c s m = 
            let t = onMessage c s m |> Async.StartAsTask
            ValueTask<Behavior<'S,'M>>(t)
        Receive(state, onMsg, defaultSignal)

    let stateless<'M> (onMessage: ICell<unit, 'M> -> 'M -> Behavior<unit, 'M>): Behavior<unit, 'M> =
        immutable<unit, 'M> () (fun c _ m -> onMessage c m)
        
    let asyncStateless<'M> (onMessage: ICell<unit, 'M> -> 'M -> Async<Behavior<unit, 'M>>): Behavior<unit, 'M> =
        asyncImmutable<unit, 'M> () (fun c _ m -> onMessage c m)