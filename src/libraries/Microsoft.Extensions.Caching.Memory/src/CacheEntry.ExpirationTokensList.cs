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
        /// the entry is being built. After concurrent reads are enabled, writers serialize on the owning
        /// <see cref="CacheEntryTokens"/> while readers walk a lock-free snapshot. Writers may initialise
        /// unused slots in place before exposing a larger count; the visible prefix remains immutable.
        /// </summary>
        private sealed class ExpirationTokensList : IList<IChangeToken>
        {
            private const int DefaultCapacity = 4;

            // This shared state is never mutated: every append must grow and replace it.
            private static readonly State s_empty = new State(Array.Empty<IChangeToken>(), 0);

            private readonly object _gate;
            private volatile State _state = new State(Array.Empty<IChangeToken>(), 0);
            private bool _concurrentReadsEnabled;

            internal ExpirationTokensList(object gate)
            {
                _gate = gate;
            }

            /// <summary>
            /// Gets the current contents, which are safe to walk without synchronizing after concurrent reads are enabled.
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
                        int count = state._count;
                        if ((uint)index >= (uint)count)
                        {
                            throw new ArgumentOutOfRangeException(nameof(index));
                        }

                        if (!_concurrentReadsEnabled)
                        {
                            state._items[index] = value;
                            return;
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
                    int count = state._count;
                    int updatedCount = count + 1;
                    state = EnsureCapacity(state, count, updatedCount);
                    state._items[count] = item;
                    SetCount(state, updatedCount);
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
                    int count = state._count;
                    int updatedCount = checked(count + source.Length);
                    state = EnsureCapacity(state, count, updatedCount);
                    source.CopyTo(new Span<IChangeToken>(state._items, count, source.Length));
                    SetCount(state, updatedCount);
                }
            }

            public void Insert(int index, IChangeToken item)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = state._count;
                    if ((uint)index > (uint)count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    int updatedCount = count + 1;
                    if (!_concurrentReadsEnabled)
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
                    _state = new State(items, updatedCount);
                }
            }

            public void RemoveAt(int index)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = state._count;
                    if ((uint)index >= (uint)count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    if (!_concurrentReadsEnabled)
                    {
                        RemoveAtBuilder(state, index);
                    }
                    else
                    {
                        RemoveAtConcurrent(state, count, index);
                    }
                }
            }

            public void Clear()
            {
                lock (_gate)
                {
                    State state = _state;
                    if (!_concurrentReadsEnabled)
                    {
                        Array.Clear(state._items, 0, state._count);
                        state._count = 0;
                    }
                    else
                    {
                        _state = s_empty;
                    }
                }
            }

            public bool Remove(IChangeToken item)
            {
                lock (_gate)
                {
                    State state = _state;
                    int count = state._count;
                    int index = Array.IndexOf(state._items, item, 0, count);
                    if (index < 0)
                    {
                        return false;
                    }

                    if (!_concurrentReadsEnabled)
                    {
                        RemoveAtBuilder(state, index);
                    }
                    else
                    {
                        RemoveAtConcurrent(state, count, index);
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

            internal void EnableConcurrentReads()
            {
                if (_concurrentReadsEnabled)
                {
                    return;
                }

                lock (_gate)
                {
                    _concurrentReadsEnabled = true;
                }
            }

            private State EnsureCapacity(State state, int count, int requiredCapacity)
            {
                if (requiredCapacity <= state._items.Length)
                {
                    return state;
                }

                var items = new IChangeToken[GetCapacity(state._items.Length, requiredCapacity)];
                Array.Copy(state._items, items, count);
                if (_concurrentReadsEnabled)
                {
                    state = new State(items, count);
                    _state = state;
                }
                else
                {
                    state._items = items;
                }
                return state;
            }

            private void SetCount(State state, int count)
            {
                if (_concurrentReadsEnabled)
                {
                    Volatile.Write(ref state._count, count);
                }
                else
                {
                    state._count = count;
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

            private void RemoveAtConcurrent(State state, int count, int index)
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
                public IChangeToken[] _items;
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
