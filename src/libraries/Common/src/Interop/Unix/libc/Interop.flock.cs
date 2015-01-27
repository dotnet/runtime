// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int flock(int fd, LockOperations operation);

        [Flags]
        internal enum LockOperations
        { 
            LOCK_SH = 1, 
            LOCK_EX = 2, 
            LOCK_NB = 4, 
            LOCK_UN = 8, 
        }
    }
}
