// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Microsoft.VisualBasic;

#pragma warning disable 8500 // sizeof of managed types

namespace System.Numerics.Tensors
{
    internal static partial class SpanHelpers
    {
        // Replacing Unsafe.IsOpportunisticallyAligned
        // Determines if the address is aligned at least to `alignment` bytes.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool IsOpportunisticallyAligned<T>(ref readonly T address, nuint alignment)
        {
            // `alignment` is expected to be a power of 2 in bytes.
            // We use Unsafe.AsPointer to convert to a pointer,
            // GC will keep alignment when moving objects (up to sizeof(void*)),
            // otherwise alignment should be considered a hint if not pinned.
            Debug.Assert(nuint.IsPow2(alignment));
            return ((nuint)Unsafe.AsPointer(ref Unsafe.AsRef(in address)) & (alignment - 1)) == 0;
        }

        public static unsafe void ClearWithReferences(ref IntPtr ip, nuint pointerSizeLength)
        {
            Debug.Assert(IsOpportunisticallyAligned(ref ip, (uint)sizeof(IntPtr)), "Should've been aligned on natural word boundary.");

            // First write backward 8 natural words at a time.
            // Writing backward allows us to get away with only simple modifications to the
            // mov instruction's base and index registers between loop iterations.

            for (; pointerSizeLength >= 8; pointerSizeLength -= 8)
            {
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -4) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -5) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -6) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -7) = default;
                Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -8) = default;
            }

            Debug.Assert(pointerSizeLength <= 7);

            // The logic below works by trying to minimize the number of branches taken for any
            // given range of lengths. For example, the lengths [ 4 .. 7 ] are handled by a single
            // branch, [ 2 .. 3 ] are handled by a single branch, and [ 1 ] is handled by a single
            // branch.
            //
            // We can write both forward and backward as a perf improvement. For example,
            // the lengths [ 4 .. 7 ] can be handled by zeroing out the first four natural
            // words and the last 3 natural words. In the best case (length = 7), there are
            // no overlapping writes. In the worst case (length = 4), there are three
            // overlapping writes near the middle of the buffer. In perf testing, the
            // penalty for performing duplicate writes is less expensive than the penalty
            // for complex branching.

            if (pointerSizeLength >= 4)
            {
                goto Write4To7;
            }
            else if (pointerSizeLength >= 2)
            {
                goto Write2To3;
            }
            else if (pointerSizeLength > 0)
            {
                goto Write1;
            }
            else
            {
                return; // nothing to write
            }

        Write4To7:
            Debug.Assert(pointerSizeLength >= 4);

            // Write first four and last three.
            Unsafe.Add(ref ip, 2) = default;
            Unsafe.Add(ref ip, 3) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = default;

        Write2To3:
            Debug.Assert(pointerSizeLength >= 2);

            // Write first two and last one.
            Unsafe.Add(ref ip, 1) = default;
            Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = default;

        Write1:
            Debug.Assert(pointerSizeLength >= 1);

            // Write only element.
            ip = default;
        }

        public static void Reverse(ref int buf, nuint length)
        {
            Debug.Assert(length > 1);

            nint remainder = (nint)length;
            nint offset = 0;

            if (Vector512.IsHardwareAccelerated && remainder >= Vector512<int>.Count * 2)
            {
                nint lastOffset = remainder - Vector512<int>.Count;
                do
                {
                    // Load in values from beginning and end of the array.
                    Vector512<int> tempFirst = Vector512.LoadUnsafe(ref buf, (nuint)offset);
                    Vector512<int> tempLast = Vector512.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Shuffle to reverse each vector:
                    //     +---------------+
                    //     | A | B | C | D |
                    //     +---------------+
                    //          --->
                    //     +---------------+
                    //     | D | C | B | A |
                    //     +---------------+
                    tempFirst = Vector512.Shuffle(tempFirst, Vector512.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
                    tempLast = Vector512.Shuffle(tempLast, Vector512.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector512<int>.Count;
                    lastOffset -= Vector512<int>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector512<int>.Count - offset;
            }
            else if (Avx2.IsSupported && remainder >= Vector256<int>.Count * 2)
            {
                nint lastOffset = remainder - Vector256<int>.Count;
                do
                {
                    // Load the values into vectors
                    Vector256<int> tempFirst = Vector256.LoadUnsafe(ref buf, (nuint)offset);
                    Vector256<int> tempLast = Vector256.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Permute to reverse each vector:
                    //     +-------------------------------+
                    //     | A | B | C | D | E | F | G | H |
                    //     +-------------------------------+
                    //         --->
                    //     +-------------------------------+
                    //     | H | G | F | E | D | C | B | A |
                    //     +-------------------------------+
                    tempFirst = Avx2.PermuteVar8x32(tempFirst, Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0));
                    tempLast = Avx2.PermuteVar8x32(tempLast, Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector256<int>.Count;
                    lastOffset -= Vector256<int>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector256<int>.Count - offset;
            }
            else if (Vector128.IsHardwareAccelerated && remainder >= Vector128<int>.Count * 2)
            {
                nint lastOffset = remainder - Vector128<int>.Count;
                do
                {
                    // Load in values from beginning and end of the array.
                    Vector128<int> tempFirst = Vector128.LoadUnsafe(ref buf, (nuint)offset);
                    Vector128<int> tempLast = Vector128.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Shuffle to reverse each vector:
                    //     +---------------+
                    //     | A | B | C | D |
                    //     +---------------+
                    //          --->
                    //     +---------------+
                    //     | D | C | B | A |
                    //     +---------------+
                    tempFirst = Vector128.Shuffle(tempFirst, Vector128.Create(3, 2, 1, 0));
                    tempLast = Vector128.Shuffle(tempLast, Vector128.Create(3, 2, 1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector128<int>.Count;
                    lastOffset -= Vector128<int>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector128<int>.Count - offset;
            }

            // Store any remaining values one-by-one
            if (remainder > 1)
            {
                ReverseInner(ref Unsafe.Add(ref buf, offset), (nuint)remainder);
            }
        }

        public static void Reverse(ref long buf, nuint length)
        {
            Debug.Assert(length > 1);

            nint remainder = (nint)length;
            nint offset = 0;

            if (Vector512.IsHardwareAccelerated && remainder >= Vector512<long>.Count * 2)
            {
                nint lastOffset = remainder - Vector512<long>.Count;
                do
                {
                    // Load in values from beginning and end of the array.
                    Vector512<long> tempFirst = Vector512.LoadUnsafe(ref buf, (nuint)offset);
                    Vector512<long> tempLast = Vector512.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Shuffle to reverse each vector:
                    //     +-------+
                    //     | A | B |
                    //     +-------+
                    //          --->
                    //     +-------+
                    //     | B | A |
                    //     +-------+
                    tempFirst = Vector512.Shuffle(tempFirst, Vector512.Create(7, 6, 5, 4, 3, 2, 1, 0));
                    tempLast = Vector512.Shuffle(tempLast, Vector512.Create(7, 6, 5, 4, 3, 2, 1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector512<long>.Count;
                    lastOffset -= Vector512<long>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector512<long>.Count - offset;
            }
            else if (Avx2.IsSupported && remainder >= Vector256<long>.Count * 2)
            {
                nint lastOffset = remainder - Vector256<long>.Count;
                do
                {
                    // Load the values into vectors
                    Vector256<long> tempFirst = Vector256.LoadUnsafe(ref buf, (nuint)offset);
                    Vector256<long> tempLast = Vector256.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Permute to reverse each vector:
                    //     +---------------+
                    //     | A | B | C | D |
                    //     +---------------+
                    //         --->
                    //     +---------------+
                    //     | D | C | B | A |
                    //     +---------------+
                    tempFirst = Avx2.Permute4x64(tempFirst, 0b00_01_10_11);
                    tempLast = Avx2.Permute4x64(tempLast, 0b00_01_10_11);

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector256<long>.Count;
                    lastOffset -= Vector256<long>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector256<long>.Count - offset;
            }
            else if (Vector128.IsHardwareAccelerated && remainder >= Vector128<long>.Count * 2)
            {
                nint lastOffset = remainder - Vector128<long>.Count;
                do
                {
                    // Load in values from beginning and end of the array.
                    Vector128<long> tempFirst = Vector128.LoadUnsafe(ref buf, (nuint)offset);
                    Vector128<long> tempLast = Vector128.LoadUnsafe(ref buf, (nuint)lastOffset);

                    // Shuffle to reverse each vector:
                    //     +-------+
                    //     | A | B |
                    //     +-------+
                    //          --->
                    //     +-------+
                    //     | B | A |
                    //     +-------+
                    tempFirst = Vector128.Shuffle(tempFirst, Vector128.Create(1, 0));
                    tempLast = Vector128.Shuffle(tempLast, Vector128.Create(1, 0));

                    // Store the reversed vectors
                    tempLast.StoreUnsafe(ref buf, (nuint)offset);
                    tempFirst.StoreUnsafe(ref buf, (nuint)lastOffset);

                    offset += Vector128<long>.Count;
                    lastOffset -= Vector128<long>.Count;
                } while (lastOffset >= offset);

                remainder = lastOffset + Vector128<long>.Count - offset;
            }

            // Store any remaining values one-by-one
            if (remainder > 1)
            {
                ReverseInner(ref Unsafe.Add(ref buf, offset), (nuint)remainder);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Reverse<T>(ref T elements, nuint length)
        {
            Debug.Assert(length > 1);

            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    Reverse(ref Unsafe.As<T, byte>(ref elements), length);
                    return;
                }
                else if (sizeof(T) == sizeof(char))
                {
                    Reverse(ref Unsafe.As<T, char>(ref elements), length);
                    return;
                }
                else if (sizeof(T) == sizeof(int))
                {
                    Reverse(ref Unsafe.As<T, int>(ref elements), length);
                    return;
                }
                else if (sizeof(T) == sizeof(long))
                {
                    Reverse(ref Unsafe.As<T, long>(ref elements), length);
                    return;
                }
            }

            ReverseInner(ref elements, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReverseInner<T>(ref T elements, nuint length)
        {
            Debug.Assert(length > 1);

            ref T first = ref elements;
            ref T last = ref Unsafe.Subtract(ref Unsafe.Add(ref first, length), 1);
            do
            {
                T temp = first;
                first = last;
                last = temp;
                first = ref Unsafe.Add(ref first, 1);
                last = ref Unsafe.Subtract(ref last, 1);
            } while (Unsafe.IsAddressLessThan(ref first, ref last));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CalculateTotalLength(ref nint[] lengths)
        {
            nint totalLength = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                totalLength *= lengths[i];
            }

            return totalLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint CalculateTotalLength(ReadOnlySpan<nint> lengths)
        {
            nint totalLength = 1;
            for (int i = 0; i < lengths.Length; i++)
            {
                totalLength *= lengths[i];
            }

            return totalLength;
        }

        /// <summary>
        /// Gets the set of strides that can be used to calculate the offset of n-dimensions in a 1-dimensional layout
        /// </summary>
        /// <returns></returns>
        public static nint[] CalculateStrides(int rank, ReadOnlySpan<nint> lengths)
        {
            nint[] strides = new nint[rank];

            nint stride = 1;

            for (int i = strides.Length - 1; i >= 0; i--)
            {
                strides[i] = stride;
                stride *= lengths[i];
            }

            return strides;
        }

        /// <summary>
        /// Calculates the 1-d index for n-d indices in layout specified by strides.
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="strides"></param>
        /// <param name="lengths"></param>
        /// <returns></returns>
        public static nint GetIndex(ReadOnlySpan<nint> indices, ReadOnlySpan<nint> strides, ReadOnlySpan<nint> lengths)
        {
            Debug.Assert(strides.Length == indices.Length);

            nint index = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] >= lengths[i])
                    ThrowHelper.ThrowIndexOutOfRangeException();
                index += strides[i] * (indices[i]);
            }

            return index;
        }

        public static void AdjustIndices(int curIndex, nint addend, ref nint[] curIndices, ReadOnlySpan<nint> lengths)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndices[curIndex] += addend;
            AdjustIndices(curIndex - 1, curIndices[curIndex] / lengths[curIndex], ref curIndices, lengths);
            curIndices[curIndex] = curIndices[curIndex] % lengths[curIndex];
        }

        public static void AdjustIndicesDown(int curIndex, nint addend, ref nint[] curIndices, ReadOnlySpan<nint> lengths)
        {
            if (addend <= 0 || curIndex < 0)
                return;
            curIndices[curIndex] -= addend;
            if (curIndices[curIndex] < 0)
            {
                curIndices[curIndex] = lengths[curIndex] - 1;
                AdjustIndices(curIndex - 1, 1, ref curIndices, lengths);
            }
        }

        public static nint CountTrueElements(Tensor<bool> filter)
        {
            var filterSpan = MemoryMarshal.CreateSpan(ref filter._values[0], (int)filter._linearLength);
            nint count = 0;
            for (int i = 0; i < filterSpan.Length; i++)
            {
                if (filterSpan[i])
                    count++;
            }

            return count;
        }
    }
}
