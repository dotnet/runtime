// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial SafeThreadPoolIOHandle CreateThreadpoolIo(SafeHandle fl, delegate* unmanaged<IntPtr, IntPtr, IntPtr, uint, UIntPtr, IntPtr, void> pfnio, IntPtr context, IntPtr pcbe);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void CloseThreadpoolIo(IntPtr pio);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void StartThreadpoolIo(SafeThreadPoolIOHandle pio);

        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial void CancelThreadpoolIo(SafeThreadPoolIOHandle pio);
    }
}
