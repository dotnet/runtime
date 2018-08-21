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
            return DriveInfoInternal.NormalizeDriveName(driveName);
        }

        public DriveType DriveType
        {
            get
            {
                // GetDriveType can't fail
                return (DriveType)Interop.Kernel32.GetDriveType(Name);
            }
        }

        public string DriveFormat
        {
            get
            {
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;

                uint oldMode;
                bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
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
                    if (success)
                        Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
                }
                return fileSystemName.ToString();
            }
        }

        public long AvailableFreeSpace
        {
            get
            {
                long userBytes, totalBytes, freeBytes;
                uint oldMode;
                bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
                try
                {
                    bool r = Interop.Kernel32.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    if (success)
                        Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
                }
                return userBytes;
            }
        }

        public long TotalFreeSpace
        {
            get
            {
                long userBytes, totalBytes, freeBytes;
                uint oldMode;
                bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
                try
                {
                    bool r = Interop.Kernel32.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    if (success)
                        Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
                }
                return freeBytes;
            }
        }

        public long TotalSize
        {
            get
            {
                // Don't cache this, to handle variable sized floppy drives
                // or other various removable media drives.
                long userBytes, totalBytes, freeBytes;
                uint oldMode;
                bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
                try
                {
                    bool r = Interop.Kernel32.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                }
                finally
                {
                    Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
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
        public string VolumeLabel
        {
            get
            {
                // NTFS uses a limit of 32 characters for the volume label,
                // as of Windows Server 2003.
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;

                uint oldMode;
                bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
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
                    if (success)
                        Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
                }
                return volumeName.ToString();
            }
            set
            {
                uint oldMode;
                bool success = Interop.Kernel32.SetThreadErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS, out oldMode);
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
                    if (success)
                        Interop.Kernel32.SetThreadErrorMode(oldMode, out oldMode);
                }
            }
        }
    }
}
