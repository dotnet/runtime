// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types
#if BIT64
using nuint = System.UInt64;
using nint = System.UInt64;
#else
using nuint = System.UInt32;
using nint = System.UInt32;
#endif

namespace System
{
    public partial class Buffer
    {
        // Returns a bool to indicate if the array is of primitive data types
        // or not.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsPrimitiveTypeArray(Array array);

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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern unsafe void __ZeroMemory(void* b, nuint byteLength);

        // The maximum block size to for __BulkMoveWithWriteBarrier FCall. This is required to avoid GC starvation.
        private const uint BulkMoveWithWriteBarrierChunk = 0x4000;

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

            if (Unsafe.AreSame(ref destination, ref source))
                return;

            if ((nuint)(nint)Unsafe.ByteOffset(ref destination, ref source) >= byteCount)
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

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern unsafe void __Memmove(byte* dest, byte* src, nuint len);

        internal static unsafe void Memcpy(byte* dest, byte* src, int len)
        {
            Debug.Assert(len >= 0, "Negative length in memcpy!");
            Memmove(dest, src, (nuint)len);
        }

        // Used by ilmarshalers.cpp
        internal static unsafe void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
        {
            Debug.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Debug.Assert(src.Length - srcIndex >= len, "not enough bytes in src");
            // If dest has 0 elements, the fixed statement will throw an
            // IndexOutOfRangeException.  Special-case 0-byte copies.
            if (len == 0)
                return;
            fixed (byte* pSrc = src)
            {
                Memcpy(pDest + destIndex, pSrc + srcIndex, len);
            }
        }
    }
}
