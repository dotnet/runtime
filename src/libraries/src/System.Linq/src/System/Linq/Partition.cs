// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    /// <summary>
    /// An iterator that can produce an array through an optimized path.
    /// </summary>
    internal interface IArrayProvider<TElement>
    {
        /// <summary>
        /// Produce an array of the sequence through an optimized path.
        /// </summary>
        /// <returns>The array.</returns>
        TElement[] ToArray();
    }

    /// <summary>
    /// An iterator that can produce a <see cref="List{TElement}"/> through an optimized path.
    /// </summary>
    internal interface IListProvider<TElement>
    {
        /// <summary>
        /// Produce a <see cref="List{TElement}"/> of the sequence through an optimized path.
        /// </summary>
        /// <returns>The <see cref="List{TElement}"/>.</returns>
        List<TElement> ToList();
    }

    internal interface IPartition<TElement> : IEnumerable<TElement>, IArrayProvider<TElement>
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

    internal sealed class EmptyPartition<TElement> : IPartition<TElement>, IListProvider<TElement>, IEnumerator<TElement>
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
            if (maxIndex >= _maxIndex) maxIndex = _maxIndex;
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
    }
}
