// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Exposes routines for exploring a drive.
**
**
===========================================================*/

using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO
{
    // Matches Win32's DRIVE_XXX #defines from winbase.h
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum DriveType 
    {
        Unknown = 0,
        NoRootDirectory = 1,
        Removable = 2,
        Fixed = 3,
        Network = 4,
        CDRom = 5,
        Ram = 6
    }

    // Ideally we'll get a better security permission, but possibly
    // not for Whidbey.
    [Serializable]
    [ComVisible(true)]
    public sealed class DriveInfo : ISerializable
    {
        private String _name;

        private const String NameField = "_name";  // For serialization

        [System.Security.SecuritySafeCritical]  // auto-generated
        public DriveInfo(String driveName) 
        {
            if (driveName == null)
                throw new ArgumentNullException("driveName");
            Contract.EndContractBlock();
            if (driveName.Length == 1)
                _name = driveName + ":\\";
            else {
                // GetPathRoot does not check all invalid characters
                Path.CheckInvalidPathChars(driveName); 
                _name = Path.GetPathRoot(driveName);
                // Disallow null or empty drive letters and UNC paths
                if (_name == null || _name.Length == 0 || _name.StartsWith("\\\\", StringComparison.Ordinal))
                    throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDriveLetterOrRootDir"));
            }
            // We want to normalize to have a trailing backslash so we don't have two equivalent forms and
            // because some Win32 API don't work without it.
            if (_name.Length == 2 && _name[1] == ':') {
                _name = _name + "\\";
            }
            
            // Now verify that the drive letter could be a real drive name.
            // On Windows this means it's between A and Z, ignoring case.
            // On a Unix platform, perhaps this should be a device name with
            // a partition like /dev/hdc0, or possibly a mount point.
            char letter = driveName[0];
            if (!((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDriveLetterOrRootDir"));

            // Now do a security check.
            String demandPath = _name + '.';
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
        }

        [System.Security.SecurityCritical]  // auto-generated
        private DriveInfo(SerializationInfo info, StreamingContext context)
        {
            // Need to add in a security check here once it has been spec'ed.
            _name = (String) info.GetValue(NameField, typeof(String));

            // Now do a security check.
            String demandPath = _name + '.';
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
        }

        public String Name {
            get { return _name; }
        }

        public DriveType DriveType {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { 
                // GetDriveType can't fail
                return (DriveType) Win32Native.GetDriveType(Name);
            }
        }

        public String DriveFormat {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;

                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool r = Win32Native.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r) {
                        int errorCode = Marshal.GetLastWin32Error();
                        __Error.WinIODriveError(Name, errorCode);
                    }
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }
                return fileSystemName.ToString();
            }
        }

        public bool IsReady {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return Directory.InternalExists(Name);
            }
        }

        public long AvailableFreeSpace {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { 
                long userBytes, totalBytes, freeBytes;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool r = Win32Native.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        __Error.WinIODriveError(Name);
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }
                return userBytes;
            }
        }

        public long TotalFreeSpace {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { 
                long userBytes, totalBytes, freeBytes;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool r = Win32Native.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        __Error.WinIODriveError(Name);
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }
                return freeBytes;
            }
        }

        public long TotalSize {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { 
                // Don't cache this, to handle variable sized floppy drives
                // or other various removable media drives.
                long userBytes, totalBytes, freeBytes;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool r = Win32Native.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        __Error.WinIODriveError(Name);
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }
                return totalBytes;
            }
        }

        public static DriveInfo[] GetDrives()
        {
            // Directory.GetLogicalDrives demands unmanaged code permission
            String[] drives = Directory.GetLogicalDrives();
            DriveInfo[] di = new DriveInfo[drives.Length];
            for(int i=0; i<drives.Length; i++)
                di[i] = new DriveInfo(drives[i]);
            return di;
        }

        public DirectoryInfo RootDirectory {
            get {
                return new DirectoryInfo(Name);
            }
        }

        // Null is a valid volume label.
        public String VolumeLabel {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // NTFS uses a limit of 32 characters for the volume label,
                // as of Windows Server 2003.
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;
                
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool r = Win32Native.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r) {
                        int errorCode = Marshal.GetLastWin32Error();
                        // Win9x appears to return ERROR_INVALID_DATA when a
                        // drive doesn't exist.
                        if (errorCode == Win32Native.ERROR_INVALID_DATA)
                            errorCode = Win32Native.ERROR_INVALID_DRIVE;
                        __Error.WinIODriveError(Name, errorCode);
                    }
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }
                return volumeName.ToString();
            }
            [System.Security.SecuritySafeCritical]  // auto-generated
            set {
                String demandPath = _name + '.';
                new FileIOPermission(FileIOPermissionAccess.Write, demandPath).Demand();

                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool r = Win32Native.SetVolumeLabel(Name, value);
                    if (!r) {
                        int errorCode = Marshal.GetLastWin32Error();
                        // Provide better message
                        if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                            throw new UnauthorizedAccessException(Environment.GetResourceString("InvalidOperation_SetVolumeLabelFailed"));
                        __Error.WinIODriveError(Name, errorCode);
                    }
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }
            }
        }

        public override String ToString()
        {
            return Name;
        }

#if FEATURE_SERIALIZATION
        /// <internalonly/>
        [System.Security.SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // No need for an additional security check - everything is public.
            info.AddValue(NameField, _name, typeof(String));
        }
#endif

    }
}
