// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        private string _fileSystemType;
        private string _fileSystemName;

        private DriveInfo(string mountPath, string fileSystemType, string fileSystemName) : this(mountPath)
        {
            Debug.Assert(fileSystemType != null);
            Debug.Assert(fileSystemName != null);
            _fileSystemType = fileSystemType;
            _fileSystemName = fileSystemName;
        }

        private static string NormalizeDriveName(string driveName)
        {
            if (driveName.Contains("\0"))
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidDriveChars, driveName), "driveName");
            }
            if (driveName.Length == 0)
            {
                throw new ArgumentException(SR.Arg_MustBeNonEmptyDriveName);
            }
            return driveName;
        }

        public DriveType DriveType
        {
            [SecuritySafeCritical]
            get { return GetDriveType(DriveFormat); }
        }

        public string DriveFormat
        {
            [SecuritySafeCritical]
            get 
            {
                if (_fileSystemType == null) 
                {
                    // If the DriveInfo was created with the public ctor, we need to
                    // enumerate all of the known drives to find its associated information.
                    LazyPopulateFileSystemTypeAndName();
                }
                return _fileSystemType;
            }
        }

        public long AvailableFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                Interop.structStatvfs stats = GetStats();
                return (long)((ulong)stats.f_bsize * (ulong)stats.f_bavail);
            }
        }

        public long TotalFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                Interop.structStatvfs stats = GetStats();
                return (long)((ulong)stats.f_bsize * (ulong)stats.f_bfree);
            }
        }

        public long TotalSize
        {
            [SecuritySafeCritical]
            get
            {
                Interop.structStatvfs stats = GetStats();
                return (long)((ulong)stats.f_bsize * (ulong)stats.f_blocks);
            }
        }

        public String VolumeLabel
        {
            [SecuritySafeCritical]
            get { return _fileSystemName; }
            [SecuritySafeCritical]
            set { throw new PlatformNotSupportedException(); }
        }

        /// <summary>Loads file system stats for the mounted path.</summary>
        /// <returns>The loaded stats.</returns>
        private Interop.structStatvfs GetStats()
        {
            Interop.structStatvfs stats;
            if (Interop.statvfs(Name, out stats) != 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == (int)Interop.Errors.ENOENT)
                {
                    throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, Name)); // match Win32 exception
                }
                throw Interop.GetExceptionForIoErrno(errno, Name, isDirectory: true);
            }
            return stats;
        }

        /// <summary>Lazily populates _fileSystemType and _fileSystemName if they weren't already filled in.</summary>
        private void LazyPopulateFileSystemTypeAndName()
        {
            Debug.Assert(_fileSystemName == null && _fileSystemType == null);
            foreach (DriveInfo drive in GetDrives()) // fill in the info by enumerating drives and copying over the associated data
            {
                if (drive.Name == this.Name)
                {
                    _fileSystemType = drive._fileSystemType;
                    _fileSystemName = drive._fileSystemName;
                    return;
                }
            }
            _fileSystemType = string.Empty;
            _fileSystemName = string.Empty;
        }

        public static unsafe DriveInfo[] GetDrives()
        {
            const int StringBufferLength = 8192; // there's no defined max size nor an indication through the API when you supply too small a buffer; choosing something that seems reasonable
            byte* strBuf = stackalloc byte[StringBufferLength];

            // Parse the mounts file
            const string MountsPath = "/proc/mounts"; // Linux mounts file
            IntPtr fp;
            Interop.CheckIoPtr(fp = Interop.setmntent(MountsPath, Interop.MNTOPT_RO), path: MountsPath);
            try
            {
                // Walk the entries in the mounts file, creating a DriveInfo for each that shouldn't be ignored.
                List<DriveInfo> drives = new List<DriveInfo>();
                Interop.mntent mntent = default(Interop.mntent);
                while (Interop.getmntent_r(fp, ref mntent, strBuf, StringBufferLength) != IntPtr.Zero)
                {
                    string type = DecodeString(mntent.mnt_type);
                    if (!string.IsNullOrWhiteSpace(type) && type != Interop.MNTTYPE_IGNORE)
                    {
                        string path = DecodeString(mntent.mnt_dir);
                        string name = DecodeString(mntent.mnt_fsname);
                        drives.Add(new DriveInfo(path, type, name));
                    }
                }
                return drives.ToArray();
            }
            finally
            {
                int result = Interop.endmntent(fp);
                Debug.Assert(result == 1); // documented to always return 1
            }
        }

        /// <summary>Gets the string starting at the specifying pointer and going until null termination.</summary>
        /// <param name="str">Pointer to the first byte in the string.</param>
        /// <returns>The decoded string.</returns>
        private static unsafe string DecodeString(byte* str)
        {
            Debug.Assert(str != null);
            if (str == null)
            {
                return string.Empty;
            }
            int length = GetNullTerminatedStringLength(str);
            return Encoding.UTF8.GetString(str, length); // TODO: determine correct encoding; UTF8 is good enough for now
        }

        /// <summary>Gets the length of the null-terminated string by searching for its null teminration.</summary>
        /// <param name="str">The string.</param>
        /// <returns>The string's length, not including the null termination.</returns>
        private static unsafe int GetNullTerminatedStringLength(byte* str)
        {
            int length = 0;
            while (*str != '\0')
            {
                length++;
                str++;
            }
            return length;
        }

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
                case "msdos":
                case "ocfs2":
                case "omfs":
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
