// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public static DriveInfo[] GetDrives()
        {
            string[] mountPoints = Interop.Sys.GetAllMountPoints();
            DriveInfo[] info = new DriveInfo[mountPoints.Length];
            for (int i = 0; i < info.Length; i++)
            {
                info[i] = new DriveInfo(mountPoints[i]);
            }

            return info;
        }

        private static string NormalizeDriveName(string driveName)
        {
            if (driveName.Contains("\0")) // string.Contains(char) is .NetCore2.1+ specific
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidDriveChars, driveName), nameof(driveName));
            }
            if (driveName.Length == 0)
            {
                throw new ArgumentException(SR.Arg_MustBeNonEmptyDriveName, nameof(driveName));
            }
            return driveName;
        }

        public DriveType DriveType
        {
            get
            {
                DriveType type;
                int result = Interop.Sys.GetFormatInfoForMountPoint(Name, out type);
                if (result == 0)
                {
                    return type;
                }
                else
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

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
        }

        public string DriveFormat
        {
            get
            {
                string format = string.Empty;
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetFormatInfoForMountPoint(Name, out format));
                return format;
            }
        }

        public long AvailableFreeSpace
        {
            get
            {
                Interop.Sys.MountPointInformation mpi = default(Interop.Sys.MountPointInformation);
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetSpaceInfoForMountPoint(Name, out mpi));
                return checked((long)mpi.AvailableFreeSpace);
            }
        }

        public long TotalFreeSpace
        {
            get
            {
                Interop.Sys.MountPointInformation mpi = default(Interop.Sys.MountPointInformation);
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetSpaceInfoForMountPoint(Name, out mpi));
                return checked((long)mpi.TotalFreeSpace);
            } 
        }

        public long TotalSize
        {
            get
            {
                Interop.Sys.MountPointInformation mpi = default(Interop.Sys.MountPointInformation);
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetSpaceInfoForMountPoint(Name, out mpi));
                return checked((long)mpi.TotalSize);
            }
        }

        public string VolumeLabel
        {
            get
            {
                return Name;
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private void CheckStatfsResultAndThrowIfNecessary(int result)
        {
            if (result != 0)
            {
                var errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.ENOENT)
                {
                    throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, Name)); // match Win32
                }
                else
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, isDirectory: true);
                }
            }
        }
    }
}
