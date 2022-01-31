// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32)]
        internal static extern unsafe IntPtr CreateThreadpoolWork(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> pfnwk, IntPtr pv, IntPtr pcbe);

        [DllImport(Libraries.Kernel32)]
        internal static extern void SubmitThreadpoolWork(IntPtr pwk);

        [DllImport(Libraries.Kernel32)]
        internal static extern void CloseThreadpoolWork(IntPtr pwk);

        [DllImport(Libraries.Kernel32)]
        internal static extern unsafe IntPtr CreateThreadpoolWait(delegate* unmanaged<IntPtr, IntPtr, IntPtr, uint, void> pfnwa, IntPtr pv, IntPtr pcbe);

        [DllImport(Libraries.Kernel32)]
        internal static extern void SetThreadpoolWait(IntPtr pwa, IntPtr h, IntPtr pftTimeout);

        [DllImport(Libraries.Kernel32)]
        internal static extern void WaitForThreadpoolWaitCallbacks(IntPtr pwa, bool fCancelPendingCallbacks);

        [DllImport(Libraries.Kernel32)]
        internal static extern void CloseThreadpoolWait(IntPtr pwa);
    }
}
