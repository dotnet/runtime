// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// A copy-on-write <see cref="IList{T}"/>: every mutation clones the backing array under a
    /// private lock and publishes the clone with a volatile write, so readers can take a
    /// <see cref="Snapshot"/> and enumerate it without synchronizing and without ever observing a
    /// partially written state.
    /// </summary>
    /// <remarks>
    /// This backs <see cref="ICacheEntry.ExpirationTokens"/>. That list is read on every cache hit
    /// but written very rarely, and the linked-entry machinery mutates a parent entry's list from
    /// whichever thread happens to commit a child entry, so a plain <see cref="List{T}"/> is not
    /// safe here. Copying on write keeps the read path lock-free; the O(n) copy per mutation is
    /// irrelevant for the handful of tokens an entry normally carries, and an entry holding enough
    /// tokens for it to matter is already paying O(n) on every single cache hit.
    /// </remarks>
    internal sealed class CopyOnWriteList<T> : IList<T>
    {
        private readonly object _lock = new object();
        private volatile T[] _items = Array.Empty<T>();

        /// <summary>
        /// Gets the current contents of the list. The returned array is shared with concurrent
        /// readers and must never be mutated.
        /// </summary>
        internal T[] Snapshot => _items;

        public int Count => _items.Length;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                T[] items = _items;
                if ((uint)index >= (uint)items.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return items[index];
            }
            set
            {
                lock (_lock)
                {
                    T[] items = _items;
                    if ((uint)index >= (uint)items.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    T[] updated = new T[items.Length];
                    Array.Copy(items, updated, items.Length);
                    updated[index] = value;
                    _items = updated;
                }
            }
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                T[] items = _items;
                T[] updated = new T[items.Length + 1];
                Array.Copy(items, updated, items.Length);
                updated[items.Length] = item;
                _items = updated;
            }
        }

        internal void AddRange(T[] source)
        {
            if (source.Length == 0)
            {
                return;
            }

            lock (_lock)
            {
                T[] items = _items;
                T[] updated = new T[items.Length + source.Length];
                Array.Copy(items, updated, items.Length);
                Array.Copy(source, 0, updated, items.Length, source.Length);
                _items = updated;
            }
        }

        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                T[] items = _items;
                if ((uint)index > (uint)items.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                T[] updated = new T[items.Length + 1];
                Array.Copy(items, updated, index);
                updated[index] = item;
                Array.Copy(items, index, updated, index + 1, items.Length - index);
                _items = updated;
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                int index = Array.IndexOf(_items, item);
                if (index < 0)
                {
                    return false;
                }

                RemoveAtCore(index);
                return true;
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                if ((uint)index >= (uint)_items.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                RemoveAtCore(index);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items = Array.Empty<T>();
            }
        }

        public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;

        public int IndexOf(T item) => Array.IndexOf(_items, item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            T[] items = _items;
            Array.Copy(items, 0, array, arrayIndex, items.Length);
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        private void RemoveAtCore(int index)
        {
            T[] items = _items;
            if (items.Length == 1)
            {
                _items = Array.Empty<T>();
                return;
            }

            T[] updated = new T[items.Length - 1];
            Array.Copy(items, updated, index);
            Array.Copy(items, index + 1, updated, index, items.Length - index - 1);
            _items = updated;
        }
    }
}
