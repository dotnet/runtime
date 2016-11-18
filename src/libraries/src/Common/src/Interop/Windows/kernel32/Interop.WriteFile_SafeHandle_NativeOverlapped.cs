// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
internal partial class Interop
{
    internal partial class Kernel32
    {
        // Note there are two different WriteFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe extern int WriteFile(SafeHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);
    }
}
