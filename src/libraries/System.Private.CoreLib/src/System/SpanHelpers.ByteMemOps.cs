// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_AMD64 || TARGET_ARM64 || (TARGET_32BIT && !TARGET_ARM) || TARGET_LOONGARCH64
// JIT is guaranteed to unroll blocks up to 64 bytes in size
#define HAS_CUSTOM_BLOCKS
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System
{
    internal static partial class SpanHelpers // .ByteMemOps
    {
#if TARGET_ARM64 || TARGET_LOONGARCH64
        private const ulong MemmoveNativeThreshold = ulong.MaxValue;
#elif TARGET_ARM
        private const nuint MemmoveNativeThreshold = 512;
#else
        private const nuint MemmoveNativeThreshold = 2048;
#endif
        private const nuint ZeroMemoryNativeThreshold = 1024;

        // Copy size at which aligning the destination of an overlapping forward copy starts to pay for the
        // extra leading block. Below it the alignment prologue costs more than the misaligned stores it
        // avoids.
        private const nuint MemmoveOverlappedAlignThreshold = 2048;

        // The platform memmove's backward copy loop stays about twice as fast as a managed descending one:
        // a descending stream defeats the hardware prefetcher, and unlike the forward direction, aligning
        // the destination doesn't recover it. So a copy towards the end of the buffer is only done here
        // while it is short enough for the QCall itself to dominate.
        private const nuint MemmoveOverlappedBackwardThreshold = 256;

#if HAS_CUSTOM_BLOCKS
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        private struct Block16 {}

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct Block64 {}
#endif // HAS_CUSTOM_BLOCKS

        [Intrinsic] // Unrolled for small constant lengths
        internal static void Memmove(ref byte dest, ref byte src, nuint len)
        {
            // P/Invoke into the native version when the buffers are overlapping.
            if ((nuint)Unsafe.ByteOffset(ref src, ref dest) < len ||
                (nuint)Unsafe.ByteOffset(ref dest, ref src) < len)
            {
                goto BuffersOverlap;
            }

            ref byte srcEnd = ref Unsafe.Add(ref src, len);
            ref byte destEnd = ref Unsafe.Add(ref dest, len);

            if (len <= 16)
                goto MCPY02;
            if (len > 64)
                goto MCPY05;

        MCPY00:
            // Copy bytes which are multiples of 16 and leave the remainder for MCPY01 to handle.
            Debug.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<Block16>(ref src));
#elif TARGET_64BIT
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<long>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 8)));
#else
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<int>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 12)));
#endif
            if (len <= 32)
                goto MCPY01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 16), Unsafe.ReadUnaligned<Block16>(ref Unsafe.Add(ref src, 16)));
#elif TARGET_64BIT
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 24), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 24)));
#else
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 24), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 24)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 28), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 28)));
#endif
            if (len <= 48)
                goto MCPY01;
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 32), Unsafe.ReadUnaligned<Block16>(ref Unsafe.Add(ref src, 32)));
#elif TARGET_64BIT
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 32), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 32)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 40), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 40)));
#else
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 32), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 32)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 36), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 36)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 40), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 40)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 44), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 44)));
#endif

        MCPY01:
            // Unconditionally copy the last 16 bytes using destEnd and srcEnd and return.
            Debug.Assert(len > 16 && len <= 64);
#if HAS_CUSTOM_BLOCKS
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -16), Unsafe.ReadUnaligned<Block16>(ref Unsafe.Add(ref srcEnd, -16)));
#elif TARGET_64BIT
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref srcEnd, -16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref srcEnd, -8)));
#else
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -4)));
#endif
            return;

        MCPY02:
            // Copy the first 8 bytes and then unconditionally copy the last 8 bytes and return.
            if ((len & 24) == 0)
                goto MCPY03;
            Debug.Assert(len >= 8 && len <= 16);
#if TARGET_64BIT
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<long>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref srcEnd, -8)));
#else
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<int>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -4)));
#endif
            return;

        MCPY03:
            // Copy the first 4 bytes and then unconditionally copy the last 4 bytes and return.
            if ((len & 4) == 0)
                goto MCPY04;
            Debug.Assert(len >= 4 && len < 8);
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<int>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -4)));
            return;

        MCPY04:
            // Copy the first byte. For pending bytes, do an unconditionally copy of the last 2 bytes and return.
            Debug.Assert(len < 4);
            if (len == 0)
                return;
            dest = src;
            if ((len & 2) == 0)
                return;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -2), Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref srcEnd, -2)));
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
                nuint misalignedElements = 64 - Unsafe.OpportunisticMisalignment(ref dest, 64);
                Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<Block64>(ref src));
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
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<Block64>(ref src));
#elif TARGET_64BIT
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<long>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 24), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 24)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 32), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 32)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 40), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 40)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 48), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 48)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 56), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, 56)));
#else
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<int>(ref src));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 4)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 20), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 20)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 24), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 24)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 28), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 28)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 32), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 32)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 36), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 36)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 40), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 40)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 44), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 44)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 48), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 48)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 52), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 52)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 56), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 56)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 60), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref src, 60)));
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
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -16), Unsafe.ReadUnaligned<Block16>(ref Unsafe.Add(ref srcEnd, -16)));
#elif TARGET_64BIT
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -16), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref srcEnd, -16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -8), Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref srcEnd, -8)));
#else
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -16), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -12), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -12)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -8), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -8)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destEnd, -4), Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcEnd, -4)));
#endif
            return;

        BuffersOverlap:
            Debug.Assert(len > 0);
            // If the buffers overlap perfectly, there's no point to copying the data.
            if (Unsafe.AreSame(ref dest, ref src))
            {
                // Both could be null with a non-zero length, perform an implicit null check.
                _ = Unsafe.ReadUnaligned<byte>(ref dest);
                return;
            }

            if (Vector128.IsHardwareAccelerated)
            {
                // 'dest' below 'src' means the data is shifted towards the start of the buffer, which is by
                // far the most common overlapping shape (List<T>.RemoveAt/RemoveRange, Queue<T>, overlapping
                // Span<T>.CopyTo, ...). Those are always copied here: the platform memmove's forward loop is
                // tuned for disjoint buffers and both of the tricks it uses backfire on an in-place shift.
                // 'rep movsb' collapses when the two buffers are less than a cache line apart, and the
                // non-temporal stores it switches to for large copies evict the very lines the rest of the
                // copy is about to read back.
                if ((nuint)Unsafe.ByteOffset(ref dest, ref src) < len)
                {
                    CopyForwardVectorized(ref dest, ref src, len);
                    return;
                }

                // 'dest' above 'src'. The platform memmove's backward loop has neither problem and outruns a
                // managed descending loop, so this is only worth doing while the QCall dominates the copy.
                if (len <= MemmoveOverlappedBackwardThreshold)
                {
                    CopyBackwardVectorized(ref dest, ref src, len);
                    return;
                }
            }

        PInvoke:
            // Implicit nullchecks
            Debug.Assert(len > 0);
            _ = Unsafe.ReadUnaligned<byte>(ref dest);
            _ = Unsafe.ReadUnaligned<byte>(ref src);
            MemmoveNative(ref dest, ref src, len);
        }

        // Copies overlapping buffers where 'dest' is at a lower address than 'src'. The blocks run in
        // strictly ascending order, so a byte is always read before the copy can overwrite it. That also
        // rules out the "copy a final block anchored at the end of the buffer" trick the non-overlapping
        // paths use - by the time that block ran, the bytes it reads would already have been rewritten.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CopyForwardVectorized(ref byte dest, ref byte src, nuint len)
        {
            Debug.Assert(len > 0);
            Debug.Assert(Vector128.IsHardwareAccelerated);

            // Align the destination. An unaligned store costs more than an unaligned load, and an
            // overlapping copy can only ever have one of the two aligned.
            if (len >= MemmoveOverlappedAlignThreshold)
            {
                nuint head = 64 - Unsafe.OpportunisticMisalignment(ref dest, 64);
                if (head != 64)
                {
                    CopyBlocksForward(ref dest, ref src, head);
                    dest = ref Unsafe.Add(ref dest, head);
                    src = ref Unsafe.Add(ref src, head);
                    len -= head;
                }
            }

            // The blocks are addressed off 'dest'/'src' with constant offsets rather than off a running
            // index so that targets with load/store-pair instructions can fold them (arm64 'ldp'/'stp').
            if (Vector256.IsHardwareAccelerated)
            {
                while (len >= 128)
                {
                    CopyBlock128(ref dest, ref src);
                    dest = ref Unsafe.Add(ref dest, 128);
                    src = ref Unsafe.Add(ref src, 128);
                    len -= 128;
                }
            }
            else
            {
                while (len >= 64)
                {
                    CopyBlock64(ref dest, ref src);
                    dest = ref Unsafe.Add(ref dest, 64);
                    src = ref Unsafe.Add(ref src, 64);
                    len -= 64;
                }
            }

            CopyBlocksForward(ref dest, ref src, len);
        }

        // Copies overlapping buffers where 'dest' is at a higher address than 'src'. Mirror image of the
        // above: the blocks run in strictly descending order, walking down from the end of the buffer.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CopyBackwardVectorized(ref byte dest, ref byte src, nuint len)
        {
            Debug.Assert(len > 0);
            Debug.Assert(Vector128.IsHardwareAccelerated);

            if (Vector256.IsHardwareAccelerated)
            {
                while (len >= 128)
                {
                    len -= 128;
                    CopyBlock128(ref Unsafe.Add(ref dest, len), ref Unsafe.Add(ref src, len));
                }
            }
            else
            {
                while (len >= 64)
                {
                    len -= 64;
                    CopyBlock64(ref Unsafe.Add(ref dest, len), ref Unsafe.Add(ref src, len));
                }
            }

            CopyBlocksBackward(ref dest, ref src, len);
        }

        // Copies fewer than 128 bytes, largest block first, so that the blocks run in ascending order.
        private static void CopyBlocksForward(ref byte dest, ref byte src, nuint len)
        {
            Debug.Assert(len < 128);

            if ((len & 64) != 0)
            {
                CopyBlock64(ref dest, ref src);
                dest = ref Unsafe.Add(ref dest, 64);
                src = ref Unsafe.Add(ref src, 64);
            }

            if ((len & 32) != 0)
            {
                CopyBlock32(ref dest, ref src);
                dest = ref Unsafe.Add(ref dest, 32);
                src = ref Unsafe.Add(ref src, 32);
            }

            if ((len & 16) != 0)
            {
                CopyBlock16(ref dest, ref src);
                dest = ref Unsafe.Add(ref dest, 16);
                src = ref Unsafe.Add(ref src, 16);
            }

            if ((len & 8) != 0)
            {
                CopyBlock8(ref dest, ref src);
                dest = ref Unsafe.Add(ref dest, 8);
                src = ref Unsafe.Add(ref src, 8);
            }

            if ((len & 4) != 0)
            {
                Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<uint>(ref src));
                dest = ref Unsafe.Add(ref dest, 4);
                src = ref Unsafe.Add(ref src, 4);
            }

            if ((len & 2) != 0)
            {
                Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<ushort>(ref src));
                dest = ref Unsafe.Add(ref dest, 2);
                src = ref Unsafe.Add(ref src, 2);
            }

            if ((len & 1) != 0)
            {
                dest = src;
            }
        }

        // Copies fewer than 128 bytes, largest block first, so that the blocks run in descending order.
        private static void CopyBlocksBackward(ref byte dest, ref byte src, nuint len)
        {
            Debug.Assert(len < 128);

            if ((len & 64) != 0)
            {
                len -= 64;
                CopyBlock64(ref Unsafe.Add(ref dest, len), ref Unsafe.Add(ref src, len));
            }

            if ((len & 32) != 0)
            {
                len -= 32;
                CopyBlock32(ref Unsafe.Add(ref dest, len), ref Unsafe.Add(ref src, len));
            }

            if ((len & 16) != 0)
            {
                len -= 16;
                CopyBlock16(ref Unsafe.Add(ref dest, len), ref Unsafe.Add(ref src, len));
            }

            if ((len & 8) != 0)
            {
                len -= 8;
                CopyBlock8(ref Unsafe.Add(ref dest, len), ref Unsafe.Add(ref src, len));
            }

            if ((len & 4) != 0)
            {
                len -= 4;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, len), Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, len)));
            }

            if ((len & 2) != 0)
            {
                len -= 2;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, len), Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref src, len)));
            }

            if ((len & 1) != 0)
            {
                Debug.Assert(len == 1);
                dest = src;
            }
        }

        // Every block below is fully loaded before any of it is stored, so it can be copied in either
        // direction without one of its own stores clobbering source bytes it still has to read.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyBlock128(ref byte dest, ref byte src)
        {
            Debug.Assert(Vector256.IsHardwareAccelerated);

            Vector256<byte> block0 = Vector256.LoadUnsafe(ref src);
            Vector256<byte> block1 = Vector256.LoadUnsafe(ref src, 32);
            Vector256<byte> block2 = Vector256.LoadUnsafe(ref src, 64);
            Vector256<byte> block3 = Vector256.LoadUnsafe(ref src, 96);
            Vector256.StoreUnsafe(block0, ref dest);
            Vector256.StoreUnsafe(block1, ref dest, 32);
            Vector256.StoreUnsafe(block2, ref dest, 64);
            Vector256.StoreUnsafe(block3, ref dest, 96);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyBlock64(ref byte dest, ref byte src)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<byte> block0 = Vector256.LoadUnsafe(ref src);
                Vector256<byte> block1 = Vector256.LoadUnsafe(ref src, 32);
                Vector256.StoreUnsafe(block0, ref dest);
                Vector256.StoreUnsafe(block1, ref dest, 32);
            }
            else
            {
                Vector128<byte> block0 = Vector128.LoadUnsafe(ref src);
                Vector128<byte> block1 = Vector128.LoadUnsafe(ref src, 16);
                Vector128<byte> block2 = Vector128.LoadUnsafe(ref src, 32);
                Vector128<byte> block3 = Vector128.LoadUnsafe(ref src, 48);
                Vector128.StoreUnsafe(block0, ref dest);
                Vector128.StoreUnsafe(block1, ref dest, 16);
                Vector128.StoreUnsafe(block2, ref dest, 32);
                Vector128.StoreUnsafe(block3, ref dest, 48);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyBlock32(ref byte dest, ref byte src)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                Vector256.StoreUnsafe(Vector256.LoadUnsafe(ref src), ref dest);
            }
            else
            {
                Vector128<byte> block0 = Vector128.LoadUnsafe(ref src);
                Vector128<byte> block1 = Vector128.LoadUnsafe(ref src, 16);
                Vector128.StoreUnsafe(block0, ref dest);
                Vector128.StoreUnsafe(block1, ref dest, 16);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyBlock16(ref byte dest, ref byte src) =>
            Vector128.StoreUnsafe(Vector128.LoadUnsafe(ref src), ref dest);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyBlock8(ref byte dest, ref byte src)
        {
#if TARGET_64BIT
            Unsafe.WriteUnaligned(ref dest, Unsafe.ReadUnaligned<ulong>(ref src));
#else
            uint block0 = Unsafe.ReadUnaligned<uint>(ref src);
            uint block1 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, 4));
            Unsafe.WriteUnaligned(ref dest, block0);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, 4), block1);
#endif
        }

        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void MemmoveNative(ref byte dest, ref byte src, nuint len)
        {
            fixed (byte* pDest = &dest)
            fixed (byte* pSrc = &src)
            {
                memmove(pDest, pSrc, len);
            }
        }

#if MONO
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void memmove(void* dest, void* src, nuint len);
#else
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "memmove")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe partial void* memmove(void* dest, void* src, nuint len);
#pragma warning restore CS3016
#endif

        [Intrinsic] // Unrolled for small sizes
        public static void ClearWithoutReferences(ref byte dest, nuint len)
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
            ZeroMemoryNative(ref dest, len);
        }

        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ZeroMemoryNative(ref byte b, nuint byteLength)
        {
            fixed (byte* ptr = &b)
            {
                byte* adjustedPtr = ptr;
#if TARGET_X86 || TARGET_AMD64
                if (byteLength > 0x100)
                {
                    // memset ends up calling rep stosb if the hardware claims to support it efficiently. rep stosb is up to 2x slower
                    // on misaligned blocks. Workaround this issue by aligning the blocks passed to memset upfront.
                    Unsafe.WriteUnaligned<Block16>(ptr, default);
                    Unsafe.WriteUnaligned<Block16>(ptr + byteLength - 16, default);

                    byte* alignedEnd = (byte*)((nuint)(ptr + byteLength - 1) & ~(nuint)(16 - 1));

                    adjustedPtr = (byte*)(((nuint)ptr + 16) & ~(nuint)(16 - 1));
                    byteLength = (nuint)(alignedEnd - adjustedPtr);
                }
#endif
                memset(adjustedPtr, 0, byteLength);
            }
        }

#if MONO
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void memset(void* dest, int value, nuint len);
#else
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "memset")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe partial void* memset(void* dest, int value, nuint len);
#pragma warning restore CS3016
#endif

        internal static void Fill(ref byte dest, byte value, nuint len)
        {
            if (!Vector.IsHardwareAccelerated)
            {
                goto CannotVectorize;
            }

            if (len >= (nuint)Vector<byte>.Count)
            {
                // We have enough data for at least one vectorized write.
                Vector<byte> vector = new(value);
                nuint stopLoopAtOffset = len & (nuint)(nint)(2 * (int)-Vector<byte>.Count); // intentional sign extension carries the negative bit
                nuint offset = 0;

                // Loop, writing 2 vectors at a time.
                // Compare 'numElements' rather than 'stopLoopAtOffset' because we don't want a dependency
                // on the very recently calculated 'stopLoopAtOffset' value.
                if (len >= (uint)(2 * Vector<byte>.Count))
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
                if ((len & (nuint)Vector<byte>.Count) != 0)
                {
                    Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest, offset), vector);
                }

                // It's possible that some small buffer remains to be populated - something that won't
                // fit an entire vector's worth of data. Instead of falling back to a loop, we'll write
                // a vector at the very end of the buffer. This may involve overwriting previously
                // populated data, which is fine since we're splatting the same value for all entries.
                // There's no need to perform a length check here because we already performed this
                // check before entering the vectorized code path.
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref dest, len - (nuint)Vector<byte>.Count), vector);

                // And we're done!
                return;
            }

        CannotVectorize:

            // If we reached this point, we cannot vectorize this T, or there are too few
            // elements for us to vectorize. Fall back to an unrolled loop.
            nuint i = 0;

            // Write 8 elements at a time
            if (len >= 8)
            {
                nuint stopLoopAtOffset = len & ~(nuint)7;
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
            if ((len & 4) != 0)
            {
                Unsafe.Add(ref dest, (nint)i + 0) = value;
                Unsafe.Add(ref dest, (nint)i + 1) = value;
                Unsafe.Add(ref dest, (nint)i + 2) = value;
                Unsafe.Add(ref dest, (nint)i + 3) = value;
                i += 4;
            }

            // Write next 2 elements if needed
            if ((len & 2) != 0)
            {
                Unsafe.Add(ref dest, (nint)i + 0) = value;
                Unsafe.Add(ref dest, (nint)i + 1) = value;
                i += 2;
            }

            // Write final element if needed
            if ((len & 1) != 0)
            {
                Unsafe.Add(ref dest, (nint)i) = value;
            }
        }
    }
}
