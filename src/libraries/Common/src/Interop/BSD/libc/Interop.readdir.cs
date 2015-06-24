// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using ino_t = System.IntPtr;
using off_t = System.Int64; // Assuming either 64-bit machine or _FILE_OFFSET_BITS == 64

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, EntryPoint = "readdir" + Interop.Libraries.INODE64SUFFIX, SetLastError = true)]
        internal static extern IntPtr readdir(SafeDirHandle dirp);

        internal static unsafe DType GetDirEntType(IntPtr dirEnt)
        {
            return ((dirent*)dirEnt)->d_type;
        }

        internal enum DType : byte
        {
            DT_UNKNOWN = 0,
            DT_FIFO = 1,
            DT_CHR = 2,
            DT_DIR = 4,
            DT_BLK = 6,
            DT_REG = 8,
            DT_LNK = 10,
            DT_SOCK = 12,
            DT_WHT = 14
        }
    }
}
