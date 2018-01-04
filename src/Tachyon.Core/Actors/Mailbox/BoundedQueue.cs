// Based on MPMCQueue.NET by Alexandr Nikitin: https://github.com/alexandrnikitin/MPMCQueue.NET
//
// Original license:
//
// MIT License
//
// Copyright(c) 2016 Alexandr Nikitin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tachyon.Actors.Mailbox
{
    [StructLayout(LayoutKind.Explicit, Size = 192, CharSet = CharSet.Ansi)]
    public sealed class BoundedQueue<T> : IMailboxQueue<T> where T : class
    {
        [FieldOffset(0)]
        private readonly Cell[] buffer;
        [FieldOffset(8)]
        private readonly int bmask;
        [FieldOffset(64)]
        private int enqueuePos;
        [FieldOffset(128)]
        private int dequeuePos;

        public int Count => enqueuePos - dequeuePos;
        public bool HasMessages => Count > 0;

        public BoundedQueue(int bufferSize)
        {
            if (bufferSize < 2) throw new ArgumentException($"{nameof(bufferSize)} should be greater than 2");
            if ((bufferSize & (bufferSize - 1)) != 0) throw new ArgumentException($"{nameof(bufferSize)} should be a power of 2");

            bmask = bufferSize - 1;
            buffer = new Cell[bufferSize];

            for (var i = 0; i < bufferSize; i++)
            {
                buffer[i] = new Cell(i, null);
            }

            enqueuePos = 0;
            dequeuePos = 0;
        }

        public bool TryPush(T item)
        {
            do
            {
                var buf = this.buffer;
                var pos = enqueuePos;
                var index = pos & bmask;
                var cell = buf[index];
                if (cell.Sequence == pos && Interlocked.CompareExchange(ref enqueuePos, pos + 1, pos) == pos)
                {
                    Volatile.Write(ref buf[index].Element, item);
                    buf[index].Sequence = pos + 1;
                    return true;
                }

                if (cell.Sequence < pos)
                {
                    return false;
                }
            } while (true);
        }

        public bool TryPop(out T result)
        {
            do
            {
                var buf = this.buffer;
                var bufferMask = bmask;
                var pos = dequeuePos;
                var index = pos & bufferMask;
                var cell = buf[index];
                if (cell.Sequence == pos + 1 && Interlocked.CompareExchange(ref dequeuePos, pos + 1, pos) == pos)
                {
                    result = Volatile.Read(ref cell.Element);
                    buf[index] = new Cell(pos + bufferMask + 1, null);
                    return true;
                }

                if (cell.Sequence < pos + 1)
                {
                    result = null;
                    return false;
                }
            } while (true);
        }

        [StructLayout(LayoutKind.Explicit, Size = 16, CharSet = CharSet.Ansi)]
        private struct Cell
        {
            [FieldOffset(0)]
            public int Sequence;
            [FieldOffset(8)]
            public T Element;

            public Cell(int sequence, T element)
            {
                Sequence = sequence;
                Element = element;
            }
        }
    }
}