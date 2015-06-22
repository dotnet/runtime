// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
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
                Interop.libc.statfs data = Interop.libc.GetStatFsForDriveName(Name);
                return GetDriveType(Interop.libc.GetMountPointFsType(data));
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
                return (long)(data.f_bsize * data.f_bfree);
            }
        }

        public long TotalSize
        {
            [SecuritySafeCritical]
            get
            {
                Interop.libc.statfs data = Interop.libc.GetStatFsForDriveName(Name);
                return (long)(data.f_bsize * data.f_blocks);
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

        public static unsafe DriveInfo[] GetDrives()
        {
            DriveInfo[] drives = null;
            Interop.libc.statfs* pBuffer = null;
            int count = Interop.libc.getmntinfo(&pBuffer, 0);
            if (count > 0)
            {
                drives = new DriveInfo[count];
                for (int i = 0; i < count; i++)
                {
                    String mountPoint = Marshal.PtrToStringAnsi((IntPtr)pBuffer[i].f_mntonname);
                    drives[i] = new DriveInfo(mountPoint);
                }
            }

            return drives;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
