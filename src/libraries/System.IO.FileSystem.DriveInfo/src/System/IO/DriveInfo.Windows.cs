// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        private static string NormalizeDriveName(string driveName)
        {
            if (driveName == null)
                throw new ArgumentNullException("driveName");
            Contract.EndContractBlock();

            string name;

            if (driveName.Length == 1)
                name = driveName + ":\\";
            else
            {
                // GetPathRoot does not check all invalid characters
                if (PathInternal.HasIllegalCharacters(driveName))
                    throw new ArgumentException(SR.Format(SR.Arg_InvalidDriveChars, driveName), "driveName");

                name = Path.GetPathRoot(driveName);
                // Disallow null or empty drive letters and UNC paths
                if (name == null || name.Length == 0 || name.StartsWith("\\\\", StringComparison.Ordinal))
                    throw new ArgumentException(SR.Arg_MustBeDriveLetterOrRootDir);
            }
            // We want to normalize to have a trailing backslash so we don't have two equivalent forms and
            // because some Win32 API don't work without it.
            if (name.Length == 2 && name[1] == ':')
            {
                name = name + "\\";
            }

            // Now verify that the drive letter could be a real drive name.
            // On Windows this means it's between A and Z, ignoring case.
            char letter = driveName[0];
            if (!((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')))
                throw new ArgumentException(SR.Arg_MustBeDriveLetterOrRootDir);

            return name;
        }

        public DriveType DriveType
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                // GetDriveType can't fail
                return (DriveType)Interop.mincore.GetDriveType(Name);
            }
        }

        public String DriveFormat
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;

                uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.mincore.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r)
                    {
                        throw __Error.GetExceptionForLastWin32DriveError(Name);
                    }
                }
                finally
                {
                    Interop.mincore.SetErrorMode(oldMode);
                }
                return fileSystemName.ToString();
            }
        }

        public long AvailableFreeSpace
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                long userBytes, totalBytes, freeBytes;
                uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.mincore.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw __Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.mincore.SetErrorMode(oldMode);
                }
                return userBytes;
            }
        }

        public long TotalFreeSpace
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                long userBytes, totalBytes, freeBytes;
                uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.mincore.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw __Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.mincore.SetErrorMode(oldMode);
                }
                return freeBytes;
            }
        }

        public long TotalSize
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                // Don't cache this, to handle variable sized floppy drives
                // or other various removable media drives.
                long userBytes, totalBytes, freeBytes;
                uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.mincore.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw __Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.mincore.SetErrorMode(oldMode);
                }
                return totalBytes;
            }
        }

        public static DriveInfo[] GetDrives()
        {
            int drives = Interop.mincore.GetLogicalDrives();
            if (drives == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();

            // GetLogicalDrives returns a bitmask starting from 
            // position 0 "A" indicating whether a drive is present.
            // Loop over each bit, creating a DriveInfo for each one
            // that is set.

            uint d = (uint)drives;
            int count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0) count++;
                d >>= 1;
            }

            DriveInfo[] result = new DriveInfo[count];
            char[] root = new char[] { 'A', ':', '\\' };
            d = (uint)drives;
            count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                {
                    result[count++] = new DriveInfo(new String(root));
                }
                d >>= 1;
                root[0]++;
            }
            return result;
        }

        // Null is a valid volume label.
        public String VolumeLabel
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                // NTFS uses a limit of 32 characters for the volume label,
                // as of Windows Server 2003.
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;

                uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.mincore.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        // Win9x appears to return ERROR_INVALID_DATA when a
                        // drive doesn't exist.
                        if (errorCode == Interop.ERROR_INVALID_DATA)
                            errorCode = Interop.ERROR_INVALID_DRIVE;
                        throw __Error.GetExceptionForWin32DriveError(errorCode, Name);
                    }
                }
                finally
                {
                    Interop.mincore.SetErrorMode(oldMode);
                }
                return volumeName.ToString();
            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            set
            {
                uint oldMode = Interop.mincore.SetErrorMode(Interop.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.mincore.SetVolumeLabel(Name, value);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        // Provide better message
                        if (errorCode == Interop.ERROR_ACCESS_DENIED)
                            throw new UnauthorizedAccessException(SR.InvalidOperation_SetVolumeLabelFailed);
                        throw __Error.GetExceptionForWin32DriveError(errorCode, Name);
                    }
                }
                finally
                {
                    Interop.mincore.SetErrorMode(oldMode);
                }
            }
        }
    }
}
