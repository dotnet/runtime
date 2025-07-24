// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class RangeIterator<T> : IList<T>, IReadOnlyList<T> where T : INumber<T>
        {
            public override IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
            {
                return new RangeSelectIterator<T, TResult>(_start, _endExclusive, selector);
            }

            public override T[] ToArray()
            {
                T start = _start;
                T[] array = new T[Count];
                FillIncrementing(array, start);
                return array;
            }

            public override List<T> ToList()
            {
                (T start, T end) = (_start, _endExclusive);
                int count = int.CreateTruncating(end - start);
                List<T> list = new List<T>(count);
                FillIncrementing(SetCountAndGetSpan(list, count), start);
                return list;
            }

            public void CopyTo(T[] array, int arrayIndex) =>
                FillIncrementing(array.AsSpan(arrayIndex, Count), _start);

            public override int GetCount(bool onlyIfCheap) => Count;

            public int Count => int.CreateTruncating(_endExclusive - _start);

            public override Iterator<T>? Skip(int count) =>
                count >= Count ? null :
                new RangeIterator<T>(_start + T.CreateTruncating(count), _endExclusive);

            public override Iterator<T> Take(int count) =>
                count >= Count ? this :
                new RangeIterator<T>(_start, _start + T.CreateTruncating(count));

            public override T TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)Count)
                {
                    found = true;
                    return _start + T.CreateTruncating(index);
                }

                found = false;
                return T.Zero;
            }

            public override T TryGetFirst(out bool found)
            {
                found = true;
                return _start;
            }

            public override T TryGetLast(out bool found)
            {
                found = true;
                return _endExclusive - T.One;
            }

            public override bool Contains(T item) =>
                uint.CreateTruncating(item - _start) < (uint)Count;

            public int IndexOf(T item)
            {
                uint index = uint.CreateTruncating(item - _start);
                if (index < (uint)Count)
                {
                    return (int)index;
                }

                return -1;
            }

            public T this[int index]
            {
                get
                {
                    if ((uint)index >= (uint)Count)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                    }

                    return _start + T.CreateTruncating(index);
                }
                set => ThrowHelper.ThrowNotSupportedException();
            }

            public bool IsReadOnly => true;

            void ICollection<T>.Add(T item) => ThrowHelper.ThrowNotSupportedException();
            void ICollection<T>.Clear() => ThrowHelper.ThrowNotSupportedException();
            void IList<T>.Insert(int index, T item) => ThrowHelper.ThrowNotSupportedException();
            bool ICollection<T>.Remove(T item) => ThrowHelper.ThrowNotSupportedException_Boolean();
            void IList<T>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();
        }
    }
}
