// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FLock", SetLastError = true)]
        internal static extern int FLock(SafeFileHandle fd, LockOperations operation);

        /// <summary>
        /// Exposing this for SafeFileHandle.ReleaseHandle() to call.
        /// Normal callers should use FLock(SafeFileHandle fd).
        /// </summary>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FLock", SetLastError = true)]
        internal static extern int FLock(IntPtr fd, LockOperations operation);
    }
}
