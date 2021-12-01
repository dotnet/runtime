// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
#if !DEBUG
using Internal.Runtime.CompilerServices;
#endif

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
        internal static extern object? InternalGet(IntPtr handle);
#else
        internal static unsafe object? InternalGet(IntPtr handle) =>
            Unsafe.As<IntPtr, object>(ref *(IntPtr*)(nint)handle);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalSet(IntPtr handle, object? value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalCompareExchange(IntPtr handle, object? value, object? oldValue);
    }
}
