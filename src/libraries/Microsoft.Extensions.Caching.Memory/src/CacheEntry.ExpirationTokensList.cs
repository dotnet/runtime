// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal sealed partial class CacheEntry
    {
        /// <summary>
        /// The list behind <see cref="ICacheEntry.ExpirationTokens"/>. A reader takes the current
        /// <see cref="State"/> and walks its visible prefix without synchronizing; a writer serializes on
        /// the owning <see cref="CacheEntryTokens"/> and publishes additions through the visible count.
        /// </summary>
        private sealed class ExpirationTokensList : IList<IChangeToken>
        {
            private const int DefaultCapacity = 4;

            // A zero-capacity state is never mutated: the first append always grows and publishes a
            // per-list state. This makes it safe to share after Clear without another allocation.
            private static readonly State s_empty = new State(Array.Empty<IChangeToken>(), 0);

            private readonly object _gate;
            private volatile State _state = s_empty;

            public ExpirationTokensList(object gate)
            {
                _gate = gate;
            }

            /// <summary>
            /// Gets the current contents, which are safe to walk without synchronizing.
            /// </summary>
            public ReadOnlySpan<IChangeToken> Snapshot
            {
                get
                {
                    State state = _state;
                    return new ReadOnlySpan<IChangeToken>(state._items, 0, Volatile.Read(ref state._count));
                }
            }

            public int Count
            {
                get
                {
                    State state = _state;
                    return Volatile.Read(ref state._count);
                }
            }

            public bool IsReadOnly => false;

            public IChangeToken this[int index]
            {
                get
                {
                    State state = _state;
                    return (uint)index >= (uint)Volatile.Read(ref state._count)
                        ? throw new ArgumentOutOfRangeException(nameof(index))
                        : state._items[index];
                }
                set
                {
                    lock (_gate)
                    {
                        State state = _state;
                        int count = Volatile.Read(ref state._count);
                        if ((uint)index >= (uint)count)
                        {
                            throw new ArgumentOutOfRangeException(nameof(index));
                        }

                        var items = new IChangeToken[count];
                        Array.Copy(state._items, items, count);
                        items[index] = value;
                        _state = new State(items, count);
                    }
                }
            }

            public void Add(IChangeToken item)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = Volatile.Read(ref state._count);

                    if (count < state._items.Length)
                    {
                        // Readers holding this state cannot observe the slot until the release write publishes
                        // the larger count. The acquire read of Count or Snapshot then observes the item.
                        state._items[count] = item;
                        Volatile.Write(ref state._count, count + 1);
                        return;
                    }

                    var items = new IChangeToken[GetCapacity(state._items.Length, count + 1)];
                    Array.Copy(state._items, items, count);
                    items[count] = item;
                    _state = new State(items, count + 1);
                }
            }

            internal void AddRange(ReadOnlySpan<IChangeToken> source)
            {
                if (source.IsEmpty)
                {
                    return;
                }

                lock (_gate)
                {
                    State state = _state;
                    int count = Volatile.Read(ref state._count);
                    int updatedCount = checked(count + source.Length);

                    if (updatedCount <= state._items.Length)
                    {
                        source.CopyTo(new Span<IChangeToken>(state._items, count, source.Length));
                        Volatile.Write(ref state._count, updatedCount);
                        return;
                    }

                    var items = new IChangeToken[GetCapacity(state._items.Length, updatedCount)];
                    Array.Copy(state._items, items, count);
                    source.CopyTo(new Span<IChangeToken>(items, count, source.Length));
                    _state = new State(items, updatedCount);
                }
            }

            public void Insert(int index, IChangeToken item)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = Volatile.Read(ref state._count);
                    if ((uint)index > (uint)count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    int updatedCount = checked(count + 1);
                    var items = new IChangeToken[updatedCount];
                    Array.Copy(state._items, 0, items, 0, index);
                    items[index] = item;
                    Array.Copy(state._items, index, items, index + 1, count - index);
                    _state = new State(items, updatedCount);
                }
            }

            public void RemoveAt(int index)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = Volatile.Read(ref state._count);
                    if ((uint)index >= (uint)count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    RemoveAtCore(state, count, index);
                }
            }

            public void Clear()
            {
                lock (_gate)
                {
                    _state = s_empty;
                }
            }

            public bool Remove(IChangeToken item)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = Volatile.Read(ref state._count);

                    // Array.IndexOf runs IChangeToken.Equals, which is user code, under the gate. The
                    // gate also coordinates token attachment and detachment, but never cache-hit readers.
                    int index = Array.IndexOf(state._items, item, 0, count);
                    if (index < 0)
                    {
                        return false;
                    }

                    RemoveAtCore(state, count, index);
                    return true;
                }
            }

            public bool Contains(IChangeToken item) => IndexOf(item) >= 0;

            public int IndexOf(IChangeToken item)
            {
                State state = _state;
                return Array.IndexOf(state._items, item, 0, Volatile.Read(ref state._count));
            }

            public void CopyTo(IChangeToken[] array, int arrayIndex)
            {
                State state = _state;
                Array.Copy(state._items, 0, array, arrayIndex, Volatile.Read(ref state._count));
            }

            public IEnumerator<IChangeToken> GetEnumerator()
            {
                // Walks a snapshot, so unlike List<T> a mutation from another thread does not invalidate
                // an enumerator that is already running; it simply is not observed by it.
                State state = _state;
                int count = Volatile.Read(ref state._count);
                for (int i = 0; i < count; i++)
                {
                    yield return state._items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            // Doubling keeps a run of appends amortized. A parent entry collects the tokens of every
            // linked child that is committed - or merely read - while the parent is the ambient entry, so
            // how far the list grows is up to the caller, and copying all of it on each append would make
            // that quadratic.
            private static int GetCapacity(int currentCapacity, int requiredCapacity)
            {
                int newCapacity = currentCapacity switch
                {
                    0 => DefaultCapacity,
                    > int.MaxValue / 2 => int.MaxValue,
                    _ => currentCapacity * 2
                };
                return Math.Max(newCapacity, requiredCapacity);
            }

            private void RemoveAtCore(State state, int count, int index)
            {
                int updatedCount = count - 1;
                if (updatedCount == 0)
                {
                    _state = s_empty;
                    return;
                }

                var items = new IChangeToken[updatedCount];
                Array.Copy(state._items, 0, items, 0, index);
                Array.Copy(state._items, index + 1, items, index, updatedCount - index);
                _state = new State(items, updatedCount);
            }

            private sealed class State
            {
                public readonly IChangeToken[] _items;
                public int _count;

                public State(IChangeToken[] items, int count)
                {
                    _items = items;
                    _count = count;
                }
            }
        }
    }
}
