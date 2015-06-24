// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using uid_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal const int MFSNAMELEN = 16; 	     /* length of type name including null */
        internal const int MNAMELEN = 88;    	     /* size of on/from name bufs */

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct fsid_t
        {
            internal fixed int val[2];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct statfs
        {
            internal uint f_version;                        /* structure version number */
            internal uint f_type;                           /* type of filesystem */
            internal ulong f_flags;                         /* copy of mount exported flags */
            internal ulong f_bsize;                         /* filesystem fragment size */
            internal ulong f_iosize;                        /* optimal transfer block size */
            internal ulong f_blocks;                        /* total data blocks in filesystem */
            internal ulong f_bfree;                         /* free blocks in filesystem */
            internal long f_bavail;                         /* free blocks avail to non-superuser */
            internal ulong f_files;                         /* total file nodes in filesystem */
            internal long f_ffree;                          /* free nodes avail to non-superuser */
            internal ulong f_syncwrites;                    /* count of sync writes since mount */
            internal ulong f_asyncwrites;                   /* count of async writes since mount */
            internal ulong f_syncreads;                     /* count of sync reads since mount */
            internal ulong f_asyncreads;                    /* count of async reads since mount */
            internal fixed ulong f_spare[10];               /* unused spare */
            internal uint f_namemax;                        /* maximum filename length */
            internal uid_t f_owner;                         /* user that mounted the filesystem */
            internal fsid_t f_fsid;                         /* filesystem id */
            internal fixed byte f_charspare[80];            /* spare string space */
            internal fixed byte f_fstypename[MFSNAMELEN];   /* filesystem type name */
            internal fixed byte f_mntfromname[MNAMELEN];    /* mounted filesystem */
            internal fixed byte f_mntonname[MNAMELEN];      /* directory on which mounted */
        }

        internal static unsafe String GetMountPointFsType(statfs data)
        {
            return Marshal.PtrToStringAnsi((IntPtr)data.f_fstypename);
        }
    }
}
