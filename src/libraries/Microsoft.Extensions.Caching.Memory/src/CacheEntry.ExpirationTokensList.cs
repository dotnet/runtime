// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal sealed partial class CacheEntry
    {
        /// <summary>
        /// The list behind <see cref="ICacheEntry.ExpirationTokens"/>. Mutations rebuild the backing
        /// array under a lock and publish it with a volatile write, so a reader can take the array
        /// and walk it without synchronizing and without observing a half-written state.
        /// </summary>
        /// <remarks>
        /// The tokens are scanned on every cache hit but written to very rarely, and the entry is
        /// already published in the cache by the time a linked child entry propagates its tokens into
        /// it - from whichever thread committed the child. Locking the reader instead would penalize
        /// the hot path, and would not help anyway: callers mutate this list directly through the
        /// public <see cref="ICacheEntry.ExpirationTokens"/> property, so the writes can only be made
        /// safe from inside the list itself.
        /// </remarks>
        private sealed class ExpirationTokensList : IList<IChangeToken>
        {
            private readonly object _lock = new object();
            private volatile IChangeToken[] _items = Array.Empty<IChangeToken>();

            /// <summary>
            /// Gets the current contents. The array is shared with concurrent readers, so callers
            /// must only read from it.
            /// </summary>
            internal IChangeToken[] Snapshot => _items;

            public int Count => _items.Length;

            public bool IsReadOnly => false;

            public IChangeToken this[int index]
            {
                get
                {
                    IChangeToken[] items = _items;
                    if ((uint)index >= (uint)items.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return items[index];
                }
                set => Mutate(list => list[index] = value);
            }

            public void Add(IChangeToken item)
            {
                lock (_lock)
                {
                    IChangeToken[] items = _items;
                    var updated = new IChangeToken[items.Length + 1];
                    Array.Copy(items, updated, items.Length);
                    updated[items.Length] = item;
                    _items = updated;
                }
            }

            internal void AddRange(IChangeToken[] source)
            {
                if (source.Length == 0)
                {
                    return;
                }

                lock (_lock)
                {
                    IChangeToken[] items = _items;
                    var updated = new IChangeToken[items.Length + source.Length];
                    Array.Copy(items, updated, items.Length);
                    Array.Copy(source, 0, updated, items.Length, source.Length);
                    _items = updated;
                }
            }

            // The remaining mutations are not used by the cache itself and are expected to be rare,
            // so they favour borrowing List<T>'s behaviour (including its argument validation) over
            // hand-rolling the array manipulation.
            public void Insert(int index, IChangeToken item) => Mutate(list => list.Insert(index, item));

            public void RemoveAt(int index) => Mutate(list => list.RemoveAt(index));

            public void Clear()
            {
                lock (_lock)
                {
                    _items = Array.Empty<IChangeToken>();
                }
            }

            public bool Remove(IChangeToken item)
            {
                lock (_lock)
                {
                    int index = Array.IndexOf(_items, item);
                    if (index < 0)
                    {
                        return false;
                    }

                    List<IChangeToken> updated = new List<IChangeToken>(_items);
                    updated.RemoveAt(index);
                    _items = updated.ToArray();
                    return true;
                }
            }

            public bool Contains(IChangeToken item) => Array.IndexOf(_items, item) >= 0;

            public int IndexOf(IChangeToken item) => Array.IndexOf(_items, item);

            public void CopyTo(IChangeToken[] array, int arrayIndex)
            {
                IChangeToken[] items = _items;
                Array.Copy(items, 0, array, arrayIndex, items.Length);
            }

            public IEnumerator<IChangeToken> GetEnumerator() => ((IEnumerable<IChangeToken>)_items).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

            private void Mutate(Action<List<IChangeToken>> mutation)
            {
                lock (_lock)
                {
                    List<IChangeToken> updated = new List<IChangeToken>(_items);
                    mutation(updated);
                    _items = updated.ToArray();
                }
            }
        }
    }
}
