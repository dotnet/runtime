// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            foreach (string name in Enum.GetNames<UnixFileSystemTypes>())
            {
                System.Diagnostics.Debug.Assert(GetDriveType(name) != DriveType.Unknown,
                    $"Expected {nameof(UnixFileSystemTypes)}.{name} to have an entry in {nameof(GetDriveType)}.");
            }
        }
#endif

        private const int MountPointFormatBufferSizeInBytes = 32;

        [StructLayout(LayoutKind.Sequential)]
        internal struct MountPointInformation
        {
            internal ulong AvailableFreeSpace;
            internal ulong TotalFreeSpace;
            internal ulong TotalSize;
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSpaceInfoForMountPoint", SetLastError = true)]
        internal static partial int GetSpaceInfoForMountPoint([MarshalAs(UnmanagedType.LPUTF8Str)] string name, out MountPointInformation mpi);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetFormatInfoForMountPoint", SetLastError = true)]
        internal static unsafe partial int GetFormatInfoForMountPoint(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            byte* formatNameBuffer,
            int bufferLength,
            long* formatType);

        internal static int GetFormatInfoForMountPoint(string name, out string format)
        {
            return GetFormatInfoForMountPoint(name, out format, out _);
        }

        internal static int GetFormatInfoForMountPoint(string name, out DriveType type)
        {
            return GetFormatInfoForMountPoint(name, out _, out type);
        }

        private static unsafe int GetFormatInfoForMountPoint(string name, out string format, out DriveType type)
        {
            byte* formatBuffer = stackalloc byte[MountPointFormatBufferSizeInBytes];    // format names should be small
            long numericFormat;
            int result = GetFormatInfoForMountPoint(name, formatBuffer, MountPointFormatBufferSizeInBytes, &numericFormat);
            if (result == 0)
            {
                // Check if we have a numeric answer or string
                format = numericFormat != -1 ?
                    Enum.GetName(typeof(UnixFileSystemTypes), numericFormat) ?? string.Empty :
                    Marshal.PtrToStringUTF8((IntPtr)formatBuffer)!;
                type = GetDriveType(format);
            }
            else
            {
                format = string.Empty;
                type = DriveType.Unknown;
            }

            return result;
        }

        /// <summary>Categorizes a file system name into a drive type.</summary>
        /// <param name="fileSystemName">The name to categorize.</param>
        /// <returns>The recognized drive type.</returns>
        private static DriveType GetDriveType(string fileSystemName)
        {
            // This list is based primarily on "man fs", "man mount", "mntent.h", "/proc/filesystems", coreutils "stat.c",
            // and "wiki.debian.org/FileSystem". It can be extended over time as we find additional file systems that should
            // be recognized as a particular drive type.
            // Keep this in sync with the UnixFileSystemTypes enum in Interop.UnixFileSystemTypes.cs
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
                case "bootfs":
                case "bpf_fs":
                case "btrfs":
                case "btrfs_test":
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
                case "lofs":
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
                case "sffs":
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
                case "vagrant":
                case "vboxfs":
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
                case "cgroup2fs":
                case "configfs":
                case "cpuset":
                case "cramfs":
                case "cramfs-wend":
                case "cryptkeeper":
                case "ctfs":
                case "debugfs":
                case "dev":
                case "devfs":
                case "devpts":
                case "devtmpfs":
                case "encfs":
                case "fd":
                case "fdesc":
                case "fuse.gvfsd-fuse":
                case "fusectl":
                case "futexfs":
                case "hugetlbfs":
                case "libpam-encfs":
                case "ibpam-mount":
                case "mntfs":
                case "mqueue":
                case "mtpfs":
                case "mythtvfs":
                case "objfs":
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
                case "sharefs":
                case "sockfs":
                case "sysfs":
                case "tmpfs":
                case "udev":
                case "usbdev":
                case "usbdevfs":
                    return DriveType.Ram;

                case "gphotofs":
                case "sdcardfs":
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
