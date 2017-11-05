namespace Tachyon.Collections.Replicable.Convergent

open Tachyon.Core

type GCounter = 
    { State: Map<ReplicaId, uint32> }
    member x.Value: uint64 = x.State |> Map.fold (fun acc _ v -> acc + uint64(v)) 0UL
    member x.Increment replicaId value = 
        match Map.tryFind replicaId x.State with
        | Some v -> { State = Map.add replicaId (v + value) x.State }
        | None -> { State = Map.add replicaId value x.State }
    member this.Merge(other) = raise (System.NotImplementedException())
    interface IConvergent<GCounter> with
        member this.Merge(other) = this.Merge(other)
    
type PNCounter = 
    { Inc: GCounter; Dec: GCounter }
    member x.Value: int64 = int64(x.Inc.Value) - int64(x.Dec.Value)
    member x.Increment replicaId value = { x with Inc = x.Inc.Increment replicaId value }
    member x.Decrement replicaId value = { x with Dec = x.Dec.Increment replicaId value }
    member x.Update replicaId (value: int) =
        if value >= 0 
        then x.Increment replicaId (uint32 value)
        else x.Decrement replicaId (uint32 value)
    interface IConvergent<PNCounter> with
        member x.Merge(other) = { Inc = x.Inc.Merge(other.Inc); Dec = x.Dec.Merge(other.Dec) }

type LWWReg<'T> = class end

type GSet<'T> = class end

type ORSet<'T> = class end

type ORMap<'T> = class end

type LWWMap<'T> = class end