// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe extern SafeThreadPoolIOHandle CreateThreadpoolIo(SafeHandle fl, [MarshalAs(UnmanagedType.FunctionPtr)] NativeIoCompletionCallback pfnio, IntPtr context, IntPtr pcbe);

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern void CloseThreadpoolIo(IntPtr pio);

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern void StartThreadpoolIo(SafeThreadPoolIOHandle pio);

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern void CancelThreadpoolIo(SafeThreadPoolIOHandle pio);
    }

    internal delegate void NativeIoCompletionCallback(IntPtr instance, IntPtr context, IntPtr overlapped, uint ioResult, UIntPtr numberOfBytesTransferred, IntPtr io);
}
