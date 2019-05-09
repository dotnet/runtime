// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
#if BIT64
using nint = System.Int64;
#else
using nint = System.Int32;
#endif
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalAlloc(object? value, GCHandleType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalFree(IntPtr handle);

#if DEBUG
        // The runtime performs additional checks in debug builds
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object InternalGet(IntPtr handle);
#else
        internal static unsafe object InternalGet(IntPtr handle) =>
            Unsafe.As<IntPtr, object>(ref *(IntPtr*)handle);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalSet(IntPtr handle, object? value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalCompareExchange(IntPtr handle, object? value, object? oldValue);
    }
}
