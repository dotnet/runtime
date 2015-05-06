// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Pipe_L2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeInfo(
            SafePipeHandle hNamedPipe,
            out int lpFlags,
            IntPtr lpOutBufferSize,
            IntPtr lpInBufferSize,
            IntPtr lpMaxInstances
        );

        [DllImport(Libraries.Pipe_L2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeInfo(
            SafePipeHandle hNamedPipe,
            IntPtr lpFlags,
            out int lpOutBufferSize,
            IntPtr lpInBufferSize,
            IntPtr lpMaxInstances
        );

        [DllImport(Libraries.Pipe_L2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeInfo(
            SafePipeHandle hNamedPipe,
            IntPtr lpFlags,
            IntPtr lpOutBufferSize,
            out int lpInBufferSize,
            IntPtr lpMaxInstances
        );
    }
}
