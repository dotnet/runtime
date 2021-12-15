// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe extern SafeThreadPoolIOHandle CreateThreadpoolIo(SafeHandle fl, delegate* unmanaged<IntPtr, IntPtr, IntPtr, uint, UIntPtr, IntPtr, void> pfnio, IntPtr context, IntPtr pcbe);

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern void CloseThreadpoolIo(IntPtr pio);

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern void StartThreadpoolIo(SafeThreadPoolIOHandle pio);

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern void CancelThreadpoolIo(SafeThreadPoolIOHandle pio);
    }
}
