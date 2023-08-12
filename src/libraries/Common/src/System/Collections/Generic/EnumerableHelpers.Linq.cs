// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Internal helper functions for working with enumerables.
    /// </summary>
    internal static partial class EnumerableHelpers
    {
        /// <summary>
        /// Copies items from an enumerable to an array.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the array to start copying to.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        internal static void Copy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(source != null);
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(array.Length - arrayIndex >= count);

            if (source is ICollection<T> collection)
            {
                Debug.Assert(collection.Count == count);
                collection.CopyTo(array, arrayIndex);
                return;
            }

            IterativeCopy(source, array, arrayIndex, count);
        }

        /// <summary>
        /// Copies items from a non-collection enumerable to an array.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the array to start copying to.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        internal static void IterativeCopy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(source != null && !(source is ICollection<T>));
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(array.Length - arrayIndex >= count);

            int endIndex = arrayIndex + count;
            foreach (T item in source)
            {
                array[arrayIndex++] = item;
            }

            Debug.Assert(arrayIndex == endIndex);
        }

        /// <summary>Converts an enumerable to an array.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <returns>The resulting array.</returns>
        internal static T[] ToArray<T>(IEnumerable<T> source)
        {
            Debug.Assert(source != null);

            if (source is ICollection<T> collection)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return Array.Empty<T>();
                }

                var result = new T[count];
                collection.CopyTo(result, arrayIndex: 0);
                return result;
            }

            ToArrayHelper<T> helper = new ToArrayHelper<T>(initialCapacity: 4);
            using (IEnumerator<T> e = source.GetEnumerator())
            {
                while (true)
                {
                    Span<T> span = helper.CurrentSpan;
                    for (int i = 0; i < span.Length; i++)
                    {
                        if (e.MoveNext())
                        {
                            span[i] = e.Current;
                        }
                        else
                        {
                            return helper.ToArray(i);
                        }
                    }
                    helper.AllocateNextBlock();
                }
            }
        }

        // when smallest block size is 4, reaching Array.MaxLength size is "29".
        // length, blockSize, totalSize
        // 1  4          4
        // 2  8          12
        // 3  16         28
        // 4  32         60
        // 5  64         124
        // 6  128        252
        // 7  256        508
        // 8  512        1020
        // 9  1024       2044
        // 10 2048       4092
        // 11 4096       8188
        // 12 8192       16380
        // 13 16384      32764
        // 14 32768      65532
        // 15 65536      131068
        // 16 131072     262140
        // 17 262144     524284
        // 18 524288     1048572
        // 19 1048576    2097148
        // 20 2097152    4194300
        // 21 4194304    8388604
        // 22 8388608    16777212
        // 23 16777216   33554428
        // 24 33554432   67108860
        // 25 67108864   134217724
        // 26 134217728  268435452
        // 27 268435456  536870908
        // 28 536870912  1073741820
        // 29 1073741771 2147483591 <- reach Array.MaxLength(2147483591)
        [InlineArray(29)]
        private struct ArrayBlock<T>
        {
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
            private T[] _array;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CA1823 // Avoid unused private fields
        }

        private struct ToArrayHelper<T>
        {
            private int _index;
            private int _count;
            private T[] _currentBlock;
            private ArrayBlock<T> _blocks;

            public ToArrayHelper(int initialCapacity)
            {
                _blocks = default(ArrayBlock<T>);
                _currentBlock = _blocks[0] = new T[initialCapacity];
            }

            public Span<T> CurrentSpan => _currentBlock;

            public void AllocateNextBlock()
            {
                _index++;
                _count += _currentBlock.Length;

                int nextSize = unchecked(_currentBlock.Length * 2);
                if (nextSize < 0 || Array.MaxLength < (_count + nextSize))
                {
                    nextSize = Array.MaxLength - _count;
                }

                _currentBlock = _blocks[_index] = new T[nextSize];
            }

            public T[] ToArray(int lastBlockCount)
            {
                T[] array = GC.AllocateUninitializedArray<T>(_count + lastBlockCount);
                Span<T> dest = array.AsSpan();
                for (int i = 0; i < _index; i++)
                {
                    _blocks[i].CopyTo(dest);
                    dest = dest.Slice(_blocks[i].Length);
                }
                _currentBlock.AsSpan(0, lastBlockCount).CopyTo(dest);
                return array;
            }
        }
    }
}
