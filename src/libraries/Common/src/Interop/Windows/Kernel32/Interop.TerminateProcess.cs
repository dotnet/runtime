// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if !SYSTEM_PRIVATE_CORELIB
using Microsoft.Win32.SafeHandles;
#endif

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
#if SYSTEM_PRIVATE_CORELIB
        // IntPtr overload for use on fatal-error paths where allocating a SafeHandle
        // (for example, to wrap the current-process pseudo handle) must be avoided.
        internal static partial bool TerminateProcess(IntPtr processHandle, int exitCode);
#else
        internal static partial bool TerminateProcess(SafeProcessHandle processHandle, int exitCode);
#endif
    }
}
