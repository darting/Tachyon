namespace Tachyon.Streams

open System
open Reactive.Streams

[<Struct>]
type internal Signal<'T> =
    | Next of value:'T
    | Error of error:exn
    | Complete

type Sink<'I, 'Effect>() =
    interface ISubscriber<'I> with
        member this.OnComplete() = raise (System.NotImplementedException())
        member this.OnError(cause) = raise (System.NotImplementedException())
        member this.OnNext(element) = raise (System.NotImplementedException())
        member this.OnSubscribe(subscription) = raise (System.NotImplementedException())

type Source<'O, 'Effect>() =
    interface IPublisher<'O> with
        member this.Subscribe(subscriber) = raise (System.NotImplementedException())

type Stream<'I, 'O, 'Effect> () =
    interface IProcessor<'I,'O> with
        member this.OnComplete() = raise (System.NotImplementedException())
        member this.OnError(cause) = raise (System.NotImplementedException())
        member this.OnNext(element) = raise (System.NotImplementedException())
        member this.OnSubscribe(subscription) = raise (System.NotImplementedException())
        member this.Subscribe(subscriber) = raise (System.NotImplementedException())