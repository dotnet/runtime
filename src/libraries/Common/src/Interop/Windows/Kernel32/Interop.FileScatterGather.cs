// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int ReadFileScatter(
            SafeHandle handle,
            long* segments,
            int numBytesToRead,
            IntPtr reserved_mustBeZero,
            NativeOverlapped* overlapped);

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int WriteFileGather(
            SafeHandle handle,
            long* segments,
            int numBytesToWrite,
            IntPtr reserved_mustBeZero,
            NativeOverlapped* overlapped);
    }
}
