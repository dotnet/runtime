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
        private static extern void BulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint len, IntPtr type_handle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Memmove<T>(ref T destination, ref T source, nuint elementCount)
        {
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
#pragma warning disable 8500 // sizeof of managed types
                // Blittable memmove
                SpanHelpers.Memmove(
                    ref Unsafe.As<T, byte>(ref destination),
                    ref Unsafe.As<T, byte>(ref source),
                    elementCount * (nuint)sizeof(T));
#pragma warning restore 8500
            }
            else if (elementCount > 0)
            {
                // Non-blittable memmove
                BulkMoveWithWriteBarrier(
                    ref Unsafe.As<T, byte>(ref destination),
                    ref Unsafe.As<T, byte>(ref source),
                    elementCount,
                    typeof(T).TypeHandle.Value);
            }
        }
    }
}
