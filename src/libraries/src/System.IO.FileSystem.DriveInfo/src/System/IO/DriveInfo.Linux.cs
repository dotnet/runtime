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
            get { return GetDriveType(DriveFormat); }
        }

        public string DriveFormat
        {
            [SecuritySafeCritical]
            get 
            {
                EnsureFileSystemTypeAndName();
                return _fileSystemType;
            }
        }

        public long AvailableFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.structStatvfs stats = GetStats();
                return (long)((ulong)stats.f_bsize * (ulong)stats.f_bavail);
            }
        }

        public long TotalFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.structStatvfs stats = GetStats();
                return (long)((ulong)stats.f_bsize * (ulong)stats.f_bfree);
            }
        }

        public long TotalSize
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.structStatvfs stats = GetStats();
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

        public static unsafe DriveInfo[] GetDrives()
        {
            const int StringBufferLength = 8192; // there's no defined max size nor an indication through the API when you supply too small a buffer; choosing something that seems reasonable
            byte* strBuf = stackalloc byte[StringBufferLength];

            // Parse the mounts file
            const string MountsPath = "/proc/mounts"; // Linux mounts file
            IntPtr fp;
            Interop.CheckIoPtr(fp = Interop.libc.setmntent(MountsPath, Interop.libc.MNTOPT_RO), path: MountsPath);
            try
            {
                // Walk the entries in the mounts file, creating a DriveInfo for each that shouldn't be ignored.
                List<DriveInfo> drives = new List<DriveInfo>();
                Interop.libc.mntent mntent = default(Interop.libc.mntent);
                while (Interop.libc.getmntent_r(fp, ref mntent, strBuf, StringBufferLength) != IntPtr.Zero)
                {
                    string type = DecodeString(mntent.mnt_type);
                    if (!string.IsNullOrWhiteSpace(type) && type != Interop.libc.MNTTYPE_IGNORE)
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
                int result = Interop.libc.endmntent(fp);
                Debug.Assert(result == 1); // documented to always return 1
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private string _fileSystemType;
        private string _fileSystemName;

        private DriveInfo(string mountPath, string fileSystemType, string fileSystemName) : this(mountPath)
        {
            Debug.Assert(fileSystemType != null);
            Debug.Assert(fileSystemName != null);
            _fileSystemType = fileSystemType;
            _fileSystemName = fileSystemName;
        }

        /// <summary>Lazily populates _fileSystemType and _fileSystemName if they weren't already filled in.</summary>
        private void EnsureFileSystemTypeAndName()
        {
            if (_fileSystemName == null || _fileSystemType == null)
            {
                foreach (DriveInfo drive in GetDrives()) // fill in the info by enumerating drives and copying over the associated data
                {
                    if (drive.Name == this.Name)
                    {
                        _fileSystemType = drive._fileSystemType;
                        _fileSystemName = drive._fileSystemName;
                        return;
                    }
                }
                throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, Name));
            }
        }

        /// <summary>Loads file system stats for the mounted path.</summary>
        /// <returns>The loaded stats.</returns>
        private Interop.libc.structStatvfs GetStats()
        {
            EnsureFileSystemTypeAndName();

            Interop.libc.structStatvfs stats;
            if (Interop.libc.statvfs(Name, out stats) != 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == Interop.Errors.ENOENT)
                {
                    throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, Name)); // match Win32 exception
                }
                throw Interop.GetExceptionForIoErrno(errno, Name, isDirectory: true);
            }
            return stats;
        }

        /// <summary>Gets the string starting at the specifying pointer and going until null termination.</summary>
        /// <param name="str">Pointer to the first byte in the string.</param>
        /// <returns>The decoded string.</returns>
        private static unsafe string DecodeString(byte* str)
        {
            return str != null ? Marshal.PtrToStringAnsi((IntPtr)str) : null;
        }
    }
}
