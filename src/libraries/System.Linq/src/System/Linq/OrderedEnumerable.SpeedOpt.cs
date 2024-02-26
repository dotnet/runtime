// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        internal abstract partial class OrderedEnumerable<TElement> : IPartition<TElement>
        {
            public virtual TElement[] ToArray()
            {
                TElement[] buffer = _source.ToArray();
                if (buffer.Length == 0)
                {
                    return buffer;
                }

                TElement[] array = new TElement[buffer.Length];
                Fill(buffer, array);
                return array;
            }

            public virtual List<TElement> ToList()
            {
                TElement[] buffer = _source.ToArray();

                List<TElement> list = new();
                if (buffer.Length > 0)
                {
                    Fill(buffer, SetCountAndGetSpan(list, buffer.Length));
                }

                return list;
            }

            private void Fill(TElement[] buffer, Span<TElement> destination)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = buffer[map[i]];
                }
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (_source is IIListProvider<TElement> listProv)
                {
                    return listProv.GetCount(onlyIfCheap);
                }

                return !onlyIfCheap || _source is ICollection<TElement> || _source is ICollection ? _source.Count() : -1;
            }

            internal TElement[] ToArray(int minIdx, int maxIdx)
            {
                TElement[] buffer = _source.ToArray();
                if (buffer.Length <= minIdx)
                {
                    return [];
                }

                if (buffer.Length <= maxIdx)
                {
                    maxIdx = buffer.Length - 1;
                }

                if (minIdx == maxIdx)
                {
                    return [GetEnumerableSorter().ElementAt(buffer, buffer.Length, minIdx)];
                }

                TElement[] array = new TElement[maxIdx - minIdx + 1];

                Fill(minIdx, maxIdx, buffer, array);

                return array;
            }

            internal List<TElement> ToList(int minIdx, int maxIdx)
            {
                TElement[] buffer = _source.ToArray();
                if (buffer.Length <= minIdx)
                {
                    return new List<TElement>();
                }

                if (buffer.Length <= maxIdx)
                {
                    maxIdx = buffer.Length - 1;
                }

                if (minIdx == maxIdx)
                {
                    return new List<TElement>(1) { GetEnumerableSorter().ElementAt(buffer, buffer.Length, minIdx) };
                }

                List<TElement> list = new();
                Fill(minIdx, maxIdx, buffer, SetCountAndGetSpan(list, maxIdx - minIdx + 1));
                return list;
            }

            private void Fill(int minIdx, int maxIdx, TElement[] buffer, Span<TElement> destination)
            {
                int[] map = SortedMap(buffer, minIdx, maxIdx);
                int idx = 0;
                while (minIdx <= maxIdx)
                {
                    destination[idx] = buffer[map[minIdx]];
                    ++idx;
                    ++minIdx;
                }
            }

            internal int GetCount(int minIdx, int maxIdx, bool onlyIfCheap)
            {
                int count = GetCount(onlyIfCheap);
                if (count <= 0)
                {
                    return count;
                }

                if (count <= minIdx)
                {
                    return 0;
                }

                return (count <= maxIdx ? count : maxIdx + 1) - minIdx;
            }

            public IPartition<TElement> Skip(int count) => new OrderedPartition<TElement>(this, count, int.MaxValue);

            public IPartition<TElement> Take(int count) => new OrderedPartition<TElement>(this, 0, count - 1);

            public TElement? TryGetElementAt(int index, out bool found)
            {
                if (index == 0)
                {
                    return TryGetFirst(out found);
                }

                if (index > 0)
                {
                    TElement[] buffer = _source.ToArray();
                    if (index < buffer.Length)
                    {
                        found = true;
                        return GetEnumerableSorter().ElementAt(buffer, buffer.Length, index);
                    }
                }

                found = false;
                return default;
            }

            public virtual TElement? TryGetFirst(out bool found)
            {
                CachingComparer<TElement> comparer = GetComparer();
                using (IEnumerator<TElement> e = _source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        found = false;
                        return default;
                    }

                    TElement value = e.Current;
                    comparer.SetElement(value);
                    while (e.MoveNext())
                    {
                        TElement x = e.Current;
                        if (comparer.Compare(x, true) < 0)
                        {
                            value = x;
                        }
                    }

                    found = true;
                    return value;
                }
            }

            public virtual TElement? TryGetLast(out bool found)
            {
                using (IEnumerator<TElement> e = _source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        found = false;
                        return default;
                    }

                    CachingComparer<TElement> comparer = GetComparer();
                    TElement value = e.Current;
                    comparer.SetElement(value);
                    while (e.MoveNext())
                    {
                        TElement current = e.Current;
                        if (comparer.Compare(current, false) >= 0)
                        {
                            value = current;
                        }
                    }

                    found = true;
                    return value;
                }
            }

            public TElement? TryGetLast(int minIdx, int maxIdx, out bool found)
            {
                TElement[] buffer = _source.ToArray();
                if (minIdx < buffer.Length)
                {
                    found = true;
                    return (maxIdx < buffer.Length - 1) ?
                        GetEnumerableSorter().ElementAt(buffer, buffer.Length, maxIdx) :
                        Last(buffer);
                }

                found = false;
                return default;
            }

            private TElement Last(TElement[] items)
            {
                CachingComparer<TElement> comparer = GetComparer();

                TElement value = items[0];
                comparer.SetElement(value);

                for (int i = 1; i < items.Length; ++i)
                {
                    TElement x = items[i];
                    if (comparer.Compare(x, cacheLower: false) >= 0)
                    {
                        value = x;
                    }
                }

                return value;
            }
        }

        internal sealed partial class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
        {
            // For complicated cases, rely on the base implementation that's more comprehensive.
            // For the simple case of OrderBy(...).First() or OrderByDescending(...).First() (i.e. where
            // there's just a single comparer we need to factor in), we can just do the iteration directly.

            public override TElement? TryGetFirst(out bool found)
            {
                if (_parent is not null)
                {
                    return base.TryGetFirst(out found);
                }

                using IEnumerator<TElement> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    IComparer<TKey> comparer = _comparer;
                    Func<TElement, TKey> keySelector = _keySelector;

                    TElement resultValue = e.Current;
                    TKey resultKey = keySelector(resultValue);

                    if (_descending)
                    {
                        while (e.MoveNext())
                        {
                            TElement nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (comparer.Compare(nextKey, resultKey) > 0)
                            {
                                resultKey = nextKey;
                                resultValue = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (e.MoveNext())
                        {
                            TElement nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (comparer.Compare(nextKey, resultKey) < 0)
                            {
                                resultKey = nextKey;
                                resultValue = nextValue;
                            }
                        }
                    }

                    found = true;
                    return resultValue;
                }

                found = false;
                return default;
            }

            public override TElement? TryGetLast(out bool found)
            {
                if (_parent is not null)
                {
                    return base.TryGetLast(out found);
                }

                using IEnumerator<TElement> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    IComparer<TKey> comparer = _comparer;
                    Func<TElement, TKey> keySelector = _keySelector;

                    TElement resultValue = e.Current;
                    TKey resultKey = keySelector(resultValue);

                    if (_descending)
                    {
                        while (e.MoveNext())
                        {
                            TElement nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (comparer.Compare(nextKey, resultKey) <= 0)
                            {
                                resultKey = nextKey;
                                resultValue = nextValue;
                            }
                        }
                    }
                    else
                    {
                        while (e.MoveNext())
                        {
                            TElement nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (comparer.Compare(nextKey, resultKey) >= 0)
                            {
                                resultKey = nextKey;
                                resultValue = nextValue;
                            }
                        }
                    }

                    found = true;
                    return resultValue;
                }

                found = false;
                return default;
            }
        }

        internal sealed partial class OrderedImplicitlyStableEnumerable<TElement> : OrderedEnumerable<TElement>
        {
            public override TElement[] ToArray()
            {
                TElement[] array = _source.ToArray();
                Sort(array, _descending);
                return array;
            }

            public override List<TElement> ToList()
            {
                List<TElement> list = _source.ToList();
                Sort(CollectionsMarshal.AsSpan(list), _descending);
                return list;
            }

            public override TElement? TryGetFirst(out bool found) =>
                TryGetFirstOrLast(out found, first: !_descending);

            public override TElement? TryGetLast(out bool found) =>
                TryGetFirstOrLast(out found, first: _descending);

            private TElement? TryGetFirstOrLast(out bool found, bool first)
            {
                if (TryGetSpan(_source, out ReadOnlySpan<TElement> span))
                {
                    if (span.Length != 0)
                    {
                        Debug.Assert(TypeIsImplicitlyStable<TElement>(), "Using Min/Max has different semantics for floating-point values.");

                        found = true;
                        return first ?
                            Min(_source) :
                            Max(_source);
                    }
                }
                else
                {
                    using IEnumerator<TElement> e = _source.GetEnumerator();

                    if (e.MoveNext())
                    {
                        TElement resultValue = e.Current;

                        if (first)
                        {
                            while (e.MoveNext())
                            {
                                TElement nextValue = e.Current;
                                if (Comparer<TElement>.Default.Compare(nextValue, resultValue) < 0)
                                {
                                    resultValue = nextValue;
                                }
                            }
                        }
                        else
                        {
                            while (e.MoveNext())
                            {
                                TElement nextValue = e.Current;
                                if (Comparer<TElement>.Default.Compare(nextValue, resultValue) >= 0)
                                {
                                    resultValue = nextValue;
                                }
                            }
                        }

                        found = true;
                        return resultValue;
                    }
                }

                found = false;
                return default;
            }
        }

        internal sealed class OrderedPartition<TElement> : Iterator<TElement>, IPartition<TElement>
        {
            private readonly OrderedEnumerable<TElement> _source;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;

            private TElement[]? _buffer;
            private int[]? _map;
            private int _maxIdx;

            public OrderedPartition(OrderedEnumerable<TElement> source, int minIdxInclusive, int maxIdxInclusive)
            {
                _source = source;
                _minIndexInclusive = minIdxInclusive;
                _maxIndexInclusive = maxIdxInclusive;
            }

            public override Iterator<TElement> Clone() => new OrderedPartition<TElement>(_source, _minIndexInclusive, _maxIndexInclusive);

            public override bool MoveNext()
            {
                int state = _state;

            Initialized:
                if (state > 1)
                {
                    Debug.Assert(_buffer is not null);
                    Debug.Assert(_map is not null);

                    int[] map = _map;
                    int i = state - 2 + _minIndexInclusive;
                    if (i <= _maxIdx)
                    {
                        _current = _buffer[map[i]];
                        _state++;
                        return true;
                    }
                }
                else if (state == 1)
                {
                    TElement[] buffer = _source.ToArray();
                    int count = buffer.Length;
                    if (count > _minIndexInclusive)
                    {
                        _maxIdx = _maxIndexInclusive;
                        if (count <= _maxIdx)
                        {
                            _maxIdx = count - 1;
                        }

                        if (_minIndexInclusive == _maxIdx)
                        {
                            _current = _source.GetEnumerableSorter().ElementAt(buffer, count, _minIndexInclusive);
                            _state = -1;
                            return true;
                        }

                        _map = _source.SortedMap(buffer, _minIndexInclusive, _maxIdx);
                        _buffer = buffer;
                        _state = state = 2;
                        goto Initialized;
                    }
                }

                Dispose();
                return false;
            }

            public IPartition<TElement>? Skip(int count)
            {
                int minIndex = _minIndexInclusive + count;
                return (uint)minIndex > (uint)_maxIndexInclusive ? null : new OrderedPartition<TElement>(_source, minIndex, _maxIndexInclusive);
            }

            public IPartition<TElement> Take(int count)
            {
                int maxIndex = _minIndexInclusive + count - 1;
                if ((uint)maxIndex >= (uint)_maxIndexInclusive)
                {
                    return this;
                }

                return new OrderedPartition<TElement>(_source, _minIndexInclusive, maxIndex);
            }

            public TElement? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive))
                {
                    return _source.TryGetElementAt(index + _minIndexInclusive, out found);
                }

                found = false;
                return default;
            }

            public TElement? TryGetFirst(out bool found) => _source.TryGetElementAt(_minIndexInclusive, out found);

            public TElement? TryGetLast(out bool found) =>
                _source.TryGetLast(_minIndexInclusive, _maxIndexInclusive, out found);

            public TElement[] ToArray() => _source.ToArray(_minIndexInclusive, _maxIndexInclusive);

            public List<TElement> ToList() => _source.ToList(_minIndexInclusive, _maxIndexInclusive);

            public int GetCount(bool onlyIfCheap) => _source.GetCount(_minIndexInclusive, _maxIndexInclusive, onlyIfCheap);
        }
    }
}
