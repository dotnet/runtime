// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        private static string NormalizeDriveName(string driveName)
        {
            if (driveName.Contains("\0"))
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidDriveChars, driveName), "driveName");
            }
            if (driveName.Length == 0)
            {
                throw new ArgumentException(SR.Arg_MustBeNonEmptyDriveName, "driveName");
            }
            return driveName;
        }

        public DriveType DriveType
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.statfs data;
                Interop.ErrorInfo errorInfo;
                if (Interop.libc.TryGetStatFsForDriveName(Name, out data, out errorInfo))
                {
                    return GetDriveType(Interop.libc.GetMountPointFsType(data));
                }

                // This is one of the few properties that doesn't throw on failure,
                // instead returning a value from the enum.
                switch (errorInfo.Error)
                {
                    case Interop.Error.ELOOP:
                    case Interop.Error.ENAMETOOLONG:
                    case Interop.Error.ENOENT:
                    case Interop.Error.ENOTDIR:
                        return DriveType.NoRootDirectory;
                    default:
                        return DriveType.Unknown;
                }
            }
        }

        public string DriveFormat
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.statfs data = Interop.libc.GetStatFsForDriveName(Name);
                return Interop.libc.GetMountPointFsType(data);
            }
        }

        public long AvailableFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.statfs data = Interop.libc.GetStatFsForDriveName(Name);
                return (long)data.f_bsize * (long)data.f_bavail;
            }
        }

        public long TotalFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.statfs data = Interop.libc.GetStatFsForDriveName(Name);
                return (long)data.f_bsize * (long)data.f_bfree;
            }
        }

        public long TotalSize
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.statfs data = Interop.libc.GetStatFsForDriveName(Name);
                return (long)data.f_bsize * (long)data.f_blocks;
            }
        }

        public String VolumeLabel
        {
            [SecuritySafeCritical]
            get
            {
                return Name;
            }
            [SecuritySafeCritical]
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Categorizes a file system name into a drive type.</summary>
        /// <param name="fileSystemName">The name to categorize.</param>
        /// <returns>The recognized drive type.</returns>
        private static DriveType GetDriveType(string fileSystemName)
        {
            // This list is based primarily on "man fs", "man mount", "mntent.h", "/proc/filesystems",
            // and "wiki.debian.org/FileSystem". It can be extended over time as we 
            // find additional file systems that should be recognized as a particular drive type.
            switch (fileSystemName)
            {
                case "iso":
                case "isofs":
                case "iso9660":
                case "fuseiso":
                case "fuseiso9660":
                case "umview-mod-umfuseiso9660":
                    return DriveType.CDRom;

                case "adfs":
                case "affs":
                case "befs":
                case "bfs":
                case "btrfs":
                case "ecryptfs":
                case "efs":
                case "ext":
                case "ext2":
                case "ext2_old":
                case "ext3":
                case "ext4":
                case "ext4dev":
                case "fat":
                case "fuseblk":
                case "fuseext2":
                case "fusefat":
                case "hfs":
                case "hfsplus":
                case "hpfs":
                case "jbd":
                case "jbd2":
                case "jfs":
                case "jffs":
                case "jffs2":
                case "minix":
                case "minix_old":
                case "minix2":
                case "minix2v2":
                case "msdos":
                case "ocfs2":
                case "omfs":
                case "openprom":
                case "ntfs":
                case "qnx4":
                case "reiserfs":
                case "squashfs":
                case "swap":
                case "sysv":
                case "ubifs":
                case "udf":
                case "ufs":
                case "umsdos":
                case "umview-mod-umfuseext2":
                case "xenix":
                case "xfs":
                case "xiafs":
                case "xmount":
                case "zfs-fuse":
                    return DriveType.Fixed;

                case "9p":
                case "autofs":
                case "autofs4":
                case "beaglefs":
                case "cifs":
                case "coda":
                case "coherent":
                case "curlftpfs":
                case "davfs2":
                case "dlm":
                case "flickrfs":
                case "fusedav":
                case "fusesmb":
                case "gfs2":
                case "glusterfs-client":
                case "gmailfs":
                case "kafs":
                case "ltspfs":
                case "ncpfs":
                case "nfs":
                case "nfs4":
                case "obexfs":
                case "s3ql":
                case "smb":
                case "smbfs":
                case "sshfs":
                case "sysfs":
                case "sysv2":
                case "sysv4":
                case "vxfs":
                case "wikipediafs":
                    return DriveType.Network;

                case "anon_inodefs":
                case "aptfs":
                case "avfs":
                case "bdev":
                case "binfmt_misc":
                case "cgroup":
                case "configfs":
                case "cramfs":
                case "cryptkeeper":
                case "cpuset":
                case "debugfs":
                case "devfs":
                case "devpts":
                case "devtmpfs":
                case "encfs":
                case "fuse":
                case "fuse.gvfsd-fuse":
                case "fusectl":
                case "hugetlbfs":
                case "libpam-encfs":
                case "ibpam-mount":
                case "mtpfs":
                case "mythtvfs":
                case "mqueue":
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
                case "sockfs":
                case "tmpfs":
                    return DriveType.Ram;

                case "gphotofs":
                case "usbfs":
                case "usbdevice":
                case "vfat":
                    return DriveType.Removable;

                case "aufs": // marking all unions as unknown
                case "funionfs":
                case "unionfs-fuse":
                case "mhddfs":
                default:
                    return DriveType.Unknown;
            }
        }
    }
}
