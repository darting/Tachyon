#region copyright
// -----------------------------------------------------------------------
//  <copyright file="IMailboxQueue.cs" creator="Bartosz Sypytkowski">
//      Copyright (C) 2017 Bartosz Sypytkowski <b.sypytkowski@gmail.com>
//  </copyright>
// -----------------------------------------------------------------------
#endregion
namespace Tachyon.Actors.Mailbox
{
    public interface IMailboxQueue<T>
    {
        bool HasMessages { get; }
        bool TryPush(ref T message);
        bool TryPop(out T message);
    }
}