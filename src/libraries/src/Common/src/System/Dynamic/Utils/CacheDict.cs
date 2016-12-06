// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Diagnostics;

namespace System.Dynamic.Utils
{
    /// <summary>
    /// Provides a dictionary-like object used for caches which holds onto a maximum
    /// number of elements specified at construction time.
    /// </summary>
    internal sealed class CacheDict<TKey, TValue>
    {
        // cache size is always ^2.
        // items are placed at [hash ^ mask]
        // new item will displace previous one at the same location.
        private readonly int _mask;
        private readonly Entry[] _entries;

        // class, to ensure atomic updates.
        private sealed class Entry
        {
            internal readonly int _hash;
            internal readonly TKey _key;
            internal readonly TValue _value;

            internal Entry(int hash, TKey key, TValue value)
            {
                _hash = hash;
                _key = key;
                _value = value;
            }
        }

        /// <summary>
        /// Creates a dictionary-like object used for caches.
        /// </summary>
        /// <param name="size">The maximum number of elements to store will be this number aligned to next ^2.</param>
        internal CacheDict(int size)
        {
            int alignedSize = AlignSize(size);
            _mask = alignedSize - 1;
            _entries = new Entry[alignedSize];
        }

        private static int AlignSize(int size)
        {
            Debug.Assert(size > 0);

            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            size++;

            Debug.Assert((size & (~size + 1)) == size, "aligned size should be a power of 2");
            return size;
        }

        /// <summary>
        /// Tries to get the value associated with 'key', returning true if it's found and
        /// false if it's not present.
        /// </summary>
        internal bool TryGetValue(TKey key, out TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & _mask;

            Entry entry = Volatile.Read(ref _entries[idx]);
            if (entry != null && entry._hash == hash && entry._key.Equals(key))
            {
                value = entry._value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Adds a new element to the cache, possibly replacing some
        /// element that is already present.
        /// </summary>
        internal void Add(TKey key, TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & _mask;

            Entry entry = Volatile.Read(ref _entries[idx]);
            if (entry == null || entry._hash != hash || !entry._key.Equals(key))
            {
                Volatile.Write(ref _entries[idx], new Entry(hash, key, value));
            }
        }

        /// <summary>
        /// Sets the value associated with the given key.
        /// </summary>
        internal TValue this[TKey key]
        {
            set
            {
                Add(key, value);
            }
        }
    }
}
