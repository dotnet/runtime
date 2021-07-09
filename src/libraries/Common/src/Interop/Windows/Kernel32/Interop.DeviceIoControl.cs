// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://docs.microsoft.com/windows/win32/api/winioctl/ni-winioctl-fsctl_get_reparse_point
        internal const int FSCTL_GET_REPARSE_POINT = 0x000900a8;

        [DllImport(Libraries.Kernel32, EntryPoint = "DeviceIoControl", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern bool DeviceIoControl(
            SafeHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            byte[] lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);
    }
}
