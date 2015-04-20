// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using off_t = System.Int64; // Assuming either 64-bit machine or _FILE_OFFSET_BITS == 64

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int posix_fadvise(int fd, off_t offset, off_t len, Advice advice);

        internal enum Advice
        {
            POSIX_FADV_NORMAL = 0,
            POSIX_FADV_RANDOM = 1,
            POSIX_FADV_SEQUENTIAL = 2,
            POSIX_FADV_WILLNEED = 3,
            POSIX_FADV_DONTNEED = 4,
            POSIX_FADV_NOREUSE = 5
        }
    }
}
