[<AutoOpen>]
module Tachyon.Core.Atomic.Atoms

open Tachyon.Core.Atomic

/// Creates an atomic cell with reference value.
let inline atom<'T when 'T: not struct>(ref: 'T): AtomicRef<'T> = AtomicRef<'T>(ref)

/// Creates an atomic cell for int32 value.
let inline atomi (value: int) = AtomicInt(value)

/// Creates an atomic cell for int64 value.
let inline atoml (value: int64) = AtomicLong(value)

/// Creates an atomic cell for boolean value.
let inline atomb (value: bool) = AtomicBool(value)

/// Gets a current value of an atomic cell.
let inline (!) (atom: #IAtomic<_>) = atom.Value

/// Swaps current atomic cell value with the provided one, returning old one as a result.
let inline (:=) (atom: #IAtomic<_>) (value) = atom.Swap(value)