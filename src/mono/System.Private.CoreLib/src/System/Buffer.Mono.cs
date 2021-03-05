// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    public partial class Buffer
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void __Memmove(byte* dest, byte* src, nuint len);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void BulkMoveWithWriteBarrier(ref byte dmem, ref byte smem, nuint len, IntPtr type_handle);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe void _ZeroMemory(ref byte b, nuint byteLength)
        {
            fixed (byte* bytePointer = &b)
            {
                __ZeroMemory(bytePointer, byteLength);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void __ZeroMemory(void* p, nuint byteLength);

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
