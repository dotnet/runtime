// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        internal static IntPtr InternalAlloc(object value, GCHandleType type) => RuntimeImports.RhHandleAlloc(value, type);

        internal static void InternalFree(IntPtr handle) => RuntimeImports.RhHandleFree(handle);

        internal static object? InternalGet(IntPtr handle) => RuntimeImports.RhHandleGet(handle);

        internal static void InternalSet(IntPtr handle, object? value) => RuntimeImports.RhHandleSet(handle, value);

#if FEATURE_JAVAMARSHAL
        internal static unsafe object? InternalGetBridgeWait(IntPtr handle)
        {
            object? target = null;

            if (InternalTryGetBridgeWait(handle, ref target))
                return target;

            InternalGetBridgeWait(handle, &target);

            return target;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeImports.RuntimeLibrary, "GCHandle_InternalTryGetBridgeWait")]
        private static extern bool InternalTryGetBridgeWait(IntPtr handle, ref object? result);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCHandle_InternalGetBridgeWait")]
        private static unsafe partial void InternalGetBridgeWait(IntPtr handle, object?* result);
#endif
    }
}
