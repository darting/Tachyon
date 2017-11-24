#region copyright
// -----------------------------------------------------------------------
//  <copyright file="IAtomic.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion


namespace Tachyon.Core.Atomic
{
    public interface IAtomic<T>
    {
        T Value { get; }
        T CompareExchange(T v, T expected);
        T Swap(T value);
    }
}