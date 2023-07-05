// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial class Buffer
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Buffer_Clear")]
        private static unsafe partial void __ZeroMemory(void* b, nuint byteLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Buffer_MemMove")]
        private static unsafe partial void __Memmove(byte* dest, byte* src, nuint len);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void __BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount);

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
    }
}
