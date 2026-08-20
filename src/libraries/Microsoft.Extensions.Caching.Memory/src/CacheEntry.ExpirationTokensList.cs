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
        /// The list behind <see cref="ICacheEntry.ExpirationTokens"/>. Its state is mutated directly while
        /// the entry is being built. After publication, writers serialize on the owning
        /// <see cref="CacheEntryTokens"/> while readers walk a lock-free snapshot.
        /// </summary>
        private sealed class ExpirationTokensList : IList<IChangeToken>
        {
            private const int DefaultCapacity = 4;

            // This shared state is never mutated: every append must grow and publish a new state.
            private static readonly State s_emptyPublished = new State(Array.Empty<IChangeToken>(), 0, isPublished: true);

            private readonly object _gate;
            private volatile State _state = new State(Array.Empty<IChangeToken>(), 0, isPublished: false);

            internal ExpirationTokensList(object gate)
            {
                _gate = gate;
            }

            /// <summary>
            /// Gets the current contents, which are safe to walk without synchronizing after publication.
            /// </summary>
            internal ReadOnlySpan<IChangeToken> Snapshot
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

                        if (!state._isPublished)
                        {
                            state._items[index] = value;
                            return;
                        }

                        var items = new IChangeToken[count];
                        Array.Copy(state._items, items, count);
                        items[index] = value;
                        _state = new State(items, count, isPublished: true);
                    }
                }
            }

            public void Add(IChangeToken item)
            {
                State state = _state;
                if (!state._isPublished)
                {
                    if (state._count == state._items.Length)
                    {
                        GrowBuilder(state, state._count + 1);
                    }

                    int count = state._count;
                    state._items[count] = item;
                    state._count = count + 1;
                    return;
                }

                lock (_gate)
                {
                    state = _state;
                    int count = Volatile.Read(ref state._count);
                    if (count < state._items.Length)
                    {
                        state._items[count] = item;
                        Volatile.Write(ref state._count, count + 1);
                        return;
                    }

                    var items = new IChangeToken[GetCapacity(state._items.Length, count + 1)];
                    Array.Copy(state._items, items, count);
                    items[count] = item;
                    _state = new State(items, count + 1, isPublished: true);
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

                    if (!state._isPublished)
                    {
                        if (updatedCount > state._items.Length)
                        {
                            GrowBuilder(state, updatedCount);
                        }

                        source.CopyTo(new Span<IChangeToken>(state._items, count, source.Length));
                        state._count = updatedCount;
                        return;
                    }

                    if (updatedCount <= state._items.Length)
                    {
                        source.CopyTo(new Span<IChangeToken>(state._items, count, source.Length));
                        Volatile.Write(ref state._count, updatedCount);
                        return;
                    }

                    var items = new IChangeToken[GetCapacity(state._items.Length, updatedCount)];
                    Array.Copy(state._items, items, count);
                    source.CopyTo(new Span<IChangeToken>(items, count, source.Length));
                    _state = new State(items, updatedCount, isPublished: true);
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
                    if (!state._isPublished)
                    {
                        if (updatedCount > state._items.Length)
                        {
                            GrowBuilder(state, updatedCount);
                        }

                        Array.Copy(state._items, index, state._items, index + 1, count - index);
                        state._items[index] = item;
                        state._count = updatedCount;
                        return;
                    }

                    var items = new IChangeToken[updatedCount];
                    Array.Copy(state._items, 0, items, 0, index);
                    items[index] = item;
                    Array.Copy(state._items, index, items, index + 1, count - index);
                    _state = new State(items, updatedCount, isPublished: true);
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

                    if (!state._isPublished)
                    {
                        RemoveAtBuilder(state, index);
                    }
                    else
                    {
                        RemoveAtPublished(state, count, index);
                    }
                }
            }

            public void Clear()
            {
                lock (_gate)
                {
                    State state = _state;
                    if (!state._isPublished)
                    {
                        Array.Clear(state._items, 0, state._count);
                        state._count = 0;
                    }
                    else
                    {
                        _state = s_emptyPublished;
                    }
                }
            }

            public bool Remove(IChangeToken item)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = Volatile.Read(ref state._count);
                    int index = Array.IndexOf(state._items, item, 0, count);
                    if (index < 0)
                    {
                        return false;
                    }

                    if (!state._isPublished)
                    {
                        RemoveAtBuilder(state, index);
                    }
                    else
                    {
                        RemoveAtPublished(state, count, index);
                    }
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
                State state = _state;
                IChangeToken[] items = state._items;
                int count = Volatile.Read(ref state._count);
                for (int i = 0; i < count; i++)
                {
                    yield return items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal void Publish()
            {
                State state = _state;
                if (state._isPublished)
                {
                    return;
                }

                lock (_gate)
                {
                    _state._isPublished = true;
                }
            }

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

            private static void GrowBuilder(State state, int requiredCapacity)
            {
                var items = new IChangeToken[GetCapacity(state._items.Length, requiredCapacity)];
                Array.Copy(state._items, items, state._count);
                state._items = items;
            }

            private static void RemoveAtBuilder(State state, int index)
            {
                int updatedCount = state._count - 1;
                Array.Copy(state._items, index + 1, state._items, index, updatedCount - index);
                state._count = updatedCount;
                state._items[updatedCount] = null!;
            }

            private void RemoveAtPublished(State state, int count, int index)
            {
                int updatedCount = count - 1;
                if (updatedCount == 0)
                {
                    _state = s_emptyPublished;
                    return;
                }

                var items = new IChangeToken[updatedCount];
                Array.Copy(state._items, 0, items, 0, index);
                Array.Copy(state._items, index + 1, items, index, updatedCount - index);
                _state = new State(items, updatedCount, isPublished: true);
            }

            private sealed class State
            {
                public IChangeToken[] _items;
                public int _count;
                public bool _isPublished;

                public State(IChangeToken[] items, int count, bool isPublished)
                {
                    _items = items;
                    _count = count;
                    _isPublished = isPublished;
                }
            }
        }
    }
}
