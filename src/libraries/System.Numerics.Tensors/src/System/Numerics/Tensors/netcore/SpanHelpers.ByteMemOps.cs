// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_AMD64 || TARGET_ARM64 || (TARGET_32BIT && !TARGET_ARM) || TARGET_LOONGARCH64
// JIT is guaranteed to unroll blocks up to 64 bytes in size
#define HAS_CUSTOM_BLOCKS
#endif

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    internal static partial class SpanHelpers // .ByteMemOps
    {
        private const nuint ZeroMemoryNativeThreshold = 1024;


#if HAS_CUSTOM_BLOCKS
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct Block16 {}

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct Block64 {}
#endif // HAS_CUSTOM_BLOCKS

#if NATIVEAOT
        [System.Runtime.RuntimeExport("RhSpanHelpers_MemCopy")]
#endif

#if NATIVEAOT
        [System.Runtime.RuntimeExport("RhSpanHelpers_MemZero")]
#endif
        //[Intrinsic] // Unrolled for small sizes
        public static unsafe void ClearWithoutReferences(ref byte dest, nuint len)
        {
            if (len == 0)
                return;

            ref byte destEnd = ref Unsafe.Add(ref dest, len);

            if (len <= 16)
                goto MZER02;
            if (len > 64)
                goto MZER05;

        MZER00:
            // Clear bytes which are multiples of 16 and leave the remainder for MZER01 to handle.
            Debug.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block16>(ref dest, default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref dest, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref dest, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 12), 0);
#endif
            if (len <= 32)
                goto MZER01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref dest, 16), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 24), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 20), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 24), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 28), 0);
#endif
            if (len <= 48)
                goto MZER01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref dest, 32), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 32), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 40), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 32), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 36), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 40), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 44), 0);
#endif

        MZER01:
            // Unconditionally clear the last 16 bytes using destEnd and return.
            Debug.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref destEnd, -16), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref destEnd, -16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref destEnd, -8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -12), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -4), 0);
#endif
            return;

        MZER02:
            // Clear the first 8 bytes and then unconditionally clear the last 8 bytes and return.
            if ((len & 24) == 0)
                goto MZER03;
            Debug.Assert(len >= 8 && len <= 16);
#if TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref dest, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref destEnd, -8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref dest, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -4), 0);
#endif
            return;

        MZER03:
            // Clear the first 4 bytes and then unconditionally clear the last 4 bytes and return.
            if ((len & 4) == 0)
                goto MZER04;
            Debug.Assert(len >= 4 && len < 8);
            Unsafe.WriteUnaligned<int>(ref dest, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -4), 0);
            return;

        MZER04:
            // Clear the first byte. For pending bytes, do an unconditionally clear of the last 2 bytes and return.
            Debug.Assert(len < 4);
            if (len == 0)
                return;
            dest = 0;
            if ((len & 2) == 0)
                return;
            Unsafe.WriteUnaligned<short>(ref Unsafe.Add(ref destEnd, -2), 0);
            return;

        MZER05:
            // PInvoke to the native version when the clear length exceeds the threshold.
            if (len > ZeroMemoryNativeThreshold)
            {
                goto PInvoke;
            }

#if HAS_CUSTOM_BLOCKS
            if (len >= 256)
            {
                // Try to opportunistically align the destination below. The input isn't pinned, so the GC
                // is free to move the references. We're therefore assuming that reads may still be unaligned.
                nuint misalignedElements = 64 - Unsafe.OpportunisticMisalignment(ref dest, 64);
                Unsafe.WriteUnaligned<Block64>(ref dest, default);
                dest = ref Unsafe.Add(ref dest, misalignedElements);
                len -= misalignedElements;
            }
#endif
            // Clear 64-bytes at a time until the remainder is less than 64.
            // If remainder is greater than 16 bytes, then jump to MZER00. Otherwise, unconditionally clear the last 16 bytes and return.
            Debug.Assert(len > 64 && len <= ZeroMemoryNativeThreshold);
            nuint n = len >> 6;

        MZER06:
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block64>(ref dest, default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref dest, 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 8), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 24), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 32), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 40), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 48), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref dest, 56), 0);
#else
            Unsafe.WriteUnaligned<int>(ref dest, 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 4), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 12), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 20), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 24), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 28), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 32), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 36), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 40), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 44), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 48), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 52), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 56), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref dest, 60), 0);
#endif
            dest = ref Unsafe.Add(ref dest, 64);
            n--;
            if (n != 0)
                goto MZER06;

            len %= 64;
            if (len > 16)
                goto MZER00;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned<Block16>(ref Unsafe.Add(ref destEnd, -16), default);
#elif TARGET_64BIT
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref destEnd, -16), 0);
            Unsafe.WriteUnaligned<long>(ref Unsafe.Add(ref destEnd, -8), 0);
#else
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -16), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -12), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -8), 0);
            Unsafe.WriteUnaligned<int>(ref Unsafe.Add(ref destEnd, -4), 0);
#endif
            return;

        PInvoke:
            // Implicit nullchecks
            _ = Unsafe.ReadUnaligned<byte>(ref dest);

            // replacing Buffer._ZeroMemory(ref dest, len);
            fixed (byte* p = &dest)
            {
                NativeMemory.Clear(p, len);
            }
        }
    }
}
