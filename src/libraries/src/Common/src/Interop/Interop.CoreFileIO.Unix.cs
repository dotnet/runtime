// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using mode_t  = System.Int32;
using off64_t = System.Int64;
using size_t  = System.IntPtr;

internal static partial class Interop
{
    [DllImport(LIBC, SetLastError = true)]
    internal static extern int open64(string filename, int flags, mode_t mode);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern long lseek64(int fd, off64_t offset, int whence);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern unsafe size_t read(int fd, byte* buf, size_t count);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern unsafe size_t write(int fd, byte* buf, size_t count);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int fsync(int fd);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int close(int fd);

    internal enum OpenFlags
    {
        O_RDONLY = 0x0,
        O_WRONLY = 0x1,
        O_RDWR = 0x2,
        O_CREAT = 0x40,
        O_EXCL = 0x80,
        O_TRUNC = 0x200,
        O_SYNC = 0x1000,
        O_ASYNC = 0x2000
    }

    internal enum SeekWhence
    {
        SEEK_SET = 0,
        SEEK_CUR = 1,
        SEEK_END = 2
    }
}