#region copyright
// -----------------------------------------------------------------------
//  <copyright file="AtomicInt.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Threading;

namespace Tachyon.Core.Atomic
{
    public sealed class AtomicInt
    {
        private int value;

        public int Value
        {
            get => Volatile.Read(ref value);
            set => Volatile.Write(ref this.value, value);
        }

        public AtomicInt(int value)
        {
            this.value = value;
        }

        public int CompareExchange(int v, int expected) => Interlocked.CompareExchange(ref this.value, v, expected);

        public int Increment() => Interlocked.Increment(ref value);
        public int Decrement() => Interlocked.Decrement(ref value);

        public static implicit operator int(AtomicInt x) => x.Value;
        public static implicit operator AtomicInt(int x) => new AtomicInt(x);
    }
}