// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    internal sealed class OrderedPartition<TElement> : IPartition<TElement>
    {
        private readonly OrderedEnumerable<TElement> _source;
        private readonly int _minIndexInclusive;
        private readonly int _maxIndexInclusive;

        public OrderedPartition(OrderedEnumerable<TElement> source, int minIdxInclusive, int maxIdxInclusive)
        {
            _source = source;
            _minIndexInclusive = minIdxInclusive;
            _maxIndexInclusive = maxIdxInclusive;
        }

        public IEnumerator<TElement> GetEnumerator() => _source.GetEnumerator(_minIndexInclusive, _maxIndexInclusive);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    public static partial class Enumerable
    {
        /// <summary>
        /// An iterator that yields the items of part of an <see cref="IList{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        [DebuggerDisplay("Count = {Count}")]
        private sealed class ListPartition<TSource> : Iterator<TSource>, IPartition<TSource>, IList<TSource>, IReadOnlyList<TSource>
        {
            private readonly IList<TSource> _source;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;

            public ListPartition(IList<TSource> source, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source != null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _minIndexInclusive = minIndexInclusive;
                _maxIndexInclusive = maxIndexInclusive;
            }

            public override Iterator<TSource> Clone() =>
                new ListPartition<TSource>(_source, _minIndexInclusive, _maxIndexInclusive);

            public override bool MoveNext()
            {
                // _state - 1 represents the zero-based index into the list.
                // Having a separate field for the index would be more readable. However, we save it
                // into _state with a bias to minimize field size of the iterator.
                int index = _state - 1;
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
                {
                    _current = _source[_minIndexInclusive + index];
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new SelectListPartitionIterator<TSource, TResult>(_source, selector, _minIndexInclusive, _maxIndexInclusive);

            public IPartition<TSource>? Skip(int count)
            {
                int minIndex = _minIndexInclusive + count;
                return (uint)minIndex > (uint)_maxIndexInclusive ? null : new ListPartition<TSource>(_source, minIndex, _maxIndexInclusive);
            }

            public IPartition<TSource> Take(int count)
            {
                int maxIndex = _minIndexInclusive + count - 1;
                return (uint)maxIndex >= (uint)_maxIndexInclusive ? this : new ListPartition<TSource>(_source, _minIndexInclusive, maxIndex);
            }

            public TSource? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
                {
                    found = true;
                    return _source[_minIndexInclusive + index];
                }

                found = false;
                return default;
            }

            public TSource? TryGetFirst(out bool found)
            {
                if (_source.Count > _minIndexInclusive)
                {
                    found = true;
                    return _source[_minIndexInclusive];
                }

                found = false;
                return default;
            }

            public TSource? TryGetLast(out bool found)
            {
                int lastIndex = _source.Count - 1;
                if (lastIndex >= _minIndexInclusive)
                {
                    found = true;
                    return _source[Math.Min(lastIndex, _maxIndexInclusive)];
                }

                found = false;
                return default;
            }

            public int Count
            {
                get
                {
                    int count = _source.Count;
                    if (count <= _minIndexInclusive)
                    {
                        return 0;
                    }

                    return Math.Min(count - 1, _maxIndexInclusive) - _minIndexInclusive + 1;
                }
            }

            public int GetCount(bool onlyIfCheap) => Count;

            public TSource[] ToArray()
            {
                int count = Count;
                if (count == 0)
                {
                    return [];
                }

                TSource[] array = new TSource[count];
                Fill(_source, array, _minIndexInclusive);
                return array;
            }

            public List<TSource> ToList()
            {
                int count = Count;
                if (count == 0)
                {
                    return new List<TSource>();
                }

                List<TSource> list = new List<TSource>(count);
                Fill(_source, SetCountAndGetSpan(list, count), _minIndexInclusive);
                return list;
            }

            public void CopyTo(TSource[] array, int arrayIndex) =>
                Fill(_source, array.AsSpan(arrayIndex, Count), _minIndexInclusive);

            private static void Fill(IList<TSource> source, Span<TSource> destination, int sourceIndex)
            {
                for (int i = 0; i < destination.Length; i++, sourceIndex++)
                {
                    destination[i] = source[sourceIndex];
                }
            }

            public bool Contains(TSource item) => IndexOf(item) >= 0;

            public int IndexOf(TSource item)
            {
                IList<TSource> source = _source;

                int end = _minIndexInclusive + Count;
                for (int i = _minIndexInclusive; i < end; i++)
                {
                    if (EqualityComparer<TSource>.Default.Equals(source[i], item))
                    {
                        return i - _minIndexInclusive;
                    }
                }

                return -1;
            }

            public TSource this[int index]
            {
                get
                {
                    if ((uint)index >= (uint)Count)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                    }

                    return _source[_minIndexInclusive + index];
                }
                set => ThrowHelper.ThrowNotSupportedException();
            }

            public bool IsReadOnly => true;

            void ICollection<TSource>.Add(TSource item) => ThrowHelper.ThrowNotSupportedException();
            void ICollection<TSource>.Clear() => ThrowHelper.ThrowNotSupportedException();
            void IList<TSource>.Insert(int index, TSource item) => ThrowHelper.ThrowNotSupportedException();
            bool ICollection<TSource>.Remove(TSource item) => ThrowHelper.ThrowNotSupportedException_Boolean();
            void IList<TSource>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();
        }

        /// <summary>
        /// An iterator that yields the items of part of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed class EnumerablePartition<TSource> : Iterator<TSource>, IPartition<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive; // -1 if we want everything past _minIndexInclusive.
                                                     // If this is -1, it's impossible to set a limit on the count.
            private IEnumerator<TSource>? _enumerator;

            internal EnumerablePartition(IEnumerable<TSource> source, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source != null);
                Debug.Assert(!(source is IList<TSource>), $"The caller needs to check for {nameof(IList<TSource>)}.");
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(maxIndexInclusive >= -1);
                // Note that although maxIndexInclusive can't grow, it can still be int.MaxValue.
                // We support partitioning enumerables with > 2B elements. For example, e.Skip(1).Take(int.MaxValue) should work.
                // But if it is int.MaxValue, then minIndexInclusive must != 0. Otherwise, our count may overflow.
                Debug.Assert(maxIndexInclusive == -1 || (maxIndexInclusive - minIndexInclusive < int.MaxValue), $"{nameof(Limit)} will overflow!");
                Debug.Assert(maxIndexInclusive == -1 || minIndexInclusive <= maxIndexInclusive);

                _source = source;
                _minIndexInclusive = minIndexInclusive;
                _maxIndexInclusive = maxIndexInclusive;
            }

            // If this is true (e.g. at least one Take call was made), then we have an upper bound
            // on how many elements we can have.
            private bool HasLimit => _maxIndexInclusive != -1;

            private int Limit => _maxIndexInclusive + 1 - _minIndexInclusive; // This is that upper bound.

            public override Iterator<TSource> Clone() =>
                new EnumerablePartition<TSource>(_source, _minIndexInclusive, _maxIndexInclusive);

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                if (!HasLimit)
                {
                    // If HasLimit is false, we contain everything past _minIndexInclusive.
                    // Therefore, we have to iterate the whole enumerable.
                    return Math.Max(_source.Count() - _minIndexInclusive, 0);
                }

                using (IEnumerator<TSource> en = _source.GetEnumerator())
                {
                    // We only want to iterate up to _maxIndexInclusive + 1.
                    // Past that, we know the enumerable will be able to fit this partition,
                    // so the count will just be _maxIndexInclusive + 1 - _minIndexInclusive.

                    // Note that it is possible for _maxIndexInclusive to be int.MaxValue here,
                    // so + 1 may result in signed integer overflow. We need to handle this.
                    // At the same time, however, we are guaranteed that our max count can fit
                    // in an int because if that is true, then _minIndexInclusive must > 0.

                    uint count = SkipAndCount((uint)_maxIndexInclusive + 1, en);
                    Debug.Assert(count != (uint)int.MaxValue + 1 || _minIndexInclusive > 0, "Our return value will be incorrect.");
                    return Math.Max((int)count - _minIndexInclusive, 0);
                }

            }

            public override bool MoveNext()
            {
                // Cases where GetEnumerator has not been called or Dispose has already
                // been called need to be handled explicitly, due to the default: clause.
                int taken = _state - 3;
                if (taken < -2)
                {
                    Dispose();
                    return false;
                }

                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        if (!SkipBeforeFirst(_enumerator))
                        {
                            // Reached the end before we finished skipping.
                            break;
                        }

                        _state = 3;
                        goto default;
                    default:
                        Debug.Assert(_enumerator != null);
                        if ((!HasLimit || taken < Limit) && _enumerator.MoveNext())
                        {
                            if (HasLimit)
                            {
                                // If we are taking an unknown number of elements, it's important not to increment _state.
                                // _state - 3 may eventually end up overflowing & we'll hit the Dispose branch even though
                                // we haven't finished enumerating.
                                _state++;
                            }
                            _current = _enumerator.Current;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new SelectIPartitionIterator<TSource, TResult>(this, selector);

            public IPartition<TSource>? Skip(int count)
            {
                int minIndex = _minIndexInclusive + count;

                if (!HasLimit)
                {
                    if (minIndex < 0)
                    {
                        // If we don't know our max count and minIndex can no longer fit in a positive int,
                        // then we will need to wrap ourselves in another iterator.
                        // This can happen, for example, during e.Skip(int.MaxValue).Skip(int.MaxValue).
                        return new EnumerablePartition<TSource>(this, count, -1);
                    }
                }
                else if ((uint)minIndex > (uint)_maxIndexInclusive)
                {
                    // If minIndex overflows and we have an upper bound, we will go down this branch.
                    // We know our upper bound must be smaller than minIndex, since our upper bound fits in an int.
                    // This branch should not be taken if we don't have a bound.
                    return null;
                }

                Debug.Assert(minIndex >= 0, $"We should have taken care of all cases when {nameof(minIndex)} overflows.");
                return new EnumerablePartition<TSource>(_source, minIndex, _maxIndexInclusive);
            }

            public IPartition<TSource> Take(int count)
            {
                int maxIndex = _minIndexInclusive + count - 1;
                if (!HasLimit)
                {
                    if (maxIndex < 0)
                    {
                        // If we don't know our max count and maxIndex can no longer fit in a positive int,
                        // then we will need to wrap ourselves in another iterator.
                        // Note that although maxIndex may be too large, the difference between it and
                        // _minIndexInclusive (which is count - 1) must fit in an int.
                        // Example: e.Skip(50).Take(int.MaxValue).

                        return new EnumerablePartition<TSource>(this, 0, count - 1);
                    }
                }
                else if ((uint)maxIndex >= (uint)_maxIndexInclusive)
                {
                    // If we don't know our max count, we can't go down this branch.
                    // It's always possible for us to contain more than count items, as the rest
                    // of the enumerable past _minIndexInclusive can be arbitrarily long.
                    return this;
                }

                Debug.Assert(maxIndex >= 0, $"We should have taken care of all cases when {nameof(maxIndex)} overflows.");
                return new EnumerablePartition<TSource>(_source, _minIndexInclusive, maxIndex);
            }

            public TSource? TryGetElementAt(int index, out bool found)
            {
                // If the index is negative or >= our max count, return early.
                if (index >= 0 && (!HasLimit || index < Limit))
                {
                    using (IEnumerator<TSource> en = _source.GetEnumerator())
                    {
                        Debug.Assert(_minIndexInclusive + index >= 0, $"Adding {nameof(index)} caused {nameof(_minIndexInclusive)} to overflow.");

                        if (SkipBefore(_minIndexInclusive + index, en) && en.MoveNext())
                        {
                            found = true;
                            return en.Current;
                        }
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetFirst(out bool found)
            {
                using (IEnumerator<TSource> en = _source.GetEnumerator())
                {
                    if (SkipBeforeFirst(en) && en.MoveNext())
                    {
                        found = true;
                        return en.Current;
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetLast(out bool found)
            {
                using (IEnumerator<TSource> en = _source.GetEnumerator())
                {
                    if (SkipBeforeFirst(en) && en.MoveNext())
                    {
                        int remaining = Limit - 1; // Max number of items left, not counting the current element.
                        int comparand = HasLimit ? 0 : int.MinValue; // If we don't have an upper bound, have the comparison always return true.
                        TSource result;

                        do
                        {
                            remaining--;
                            result = en.Current;
                        }
                        while (remaining >= comparand && en.MoveNext());

                        found = true;
                        return result;
                    }
                }

                found = false;
                return default;
            }

            public TSource[] ToArray()
            {
                using (IEnumerator<TSource> en = _source.GetEnumerator())
                {
                    if (SkipBeforeFirst(en) && en.MoveNext())
                    {
                        int remaining = Limit - 1; // Max number of items left, not counting the current element.
                        int comparand = HasLimit ? 0 : int.MinValue; // If we don't have an upper bound, have the comparison always return true.

                        SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                        SegmentedArrayBuilder<TSource> builder = new(scratch);
                        do
                        {
                            remaining--;
                            builder.Add(en.Current);
                        }
                        while (remaining >= comparand && en.MoveNext());

                        TSource[] result = builder.ToArray();
                        builder.Dispose();

                        return result;
                    }
                }

                return [];
            }

            public List<TSource> ToList()
            {
                var list = new List<TSource>();

                using (IEnumerator<TSource> en = _source.GetEnumerator())
                {
                    if (SkipBeforeFirst(en) && en.MoveNext())
                    {
                        int remaining = Limit - 1; // Max number of items left, not counting the current element.
                        int comparand = HasLimit ? 0 : int.MinValue; // If we don't have an upper bound, have the comparison always return true.

                        do
                        {
                            remaining--;
                            list.Add(en.Current);
                        }
                        while (remaining >= comparand && en.MoveNext());
                    }
                }

                return list;
            }

            private bool SkipBeforeFirst(IEnumerator<TSource> en) => SkipBefore(_minIndexInclusive, en);

            private static bool SkipBefore(int index, IEnumerator<TSource> en) => SkipAndCount(index, en) == index;

            private static int SkipAndCount(int index, IEnumerator<TSource> en)
            {
                Debug.Assert(index >= 0);
                return (int)SkipAndCount((uint)index, en);
            }

            private static uint SkipAndCount(uint index, IEnumerator<TSource> en)
            {
                Debug.Assert(en != null);

                for (uint i = 0; i < index; i++)
                {
                    if (!en.MoveNext())
                    {
                        return i;
                    }
                }

                return index;
            }
        }
    }
}
