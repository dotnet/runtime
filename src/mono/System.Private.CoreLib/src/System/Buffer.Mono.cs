// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public partial class Buffer
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void __ZeroMemory(void* b, nuint byteLength);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void __Memmove(byte* dest, byte* src, nuint len);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void __BulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint len, IntPtr type_handle);
    }
}
