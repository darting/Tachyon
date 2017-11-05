#region copyright
// -----------------------------------------------------------------------
//  <copyright file="AtomicLong.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Threading;

namespace Tachyon.Core.Atomic
{
    public sealed class AtomicLong
    {
        private long value;

        public long Value
        {
            get => Volatile.Read(ref value);
            set => Volatile.Write(ref this.value, value);
        }

        public AtomicLong(long value)
        {
            this.value = value;
        }

        public long CompareExchange(int v, int expected) => Interlocked.CompareExchange(ref this.value, v, expected);

        public long Increment() => Interlocked.Increment(ref value);
        public long Decrement() => Interlocked.Decrement(ref value);

        public static implicit operator long(AtomicLong x) => x.Value;
        public static implicit operator AtomicLong(long x) => new AtomicLong(x);
    }
}