// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using uid_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal const int MAXPATHLEN = 1024;
        internal const int MFSTYPENAMELEN = 16;

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

        /// <summary>
        /// Gets all the current mount points on the system
        /// </summary>
        /// <param name="ppBuffer">A pointer to an array of mount points</param>
        /// <param name="flags">Flags that are passed to the getfsstat call for each statfs struct</param>
        /// <returns>Returns the number of retrieved statfs structs</returns>
        /// <remarks
        /// Do NOT free this memory...this memory is allocated by the OS, which is responsible for it.
        /// This call could also block for a bit to wait for slow network drives.
        /// </remarks>
        [DllImport(Interop.Libraries.libc, EntryPoint = "getmntinfo" + Interop.Libraries.INODE64SUFFIX)]
        internal static unsafe extern int getmntinfo(statfs** ppBuffer, int flags);

        /// <summary>
        /// Gets a statfs struct for the given path that describes that mount point
        /// </summary>
        /// <param name="path">The path to retrieve the statfs for</param>
        /// <param name="buffer">The output statfs struct describing the mount point</param>
        /// <returns>Returns 0 on success, -1 on failure</returns>
        [DllImport(Interop.Libraries.libc, EntryPoint = "statfs" + Interop.Libraries.INODE64SUFFIX)]
        private static unsafe extern int get_statfs(string path, statfs* buffer);

        /// <summary>
        /// Gets a statfs struct for a given mount point
        /// </summary>
        /// <param name="name">The drive name to retrieve the statfs data for</param>
        /// <returns>Returns </returns>
        internal static unsafe statfs GetStatFsForDriveName(string name)
        {
            statfs data = default(statfs);
            int result = get_statfs(name, &data);
            if (result < 0)
                throw Interop.GetExceptionForIoErrno(Marshal.GetLastWin32Error());
            else
                return data;
        }

        internal static unsafe String GetMountPointName(statfs data)
        {
            return Marshal.PtrToStringAnsi((IntPtr)data.f_mntonname);
        }

        internal static unsafe String GetMountPointFsType(statfs data)
        {
            return Marshal.PtrToStringAnsi((IntPtr)data.f_fstypename);
        }
    }
}