#region copyright
// -----------------------------------------------------------------------
//  <copyright file="AtomicBool.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Runtime.CompilerServices;
using System.Threading;

namespace Tachyon.Core.Atomic
{
    public sealed class AtomicBool
    {
        private const int TRUE = 1;
        private const int FALSE = 0;

        private int value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Normalize(bool v) => v ? TRUE : FALSE;

        public bool Value
        {
            get => Volatile.Read(ref value) == TRUE;
            set => Volatile.Write(ref this.value, Normalize(value));
        }

        public AtomicBool(bool value)
        {
            this.value = value ? TRUE : FALSE;
        }

        public bool CompareExchange(bool v, bool expected)
        {
            return Interlocked.CompareExchange(ref value, Normalize(v), Normalize(expected)) == TRUE;
        }

        public static implicit operator bool(AtomicBool x) => x.Value;
        public static implicit operator AtomicBool(bool x) => new AtomicBool(x);
    }
}