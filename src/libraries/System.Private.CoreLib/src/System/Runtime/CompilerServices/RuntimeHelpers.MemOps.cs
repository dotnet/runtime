// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_AMD64 || TARGET_ARM64 || (TARGET_32BIT && !TARGET_ARM) || TARGET_LOONGARCH64
#define HAS_CUSTOM_BLOCKS
#endif

using System.Diagnostics;
using System.Numerics;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
#if NATIVEAOT
        [System.Runtime.RuntimeExport("RhRuntimeHelpers_MemSet")]
#endif
        private static unsafe void MemSet(ref byte dest, byte value, nuint numElements)
        {
            if (numElements == 0)
            {
                return;
            }

            // Implicit nullcheck
            _ = Unsafe.ReadUnaligned<byte>(ref dest);

            // Early checks to see if it's even possible to vectorize - JIT will turn these checks into consts.
            // - T cannot contain references (GC can't track references in vectors)
            // - Vectorization must be hardware-accelerated
            // - T's size must not exceed the vector's size
            // - T's size must be a whole power of 2

            if (!Vector.IsHardwareAccelerated) { goto CannotVectorize; }

            if (numElements >= (uint)(Vector<byte>.Count))
            {
                // We have enough data for at least one vectorized write.

                Vector<byte> vector = new(value);

                nuint stopLoopAtOffset = numElements & (nuint)(nint)(2 * (int)-Vector<byte>.Count); // intentional sign extension carries the negative bit
                nuint offset = 0;

                // Loop, writing 2 vectors at a time.
                // Compare 'numElements' rather than 'stopLoopAtOffset' because we don't want a dependency
                // on the very recently calculated 'stopLoopAtOffset' value.

                if (numElements >= (uint)(2 * Vector<byte>.Count))
                {
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest, offset), vector);
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest, offset + (nuint)Vector<byte>.Count), vector);
                        offset += (uint)(2 * Vector<byte>.Count);
                    } while (offset < stopLoopAtOffset);
                }

                // At this point, if any data remains to be written, it's strictly less than
                // 2 * sizeof(Vector) bytes. The loop above had us write an even number of vectors.
                // If the total byte length instead involves us writing an odd number of vectors, write
                // one additional vector now. The bit check below tells us if we're in an "odd vector
                // count" situation.

                if ((numElements & (nuint)Vector<byte>.Count) != 0)
                {
                    Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest, offset), vector);
                }

                // It's possible that some small buffer remains to be populated - something that won't
                // fit an entire vector's worth of data. Instead of falling back to a loop, we'll write
                // a vector at the very end of the buffer. This may involve overwriting previously
                // populated data, which is fine since we're splatting the same value for all entries.
                // There's no need to perform a length check here because we already performed this
                // check before entering the vectorized code path.

                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest, numElements - (nuint)Vector<byte>.Count), vector);

                // And we're done!
                return;
            }

        CannotVectorize:
            // If we reached this point, we cannot vectorize this T, or there are too few
            // elements for us to vectorize. Fall back to an unrolled loop.

            nuint i = 0;

            // Write 8 elements at a time
            if (numElements >= 8)
            {
                nuint stopLoopAtOffset = numElements & ~(nuint)7;
                do
                {
                    Unsafe.Add(ref dest, (nint)i + 0) = value;
                    Unsafe.Add(ref dest, (nint)i + 1) = value;
                    Unsafe.Add(ref dest, (nint)i + 2) = value;
                    Unsafe.Add(ref dest, (nint)i + 3) = value;
                    Unsafe.Add(ref dest, (nint)i + 4) = value;
                    Unsafe.Add(ref dest, (nint)i + 5) = value;
                    Unsafe.Add(ref dest, (nint)i + 6) = value;
                    Unsafe.Add(ref dest, (nint)i + 7) = value;
                } while ((i += 8) < stopLoopAtOffset);
            }

            // Write next 4 elements if needed
            if ((numElements & 4) != 0)
            {
                Unsafe.Add(ref dest, (nint)i + 0) = value;
                Unsafe.Add(ref dest, (nint)i + 1) = value;
                Unsafe.Add(ref dest, (nint)i + 2) = value;
                Unsafe.Add(ref dest, (nint)i + 3) = value;
                i += 4;
            }

            // Write next 2 elements if needed
            if ((numElements & 2) != 0)
            {
                Unsafe.Add(ref dest, (nint)i + 0) = value;
                Unsafe.Add(ref dest, (nint)i + 1) = value;
                i += 2;
            }

            // Write final element if needed
            if ((numElements & 1) != 0)
            {
                Unsafe.Add(ref dest, (nint)i) = value;
            }
        }

#if NATIVEAOT
        [System.Runtime.RuntimeExport("RhRuntimeHelpers_MemZero")]
#endif
        private static unsafe void MemZero(ref byte b, nuint byteLength)
        {
            if (byteLength == 0)
                return;

            // Implicit nullcheck
            _ = Unsafe.ReadUnaligned<byte>(ref b);

            ref byte bEnd = ref Unsafe.Add(ref b, byteLength);

            if (byteLength <= 16)
                goto MZER02;
            if (byteLength > 64)
                goto MZER05;

        MZER00:
            // Clear bytes which are multiples of 16 and leave the remainder for MZER01 to handle.
            Debug.Assert(byteLength > 16 && byteLength <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Buffer.Block16>(ref b, default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref b, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref b, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 12), 0);
#endif
            if (byteLength <= 32)
                goto MZER01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Buffer.Block16>(ref Unsafe.Add(ref b, 16), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 24), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 20), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 24), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 28), 0);
#endif
            if (byteLength <= 48)
                goto MZER01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Buffer.Block16>(ref Unsafe.Add(ref b, 32), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 32), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref b, 40), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 32), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 36), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 40), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref b, 44), 0);
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

#if NATIVEAOT
        [System.Runtime.RuntimeExport("RhRuntimeHelpers_MemCopy")]
#endif
        private static unsafe void MemCopy(ref byte dest, ref byte src, nuint len)
        {
            if (len == 0)
            {
                return;
            }

            // Implicit nullchecks
            _ = Unsafe.ReadUnaligned<byte>(ref dest);
            _ = Unsafe.ReadUnaligned<byte>(ref src);

            // P/Invoke into the native version when the buffers are overlapping.
            if (((nuint)(nint)Unsafe.ByteOffset(ref src, ref dest) < len) || ((nuint)(nint)Unsafe.ByteOffset(ref dest, ref src) < len))
            {
                goto BuffersOverlap;
            }

            // Use "(IntPtr)(nint)len" to avoid overflow checking on the explicit cast to IntPtr

            ref byte srcEnd = ref Unsafe.Add(ref src, (IntPtr)(nint)len);
            ref byte destEnd = ref Unsafe.Add(ref dest, (IntPtr)(nint)len);

            if (len <= 16)
                goto MCPY02;
            if (len > 64)
                goto MCPY05;

            MCPY00:
            // Copy bytes which are multiples of 16 and leave the remainder for MCPY01 to handle.
            Debug.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Buffer.Block16>(ref dest) = Unsafe.As<byte, Buffer.Block16>(ref src); // [0,16]
#elif TARGET_64BIT
            Unsafe.As<byte, long>(ref dest) = Unsafe.As<byte, long>(ref src);
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 8)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 8)); // [0,16]
#else
            Unsafe.As<byte, int>(ref dest) = Unsafe.As<byte, int>(ref src);
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 4));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 8)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 8));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 12)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 12)); // [0,16]
#endif
            if (len <= 32)
                goto MCPY01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref src, 16)); // [0,32]
#elif TARGET_64BIT
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 16));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 24)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 24)); // [0,32]
#else
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 16));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 20)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 20));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 24)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 24));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 28)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 28)); // [0,32]
#endif
            if (len <= 48)
                goto MCPY01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref src, 32)); // [0,48]
#elif TARGET_64BIT
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 32));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 40)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 40)); // [0,48]
#else
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 32));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 36)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 36));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 40)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 40));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 44)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 44)); // [0,48]
#endif

        MCPY01:
            // Unconditionally copy the last 16 bytes using destEnd and srcEnd and return.
            Debug.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref srcEnd, -16));
#elif TARGET_64BIT
            Unsafe.As<byte, long>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref srcEnd, -16));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref destEnd, -8)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref srcEnd, -8));
#else
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -16));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -12)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -12));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -8)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -8));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -4));
#endif
            return;

        MCPY02:
            // Copy the first 8 bytes and then unconditionally copy the last 8 bytes and return.
            if ((len & 24) == 0)
                goto MCPY03;
            Debug.Assert(len >= 8 && len <= 16);
#if TARGET_64BIT
            Unsafe.As<byte, long>(ref dest) = Unsafe.As<byte, long>(ref src);
            Unsafe.As<byte, long>(ref Unsafe.Add(ref destEnd, -8)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref srcEnd, -8));
#else
            Unsafe.As<byte, int>(ref dest) = Unsafe.As<byte, int>(ref src);
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 4));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -8)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -8));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -4));
#endif
            return;

        MCPY03:
            // Copy the first 4 bytes and then unconditionally copy the last 4 bytes and return.
            if ((len & 4) == 0)
                goto MCPY04;
            Debug.Assert(len >= 4 && len < 8);
            Unsafe.As<byte, int>(ref dest) = Unsafe.As<byte, int>(ref src);
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -4));
            return;

        MCPY04:
            // Copy the first byte. For pending bytes, do an unconditionally copy of the last 2 bytes and return.
            Debug.Assert(len < 4);
            if (len == 0)
                return;
            dest = src;
            if ((len & 2) == 0)
                return;
            Unsafe.As<byte, short>(ref Unsafe.Add(ref destEnd, -2)) = Unsafe.As<byte, short>(ref Unsafe.Add(ref srcEnd, -2));
            return;

        MCPY05:
            // PInvoke to the native version when the copy length exceeds the threshold.
            if (len > Buffer.MemmoveNativeThreshold)
            {
                goto PInvoke;
            }

#if HAS_CUSTOM_BLOCKS
            if (len >= 256)
            {
                // Try to opportunistically align the destination below. The input isn't pinned, so the GC
                // is free to move the references. We're therefore assuming that reads may still be unaligned.
                //
                // dest is more important to align than src because an unaligned store is more expensive
                // than an unaligned load.
                nuint misalignedElements = 64 - (nuint)Unsafe.AsPointer(ref dest) & 63;
                Unsafe.As<byte, Buffer.Block64>(ref dest) = Unsafe.As<byte, Buffer.Block64>(ref src);
                src = ref Unsafe.Add(ref src, misalignedElements);
                dest = ref Unsafe.Add(ref dest, misalignedElements);
                len -= misalignedElements;
            }
#endif

            // Copy 64-bytes at a time until the remainder is less than 64.
            // If remainder is greater than 16 bytes, then jump to MCPY00. Otherwise, unconditionally copy the last 16 bytes and return.
            Debug.Assert(len > 64 && len <= Buffer.MemmoveNativeThreshold);
            nuint n = len >> 6;

        MCPY06:
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Buffer.Block64>(ref dest) = Unsafe.As<byte, Buffer.Block64>(ref src);
#elif TARGET_64BIT
            Unsafe.As<byte, long>(ref dest) = Unsafe.As<byte, long>(ref src);
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 8)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 8));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 16));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 24)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 24));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 32));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 40)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 40));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 48)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 48));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref dest, 56)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref src, 56));
#else
            Unsafe.As<byte, int>(ref dest) = Unsafe.As<byte, int>(ref src);
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 4));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 8)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 8));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 12)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 12));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 16));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 20)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 20));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 24)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 24));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 28)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 28));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 32));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 36)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 36));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 40)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 40));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 44)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 44));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 48)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 48));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 52)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 52));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 56)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 56));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref dest, 60)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref src, 60));
#endif
            dest = ref Unsafe.Add(ref dest, 64);
            src = ref Unsafe.Add(ref src, 64);
            n--;
            if (n != 0)
                goto MCPY06;

            len %= 64;
            if (len > 16)
                goto MCPY00;
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, Buffer.Block16>(ref Unsafe.Add(ref srcEnd, -16));
#elif TARGET_64BIT
            Unsafe.As<byte, long>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref srcEnd, -16));
            Unsafe.As<byte, long>(ref Unsafe.Add(ref destEnd, -8)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref srcEnd, -8));
#else
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -16));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -12)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -12));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -8)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -8));
            Unsafe.As<byte, int>(ref Unsafe.Add(ref destEnd, -4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref srcEnd, -4));
#endif
            return;

        BuffersOverlap:
            // If the buffers overlap perfectly, there's no point to copying the data.
            if (Unsafe.AreSame(ref dest, ref src))
            {
                return;
            }

        PInvoke:
            Buffer._Memmove(ref dest, ref src, len);
        }
    }
}
