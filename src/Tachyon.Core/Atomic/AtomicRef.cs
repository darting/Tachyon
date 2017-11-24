#region copyright
// -----------------------------------------------------------------------
//  <copyright file="AtomicRef.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Threading;

namespace Tachyon.Core.Atomic
{

    public struct AtomicRef<T> : IAtomic<T> where T : class
    {
        private T value;

        public T Value => Volatile.Read(ref value);

        public AtomicRef(T value)
        {
            this.value = value;
        }

        public T CompareExchange(T v, T expected) => Interlocked.CompareExchange(ref this.value, v, expected);
        public T Swap(T value) => Interlocked.Exchange(ref this.value, value);
    }
}