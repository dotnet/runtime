// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class RangeIterator : IPartition<int>, IList<int>, IReadOnlyList<int>
        {
            public override IEnumerable<TResult> Select<TResult>(Func<int, TResult> selector)
            {
                return new SelectRangeIterator<TResult>(_start, _end, selector);
            }

            public int[] ToArray()
            {
                int[] array = new int[_end - _start];
                Fill(array, _start);
                return array;
            }

            public List<int> ToList()
            {
                List<int> list = new List<int>(_end - _start);
                Fill(SetCountAndGetSpan(list, _end - _start), _start);
                return list;
            }

            public void CopyTo(int[] array, int arrayIndex) =>
                Fill(array.AsSpan(arrayIndex, _end - _start), _start);

            private static void Fill(Span<int> destination, int value)
            {
                for (int i = 0; i < destination.Length; i++, value++)
                {
                    destination[i] = value;
                }
            }

            public int GetCount(bool onlyIfCheap) => unchecked(_end - _start);

            public int Count => _end - _start;

            public IPartition<int> Skip(int count)
            {
                if (count >= _end - _start)
                {
                    return EmptyPartition<int>.Instance;
                }

                return new RangeIterator(_start + count, _end - _start - count);
            }

            public IPartition<int> Take(int count)
            {
                int curCount = _end - _start;
                if (count >= curCount)
                {
                    return this;
                }

                return new RangeIterator(_start, count);
            }

            public int TryGetElementAt(int index, out bool found)
            {
                if (unchecked((uint)index < (uint)(_end - _start)))
                {
                    found = true;
                    return _start + index;
                }

                found = false;
                return 0;
            }

            public int TryGetFirst(out bool found)
            {
                found = true;
                return _start;
            }

            public int TryGetLast(out bool found)
            {
                found = true;
                return _end - 1;
            }

            public bool Contains(int item) =>
                (uint)(item - _start) < (uint)(_end - _start);

            public int IndexOf(int item) =>
                Contains(item) ? item - _start : -1;

            public int this[int index]
            {
                get
                {
                    if ((uint)index >= (uint)(_end - _start))
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                    }

                    return _start + index;
                }
                set => ThrowHelper.ThrowNotSupportedException();
            }

            public bool IsReadOnly => true;

            void ICollection<int>.Add(int item) => ThrowHelper.ThrowNotSupportedException();
            void ICollection<int>.Clear() => ThrowHelper.ThrowNotSupportedException();
            void IList<int>.Insert(int index, int item) => ThrowHelper.ThrowNotSupportedException();
            bool ICollection<int>.Remove(int item) => ThrowHelper.ThrowNotSupportedException_Boolean();
            void IList<int>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();
        }
    }
}
