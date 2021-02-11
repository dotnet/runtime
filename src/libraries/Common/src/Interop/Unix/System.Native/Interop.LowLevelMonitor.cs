// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Create")]
        internal static extern IntPtr LowLevelMonitor_Create();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Destroy")]
        internal static extern void LowLevelMonitor_Destroy(IntPtr monitor);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Acquire")]
        internal static extern void LowLevelMonitor_Acquire(IntPtr monitor);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Release")]
        internal static extern void LowLevelMonitor_Release(IntPtr monitor);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Wait")]
        internal static extern void LowLevelMonitor_Wait(IntPtr monitor);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_TimedWait")]
        internal static extern bool LowLevelMonitor_TimedWait(IntPtr monitor, int timeoutMilliseconds);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Signal_Release")]
        internal static extern void LowLevelMonitor_Signal_Release(IntPtr monitor);
    }
}
