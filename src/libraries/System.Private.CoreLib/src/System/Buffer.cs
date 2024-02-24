// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // Non-inlinable wrapper around the QCall that avoids polluting the fast path
        // with P/Invoke prolog/epilog.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe void _Memmove(ref byte dest, ref byte src, nuint len)
        {
            fixed (byte* pDest = &dest)
            fixed (byte* pSrc = &src)
                __Memmove(pDest, pSrc, len);
        }

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
                SpanHelpers.Memmove(
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
}
