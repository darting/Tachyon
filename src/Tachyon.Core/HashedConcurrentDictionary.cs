// -----------------------------------------------------------------------
//   <copyright file="HashedConcurrentDictionary.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//       Copyright (C) 2018 Bartosz Sypytkowski
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Tachyon.Actors;

namespace Tachyon.Core
{
    public struct HashedConcurrentDictionary : IEnumerable<KeyValuePair<string, ICell>>
    {
        private const int HashSize = 1024;
        private readonly Partition[] _partitions;

        public HashedConcurrentDictionary(IEnumerable<Tuple<string, ICell>> init)
        {
            _partitions = new Partition[HashSize];
            for (var i = 0; i < _partitions.Length; i++)
            {
                _partitions[i] = new Partition();
            }

            if (init != null)
            {
                foreach (var tuple in init)
                {
                    TryAdd(tuple.Item1, tuple.Item2);
                }
            }
        }
        
        private Partition GetPartition(string key)
        {
            var hash = Math.Abs(key.GetHashCode()) % HashSize;
            var p = _partitions[hash];
            return p;
        }

        public bool TryAdd(string key, ICell cell)
        {
            var p = GetPartition(key);
            lock (p)
            {
                if (p.ContainsKey(key))
                {
                    return false;
                }
                p.Add(key, cell);
                return true;
            }
        }

        public bool TryGetValue(string key, out ICell cell)
        {
            var p = GetPartition(key);
            lock (p)
            {
                return p.TryGetValue(key, out cell);
            }
        }

        public void Remove(string key)
        {
            var p = GetPartition(key);
            lock (p)
            {
                p.Remove(key);
            }
        }

        public sealed class Partition : Dictionary<string, ICell> { }

        public IEnumerator<KeyValuePair<string, ICell>> GetEnumerator()
        {
            foreach (var partition in _partitions)
            {
                foreach (KeyValuePair<string, ICell> pair in partition)
                {
                    yield return pair;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}