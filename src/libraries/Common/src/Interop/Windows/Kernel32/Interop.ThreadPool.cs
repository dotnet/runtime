// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial IntPtr CreateThreadpoolWork(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> pfnwk, IntPtr pv, IntPtr pcbe);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static partial void SubmitThreadpoolWork(IntPtr pwk);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static partial void CloseThreadpoolWork(IntPtr pwk);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial IntPtr CreateThreadpoolWait(delegate* unmanaged<IntPtr, IntPtr, IntPtr, uint, void> pfnwa, IntPtr pv, IntPtr pcbe);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static partial void SetThreadpoolWait(IntPtr pwa, IntPtr h, IntPtr pftTimeout);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static partial void WaitForThreadpoolWaitCallbacks(IntPtr pwa, [MarshalAs(UnmanagedType.Bool)] bool fCancelPendingCallbacks);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        internal static partial void CloseThreadpoolWait(IntPtr pwa);
    }
}
