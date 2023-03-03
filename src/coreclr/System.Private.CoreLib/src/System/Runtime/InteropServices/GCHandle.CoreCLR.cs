// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr InternalAlloc(object? value, GCHandleType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalFree(IntPtr handle);

#if DEBUG
        // The runtime performs additional checks in debug builds
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalGet(IntPtr handle);
#else
#pragma warning disable 8500 // address of managed types
        internal static unsafe object? InternalGet(IntPtr handle) => *(object*)handle;
#pragma warning restore 8500
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalSet(IntPtr handle, object? value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalCompareExchange(IntPtr handle, object? value, object? oldValue);
    }
}
