// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using uid_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        private const int MAXPATHLEN = 1024;
        private const int MFSTYPENAMELEN = 16;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct fsid_t
        {
            internal fixed int val[2];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct statfs
        {
            internal uint       f_bsize;                        /* fundamental file system block size */
            internal int        f_iosize;                       /* optimal transfer block size */
            internal ulong      f_blocks;                       /* total data blocks in file system */
            internal ulong      f_bfree;                        /* free blocks in fs */
            internal ulong      f_bavail;                       /* free blocks avail to non-superuser */
            internal ulong      f_files;                        /* total file nodes in file system */
            internal ulong      f_ffree;                        /* free file nodes in fs */
            internal fsid_t     f_fsid;                         /* file system id */
            internal uid_t      f_owner;                        /* user that mounted the filesystem */
            internal uint       f_type;                         /* type of filesystem */
            internal uint       f_flags;                        /* copy of mount exported flags */
            internal uint       f_fssubtype;                    /* fs sub-type (flavor) */
            internal fixed byte f_fstypename[MFSTYPENAMELEN];   /* fs type name */
            internal fixed byte f_mntonname[MAXPATHLEN];        /* directory on which mounted */
            internal fixed byte f_mntfromname[MAXPATHLEN];      /* mounted filesystem */
            internal fixed uint f_reserved[8];                  /* For future use */
        }

        internal static unsafe String GetMountPointFsType(statfs data)
        {
            return Marshal.PtrToStringAnsi((IntPtr)data.f_fstypename);
        }
    }
}
