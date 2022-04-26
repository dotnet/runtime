// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        private static IntPtr InternalAlloc(object value, GCHandleType type) => RuntimeImports.RhHandleAlloc(value, type);

        private static void InternalFree(IntPtr handle) => RuntimeImports.RhHandleFree(handle);

        private static object? InternalGet(IntPtr handle) => RuntimeImports.RhHandleGet(handle);

        private static void InternalSet(IntPtr handle, object? value) => RuntimeImports.RhHandleSet(handle, value);
    }
}
