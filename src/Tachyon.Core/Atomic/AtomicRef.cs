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
    public class AtomicRef<T> where T : class
    {
        private T value;

        public T Value
        {
            get => Volatile.Read(ref value);
            set => Volatile.Write(ref this.value, value);
        }

        public AtomicRef(T value)
        {
            this.value = value;
        }

        public T CompareExchange(T v, T expected) => Interlocked.CompareExchange(ref this.value, v, expected);
    }
}