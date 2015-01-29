// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using fsblkcnt_t = System.UIntPtr;
using fsfilcnt_t = System.UIntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int statvfs(string path, out structStatvfs buf);

        internal unsafe struct structStatvfs
        {
            internal UIntPtr    f_bsize;
            internal UIntPtr    f_frsize;
            internal fsblkcnt_t f_blocks;
            internal fsblkcnt_t f_bfree;
            internal fsblkcnt_t f_bavail;
            internal fsfilcnt_t f_files;
            internal fsfilcnt_t f_ffree;
            internal fsfilcnt_t f_favail;
            private  ulong      f_fsid; // defined as "unsigned long int", but on 32-bit it's followed by an alignment __f_unused int
            internal UIntPtr    f_flag;
            internal UIntPtr    f_namemax;
            private  fixed int  __f_spare[6];
        }
    }
}
