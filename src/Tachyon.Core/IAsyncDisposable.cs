#region copyright
// -----------------------------------------------------------------------
//  <copyright file="IAsyncDisposable.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2018 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tachyon.Core
{
    /// <summary>
    /// A variant of <see cref="IDisposable"/> interface, allows to asynchronously
    /// call resource disposal.
    /// </summary>
    public interface IAsyncDisposable : IDisposable
    {
        Task DisposeAsync(CancellationToken token = default(CancellationToken));
    }
}