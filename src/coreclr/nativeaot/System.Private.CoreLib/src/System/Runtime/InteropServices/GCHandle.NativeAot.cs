// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        internal static IntPtr InternalAlloc(object value, GCHandleType type) => RuntimeImports.RhHandleAlloc(value, type);

        internal static void InternalFree(IntPtr handle) => RuntimeImports.RhHandleFree(handle);

        internal static object? InternalGet(IntPtr handle) => RuntimeImports.RhHandleGet(handle);

        internal static void InternalSet(IntPtr handle, object? value) => RuntimeImports.RhHandleSet(handle, value);
    }
}
