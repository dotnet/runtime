// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
#if DEBUG
        static Sys()
        {
            foreach (string name in Enum.GetNames(typeof(UnixFileSystemTypes)))
            {
                System.Diagnostics.Debug.Assert(GetDriveType(name) != DriveType.Unknown,
                    $"Expected {nameof(UnixFileSystemTypes)}.{name} to have an entry in {nameof(GetDriveType)}.");
            }
        }
#endif

        private const int MountPointFormatBufferSizeInBytes = 32;

        /// <summary>
        /// Internal FileSystem names and magic numbers taken from man(2) statfs
        /// </summary>
        /// <remarks>
        /// These value names MUST be kept in sync with those in GetDriveType below,
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
            binfmt_misc = 0x42494E4D,
            btrfs = 0x9123683E,
            ceph = 0x00C36400,
            cgroupfs = 0x0027E0EB,
            cifs = 0xFF534D42,
            coda = 0x73757245,
            coherent = 0x012FF7B7,
            configfs = 0x62656570,
            cramfs = 0x28CD3D45,
            debugfs = 0x64626720,
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
            fat = 0x4006,
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
            logfs = 0xC97E8168,
            lustre = 0x0BD00BD0,
            minix_old = 0x137F, /* orig. minix */
            minix = 0x138F, /* 30 char minix */
            minix2 = 0x2468, /* minix V2 */
            minix2v2 = 0x2478, /* MINIX V2, 30 char names */
            minix3 = 0x4D5A,
            mqueue = 0x19800202,
            msdos = 0x4D44,
            nfs = 0x6969,
            nfsd = 0x6E667364,
            nilfs = 0x3434,
            novell = 0x564C,
            ntfs = 0x5346544E,
            openprom = 0x9FA1,
            ocfs2 = 0x7461636F,
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
            smb = 0x517B,
            sockfs = 0x534F434B,
            squashfs = 0x73717368,
            sysfs = 0x62656572,
            sysv2 = 0x012FF7B6,
            sysv4 = 0x012FF7B5,
            tmpfs = 0x01021994,
            ubifs = 0x24051905,
            udf = 0x15013346,
            ufs = 0x00011954,
            ufscigam = 0x54190100, // ufs byteswapped
            ufs2 = 0x19540119,
            usbdevice = 0x9FA2,
            v9fs = 0x01021997,
            vmhgfs = 0xBACBACBC,
            vxfs = 0xA501FCF5,
            vzfs = 0x565A4653,
            xenfs = 0xABBA1974,
            xenix = 0x012FF7B4,
            xfs = 0x58465342,
            xia = 0x012FD16D,
            zfs = 0x2FC12FC1,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MountPointInformation
        {
            internal ulong AvailableFreeSpace;
            internal ulong TotalFreeSpace;
            internal ulong TotalSize;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSpaceInfoForMountPoint", SetLastError = true)]
        internal static extern int GetSpaceInfoForMountPoint([MarshalAs(UnmanagedType.LPStr)]string name, out MountPointInformation mpi);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFormatInfoForMountPoint", SetLastError = true)]
        private static extern unsafe int GetFormatInfoForMountPoint(
            [MarshalAs(UnmanagedType.LPStr)]string name,
            byte* formatNameBuffer,
            int bufferLength,
            long* formatType);

        internal static int GetFormatInfoForMountPoint(string name, out string format)
        {
            DriveType temp;
            return GetFormatInfoForMountPoint(name, out format, out temp);
        }

        internal static int GetFormatInfoForMountPoint(string name, out DriveType type)
        {
            string temp;
            return GetFormatInfoForMountPoint(name, out temp, out type);
        }

        private static int GetFormatInfoForMountPoint(string name, out string format, out DriveType type)
        {
            unsafe
            {
                byte* formatBuffer = stackalloc byte[MountPointFormatBufferSizeInBytes];    // format names should be small
                long numericFormat;
                int result = GetFormatInfoForMountPoint(name, formatBuffer, MountPointFormatBufferSizeInBytes, &numericFormat);
                if (result == 0)
                {
                    // Check if we have a numeric answer or string
                    format = numericFormat != -1 ?
                        Enum.GetName(typeof(UnixFileSystemTypes), numericFormat) :
                        Marshal.PtrToStringAnsi((IntPtr)formatBuffer);
                    type = GetDriveType(format);
                }
                else
                {
                    format = string.Empty;
                    type = DriveType.Unknown;
                }

                return result;
            }
        }

        /// <summary>Categorizes a file system name into a drive type.</summary>
        /// <param name="fileSystemName">The name to categorize.</param>
        /// <returns>The recognized drive type.</returns>
        private static DriveType GetDriveType(string fileSystemName)
        {
            // This list is based primarily on "man fs", "man mount", "mntent.h", "/proc/filesystems", coreutils "stat.c",
            // and "wiki.debian.org/FileSystem". It can be extended over time as we find additional file systems that should
            // be recognized as a particular drive type.
            switch (fileSystemName)
            {
                case "cddafs":
                case "cd9660":
                case "iso":
                case "isofs":
                case "iso9660":
                case "fuseiso":
                case "fuseiso9660":
                case "udf":
                case "umview-mod-umfuseiso9660":
                    return DriveType.CDRom;

                case "aafs":
                case "adfs":
                case "affs":
                case "anoninode":
                case "anon-inode FS":
                case "apfs":
                case "balloon-kvm-fs":
                case "bdevfs":
                case "befs":
                case "bfs":
                case "bpf_fs":
                case "btrfs":
                case "btrfs_test":
                case "cgroup2fs":
                case "coh":
                case "daxfs":
                case "drvfs":
                case "efivarfs":
                case "efs":
                case "exfat":
                case "exofs":
                case "ext":
                case "ext2":
                case "ext2_old":
                case "ext3":
                case "ext2/ext3":
                case "ext4":
                case "ext4dev":
                case "f2fs":
                case "fat":
                case "fuseext2":
                case "fusefat":
                case "hfs":
                case "hfs+":
                case "hfsplus":
                case "hfsx":
                case "hostfs":
                case "hpfs":
                case "inodefs":
                case "inotifyfs":
                case "jbd":
                case "jbd2":
                case "jffs":
                case "jffs2":
                case "jfs":
                case "logfs":
                case "lxfs":
                case "minix (30 char.)":
                case "minix v2 (30 char.)":
                case "minix v2":
                case "minix":
                case "minix_old":
                case "minix2":
                case "minix2v2":
                case "minix2 v2":
                case "minix3":
                case "mlfs":
                case "msdos":
                case "nilfs":
                case "nsfs":
                case "ntfs":
                case "ntfs-3g":
                case "ocfs2":
                case "omfs":
                case "overlay":
                case "overlayfs":
                case "pstorefs":
                case "qnx4":
                case "qnx6":
                case "reiserfs":
                case "rpc_pipefs":
                case "smackfs":
                case "squashfs":
                case "swap":
                case "sysv":
                case "sysv2":
                case "sysv4":
                case "tracefs":
                case "ubifs":
                case "ufs":
                case "ufscigam":
                case "ufs2":
                case "umsdos":
                case "umview-mod-umfuseext2":
                case "v9fs":
                case "vxfs":
                case "vxfs_olt":
                case "vzfs":
                case "wslfs":
                case "xenix":
                case "xfs":
                case "xia":
                case "xiafs":
                case "xmount":
                case "zfs":
                case "zfs-fuse":
                case "zsmallocfs":
                    return DriveType.Fixed;

                case "9p":
                case "acfs":
                case "afp":
                case "afpfs":
                case "afs":
                case "aufs":
                case "autofs":
                case "autofs4":
                case "beaglefs":
                case "ceph":
                case "cifs":
                case "coda":
                case "coherent":
                case "curlftpfs":
                case "davfs2":
                case "dlm":
                case "ecryptfs":
                case "eCryptfs":
                case "fhgfs":
                case "flickrfs":
                case "ftp":
                case "fuse":
                case "fuseblk":
                case "fusedav":
                case "fusesmb":
                case "gfsgfs2":
                case "gfs/gfs2":
                case "gfs2":
                case "glusterfs-client":
                case "gmailfs":
                case "gpfs":
                case "ibrix":
                case "k-afs":
                case "kafs":
                case "kbfuse":
                case "ltspfs":
                case "lustre":
                case "ncp":
                case "ncpfs":
                case "nfs":
                case "nfs4":
                case "nfsd":
                case "novell":
                case "obexfs":
                case "panfs":
                case "prl_fs":
                case "s3ql":
                case "samba":
                case "smb":
                case "smb2":
                case "smbfs":
                case "snfs":
                case "sshfs":
                case "vmhgfs":
                case "webdav":
                case "wikipediafs":
                case "xenfs":
                    return DriveType.Network;

                case "anon_inode":
                case "anon_inodefs":
                case "aptfs":
                case "avfs":
                case "bdev":
                case "binfmt_misc":
                case "cgroup":
                case "cgroupfs":
                case "configfs":
                case "cramfs":
                case "cramfs-wend":
                case "cryptkeeper":
                case "cpuset":
                case "debugfs":
                case "devfs":
                case "devpts":
                case "devtmpfs":
                case "encfs":
                case "fdesc":
                case "fuse.gvfsd-fuse":
                case "fusectl":
                case "futexfs":
                case "hugetlbfs":
                case "libpam-encfs":
                case "ibpam-mount":
                case "mtpfs":
                case "mythtvfs":
                case "mqueue":
                case "openprom":
                case "openpromfs":
                case "pipefs":
                case "plptools":
                case "proc":
                case "pstore":
                case "pytagsfs":
                case "ramfs":
                case "rofs":
                case "romfs":
                case "rootfs":
                case "securityfs":
                case "selinux":
                case "selinuxfs":
                case "sockfs":
                case "sysfs":
                case "tmpfs":
                case "usbdev":
                case "usbdevfs":
                    return DriveType.Ram;

                case "gphotofs":
                case "usbfs":
                case "usbdevice":
                case "vfat":
                    return DriveType.Removable;

                    // Categorize as "Unknown" everything else not explicitly
                    // recognized as a particular drive type.
                default:
                    return DriveType.Unknown;
            }
        }
    }
}
