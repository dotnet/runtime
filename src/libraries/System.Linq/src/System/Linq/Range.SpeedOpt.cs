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
        private sealed partial class RangeIterator : IPartition<int>
        {
            public override IEnumerable<TResult> Select<TResult>(Func<int, TResult> selector)
            {
                return new SelectRangeIterator<TResult>(_start, _end, selector);
            }

            public int[] ToArray()
            {
                int[] array = new int[_end - _start];
                InitializeSpan(array);
                return array;
            }

            public List<int> ToList()
            {
                List<int> list = new List<int>(_end - _start);
                for (int cur = _start; cur != _end; cur++)
                {
                    list.Add(cur);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap) => unchecked(_end - _start);

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

            // Destination *must* be non-empty and exactly match the range length
            private void InitializeSpan(Span<int> destination)
            {
                if (destination.Length < Vector<int>.Count * 2)
                {
                    int end = _end;
                    ref int pos = ref MemoryMarshal.GetReference(destination);
                    for (int cur = _start; cur < end; cur++)
                    {
                        pos = cur;
                        pos = ref Unsafe.Add(ref pos, 1);
                    }
                }
                else
                {
                    InitializeSpanCore(destination);
                }
            }

            private void InitializeSpanCore(Span<int> destination)
            {
                int width = Vector<int>.Count;
                int stride = Vector<int>.Count * 2;
                int remainder = destination.Length % stride;

                // Up to 16 elements which corresponds to AVX512
                Vector<int> initMask = Unsafe.ReadUnaligned<Vector<int>>(
                    ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(
                        (ReadOnlySpan<int>)new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 })));

                Vector<int> mask = new Vector<int>(stride);
                Vector<int> value = new Vector<int>(_start) + initMask;
                Vector<int> value2 = value + new Vector<int>(width);

                ref int pos = ref MemoryMarshal.GetReference(destination);
                ref int limit = ref Unsafe.Add(ref pos, destination.Length - remainder);
                while (!Unsafe.AreSame(ref pos, ref limit))
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref pos), value);
                    Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref pos, width)), value2);

                    value += mask;
                    value2 += mask;
                    pos = ref Unsafe.Add(ref pos, stride);
                }

                int cur = _start + (destination.Length - remainder);
                int end = _end;
                while (cur < end)
                {
                    pos = cur;
                    pos = ref Unsafe.Add(ref pos, 1);
                    cur++;
                }
            }
        }
    }
}
