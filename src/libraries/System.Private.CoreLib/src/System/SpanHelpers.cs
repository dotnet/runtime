// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#pragma warning disable 8500 // sizeof of managed types

namespace System
{
    internal static partial class SpanHelpers
    {
        public static void ClearWithoutReferences(ref byte b, nuint byteLength)
        {
            if (byteLength > Buffer.ZeroMemoryNativeThreshold)
                goto PInvoke;

            if (Vector.IsHardwareAccelerated && byteLength >= (uint)(Vector<byte>.Count))
            {
                // We have enough data for at least one vectorized write.
                nuint stopLoopAtOffset = byteLength & (nuint)(nint)(2 * (int)-Vector<byte>.Count); // intentional sign extension carries the negative bit
                nuint offset = 0;

                // Loop, writing 2 vectors at a time.
                // Compare 'numElements' rather than 'stopLoopAtOffset' because we don't want a dependency
                // on the very recently calculated 'stopLoopAtOffset' value.
                if (byteLength >= (uint)(2 * Vector<byte>.Count))
                {
                    do
                    {
                        Vector<byte>.Zero.StoreUnsafe(ref b, offset);
                        Vector<byte>.Zero.StoreUnsafe(ref b, offset + (nuint)Vector<byte>.Count);
                        offset += (uint)(2 * Vector<byte>.Count);
                    } while (offset < stopLoopAtOffset);
                }

                // At this point, if any data remains to be written, it's strictly less than
                // 2 * sizeof(Vector) bytes. The loop above had us write an even number of vectors.
                // If the total byte length instead involves us writing an odd number of vectors, write
                // one additional vector now. The bit check below tells us if we're in an "odd vector
                // count" situation.
                if ((byteLength & (nuint)Vector<byte>.Count) != 0)
                {
                    Vector<byte>.Zero.StoreUnsafe(ref b, offset);
                }

                // It's possible that some small buffer remains to be populated - something that won't
                // fit an entire vector's worth of data. Instead of falling back to a loop, we'll write
                // a vector at the very end of the buffer. This may involve overwriting previously
                // populated data, which is fine since we're splatting the same value for all entries.
                // There's no need to perform a length check here because we already performed this
                // check before entering the vectorized code path.
                Vector<byte>.Zero.StoreUnsafe(ref b, byteLength - (nuint)Vector<byte>.Count);

                // And we're done!
                return;
            }

            // If we reached this point, we cannot vectorize this data, or there are too few
            // elements for us to vectorize. Fall back to an unrolled loop.
            nuint i = 0;

            // Write 8 elements at a time
            if (byteLength >= 8)
            {
                nuint stopLoopAtOffset = byteLength & ~(nuint)7;
                do
                {
                    // JIT is expected to coalesce these stores into a single 8-byte store on 64-bit platforms
                    Unsafe.AddByteOffset(ref Unsafe.As<byte, uint>(ref b), (nint)i) = 0;
                    Unsafe.AddByteOffset(ref Unsafe.As<byte, uint>(ref b), (nint)i + 4) = 0;
                } while ((i += 8) < stopLoopAtOffset);
            }

            // Write next 4 elements if needed
            if ((byteLength & 4) != 0)
            {
                Unsafe.AddByteOffset(ref Unsafe.As<byte, uint>(ref b), (nint)i) = 0;
                i += 4;
            }

            // Write next 2 elements if needed
            if ((byteLength & 2) != 0)
            {
                Unsafe.AddByteOffset(ref Unsafe.As<byte, ushort>(ref b), (nint)i) = 0;
                i += 2;
            }

            // Write final element if needed
            if ((byteLength & 1) != 0)
            {
                Unsafe.AddByteOffset(ref b, (nint)i) = 0;
            }

        PInvoke:
            Buffer._ZeroMemory(ref b, byteLength);
        }

        public static unsafe void ClearWithReferences(ref IntPtr ip, nuint pointerSizeLength)
        {
            Debug.Assert((int)Unsafe.AsPointer(ref ip) % sizeof(IntPtr) == 0, "Should've been aligned on natural word boundary.");

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

#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228
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
#pragma warning restore IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228
    }
}
