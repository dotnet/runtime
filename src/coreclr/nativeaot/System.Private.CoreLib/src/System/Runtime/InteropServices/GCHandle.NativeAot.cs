// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Java;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        internal static IntPtr InternalAlloc(object value, GCHandleType type) => RuntimeImports.RhHandleAlloc(value, type);

        internal static void InternalFree(IntPtr handle) => RuntimeImports.RhHandleFree(handle);

        internal static object? InternalGet(IntPtr handle) => RuntimeImports.RhHandleGet(handle);

        internal static void InternalSet(IntPtr handle, object? value) => RuntimeImports.RhHandleSet(handle, value);

#if FEATURE_JAVAMARSHAL
        internal static object? InternalGetBridgeWait(IntPtr handle)
        {
            if (RuntimeImports.RhIsGCBridgeActive())
            {
#pragma warning disable CA1416 // Validate platform compatibility
                JavaMarshal.WaitForGCBridgeFinish();
#pragma warning restore CA1416 // Validate platform compatibility
            }
            return InternalGet(handle);
        }
#endif
    }
}
