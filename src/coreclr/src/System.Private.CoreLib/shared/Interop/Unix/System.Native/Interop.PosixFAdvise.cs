// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum FileAdvice : int
        {
            POSIX_FADV_NORMAL       = 0,    /* no special advice, the default value */
            POSIX_FADV_RANDOM       = 1,    /* random I/O access */
            POSIX_FADV_SEQUENTIAL   = 2,    /* sequential I/O access */
            POSIX_FADV_WILLNEED     = 3,    /* will need specified pages */
            POSIX_FADV_DONTNEED     = 4,    /* don't need the specified pages */
            POSIX_FADV_NOREUSE      = 5,    /* data will only be accessed once */
        }

        /// <summary>
        /// Notifies the OS kernel that the specified file will be accessed in a particular way soon; this allows the kernel to
        /// potentially optimize the access pattern of the file.
        /// </summary>
        /// <param name="fd">The file descriptor of the file</param>
        /// <param name="offset">The start of the region to advise about</param>
        /// <param name="length">The number of bytes of the region (until the end of the file if 0)</param>
        /// <param name="advice">The type of advice to give the kernel about the specified region</param>
        /// <returns>
        /// Returns 0 on success; otherwise, the error code is returned
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_PosixFAdvise", SetLastError = false /* this is explicitly called out in the man page */)]
        internal static extern int PosixFAdvise(SafeFileHandle fd, long offset, long length, FileAdvice advice);
    }
}
