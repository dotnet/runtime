// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq;

public static partial class Enumerable
{
    /// <summary>
    /// An iterator that implements <see cref="IList{T}"/>. This is used primarily in size-optimized
    /// code to turn linear-time iterators into constant-time iterators.  The primary cost is
    /// additional type checks, which are small compared to generic virtual calls.
    /// </summary>
    private sealed class SizeOptIListSelectIterator<TSource, TResult>(IList<TSource> _source, Func<TSource, TResult> _selector)
        : Iterator<TResult>, IList<TResult>
    {
        TResult IList<TResult>.this[int index]
        {
            get => _selector(_source[index]);
            set => ThrowHelper.ThrowNotSupportedException();
        }

        int ICollection<TResult>.Count => _source.Count;
        bool ICollection<TResult>.IsReadOnly => true;

        void ICollection<TResult>.Add(TResult item) => ThrowHelper.ThrowNotSupportedException();
        void ICollection<TResult>.Clear() => ThrowHelper.ThrowNotSupportedException();
        bool ICollection<TResult>.Contains(TResult item)
            => IndexOf(item) >= 0;

        int IList<TResult>.IndexOf(TResult item) => IndexOf(item);

        private int IndexOf(TResult item)
        {
            for (int i = 0; i < _source.Count; i++)
            {
                if (EqualityComparer<TResult>.Default.Equals(_selector(_source[i]), item))
                {
                    return i;
                }
            }
            return -1;
        }

        void ICollection<TResult>.CopyTo(TResult[] array, int arrayIndex)
        {
            for (int i = 0; i < _source.Count; i++)
            {
                array[arrayIndex + i] = _selector(_source[i]);
            }
        }

        void IList<TResult>.Insert(int index, TResult item) => ThrowHelper.ThrowNotSupportedException();
        bool ICollection<TResult>.Remove(TResult item) => ThrowHelper.ThrowNotSupportedException_Boolean();
        void IList<TResult>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

        private protected override Iterator<TResult> Clone()
            => new SizeOptIListSelectIterator<TSource, TResult>(_source, _selector);

        public override bool MoveNext()
        {
            var source = _source;
            int index = _state - 1;
            if ((uint)index < (uint)source.Count)
            {
                _state++;
                _current = _selector(source[index]);
                return true;
            }

            Dispose();
            return false;
        }

        public override TResult[] ToArray()
        {
            TResult[] array = new TResult[_source.Count];
            for (int i = 0; i < _source.Count; i++)
            {
                array[i] = _selector(_source[i]);
            }
            return array;
        }
        public override List<TResult> ToList()
        {
            List<TResult> list = new List<TResult>(_source.Count);
            for (int i = 0; i < _source.Count; i++)
            {
                list.Add(_selector(_source[i]));
            }
            return list;
        }
        public override int GetCount(bool onlyIfCheap) => _source.Count;
    }
}
