// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial int ReadFileScatter(
            SafeHandle hFile,
            long* aSegmentArray,
            int nNumberOfBytesToRead,
            IntPtr lpReserved,
            NativeOverlapped* lpOverlapped);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial int WriteFileGather(
            SafeHandle hFile,
            long* aSegmentArray,
            int nNumberOfBytesToWrite,
            IntPtr lpReserved,
            NativeOverlapped* lpOverlapped);
    }
}
