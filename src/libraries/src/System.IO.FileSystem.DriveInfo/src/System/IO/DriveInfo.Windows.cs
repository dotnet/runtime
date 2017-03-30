// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        private static string NormalizeDriveName(string driveName)
        {
            Debug.Assert(driveName != null);

            string name;

            if (driveName.Length == 1)
                name = driveName + ":\\";
            else
            {
                // GetPathRoot does not check all invalid characters
                if (PathInternal.HasIllegalCharacters(driveName))
                    throw new ArgumentException(SR.Format(SR.Arg_InvalidDriveChars, driveName), nameof(driveName));

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
                return (DriveType)Interop.Kernel32.GetDriveType(Name);
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

                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.Kernel32.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r)
                    {
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                    }
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
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
                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.Kernel32.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
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
                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.Kernel32.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
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
                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.Kernel32.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
                }
                return totalBytes;
            }
        }

        public static DriveInfo[] GetDrives()
        {
            string[] drives = DriveInfoInternal.GetLogicalDrives();
            DriveInfo[] result = new DriveInfo[drives.Length];
            for (int i = 0; i < drives.Length; i++)
            {
                result[i] = new DriveInfo(drives[i]);
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

                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.Kernel32.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        // Win9x appears to return ERROR_INVALID_DATA when a
                        // drive doesn't exist.
                        if (errorCode == Interop.Errors.ERROR_INVALID_DATA)
                            errorCode = Interop.Errors.ERROR_INVALID_DRIVE;
                        throw Error.GetExceptionForWin32DriveError(errorCode, Name);
                    }
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
                }
                return volumeName.ToString();
            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            set
            {
                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Interop.Kernel32.SetVolumeLabel(Name, value);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        // Provide better message
                        if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
                            throw new UnauthorizedAccessException(SR.InvalidOperation_SetVolumeLabelFailed);
                        throw Error.GetExceptionForWin32DriveError(errorCode, Name);
                    }
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
                }
            }
        }
    }
}
