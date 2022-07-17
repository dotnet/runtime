// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Create")]
        internal static partial IntPtr LowLevelMonitor_Create();

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Destroy")]
        internal static partial void LowLevelMonitor_Destroy(IntPtr monitor);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Acquire")]
        internal static partial void LowLevelMonitor_Acquire(IntPtr monitor);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Release")]
        internal static partial void LowLevelMonitor_Release(IntPtr monitor);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Wait")]
        internal static partial void LowLevelMonitor_Wait(IntPtr monitor);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_TimedWait")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LowLevelMonitor_TimedWait(IntPtr monitor, int timeoutMilliseconds);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Signal_Release")]
        internal static partial void LowLevelMonitor_Signal_Release(IntPtr monitor);
    }
}
