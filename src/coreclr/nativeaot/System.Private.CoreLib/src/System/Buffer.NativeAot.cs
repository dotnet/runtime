// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    public static partial class Buffer
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Buffer_Clear")]
        private static unsafe partial void ZeroMemoryInternal(void* b, nuint byteLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Buffer_MemMove")]
        private static unsafe partial void MemmoveInternal(byte* dest, byte* src, nuint len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BulkMoveWithWriteBarrierInternal(ref byte destination, ref byte source, nuint byteCount) =>
            RuntimeImports.RhBulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
    }
}
