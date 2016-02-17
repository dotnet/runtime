// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        TElement ElementAt(int index);

        TElement ElementAtOrDefault(int index);

        TElement First();

        TElement FirstOrDefault();

        TElement Last();

        TElement LastOrDefault();
    }

    internal sealed class EmptyPartition<TElement> : IPartition<TElement>, IEnumerator<TElement>
    {
        public EmptyPartition()
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
            return new EmptyPartition<TElement>();
        }

        public IPartition<TElement> Take(int count)
        {
            return new EmptyPartition<TElement>();
        }

        public TElement ElementAt(int index)
        {
            throw Error.ArgumentOutOfRange("index");
        }

        public TElement ElementAtOrDefault(int index)
        {
            return default(TElement);
        }

        public TElement First()
        {
            throw Error.NoElements();
        }

        public TElement FirstOrDefault()
        {
            return default(TElement);
        }

        public TElement Last()
        {
            throw Error.NoElements();
        }

        public TElement LastOrDefault()
        {
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
        private readonly int _minIndex;
        private readonly int _maxIndex;

        public OrderedPartition(OrderedEnumerable<TElement> source, int minIdx, int maxIdx)
        {
            _source = source;
            _minIndex = minIdx;
            _maxIndex = maxIdx;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return _source.GetEnumerator(_minIndex, _maxIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IPartition<TElement> Skip(int count)
        {
            int minIndex = _minIndex + count;
            return minIndex >= _maxIndex
                ? (IPartition<TElement>)new EmptyPartition<TElement>()
                : new OrderedPartition<TElement>(_source, minIndex, _maxIndex);
        }

        public IPartition<TElement> Take(int count)
        {
            int maxIndex = _minIndex + count - 1;
            if ((uint)maxIndex >= (uint)_maxIndex) maxIndex = _maxIndex;
            return new OrderedPartition<TElement>(_source, _minIndex, maxIndex);
        }

        public TElement ElementAt(int index)
        {
            if ((uint)index > (uint)_maxIndex - _minIndex) throw Error.ArgumentOutOfRange("index");
            return _source.ElementAt(index + _minIndex);
        }

        public TElement ElementAtOrDefault(int index)
        {
            return (uint)index <= (uint)_maxIndex - _minIndex ? _source.ElementAtOrDefault(index + _minIndex) : default(TElement);
        }

        public TElement First()
        {
            TElement result;
            if (!_source.TryGetElementAt(_minIndex, out result)) throw Error.NoElements();
            return result;
        }

        public TElement FirstOrDefault()
        {
            return _source.ElementAtOrDefault(_minIndex);
        }

        public TElement Last()
        {
            return _source.Last(_minIndex, _maxIndex);
        }

        public TElement LastOrDefault()
        {
            return _source.LastOrDefault(_minIndex, _maxIndex);
        }

        public TElement[] ToArray()
        {
            return _source.ToArray(_minIndex, _maxIndex);
        }

        public List<TElement> ToList()
        {
            return _source.ToList(_minIndex, _maxIndex);
        }

        public int GetCount(bool onlyIfCheap)
        {
            return _source.GetCount(_minIndex, _maxIndex, onlyIfCheap);
        }
    }

    public static partial class Enumerable
    {
        private sealed class ListPartition<TSource> : Iterator<TSource>, IPartition<TSource>
        {
            private readonly IList<TSource> _source;
            private readonly int _minIndex;
            private readonly int _maxIndex;
            private int _index;

            public ListPartition(IList<TSource> source, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source != null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _minIndex = minIndexInclusive;
                _maxIndex = maxIndexInclusive;
                _index = minIndexInclusive;
            }

            public override Iterator<TSource> Clone()
            {
                return new ListPartition<TSource>(_source, _minIndex, _maxIndex);
            }

            public override bool MoveNext()
            {
                if ((state == 1 & _index <= _maxIndex) && _index < _source.Count)
                {
                    current = _source[_index];
                    ++_index;
                    return true;
                }
                Dispose();
                return false;
            }

            public IPartition<TSource> Skip(int count)
            {
                int minIndex = _minIndex + count;
                return minIndex >= _maxIndex
                    ? (IPartition<TSource>)new EmptyPartition<TSource>()
                    : new ListPartition<TSource>(_source, minIndex, _maxIndex);
            }

            public IPartition<TSource> Take(int count)
            {
                int maxIndex = _minIndex + count - 1;
                return new ListPartition<TSource>(_source, _minIndex, (uint)maxIndex >= (uint)_maxIndex ? _maxIndex : maxIndex);
            }

            public TSource ElementAt(int index)
            {
                if (((uint)index > (uint)_maxIndex - _minIndex) || (index >= _source.Count - _minIndex)) throw Error.ArgumentOutOfRange("index");
                return _source[_minIndex + index];
            }

            public TSource ElementAtOrDefault(int index)
            {
                return ((uint)index > (uint)_maxIndex - _minIndex) || (index >= _source.Count - _minIndex) ? default(TSource) : _source[_minIndex + index];
            }

            public TSource First()
            {
                if (_source.Count <= _minIndex) throw Error.NoElements();
                return _source[_minIndex];
            }

            public TSource FirstOrDefault()
            {
                return _source.Count <= _minIndex ? default(TSource) : _source[_minIndex];
            }

            public TSource Last()
            {
                int lastIndex = _source.Count - 1;
                if (lastIndex < _minIndex) throw Error.NoElements();
                return _source[Math.Min(lastIndex, _maxIndex)];
            }

            public TSource LastOrDefault()
            {
                int lastIndex = _source.Count - 1;
                return lastIndex < _minIndex ? default(TSource) : _source[Math.Min(lastIndex, _maxIndex)];
            }

            private int Count
            {
                get
                {
                    int count = _source.Count;
                    if (count <= _minIndex) return 0;
                    return Math.Min(count - 1, _maxIndex) - _minIndex + 1;
                }
            }

            public TSource[] ToArray()
            {
                int count = Count;
                if (count == 0) return Array.Empty<TSource>();
                TSource[] array = new TSource[count];
                for (int i = 0, curIdx = _minIndex; i != array.Length; ++i, ++curIdx)
                    array[i] = _source[curIdx];
                return array;
            }

            public List<TSource> ToList()
            {
                int count = Count;
                if (count == 0) return new List<TSource>();
                List<TSource> list = new List<TSource>(count);
                int end = _minIndex + count;
                for (int i = _minIndex; i != end; ++i)
                    list.Add(_source[i]);
                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return Count;
            }
        }
    }
}
