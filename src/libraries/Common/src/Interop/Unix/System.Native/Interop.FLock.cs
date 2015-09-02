// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum LockOperations : int
        {
            LOCK_SH = 1,    /* shared lock */
            LOCK_EX = 2,    /* exclusive lock */
            LOCK_NB = 4,    /* don't block when locking*/
            LOCK_UN = 8,    /* unlock */
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int FLock(int fd, LockOperations operation);
    }
}
