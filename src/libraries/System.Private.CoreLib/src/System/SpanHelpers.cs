// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_AMD64 || TARGET_ARM64 || (TARGET_32BIT && !TARGET_ARM) || TARGET_LOONGARCH64
#define HAS_CUSTOM_BLOCKS
#endif

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
            ref byte bEnd = ref Unsafe.Add(ref b, byteLength);

            if (byteLength <= 16)
                goto MZER02;
            if (byteLength > 64)
                goto MZER05;

        MZER00:
            // Clear bytes which are multiples of 16 and leave the remainder for MZER01 to handle.
            Debug.Assert(byteLength > 16 && byteLength <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Buffer.Block16>(ref b, default); // [0,16]
#elif TARGET_64BIT
        Unsafe.WriteUnaligned<long>(ref b, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 8), 0); // [0,16]
#else
            Unsafe.WriteUnaligned<int>(ref b, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 12), 0); // [0,16]
#endif
            if (byteLength <= 32)
                goto MZER01;
#if HAS_CUSTOM_BLOCKS
        Unsafe.WriteUnaligned<Buffer.Block16>(ref Unsafe.Add(ref b, 16), default); // [0,32]
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 16), 0);
        Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 24), 0); // [0,32]
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 20), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 24), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 28), 0); // [0,32]
#endif
            if (byteLength <= 48)
                goto MZER01;
#if HAS_CUSTOM_BLOCKS
        Unsafe.WriteUnaligned<Buffer.Block16>(ref Unsafe.Add(ref b, 32), default); // [0,48]
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 32), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 40), 0); // [0,48]
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 32), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 36), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 40), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 44), 0); // [0,48]
#endif

        MZER01:
            // Unconditionally clear the last 16 bytes using bEnd and return.
            Debug.Assert(byteLength > 16 && byteLength <= 64);
#if HAS_CUSTOM_BLOCKS
        Unsafe.WriteUnaligned<Buffer.Block16>(ref Unsafe.Add(ref bEnd, -16), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref bEnd, -16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref bEnd, -8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -12), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -4), 0);
#endif
            return;

        MZER02:
            // Clear the first 8 bytes and then unconditionally clear the last 8 bytes and return.
            if ((byteLength & 24) == 0)
                goto MZER03;
            Debug.Assert(byteLength >= 8 && byteLength <= 16);
#if TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref b, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref bEnd, -8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref b, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -4), 0);
#endif
            return;

        MZER03:
            // Clear the first 4 bytes and then unconditionally clear the last 4 bytes and return.
            if ((byteLength & 4) == 0)
                goto MZER04;
            Debug.Assert(byteLength >= 4 && byteLength < 8);
            Unsafe.WriteUnaligned<int>(ref b, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -4), 0);
            return;

        MZER04:
            // Clear the first byte. For pending bytes, do an unconditionally clear of the last 2 bytes and return.
            Debug.Assert(byteLength < 4);
            if (byteLength == 0)
                return;
            b = 0;
            if ((byteLength & 2) == 0)
                return;
            Unsafe.WriteUnaligned<short>(ref Unsafe.Add(ref bEnd, -2), 0);
            return;

        MZER05:
            // PInvoke to the native version when the clear length exceeds the threshold.
            if (byteLength > Buffer.ZeroMemoryNativeThreshold)
            {
                goto PInvoke;
            }

#if HAS_CUSTOM_BLOCKS
            if (byteLength >= 256)
            {
                unsafe
                {
                    // Try to opportunistically align the destination below. The input isn't pinned, so the GC
                    // is free to move the references. We're therefore assuming that reads may still be unaligned.
                    nuint misalignedElements = 64 - (nuint)Unsafe.AsPointer(ref b) & 63;
                    Unsafe.WriteUnaligned<Buffer.Block64>(ref b, default);
                    b = ref Unsafe.Add(ref b, misalignedElements);
                    byteLength -= misalignedElements;
                }
            }
#endif
            // Clear 64-bytes at a time until the remainder is less than 64.
            // If remainder is greater than 16 bytes, then jump to MZER00. Otherwise, unconditionally clear the last 16 bytes and return.
            Debug.Assert(byteLength > 64 && byteLength <= Buffer.ZeroMemoryNativeThreshold);
            nuint n = byteLength >> 6;

        MZER06:
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Buffer.Block64>(ref b, default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref b, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 8), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 24), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 32), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 40), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 48), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 56), 0);
#else
            Unsafe.WriteUnaligned<int>(ref b, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 12), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 20), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 24), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 28), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 32), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 36), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 40), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 44), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 48), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 52), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 56), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 60), 0);
#endif
            b = ref Unsafe.Add(ref b, 64);
            n--;
            if (n != 0)
                goto MZER06;

            byteLength %= 64;
            if (byteLength > 16)
                goto MZER00;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Buffer.Block16>(ref Unsafe.Add(ref bEnd, -16), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref bEnd, -16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref bEnd, -8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -12), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref bEnd, -4), 0);
#endif
            return;

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
