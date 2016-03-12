// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    /// <summary>
    /// An iterator that can produce an array or <see cref="List{TElement}"/> through an optimized path.
    /// </summary>
    internal interface IIListProvider<TElement> : IEnumerable<TElement>
    {
        /// <summary>
        /// Produce an array of the sequence through an optimized path.
        /// </summary>
        /// <returns>The array.</returns>
        TElement[] ToArray();

        /// <summary>
        /// Produce a <see cref="List{TElement}"/> of the sequence through an optimized path.
        /// </summary>
        /// <returns>The <see cref="List{TElement}"/>.</returns>
        List<TElement> ToList();

        /// <summary>
        /// Returns the count of elements in the sequence.
        /// </summary>
        /// <param name="onlyIfCheap">If true then the count should only be calculated if doing
        /// so is quick (sure or likely to be constant time), otherwise -1 should be returned.</param>
        /// <returns>The number of elements.</returns>
        int GetCount(bool onlyIfCheap);
    }

    internal interface IPartition<TElement> : IIListProvider<TElement>
    {
        IPartition<TElement> Skip(int count);

        IPartition<TElement> Take(int count);

        TElement TryGetElementAt(int index, out bool found);

        TElement TryGetFirst(out bool found);

        TElement TryGetLast(out bool found);
    }

    internal sealed class EmptyPartition<TElement> : IPartition<TElement>, IEnumerator<TElement>
    {
        public static readonly IPartition<TElement> Instance = new EmptyPartition<TElement>();

        private EmptyPartition()
        {
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return false;
        }

        [ExcludeFromCodeCoverage] // Shouldn't be called, and as undefined can return or throw anything anyway.
        public TElement Current
        {
            get { return default(TElement); }
        }

        [ExcludeFromCodeCoverage] // Shouldn't be called, and as undefined can return or throw anything anyway.
        object IEnumerator.Current
        {
            get { return default(TElement); }
        }

        void IEnumerator.Reset()
        {
            throw Error.NotSupported();
        }

        void IDisposable.Dispose()
        {
            // Do nothing.
        }

        public IPartition<TElement> Skip(int count)
        {
            return this;
        }

        public IPartition<TElement> Take(int count)
        {
            return this;
        }

        public TElement TryGetElementAt(int index, out bool found)
        {
            found = false;
            return default(TElement);
        }

        public TElement TryGetFirst(out bool found)
        {
            found = false;
            return default(TElement);
        }

        public TElement TryGetLast(out bool found)
        {
            found = false;
            return default(TElement);
        }

        public TElement[] ToArray()
        {
            return Array.Empty<TElement>();
        }

        public List<TElement> ToList()
        {
            return new List<TElement>();
        }

        public int GetCount(bool onlyIfCheap)
        {
            return 0;
        }
    }

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

        public IEnumerator<TElement> GetEnumerator()
        {
            return _source.GetEnumerator(_minIndexInclusive, _maxIndexInclusive);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IPartition<TElement> Skip(int count)
        {
            int minIndex = _minIndexInclusive + count;
            return (uint)minIndex > (uint)_maxIndexInclusive ? EmptyPartition<TElement>.Instance : new OrderedPartition<TElement>(_source, minIndex, _maxIndexInclusive);
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

        public TElement TryGetElementAt(int index, out bool found)
        {
            if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive))
            {
                return _source.TryGetElementAt(index + _minIndexInclusive, out found);
            }

            found = false;
            return default(TElement);
        }

        public TElement TryGetFirst(out bool found)
        {
            return _source.TryGetElementAt(_minIndexInclusive, out found);
        }

        public TElement TryGetLast(out bool found)
        {
            return _source.TryGetLast(_minIndexInclusive, _maxIndexInclusive, out found);
        }

        public TElement[] ToArray()
        {
            return _source.ToArray(_minIndexInclusive, _maxIndexInclusive);
        }

        public List<TElement> ToList()
        {
            return _source.ToList(_minIndexInclusive, _maxIndexInclusive);
        }

        public int GetCount(bool onlyIfCheap)
        {
            return _source.GetCount(_minIndexInclusive, _maxIndexInclusive, onlyIfCheap);
        }
    }

    public static partial class Enumerable
    {
        private sealed class ListPartition<TSource> : Iterator<TSource>, IPartition<TSource>
        {
            private readonly IList<TSource> _source;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;
            private int _index;

            public ListPartition(IList<TSource> source, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source != null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _minIndexInclusive = minIndexInclusive;
                _maxIndexInclusive = maxIndexInclusive;
                _index = minIndexInclusive;
            }

            public override Iterator<TSource> Clone()
            {
                return new ListPartition<TSource>(_source, _minIndexInclusive, _maxIndexInclusive);
            }

            public override bool MoveNext()
            {
                if ((_state == 1 & _index <= _maxIndexInclusive) && _index < _source.Count)
                {
                    _current = _source[_index];
                    ++_index;
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new SelectListPartitionIterator<TSource, TResult>(_source, selector, _minIndexInclusive, _maxIndexInclusive);
            }

            public IPartition<TSource> Skip(int count)
            {
                int minIndex = _minIndexInclusive + count;
                return (uint)minIndex > (uint)_maxIndexInclusive ? EmptyPartition<TSource>.Instance : new ListPartition<TSource>(_source, minIndex, _maxIndexInclusive);
            }

            public IPartition<TSource> Take(int count)
            {
                int maxIndex = _minIndexInclusive + count - 1;
                return (uint)maxIndex >= (uint)_maxIndexInclusive ? this : new ListPartition<TSource>(_source, _minIndexInclusive, maxIndex);
            }

            public TSource TryGetElementAt(int index, out bool found)
            {
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
                {
                    found = true;
                    return _source[_minIndexInclusive + index];
                }

                found = false;
                return default(TSource);
            }

            public TSource TryGetFirst(out bool found)
            {
                if (_source.Count > _minIndexInclusive)
                {
                    found = true;
                    return _source[_minIndexInclusive];
                }

                found = false;
                return default(TSource);
            }

            public TSource TryGetLast(out bool found)
            {
                int lastIndex = _source.Count - 1;
                if (lastIndex >= _minIndexInclusive)
                {
                    found = true;
                    return _source[Math.Min(lastIndex, _maxIndexInclusive)];
                }

                found = false;
                return default(TSource);
            }

            private int Count
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

            public TSource[] ToArray()
            {
                int count = Count;
                if (count == 0)
                {
                    return Array.Empty<TSource>();
                }

                TSource[] array = new TSource[count];
                for (int i = 0, curIdx = _minIndexInclusive; i != array.Length; ++i, ++curIdx)
                {
                    array[i] = _source[curIdx];
                }

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
                int end = _minIndexInclusive + count;
                for (int i = _minIndexInclusive; i != end; ++i)
                {
                    list.Add(_source[i]);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return Count;
            }
        }
    }
}
