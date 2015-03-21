// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Pipe, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        unsafe internal static extern bool ConnectNamedPipe(SafePipeHandle handle, NativeOverlapped* overlapped);

        [DllImport(Libraries.Pipe, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConnectNamedPipe(SafePipeHandle handle, IntPtr overlapped);
    }
}
