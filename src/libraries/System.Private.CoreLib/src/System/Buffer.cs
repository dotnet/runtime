// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_AMD64 || TARGET_ARM64 || (TARGET_32BIT && !TARGET_ARM) || TARGET_LOONGARCH64
#define HAS_CUSTOM_BLOCKS
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Buffer
    {
        // Copies from one primitive array to another primitive array without
        // respecting types.  This calls memmove internally.  The count and
        // offset parameters here are in bytes.  If you want to use traditional
        // array element indices and counts, use Array.Copy.
        public static unsafe void BlockCopy(Array src, int srcOffset, Array dst, int dstOffset, int count)
        {
            ArgumentNullException.ThrowIfNull(src);
            ArgumentNullException.ThrowIfNull(dst);

            nuint uSrcLen = src.NativeLength;
            if (src.GetType() != typeof(byte[]))
            {
                if (!src.GetCorElementTypeOfElementType().IsPrimitiveType())
                    throw new ArgumentException(SR.Arg_MustBePrimArray, nameof(src));
                uSrcLen *= (nuint)src.GetElementSize();
            }

            nuint uDstLen = uSrcLen;
            if (src != dst)
            {
                uDstLen = dst.NativeLength;
                if (dst.GetType() != typeof(byte[]))
                {
                    if (!dst.GetCorElementTypeOfElementType().IsPrimitiveType())
                        throw new ArgumentException(SR.Arg_MustBePrimArray, nameof(dst));
                    uDstLen *= (nuint)dst.GetElementSize();
                }
            }

            ArgumentOutOfRangeException.ThrowIfNegative(srcOffset);
            ArgumentOutOfRangeException.ThrowIfNegative(dstOffset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            nuint uCount = (nuint)count;
            nuint uSrcOffset = (nuint)srcOffset;
            nuint uDstOffset = (nuint)dstOffset;

            if ((uSrcLen < uSrcOffset + uCount) || (uDstLen < uDstOffset + uCount))
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            Memmove(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(dst), uDstOffset), ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(src), uSrcOffset), uCount);
        }

        public static int ByteLength(Array array)
        {
            ArgumentNullException.ThrowIfNull(array);

            // Is it of primitive types?
            if (!array.GetCorElementTypeOfElementType().IsPrimitiveType())
                throw new ArgumentException(SR.Arg_MustBePrimArray, nameof(array));

            nuint byteLength = array.NativeLength * (nuint)array.GetElementSize();

            // This API is explosed both as Buffer.ByteLength and also used indirectly in argument
            // checks for Buffer.GetByte/SetByte.
            //
            // If somebody called Get/SetByte on 2GB+ arrays, there is a decent chance that
            // the computation of the index has overflowed. Thus we intentionally always
            // throw on 2GB+ arrays in Get/SetByte argument checks (even for indices <2GB)
            // to prevent people from running into a trap silently.

            return checked((int)byteLength);
        }

        public static byte GetByte(Array array, int index)
        {
            // array argument validation done via ByteLength
            if ((uint)index >= (uint)ByteLength(array))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
        }

        public static void SetByte(Array array, int index, byte value)
        {
            // array argument validation done via ByteLength
            if ((uint)index >= (uint)ByteLength(array))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index) = value;
        }

        // The attributes on this method are chosen for best JIT performance.
        // Please do not edit unless intentional.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
            }

            Memmove(ref *(byte*)destination, ref *(byte*)source, checked((nuint)sourceBytesToCopy));
        }

        // The attributes on this method are chosen for best JIT performance.
        // Please do not edit unless intentional.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
            }

            Memmove(ref *(byte*)destination, ref *(byte*)source, checked((nuint)sourceBytesToCopy));
        }

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
            _Memmove(ref dest, ref src, len);
        }

        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe void _Memmove(ref byte dest, ref byte src, nuint len)
        {
            fixed (byte* pDest = &dest)
            fixed (byte* pSrc = &src)
                __Memmove(pDest, pSrc, len);
        }

#if HAS_CUSTOM_BLOCKS
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        internal struct Block16 { }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        internal struct Block64 { }
#endif // HAS_CUSTOM_BLOCKS

        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe void _ZeroMemory(ref byte b, nuint byteLength)
        {
            fixed (byte* bytePointer = &b)
            {
                __ZeroMemory(bytePointer, byteLength);
            }
        }

#if !MONO // Mono BulkMoveWithWriteBarrier is in terms of elements (not bytes) and takes a type handle.

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Memmove<T>(ref T destination, ref T source, nuint elementCount)
        {
#pragma warning disable 8500 // sizeof of managed types
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                // Blittable memmove
                Memmove(
                    ref Unsafe.As<T, byte>(ref destination),
                    ref Unsafe.As<T, byte>(ref source),
                    elementCount * (nuint)sizeof(T));
            }
            else
            {
                // Non-blittable memmove
                BulkMoveWithWriteBarrier(
                    ref Unsafe.As<T, byte>(ref destination),
                    ref Unsafe.As<T, byte>(ref source),
                    elementCount * (nuint)sizeof(T));
            }
#pragma warning restore 8500
        }

        // The maximum block size to for __BulkMoveWithWriteBarrier FCall. This is required to avoid GC starvation.
#if DEBUG // Stress the mechanism in debug builds
        private const uint BulkMoveWithWriteBarrierChunk = 0x400;
#else
        private const uint BulkMoveWithWriteBarrierChunk = 0x4000;
#endif

        internal static void BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount)
        {
            if (byteCount <= BulkMoveWithWriteBarrierChunk)
                __BulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
            else
                _BulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
        }

#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228
        // Non-inlinable wrapper around the loop for copying large blocks in chunks
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void _BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount)
        {
            Debug.Assert(byteCount > BulkMoveWithWriteBarrierChunk);

            if (Unsafe.AreSame(ref source, ref destination))
                return;

            // This is equivalent to: (destination - source) >= byteCount || (destination - source) < 0
            if ((nuint)(nint)Unsafe.ByteOffset(ref source, ref destination) >= byteCount)
            {
                // Copy forwards
                do
                {
                    byteCount -= BulkMoveWithWriteBarrierChunk;
                    __BulkMoveWithWriteBarrier(ref destination, ref source, BulkMoveWithWriteBarrierChunk);
                    destination = ref Unsafe.AddByteOffset(ref destination, BulkMoveWithWriteBarrierChunk);
                    source = ref Unsafe.AddByteOffset(ref source, BulkMoveWithWriteBarrierChunk);
                }
                while (byteCount > BulkMoveWithWriteBarrierChunk);
            }
            else
            {
                // Copy backwards
                do
                {
                    byteCount -= BulkMoveWithWriteBarrierChunk;
                    __BulkMoveWithWriteBarrier(ref Unsafe.AddByteOffset(ref destination, byteCount), ref Unsafe.AddByteOffset(ref source, byteCount), BulkMoveWithWriteBarrierChunk);
                }
                while (byteCount > BulkMoveWithWriteBarrierChunk);
            }
            __BulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
        }
#pragma warning restore IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228

#endif // !MONO
    }

    internal static partial class SpanHelpers
    {
        [Intrinsic] // Unrolled for small sizes
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
                    Unsafe.Add(ref b, 2) = 0;
                    return;
                case 4:
                    Unsafe.As<byte, int>(ref b) = 0;
                    return;
                case 5:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.Add(ref b, 4) = 0;
                    return;
                case 6:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 4)) = 0;
                    return;
                case 7:
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.Add(ref b, 6) = 0;
                    return;
                case 8:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    return;
                case 9:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.Add(ref b, 8) = 0;
                    return;
                case 10:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 8)) = 0;
                    return;
                case 11:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.Add(ref b, 10) = 0;
                    return;
                case 12:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    return;
                case 13:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.Add(ref b, 12) = 0;
                    return;
                case 14:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 12)) = 0;
                    return;
                case 15:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 12)) = 0;
                    Unsafe.Add(ref b, 14) = 0;
                    return;
                case 16:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    return;
                case 17:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    Unsafe.Add(ref b, 16) = 0;
                    return;
                case 18:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 16)) = 0;
                    return;
                case 19:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 16)) = 0;
                    Unsafe.Add(ref b, 18) = 0;
                    return;
                case 20:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 16)) = 0;
                    return;
                case 21:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 16)) = 0;
                    Unsafe.Add(ref b, 20) = 0;
                    return;
                case 22:
#if TARGET_64BIT
                    Unsafe.As<byte, long>(ref b) = 0;
                    Unsafe.As<byte, long>(ref Unsafe.Add<byte>(ref b, 8)) = 0;
#else
                    Unsafe.As<byte, int>(ref b) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 4)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 8)) = 0;
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 12)) = 0;
#endif
                    Unsafe.As<byte, int>(ref Unsafe.Add(ref b, 16)) = 0;
                    Unsafe.As<byte, short>(ref Unsafe.Add(ref b, 20)) = 0;
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
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset(ref b, i)) = 0;
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
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i)) = 0;
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
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i + 4)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i + 8)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i + 12)) = 0;
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
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i)) = 0;
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i + 4)) = 0;
#endif
                i += 8;
            }
            if ((byteLength & 4) != 0)
            {
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref b, i)) = 0;
                i += 4;
            }
            if ((byteLength & 2) != 0)
            {
                Unsafe.As<byte, short>(ref Unsafe.AddByteOffset(ref b, i)) = 0;
                i += 2;
            }
            if ((byteLength & 1) != 0)
            {
                Unsafe.AddByteOffset(ref b, i) = 0;
                // We're not using i after this, so not needed
                // i += 1;
            }

            return;
#endif

        PInvoke:
            Buffer._ZeroMemory(ref b, byteLength);
        }
    }
}
