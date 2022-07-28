// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // Note there are two different WriteFile prototypes - this is to use
        // the type system to force you to not trip across a "feature" in
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial int WriteFile(
            SafeHandle handle,
            byte* bytes,
            int numBytesToWrite,
            IntPtr numBytesWritten_mustBeZero,
            NativeOverlapped* lpOverlapped);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial int WriteFile(
            SafeHandle handle,
            byte* bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            NativeOverlapped* lpOverlapped);
    }
}
