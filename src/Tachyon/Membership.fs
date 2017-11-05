namespace Tachyon.Membership

open System
open Tachyon.Core
open Tachyon.Actors

type Tag = string

type MemberInfo =
    { NodeAddress: Uri
      NodeId: NodeId 
      Tags: Tag[] } 
    interface IConvergent<MemberInfo> with
        member this.Merge(arg1) = raise (System.NotImplementedException())
    interface IPartiallyComparable<MemberInfo> with
        member this.PartiallyCompareTo(arg1) = raise (System.NotImplementedException())
