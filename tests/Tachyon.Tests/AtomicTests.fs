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
