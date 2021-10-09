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
            SafeHandle hFile,
            long* aSegmentArray,
            int nNumberOfBytesToRead,
            IntPtr lpReserved,
            NativeOverlapped* lpOverlapped);

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int WriteFileGather(
            SafeHandle hFile,
            long* aSegmentArray,
            int nNumberOfBytesToWrite,
            IntPtr lpReserved,
            NativeOverlapped* lpOverlapped);
    }
}
