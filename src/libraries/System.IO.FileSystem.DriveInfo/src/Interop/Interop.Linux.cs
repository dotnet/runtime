// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using fsblkcnt_t = System.UIntPtr;
using fsfilcnt_t = System.UIntPtr;

internal static partial class Interop
{
    private const string LIBC = "libc";

    [DllImport(LIBC, SetLastError = true)]
    internal static extern IntPtr setmntent(string filename, string type);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern unsafe IntPtr getmntent_r(IntPtr fp, ref mntent mntbuf, byte* buf, int buflen);

    [DllImport(LIBC)]
    internal static extern int endmntent(IntPtr fp);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int statvfs(string path, out structStatvfs buf);

    internal const string MNTTYPE_IGNORE = "ignore";
    internal const string MNTOPT_RO = "ro";

#pragma warning disable 0649 // set via P/Invoke
    internal unsafe struct mntent
    {
        internal byte* mnt_fsname;
        internal byte* mnt_dir;
        internal byte* mnt_type;
        internal byte* mnt_opts;
        internal int mnt_freq;
        internal int mnt_passno;
    }

    internal unsafe struct structStatvfs
    {
        internal UIntPtr f_bsize;
        internal UIntPtr f_frsize;
        internal fsblkcnt_t f_blocks;
        internal fsblkcnt_t f_bfree;
        internal fsblkcnt_t f_bavail;
        internal fsfilcnt_t f_files;
        internal fsfilcnt_t f_ffree;
        internal fsfilcnt_t f_favail;
        private ulong f_fsid; // defined as "unsigned long int", but on 32-bit it's followed by an alignment __f_unused int
        internal UIntPtr f_flag;
        internal UIntPtr f_namemax;
        private fixed int __f_spare[6];
    }
#pragma warning restore 0649
}
