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
                if (!File.Exists(sourceFullPath)) throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, sourceFullPath), sourceFullPath);
                throw new IOException(SR.Format(overwrite ? SR.IO_SharingViolation_File : SR.IO_FileExists_Name, destFullPath));
            }

            // Start by locking, creating relevant file handles, and reading out file status info.
            using SafeFileHandle src = SafeFileHandle.OpenReadOnly(sourceFullPath, FileOptions.None, out Interop.Sys.FileStatus fileStatus);
            UnixFileMode filePermissions = SafeFileHandle.GetFileMode(fileStatus);

            // Read FileStatus of destination file to determine how to continue
            int destError = Interop.Sys.Stat(destFullPath, out Interop.Sys.FileStatus destStat);
            if (destError != 0)
            {
                // stat failed. If the destination doesn't exist (which is the expected
                // cause), try clonefile; otherwise, fall back to a normal copy.
                if (Interop.Sys.GetLastError() == Interop.Error.ENOENT)
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
            {
                // Delete the destination. This should fail on directories.
                // Get a lock to the dest file to ensure we don't copy onto it when it's locked by something else, and then delete it.
                try
                {
                    using SafeFileHandle? dstHandle = SafeFileHandle.Open(destFullPath, FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, FileOptions.None, preallocationSize: 0, filePermissions);
                    File.Delete(destFullPath);
                }
                catch (FileNotFoundException)
                {
                    // We don't want to throw if it's just the file not existing, since we're trying to delete it.
                }
            }

            // Try clonefile:
            tryCloneFile:
            {
                if (Interop.@libc.clonefile(sourceFullPath, destFullPath, Interop.@libc.CLONE_ACL) == 0)
                {
                    // Success
                    return;
                }

                // Clonefile fails if the destination exists (which we try to avoid), so throw if error is
                // the destination exists and we're not overwriting, otherwise we will go to fallback path.
                if (Interop.Sys.GetLastError() == Interop.Error.EEXIST && !overwrite)
                {
                    throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EEXIST));
                }
            }

            // Try fallback:
            tryFallback:
            {
                // Open the dst handle
                using SafeFileHandle dst = SafeFileHandle.Open(destFullPath, overwrite ? FileMode.Create : FileMode.CreateNew,
                    FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize: 0, unixCreateMode: filePermissions,
                    CreateOpenException);

                // Exception handler for SafeFileHandle.Open failing.
                static Exception? CreateOpenException(Interop.ErrorInfo error, Interop.Sys.OpenFlags flags, string path)
                {
                    // If the destination path points to a directory, we throw to match Windows behaviour.
                    if (error.Error == Interop.Error.EEXIST && DirectoryExists(path))
                    {
                        return new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, path));
                    }

                    return null; // Let SafeFileHandle create the exception for this error.
                }

                // Copy the file using the standard unix implementation.
                Interop.CheckIo(Interop.Sys.CopyFile(src, dst, fileStatus.Size));
            }
        }
    }
}
