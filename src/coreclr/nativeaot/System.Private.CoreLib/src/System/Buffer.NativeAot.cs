// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    public static partial class Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void __ZeroMemory(void* b, nuint byteLength) =>
            RuntimeImports.memset((byte*)b, 0, byteLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void __Memmove(byte* dest, byte* src, nuint len) =>
            RuntimeImports.memmove(dest, src, len);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void __BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount) =>
            RuntimeImports.RhBulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
    }
}
