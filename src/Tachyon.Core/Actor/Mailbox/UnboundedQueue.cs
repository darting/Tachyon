﻿#region copyright
// -----------------------------------------------------------------------
//  <copyright file="UnboundedQueue.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion

using System.Collections.Concurrent;

namespace Tachyon.Actor.Mailbox
{
    internal sealed class UnboundedQueue<T> : IMailboxQueue<T>
    {
        private readonly ConcurrentQueue<T> inner = new ConcurrentQueue<T>();

        public bool HasMessages => !inner.IsEmpty;
        public bool TryPush(ref T message)
        {
            inner.Enqueue(message);
            return true;
        }

        public bool TryPop(out T message) => inner.TryDequeue(out message);
    }
}