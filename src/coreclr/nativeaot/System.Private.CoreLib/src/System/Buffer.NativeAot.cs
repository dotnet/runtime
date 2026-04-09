// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    public static partial class Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BulkMoveWithWriteBarrierInternal(ref byte destination, ref byte source, nuint byteCount) =>
            RuntimeImports.RhBulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
    }
}
