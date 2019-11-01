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

        public unsafe string DriveFormat
        {
            get
            {
                char* fileSystemName = stackalloc char[Interop.Kernel32.MAX_PATH + 1];

                using (DisableMediaInsertionPrompt.Create())
                {
                    if (!Interop.Kernel32.GetVolumeInformation(Name, null, 0, null, null, out int fileSystemFlags, fileSystemName, Interop.Kernel32.MAX_PATH + 1))
                    {
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                    }
                }
                return new string(fileSystemName);
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
        public unsafe string VolumeLabel
        {
            get
            {
                char* volumeName = stackalloc char[Interop.Kernel32.MAX_PATH + 1];

                using (DisableMediaInsertionPrompt.Create())
                {
                    if (!Interop.Kernel32.GetVolumeInformation(Name, volumeName, Interop.Kernel32.MAX_PATH + 1, null, null, out int fileSystemFlags, null, 0))
                    {
                        throw Error.GetExceptionForLastWin32DriveError(Name);
                    }
                }

                return new string(volumeName);
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
