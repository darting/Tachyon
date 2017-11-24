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
    public struct AtomicLong : IAtomic<long>
    {
        private long value;

        public long Value => Volatile.Read(ref value);

        public AtomicLong(long value)
        {
            this.value = value;
        }

        public long CompareExchange(long v, long expected) => Interlocked.CompareExchange(ref this.value, v, expected);
        public long Swap(long value) => Interlocked.Exchange(ref this.value, value);

        public long Increment() => Interlocked.Increment(ref value);
        public long Decrement() => Interlocked.Decrement(ref value);
        
        public static implicit operator long(AtomicLong x) => x.Value;
        public static implicit operator AtomicLong(long x) => new AtomicLong(x);
    }
}