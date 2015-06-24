// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        /// <summary>
        /// Internal FileSystem names and magic numbers taken from man(2) statfs
        /// </summary>
        /// <remarks>
        /// These value names MUST be kept in sync with those in DriveInfo.Unix.GetDriveType
        /// </remarks>
        private enum LinuxFileSystemTypes : long
        {
            adfs = 0xadf5,
            affs = 0xADFF,
            befs = 0x42465331,
            bfs = 0x1BADFACE,
            cifs = 0xFF534D42,
            coda = 0x73757245,
            coherent = 0x012FF7B7,
            cramfs = 0x28cd3d45,
            devfs = 0x1373,
            efs = 0x00414A53,
            ext = 0x137D,
            ext2_old = 0xEF51,
            ext2 = 0xEF53,
            ext3 = 0xEF53,
            ext4 = 0xEF53,
            hfs = 0x4244,
            hpfs = 0xF995E849,
            hugetlbfs = 0x958458f6,
            isofs = 0x9660,
            jffs2 = 0x72b6,
            jfs = 0x3153464a,
            minix_old = 0x137F, /* orig. minix */
            minix = 0x138F, /* 30 char minix */
            minix2 = 0x2468, /* minix V2 */
            minix2v2 = 0x2478, /* minix V2, 30 char names */
            msdos = 0x4d44,
            ncpfs = 0x564c,
            nfs = 0x6969,
            ntfs = 0x5346544e,
            openprom = 0x9fa1,
            proc = 0x9fa0,
            qnx4 = 0x002f,
            reiserfs = 0x52654973,
            romfs = 0x7275,
            smb = 0x517B,
            sysv2 = 0x012FF7B6,
            sysv4 = 0x012FF7B5,
            tmpfs = 0x01021994,
            udf = 0x15013346,
            ufs = 0x00011954,
            usbdevice = 0x9fa2,
            vxfs = 0xa501FCF5,
            xenix = 0x012FF7B4,
            xfs = 0x58465342,
            xiafs = 0x012FD16D,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct fsid_t
        {
            internal fixed int val[2];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct statfs
        {
            internal long f_type;
            internal long f_bsize;
            internal ulong f_blocks;
            internal ulong f_bfree;
            internal ulong f_bavail;
            internal ulong f_files;
            internal ulong f_ffree;
            internal fsid_t f_fsid;
            internal long f_namelen;
            internal long f_frsize;
            internal long f_flags;
            internal fixed long f_space[4];
        }

        internal static unsafe String GetMountPointFsType(statfs data)
        {
            return ((LinuxFileSystemTypes)data.f_type).ToString();
        }
    }
}
