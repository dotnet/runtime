// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static partial class FileSystem
    {
        private static partial bool TryCloneFile(string sourceFullPath, in Interop.Sys.FileStatus srcStat, string destFullPath, bool overwrite)
        {
            //get the file permissions for source file
            UnixFileMode filePermissions = srcStat.Mode & SafeFileHandle.PermissionMask;

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
                    return false;
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
                if (srcStat.Dev != destStat.Dev)
                {
                    // On different device
                    return false;
                }
                if (srcStat.Ino == destStat.Ino)
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
                    return true;
                }

                // Clonefile fails if the destination exists (which we try to avoid), so throw if error is
                // the destination exists and we're not overwriting, otherwise we will go to fallback path.
                if (Interop.Sys.GetLastError() == Interop.Error.EEXIST && !overwrite)
                {
                    throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EEXIST));
                }
            }

            // Otherwise we want to go the the fallback
            return false;
        }
    }
}
