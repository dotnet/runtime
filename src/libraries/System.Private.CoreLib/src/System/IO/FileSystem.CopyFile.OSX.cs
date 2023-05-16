// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static partial void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // Fail fast for blatantly copying onto self
            if (sourceFullPath == destFullPath)
            {
                throw new IOException(SR.Format(overwrite ? SR.IO_SharingViolation_File : SR.IO_FileExists_Name, destFullPath));
            }

            // Start by locking, creating relevant file handles, and reading out file status info.
            using SafeFileHandle src = SafeFileHandle.OpenReadOnly(sourceFullPath, FileOptions.None, out var fileStatus);
            UnixFileMode filePermissions = SafeFileHandle.GetFileMode(fileStatus);

            // Read FileStatus of destination file to determine how to continue
            int destError = Interop.Sys.Stat(destFullPath, out var destStat);

            // Interpret the error from stat
            if (destError != 0)
            {
                // Some error, let's see what it is:
                Interop.Error error = Interop.Sys.GetLastError();

                // Destination not existing is expected, so if this is the case we try clonefile,
                // otherwise we got some other error that the fallback code can deal with.
                if (error == Interop.Error.ENOENT)
                {
                    goto tryCloneFile;
                }
                else
                {
                    goto tryFallback;
                }
            }
            else
            {
                // Destination exists:
                if (!overwrite)
                {
                    // Throw an error if we're not overriding
                    throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EEXIST));
                }
                if (fileStatus.Dev != destStat.Dev)
                {
                    // On different device
                    goto tryFallback;
                }
                if (fileStatus.Ino == destStat.Ino)
                {
                    // Copying onto itself
                    throw new IOException(SR.Format(SR.IO_SharingViolation_File, destFullPath));
                }
            }

            // Try deleting destination:
            tryDelete:
            {
                // Delete the destination. This should fail on directories. And update the mode.
                // Get a lock to the dest file for compat reasons, and then delete it.
                using SafeFileHandle? dstHandle = OpenCopyFileDstHandle(destFullPath, true, filePermissions, false);
                File.Delete(destFullPath);
            }

            // Try clonefile:
            tryCloneFile:
            {
                if (Interop.@libc.clonefile(sourceFullPath, destFullPath, Interop.@libc.CLONE_ACL) == 0)
                {
                    // Success
                    return;
                }

                // Throw if the file already exists and we're not overwriting
                if (Interop.Sys.GetLastError() == Interop.Error.EEXIST && !overwrite)
                {
                    throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EEXIST));
                }
            }

            // Try fallback:
            tryFallback:
            {
                // Open the dst handle
                // ! because OpenCopyFileDstHandle doesn't return null when openNewFile is true
                using SafeFileHandle dst = OpenCopyFileDstHandle(destFullPath, overwrite, filePermissions, openNewFile: true)!;

                // Copy the file using the standard unix implementation.
                // Note: this code needs to be kept in sync with the code in FileSystem.CopyFile.OtherUnix.cs.
                Interop.CheckIo(Interop.Sys.CopyFile(src, dst, fileStatus.Size));
            }
        }
    }
}
