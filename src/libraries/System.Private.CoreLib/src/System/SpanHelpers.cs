// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System
{
    internal static partial class SpanHelpers
    {
        public static unsafe void ClearWithoutReferences(ref byte b, nuint byteLength)
        {
            if (byteLength == 0)
                return;

#if TARGET_AMD64 || TARGET_ARM64 || TARGET_LOONGARCH64
            // The exact matrix on when ZeroMemory is faster than InitBlockUnaligned is very complex. The factors to consider include
            // type of hardware and memory alignment. This threshold was chosen as a good balance across different configurations.
            if (byteLength > 768)
                goto PInvoke;
            Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
            return;
#else
            // TODO: Optimize other platforms to be on par with AMD64 CoreCLR
            // Note: It's important that this switch handles lengths at least up to 22.
            // See notes below near the main loop for why.

            // The switch will be very fast since it can be implemented using a jump
            // table in assembly. See http://stackoverflow.com/a/449297/4077294 for more info.

            switch (byteLength)
            {
                case 1:
                    b = 0;
                    return;
                case 2:
                    Unsafe.As<byte, short>(ref b) = 0;
                    return;
                case 3:
                    Unsafe.As<byte, short>(ref b) = 0;
                    Unsafe.Add<byte>(ref b, 2) = 0;
                    return;
                case 4:
                    Unsafe.As<byte, int>(ref b) = 0;
                    return;
                case 5:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.Add<byte>(ref b, 4) = 0;
                    return;
                case 6:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    return;
                case 7:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.Add<byte>(ref b, 6) = 0;
                    return;
                case 8:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    return;
                case 9:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.Add<byte>(ref b, 8) = 0;
                    return;
                case 10:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 11:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.Add<byte>(ref b, 10) = 0;
                    return;
                case 12:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    return;
                case 13:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.Add<byte>(ref b, 12) = 0;
                    return;
                case 14:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
                    return;
                case 15:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
                    Unsafe.Add<byte>(ref b, 14) = 0;
                    return;
                case 16:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    return;
                case 17:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.Add<byte>(ref b, 16) = 0;
                    return;
                case 18:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    return;
                case 19:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.Add<byte>(ref b, 18) = 0;
                    return;
                case 20:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    return;
                case 21:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.Add<byte>(ref b, 20) = 0;
                    return;
                case 22:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add<byte>(ref b, 16)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add<byte>(ref b, 20)) = 0;
                    return;
            }

            // P/Invoke into the native version for large lengths
            if (byteLength >= 512) goto PInvoke;

            nuint i = 0; // byte offset at which we're copying

            if (((nuint)Unsafe.AsPointer(ref b) & 3) != 0)
            {
                if (((nuint)Unsafe.AsPointer(ref b) & 1) != 0)
                {
                    b = 0;
                    i += 1;
                    if (((nuint)Unsafe.AsPointer(ref b) & 2) != 0)
                        goto IntAligned;
                }
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 2;
            }

            IntAligned:

            // On 64-bit IntPtr.Size == 8, so we want to advance to the next 8-aligned address. If
            // (int)b % 8 is 0, 5, 6, or 7, we will already have advanced by 0, 3, 2, or 1
            // bytes to the next aligned address (respectively), so do nothing. On the other hand,
            // if it is 1, 2, 3, or 4 we will want to copy-and-advance another 4 bytes until
            // we're aligned.
            // The thing 1, 2, 3, and 4 have in common that the others don't is that if you
            // subtract one from them, their 3rd lsb will not be set. Hence, the below check.

            if ((((nuint)Unsafe.AsPointer(ref b) - 1) & 4) == 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 4;
            }

            nuint end = byteLength - 16;
            byteLength -= i; // lower 4 bits of byteLength represent how many bytes are left *after* the unrolled loop

            // We know due to the above switch-case that this loop will always run 1 iteration; max
            // bytes we clear before checking is 23 (7 to align the pointers, 16 for 1 iteration) so
            // the switch handles lengths 0-22.
            Debug.Assert(end >= 7 && i <= end);

            // This is separated out into a different variable, so the i + 16 addition can be
            // performed at the start of the pipeline and the loop condition does not have
            // a dependency on the writes.
            nuint counter;

            do
            {
                counter = i + 16;

                // This loop looks very costly since there appear to be a bunch of temporary values
                // being created with the adds, but the jit (for x86 anyways) will convert each of
                // these to use memory addressing operands.

                // So the only cost is a bit of code size, which is made up for by the fact that
                // we save on writes to b.

#if TARGET_64BIT
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i + 8)) = 0;
#else
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 4)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 8)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 12)) = 0;
#endif

                i = counter;

                // See notes above for why this wasn't used instead
                // i += 16;
            }
            while (counter <= end);

            if ((byteLength & 8) != 0)
            {
#if TARGET_64BIT
                Unsafe.As<byte, long>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
#else
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i + 4)) = 0;
#endif
                i += 8;
            }
            if ((byteLength & 4) != 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 4;
            }
            if ((byteLength & 2) != 0)
            {
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset<byte>(ref b, i)) = 0;
                i += 2;
            }
            if ((byteLength & 1) != 0)
            {
                Unsafe.AddByteOffset<byte>(ref b, i) = 0;
                // We're not using i after this, so not needed
                // i += 1;
            }

            return;
#endif

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
            if (Avx2.IsSupported && (nuint)Vector256<int>.Count * 2 <= length)
            {
                nuint numElements = (nuint)Vector256<int>.Count;
                nuint numIters = (length / numElements) / 2;
                Vector256<int> reverseMask = Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0);
                for (nuint i = 0; i < numIters; i++)
                {
                    nuint firstOffset = i * numElements;
                    nuint lastOffset = length - ((1 + i) * numElements);

                    // Load the values into vectors
                    Vector256<int> tempFirst = Vector256.LoadUnsafe(ref buf, firstOffset);
                    Vector256<int> tempLast = Vector256.LoadUnsafe(ref buf, lastOffset);

                    // Permute to reverse each vector:
                    //     +-------------------------------+
                    //     | A | B | C | D | E | F | G | H |
                    //     +-------------------------------+
                    //         --->
                    //     +-------------------------------+
                    //     | H | G | F | E | D | C | B | A |
                    //     +-------------------------------+
                    tempFirst = Avx2.PermuteVar8x32(tempFirst, reverseMask);
                    tempLast = Avx2.PermuteVar8x32(tempLast, reverseMask);

                    // Store the values into final location
                    tempLast.StoreUnsafe(ref buf, firstOffset);
                    tempFirst.StoreUnsafe(ref buf, lastOffset);
                }
                buf = ref Unsafe.Add(ref buf, numIters * numElements);
                length -= numIters * numElements * 2;
            }
            else if (Sse2.IsSupported && (nuint)Vector128<int>.Count * 2 <= length)
            {
                nuint numElements = (nuint)Vector128<int>.Count;
                nuint numIters = (length / numElements) / 2;
                for (nuint i = 0; i < numIters; i++)
                {
                    nuint firstOffset = i * numElements;
                    nuint lastOffset = length - ((1 + i) * numElements);

                    // Load the values into vectors
                    Vector128<int> tempFirst = Vector128.LoadUnsafe(ref buf, firstOffset);
                    Vector128<int> tempLast = Vector128.LoadUnsafe(ref buf, lastOffset);

                    // Shuffle to reverse each vector:
                    //     +---------------+
                    //     | A | B | C | D |
                    //     +---------------+
                    //          --->
                    //     +---------------+
                    //     | D | C | B | A |
                    //     +---------------+
                    tempFirst = Sse2.Shuffle(tempFirst, 0b00_01_10_11);
                    tempLast = Sse2.Shuffle(tempLast, 0b00_01_10_11);

                    // Store the values into final location
                    tempLast.StoreUnsafe(ref buf, firstOffset);
                    tempFirst.StoreUnsafe(ref buf, lastOffset);
                }
                buf = ref Unsafe.Add(ref buf, numIters * numElements);
                length -= numIters * numElements * 2;
            }

            // Store any remaining values one-by-one
            for (nuint i = 0; i < (length / 2); i++)
            {
                ref int firstInt = ref Unsafe.Add(ref buf, i);
                ref int lastInt = ref Unsafe.Add(ref buf, length - 1 - i);
                (lastInt, firstInt) = (firstInt, lastInt);
            }
        }

        public static void Reverse(ref long buf, nuint length)
        {
            if (Avx2.IsSupported && (nuint)Vector256<long>.Count * 2 <= length)
            {
                nuint numElements = (nuint)Vector256<long>.Count;
                nuint numIters = (length / numElements) / 2;
                for (nuint i = 0; i < numIters; i++)
                {
                    nuint firstOffset = i * numElements;
                    nuint lastOffset = length - ((1 + i) * numElements);
                    // Load the values into vectors
                    Vector256<long> tempFirst = Vector256.LoadUnsafe(ref buf, firstOffset);
                    Vector256<long> tempLast = Vector256.LoadUnsafe(ref buf, lastOffset);

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

                    // Store the values into final location
                    tempLast.StoreUnsafe(ref buf, firstOffset);
                    tempFirst.StoreUnsafe(ref buf, lastOffset);
                }
                buf = ref Unsafe.Add(ref buf, numIters * numElements);
                length -= numIters * numElements * 2;
            }
            else if (Sse2.IsSupported && (nuint)Vector128<long>.Count * 2 <= length)
            {
                ref int bufInt = ref Unsafe.As<long, int>(ref buf);
                nuint intLength = length * (sizeof(long) / sizeof(int));
                nuint numElements = (nuint)Vector128<int>.Count;
                nuint numIters = (intLength / numElements) / 2;
                for (nuint i = 0; i < numIters; i++)
                {
                    nuint firstOffset = i * numElements;
                    nuint lastOffset = intLength - ((1 + i) * numElements);
                    // Load the values into vectors
                    Vector128<int> tempFirst = Vector128.LoadUnsafe(ref bufInt, firstOffset);
                    Vector128<int> tempLast = Vector128.LoadUnsafe(ref bufInt, lastOffset);

                    // Shuffle to reverse each vector:
                    //     +-------+
                    //     | A | B |
                    //     +-------+
                    //          --->
                    //     +-------+
                    //     | B | A |
                    //     +-------+
                    tempFirst = Sse2.Shuffle(tempFirst, 0b0100_1110);
                    tempLast = Sse2.Shuffle(tempLast, 0b0100_1110);

                    // Store the values into final location
                    tempLast.StoreUnsafe(ref bufInt, firstOffset);
                    tempFirst.StoreUnsafe(ref bufInt, lastOffset);
                }
                bufInt = ref Unsafe.Add(ref bufInt, numIters * numElements);
                buf = ref Unsafe.As<int, long>(ref bufInt);
                length -= numIters * (nuint)Vector128<long>.Count * 2;
            }

            // Store any remaining values one-by-one
            for (nuint i = 0; i < (length / 2); i++)
            {
                ref long firstLong = ref Unsafe.Add(ref buf, i);
                ref long lastLong = ref Unsafe.Add(ref buf, length - 1 - i);
                (lastLong, firstLong) = (firstLong, lastLong);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse<T>(ref T elements, nuint length)
        {
            Debug.Assert(length > 0);
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                {
                    Reverse(ref Unsafe.As<T, byte>(ref elements), length);
                    return;
                }
                else if (Unsafe.SizeOf<T>() == sizeof(char))
                {
                    Reverse(ref Unsafe.As<T, char>(ref elements), length);
                    return;
                }
                else if (Unsafe.SizeOf<T>() == sizeof(int))
                {
                    Reverse(ref Unsafe.As<T, int>(ref elements), length);
                    return;
                }
                else if (Unsafe.SizeOf<T>() == sizeof(long))
                {
                    Reverse(ref Unsafe.As<T, long>(ref elements), length);
                    return;
                }
            }
            ReverseInner(ref elements, length);
        }

        private static void ReverseInner<T>(ref T elements, nuint length)
        {
            Debug.Assert(length > 0);
            ref T first = ref elements;
            ref T last = ref Unsafe.Subtract(ref Unsafe.Add(ref first, (int)length), 1);
            do
            {
                T temp = first;
                first = last;
                last = temp;
                first = ref Unsafe.Add(ref first, 1);
                last = ref Unsafe.Subtract(ref last, 1);
            } while (Unsafe.IsAddressLessThan(ref first, ref last));
        }
    }
}
