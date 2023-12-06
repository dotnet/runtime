// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum LockType : short
        {
            F_RDLCK = 0,    // shared or read lock
            F_WRLCK = 1,    // exclusive or write lock
            F_UNLCK = 2     // unlock
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LockFileRegion", SetLastError = true)]
        internal static partial int LockFileRegion(SafeHandle fd, long offset, long length, LockType lockType);
    }
}
