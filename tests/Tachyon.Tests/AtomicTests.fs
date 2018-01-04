module Tachyon.Tests.AtomicTests

open System
open Xunit
open Tachyon.Core.Atomic.Atoms
open Swensen.Unquote

[<Fact>]
let ``Atomic swap return previous value`` () =
    let a = atomi 1
    let b = a := !a + 1
    test <@ !a = 2 @>
    test <@ b = 1 @>
    
//[<Fact>]
//let ``Atomic applies compare-and-swap semantics`` () =
//    let a = atomi 1
//    let b = a ? 1 <- 2
//    test <@ !a = 2 @>
//    test <@ b = 1 @>
    
//[<Fact>]
//let ``Atomic applies compare-and-swap semantics 2`` () =
//    let a = atomi 1
//    let b = a ? 2 <- 3
//    test <@ !a = 1 @>
//    test <@ b = 1 @>