// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_AMD64 || TARGET_ARM64 || (TARGET_32BIT && !TARGET_ARM) || TARGET_LOONGARCH64
// JIT is guaranteed to unroll blocks up to 64 bytes in size
#define HAS_CUSTOM_BLOCKS
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static partial class SpanHelpers // .ByteMemOps
    {
#if TARGET_ARM64 || TARGET_LOONGARCH64
        // TODO: Determine optimal value
        // https://github.com/dotnet/runtime/issues/8897 (Linux)
        // https://github.com/dotnet/runtime/issues/8896 (Windows)
        private static nuint MemmoveNativeThreshold => nuint.MaxValue;
#elif TARGET_ARM
        private const nuint MemmoveNativeThreshold = 512;
#else
        private const nuint MemmoveNativeThreshold = 2048;
#endif
        // TODO: Determine optimal value
        private const nuint ZeroMemoryNativeThreshold = 1024;

#if HAS_CUSTOM_BLOCKS
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct Block16 {}

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct Block64 {}
#endif // HAS_CUSTOM_BLOCKS

        [Intrinsic] // Unrolled for small constant lengths
        internal static unsafe void Memmove(ref byte dest, ref byte src, nuint len)
        {
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
            Unsafe.As<byte, Block16>(ref dest) = Unsafe.As<byte, Block16>(ref src); // [0,16]
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
            Unsafe.As<byte, Block16>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref src, 16)); // [0,32]
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
            Unsafe.As<byte, Block16>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref src, 32)); // [0,48]
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
            Unsafe.As<byte, Block16>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref srcEnd, -16));
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
            if (len > MemmoveNativeThreshold)
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
                Unsafe.As<byte, Block64>(ref dest) = Unsafe.As<byte, Block64>(ref src);
                src = ref Unsafe.Add(ref src, misalignedElements);
                dest = ref Unsafe.Add(ref dest, misalignedElements);
                len -= misalignedElements;
            }
#endif

            // Copy 64-bytes at a time until the remainder is less than 64.
            // If remainder is greater than 16 bytes, then jump to MCPY00. Otherwise, unconditionally copy the last 16 bytes and return.
            Debug.Assert(len > 64 && len <= MemmoveNativeThreshold);
            nuint n = len >> 6;

        MCPY06:
#if HAS_CUSTOM_BLOCKS
            Unsafe.As<byte, Block64>(ref dest) = Unsafe.As<byte, Block64>(ref src);
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
            Unsafe.As<byte, Block16>(ref Unsafe.Add(ref destEnd, -16)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref srcEnd, -16));
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

        [Intrinsic] // Unrolled for small sizes
        public static unsafe void ClearWithoutReferences(ref byte b, nuint byteLength)
        {
            if (byteLength == 0)
                return;

            ref byte bEnd = ref Unsafe.Add(ref b, byteLength);

            if (byteLength <= 16)
                goto MZER02;
            if (byteLength > 64)
                goto MZER05;

        MZER00:
            // Clear bytes which are multiples of 16 and leave the remainder for MZER01 to handle.
            Debug.Assert(byteLength > 16 && byteLength <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block16>(ref b, default);
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
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref b, 16), default);
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
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref b, 32), default);
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
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref bEnd, -16), default);
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
            if (byteLength > ZeroMemoryNativeThreshold)
            {
                goto PInvoke;
            }

#if HAS_CUSTOM_BLOCKS
            if (byteLength >= 256)
            {
                // Try to opportunistically align the destination below. The input isn't pinned, so the GC
                // is free to move the references. We're therefore assuming that reads may still be unaligned.
                nuint misalignedElements = 64 - (nuint)Unsafe.AsPointer(ref b) & 63;
                Unsafe.WriteUnaligned<Block64>(ref b, default);
                b = ref Unsafe.Add(ref b, misalignedElements);
                byteLength -= misalignedElements;
            }
#endif
            // Clear 64-bytes at a time until the remainder is less than 64.
            // If remainder is greater than 16 bytes, then jump to MZER00. Otherwise, unconditionally clear the last 16 bytes and return.
            Debug.Assert(byteLength > 64 && byteLength <= ZeroMemoryNativeThreshold);
            nuint n = byteLength >> 6;

        MZER06:
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block64>(ref b, default);
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
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref bEnd, -16), default);
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
    }
}
