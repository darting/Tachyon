#region copyright
// -----------------------------------------------------------------------
//  <copyright file="IPartiallyComparable.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Tachyon.Core
{
    public enum PartialOrder : sbyte
    {
        Lt = -1,
        Eq = 0,
        Gt = 1,
        Concurrent = sbyte.MaxValue
    }

    public interface IPartiallyComparable<in T>
    {
        PartialOrder PartiallyCompareTo(T other);
    }

    public interface IPartialComparer<in T>
    {
        PartialOrder PariallyCompare(T x, T y);
    }
}