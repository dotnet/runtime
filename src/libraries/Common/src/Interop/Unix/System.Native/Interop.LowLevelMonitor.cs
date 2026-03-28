// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Create")]
        internal static partial IntPtr LowLevelMonitor_Create();

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Destroy")]
        internal static partial void LowLevelMonitor_Destroy(IntPtr monitor);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Acquire")]
        internal static partial void LowLevelMonitor_Acquire(IntPtr monitor);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Release")]
        internal static partial void LowLevelMonitor_Release(IntPtr monitor);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Wait")]
        internal static partial void LowLevelMonitor_Wait(IntPtr monitor);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_TimedWait")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool LowLevelMonitor_TimedWait(IntPtr monitor, int timeoutMilliseconds);

        [RequiresUnsafe]
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelMonitor_Signal_Release")]
        internal static partial void LowLevelMonitor_Signal_Release(IntPtr monitor);
    }
}
