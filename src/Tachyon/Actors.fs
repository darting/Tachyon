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
type ICell<'M> = 
    abstract member Self: IRef<'M>
    abstract member Runtime: IRuntime

type Receive<'M> = ICell<'M> -> 'M -> ValueTask<Behavior<'M>>
and  Signalize<'M> = ICell<'M> -> ISignal -> ValueTask<Behavior<'M>>

and Behavior<'M> =
    | Deferred of (unit -> Behavior<'M>)
    | Unhandled
    | Same
    | Receive of onMessage:Receive<'M> * onSignal:Signalize<'M>
    member x.OnSignal(onSignal: Signalize<'M>) =
        match x with
        | Receive(onMessage,_) -> Receive(onMessage, onSignal)
        | _ -> invalidOp "Only receive behavior can define Signal receiver"

module Actor =

    let inline defaultSignal _ signal =
        match signal with
        | _ -> ValueTask(Same)
    
    let immutable<'M>(onMessage: ICell<'M> -> 'M -> Behavior<'M>): Behavior<'M> =
        let onMsg c m = onMessage c m |> ValueTask
        Receive(onMsg, defaultSignal)
        
    let asyncImmutable<'M>(onMessage: ICell<'M> -> 'M -> Async<Behavior<'M>>): Behavior<'M> =
        let onMsg c m = 
            let t = onMessage c m |> Async.StartAsTask
            ValueTask<Behavior<'M>>(t)
        Receive(onMsg, defaultSignal)