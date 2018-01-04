namespace Tachyon.Core

open System

[<Interface>]
type IConvergent<'T> =
    abstract member Merge: other:'T -> 'T

type ReplicaId = int16

[<Struct>]
type VectorTime = 
    { PartialTimes: Map<ReplicaId, uint64> }
    interface IPartiallyComparable<VectorTime> with
        member this.PartiallyCompareTo(other) = raise (System.NotImplementedException())
    interface IConvergent<VectorTime> with
        member this.Merge(arg1) = raise (System.NotImplementedException())

[<Struct;CustomComparison;CustomEquality>]
type Version =
    { SystemTime: DateTime
      VectorTime: VectorTime }
    interface IComparable<Version> with
        member this.CompareTo(other) = raise (System.NotImplementedException())
    interface IEquatable<Version> with
        member this.Equals(other: Version): bool = raise (System.NotImplementedException())
    interface IConvergent<Version> with
        member this.Merge(arg1) = raise (System.NotImplementedException())

[<Struct>]
type Versioned<'T> =
    { Value: 'T
      Version: Version }

[<RequireQualifiedAccess>]
module Option =

    let orElse (value: 'a) = function Some a -> a | None -> value
    let orElseLazy (valueFn: unit -> 'a) = function Some a -> a | None -> valueFn ()

[<AutoOpen>]
module Prolog =
    let inline dispose (d: #IDisposable) = d.Dispose()
    let inline disposeAsync token (d: #IAsyncDisposable) = d.DisposeAsync(token)