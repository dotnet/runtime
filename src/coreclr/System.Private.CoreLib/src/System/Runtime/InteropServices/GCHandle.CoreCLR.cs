// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern nint InternalAlloc(object? value, GCHandleType type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalFree(nint handle);

#if DEBUG
        // The runtime performs additional checks in debug builds
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalGet(nint handle);
#else
        internal static unsafe object? InternalGet(nint handle) =>
            Unsafe.As<nint, object>(ref *(nint*)(nint)handle);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalSet(nint handle, object? value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalCompareExchange(nint handle, object? value, object? oldValue);
    }
}
