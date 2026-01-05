// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public DriveType DriveType
        {
            get
            {
                DriveType type;
                Interop.Error error = Interop.Sys.GetDriveTypeForMountPoint(Name, out type);

                // This is one of the few properties that doesn't throw on failure,
                // instead returning a value from the enum.
                switch (error)
                {
                    case Interop.Error.SUCCESS:
                        return type;

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
            get
            {
                string format;
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetFileSystemTypeNameForMountPoint(Name, out format));
                return format;
            }
        }

        public long AvailableFreeSpace
        {
            get
            {
                Interop.Sys.MountPointInformation mpi;
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetSpaceInfoForMountPoint(Name, out mpi));
                return checked((long)mpi.AvailableFreeSpace);
            }
        }

        public long TotalFreeSpace
        {
            get
            {
                Interop.Sys.MountPointInformation mpi;
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetSpaceInfoForMountPoint(Name, out mpi));
                return checked((long)mpi.TotalFreeSpace);
            }
        }

        public long TotalSize
        {
            get
            {
                Interop.Sys.MountPointInformation mpi;
                CheckStatfsResultAndThrowIfNecessary(Interop.Sys.GetSpaceInfoForMountPoint(Name, out mpi));
                return checked((long)mpi.TotalSize);
            }
        }

        private void CheckStatfsResultAndThrowIfNecessary(Interop.Error error)
        {
            if (error != Interop.Error.SUCCESS)
            {
                ThrowForError(error);
            }
        }

        private void CheckStatfsResultAndThrowIfNecessary(int result)
        {
            if (result != 0)
            {
                ThrowForError(Interop.Sys.GetLastError());
            }
        }

        private void ThrowForError(Interop.Error error)
        {
            if (error == Interop.Error.ENOENT)
            {
                throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, Name)); // match Win32
            }
            else
            {
                throw Interop.GetExceptionForIoErrno(error.Info());
            }
        }

        private static string[] GetMountPoints() => Interop.Sys.GetAllMountPoints();
    }
}
