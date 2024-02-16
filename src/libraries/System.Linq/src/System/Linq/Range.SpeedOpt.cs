// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
                int start = _start;
                int[] array = new int[_end - start];
                Fill(array, start);
                return array;
            }

            public List<int> ToList()
            {
                (int start, int end) = (_start, _end);
                List<int> list = new List<int>(end - start);
                Fill(SetCountAndGetSpan(list, end - start), start);
                return list;
            }

            public void CopyTo(int[] array, int arrayIndex) =>
                Fill(array.AsSpan(arrayIndex, _end - _start), _start);

            private static void Fill(Span<int> destination, int value)
            {
                ref int pos = ref MemoryMarshal.GetReference(destination);
                ref int end = ref Unsafe.Add(ref pos, destination.Length);

                if (Vector.IsHardwareAccelerated &&
                    destination.Length >= Vector<int>.Count)
                {
                    Vector<int> init = Vector<int>.Indices;
                    Vector<int> current = new Vector<int>(value) + init;
                    Vector<int> increment = new Vector<int>(Vector<int>.Count);

                    ref int oneVectorFromEnd = ref Unsafe.Subtract(ref end, Vector<int>.Count);
                    do
                    {
                        current.StoreUnsafe(ref pos);
                        current += increment;
                        pos = ref Unsafe.Add(ref pos, Vector<int>.Count);
                    }
                    while (!Unsafe.IsAddressGreaterThan(ref pos, ref oneVectorFromEnd));

                    value = current[0];
                }

                while (Unsafe.IsAddressLessThan(ref pos, ref end))
                {
                    pos = value++;
                    pos = ref Unsafe.Add(ref pos, 1);
                }
            }

            public int GetCount(bool onlyIfCheap) => _end - _start;

            public int Count => _end - _start;

            public IPartition<int>? Skip(int count)
            {
                if (count >= _end - _start)
                {
                    return null;
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
                if ((uint)index < (uint)(_end - _start))
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
