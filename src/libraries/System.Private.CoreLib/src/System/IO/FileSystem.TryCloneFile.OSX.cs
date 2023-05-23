// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.IO
{
    internal static partial class FileSystem
    {
        private static partial bool TryCloneFile(string sourceFullPath, in Interop.Sys.FileStatus srcStat, string destFullPath, bool overwrite)
        {
            // Try to clone the file immediately, this will only succeed if the destination doesn't exist,
            // so we don't worry about locking for this one.
            if (Interop.@libc.clonefile(sourceFullPath, destFullPath, Interop.@libc.CLONE_ACL) == 0)
            {
                // Success
                return true;
            }
            Interop.Error error = Interop.Sys.GetLastError();

            // Try to delete the destination file if we're overwriting.
            if (error == Interop.Error.EEXIST && overwrite)
            {
                // Read FileStatus of destination file to determine how to continue (we need to check that
                // destination doesn't point to the same file as the source file so we can fail appropriately)
                int destError = Interop.Sys.Stat(destFullPath, out Interop.Sys.FileStatus destStat);
                if (destError != 0)
                {
                    // stat failed. If the destination doesn't exist anymore,
                    // try clonefile; otherwise, fall back to a normal copy.
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
                    if (srcStat.Dev != destStat.Dev)
                    {
                        // On different device, so fall back to normal copy
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
                            FileShare.None, FileOptions.None, preallocationSize: 0);
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
                    error = Interop.Sys.GetLastError();
                }
            }

            // Check if it's not supported, or if they're on different filesystems.
            if (error == Interop.Error.ENOTSUP || error == Interop.Error.EXDEV)
            {
                // Fall back to normal copy
                return false;
            }

            // Check we didn't get an error due to 'invalid flags' (which should never happen)
            Debug.Assert(error != Interop.Error.EINVAL);
            if (error == Interop.Error.EINVAL)
            {
                // If we do somehow get here in release, we probably don't want
                // copying to completely fail, so fall back to regular copy.
                return false;
            }

            // Throw the appropriate exception
            Debug.Assert(error != Interop.Error.SUCCESS); // We shouldn't fail with success
            throw Interop.GetExceptionForIoErrno(error.Info());
        }
    }
}
