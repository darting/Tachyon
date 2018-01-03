#region copyright
// -----------------------------------------------------------------------
//  <copyright file="Contracts.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2018 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Tachyon.Core;

namespace Tachyon.Actors
{
    /// <summary>
    /// Messages marked as silient will not be send to dead letters when unhandled.
    /// </summary>
    public interface ISilient { }

    /// <summary>
    /// Signals are system messages. They are used to control actor's lifecycle and
    /// to inform about system-wide events. Signals are handled with higher priority
    /// than user-defined messages.
    /// </summary>
    public interface ISignal : ISilient { }

    /// <summary>
    /// Addressable interface allows to send signal to any actor.
    /// </summary>
    public interface IAddressable
    {
        void Send(ISignal signal);
    }

    /// <summary>
    /// Refs are serializable pointers to a target actor instance, no matter where
    /// in the cluster does it live.
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public interface IRef<in M> : IAddressable
    {
        void Send(M message);
    }
    
    public interface ITimer : IDisposable
    {
        void Schedule<M>(TimeSpan delay, IRef<M> target, M message, CancellationToken token = default(CancellationToken));
        void Schedule<M>(TimeSpan delay, TimeSpan interval, IRef<M> target, M message, CancellationToken token = default(CancellationToken));
    }
}