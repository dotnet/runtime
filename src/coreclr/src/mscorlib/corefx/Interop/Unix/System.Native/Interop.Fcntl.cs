// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum LockType : short
        {
            F_UNLCK = 2,    // unlock
            F_WRLCK = 3     // exclusive or write lock
        }
        
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LockFileRegion", SetLastError=true)]
        internal static extern int LockFileRegion(SafeHandle fd, long offset, long length, LockType lockType);
    }
}
