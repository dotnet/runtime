// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial class Buffer
    {
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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Buffer_Clear")]
        private static unsafe partial void __ZeroMemory(void* b, nuint byteLength);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void __BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Buffer_MemMove")]
        private static unsafe partial void __Memmove(byte* dest, byte* src, nuint len);

        // Used by ilmarshalers.cpp
        internal static unsafe void Memcpy(byte* dest, byte* src, int len)
        {
            Debug.Assert(len >= 0, "Negative length in memcpy!");
            Memmove(ref *dest, ref *src, (nuint)(uint)len /* force zero-extension */);
        }

        // Used by ilmarshalers.cpp
        internal static unsafe void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
        {
            Debug.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Debug.Assert(src.Length - srcIndex >= len, "not enough bytes in src");

            Memmove(ref *(pDest + (uint)destIndex), ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(src), (nint)(uint)srcIndex /* force zero-extension */), (uint)len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Memmove<T>(ref T destination, ref T source, nuint elementCount)
        {
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                // Blittable memmove
                Memmove(
                    ref Unsafe.As<T, byte>(ref destination),
                    ref Unsafe.As<T, byte>(ref source),
                    elementCount * (nuint)Unsafe.SizeOf<T>());
            }
            else
            {
                // Non-blittable memmove
                BulkMoveWithWriteBarrier(
                    ref Unsafe.As<T, byte>(ref destination),
                    ref Unsafe.As<T, byte>(ref source),
                    elementCount * (nuint)Unsafe.SizeOf<T>());
            }
        }
    }
}
