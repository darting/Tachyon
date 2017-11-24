[<AutoOpen>]
module Tachyon.Core.Atomic.Atoms

open Tachyon.Core.Atomic

let inline atom (ref: 'T): AtomicRef<'T> = AtomicRef(ref)
