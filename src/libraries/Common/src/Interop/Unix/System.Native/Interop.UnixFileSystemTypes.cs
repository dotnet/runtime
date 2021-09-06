// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Internal FileSystem names and magic numbers taken from man(2) statfs
        /// </summary>
        /// <remarks>
        /// These value names MUST be kept in sync with those in GetDriveType (moved to Interop.MountPoints.FormatInfo.cs),
        /// where this enum must be a subset of the GetDriveType list, with the enum
        /// values here exactly matching a string there.
        /// </remarks>
        internal enum UnixFileSystemTypes : long
        {
            adfs = 0xADF5,
            affs = 0xADFF,
            afs = 0x5346414F,
            anoninode = 0x09041934,
            aufs = 0x61756673,
            autofs = 0x0187,
            autofs4 = 0x6D4A556D,
            befs = 0x42465331,
            bdevfs = 0x62646576,
            bfs = 0x1BADFACE,
            bpf_fs = 0xCAFE4A11,
            binfmt_misc = 0x42494E4D,
            bootfs = 0xA56D3FF9,
            btrfs = 0x9123683E,
            ceph = 0x00C36400,
            cgroupfs = 0x0027E0EB,
            cgroup2fs = 0x63677270,
            cifs = 0xFF534D42,
            coda = 0x73757245,
            coherent = 0x012FF7B7,
            configfs = 0x62656570,
            cpuset = 0x01021994, // same as tmpfs
            cramfs = 0x28CD3D45,
            ctfs = 0x01021994, // same as tmpfs
            debugfs = 0x64626720,
            dev = 0x1373, // same as devfs
            devfs = 0x1373,
            devpts = 0x1CD1,
            ecryptfs = 0xF15F,
            efs = 0x00414A53,
            exofs = 0x5DF5,
            ext = 0x137D,
            ext2_old = 0xEF51,
            ext2 = 0xEF53,
            ext3 = 0xEF53,
            ext4 = 0xEF53,
            f2fs = 0xF2F52010,
            fat = 0x4006,
            fd = 0xF00D1E,
            fhgfs = 0x19830326,
            fuse = 0x65735546,
            fuseblk = 0x65735546,
            fusectl = 0x65735543,
            futexfs = 0x0BAD1DEA,
            gfsgfs2 = 0x1161970,
            gfs2 = 0x01161970,
            gpfs = 0x47504653,
            hfs = 0x4244,
            hfsplus = 0x482B,
            hpfs = 0xF995E849,
            hugetlbfs = 0x958458F6,
            inodefs = 0x11307854,
            inotifyfs = 0x2BAD1DEA,
            isofs = 0x9660,
            // isofs = 0x4004, // R_WIN
            // isofs = 0x4000, // WIN
            jffs = 0x07C0,
            jffs2 = 0x72B6,
            jfs = 0x3153464A,
            kafs = 0x6B414653,
            lofs = 0xEF53, /* loopback filesystem, magic same as ext2 */
            logfs = 0xC97E8168,
            lustre = 0x0BD00BD0,
            minix_old = 0x137F, /* orig. minix */
            minix = 0x138F, /* 30 char minix */
            minix2 = 0x2468, /* minix V2 */
            minix2v2 = 0x2478, /* MINIX V2, 30 char names */
            minix3 = 0x4D5A,
            mntfs = 0x01021994, // same as tmpfs
            mqueue = 0x19800202,
            msdos = 0x4D44,
            nfs = 0x6969,
            nfsd = 0x6E667364,
            nilfs = 0x3434,
            novell = 0x564C,
            ntfs = 0x5346544E,
            objfs = 0x01021994, // same as tmpfs
            ocfs2 = 0x7461636F,
            openprom = 0x9FA1,
            omfs = 0xC2993D87,
            overlay = 0x794C7630,
            overlayfs = 0x794C764F,
            panfs = 0xAAD7AAEA,
            pipefs = 0x50495045,
            proc = 0x9FA0,
            pstorefs = 0x6165676C,
            qnx4 = 0x002F,
            qnx6 = 0x68191122,
            ramfs = 0x858458F6,
            reiserfs = 0x52654973,
            romfs = 0x7275,
            rootfs = 0x53464846,
            rpc_pipefs = 0x67596969,
            samba = 0x517B,
            securityfs = 0x73636673,
            selinux = 0xF97CFF8C,
            sffs = 0x786F4256, // same as vboxfs
            sharefs = 0x01021994, // same as tmpfs
            smb = 0x517B,
            smb2 = 0xFE534D42,
            sockfs = 0x534F434B,
            squashfs = 0x73717368,
            sysfs = 0x62656572,
            sysv2 = 0x012FF7B6,
            sysv4 = 0x012FF7B5,
            tmpfs = 0x01021994,
            tracefs = 0x74726163,
            ubifs = 0x24051905,
            udf = 0x15013346,
            ufs = 0x00011954,
            ufscigam = 0x54190100, // ufs byteswapped
            ufs2 = 0x19540119,
            usbdevice = 0x9FA2,
            v9fs = 0x01021997,
            vagrant = 0x786F4256, // same as vboxfs
            vboxfs = 0x786F4256,
            vmhgfs = 0xBACBACBC,
            vxfs = 0xA501FCF5,
            vzfs = 0x565A4653,
            xenfs = 0xABBA1974,
            xenix = 0x012FF7B4,
            xfs = 0x58465342,
            xia = 0x012FD16D,
            udev = 0x01021994, // same as tmpfs
            zfs = 0x2FC12FC1,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFileSystemType")]
        private static extern long GetFileSystemType(SafeFileHandle fd);

        internal static bool TryGetFileSystemType(SafeFileHandle fd, out UnixFileSystemTypes fileSystemType)
        {
            long fstatfsResult = GetFileSystemType(fd);
            fileSystemType = (UnixFileSystemTypes)fstatfsResult;
            return fstatfsResult != -1;
        }
    }
}
