// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.IO
{
    internal static partial class FileSystem
    {
        private static bool TryCloneFile(string sourceFullPath, string destFullPath, bool overwrite, Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?> createOpenException)
        {
            // This helper function calls out to clonefile, and returns the error.
            static bool TryCloneFile(string sourceFullPath, string destFullPath, int flags, out Interop.Error error)
            {
                if (Interop.@libc.clonefile(sourceFullPath, destFullPath, flags) == 0)
                {
                    // Success.
                    error = default;
                    return true;
                }
                error = Interop.Sys.GetLastError();
                return false;
            }

            // Try to clone the file immediately, this will only succeed if the
            // destination doesn't exist, so we don't worry about locking for this one.
            int flags = Interop.@libc.CLONE_ACL;
            Interop.Error error;
            if (TryCloneFile(sourceFullPath, destFullPath, flags, out error)) return true;

            // Some filesystems don't support ACLs, so may fail due to trying to copy ACLs.
            // This will disable them and allow trying again (a maximum of 1 time).
            if (error == Interop.Error.EINVAL)
            {
                flags = 0;
                if (TryCloneFile(sourceFullPath, destFullPath, flags, out error)) return true;
            }

            // Try to delete the destination file if we're overwriting.
            if (error == Interop.Error.EEXIST && overwrite)
            {
                // Delete the destination. This should fail on directories. Get a lock to the dest file to ensure we don't copy onto it when
                // it's locked by something else, and then delete it. It should also fail if destination == source since it's already locked.
                try
                {
                    using SafeFileHandle? dstHandle = SafeFileHandle.Open(destFullPath, FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, FileOptions.None, preallocationSize: 0, createOpenException: createOpenException);
                    if (Interop.Sys.Unlink(destFullPath) < 0)
                    {
                        Interop.Error errorInfo = Interop.Sys.GetLastError();
                        if (error != Interop.Error.ENOENT)
                        {
                            // Fall back to standard copy as an unexpected error has occurred.
                            return false;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // We don't want to throw if it's just the file not existing, since we're trying to delete it.
                }

                // Try clonefile now we've deleted the destination file.
                if (TryCloneFile(sourceFullPath, destFullPath, flags, out error)) return true;

                // Some filesystems don't support ACLs, so may fail due to trying to copy ACLs.
                // This will disable them and allow trying again (a maximum of 1 time).
                if (flags != 0 && error == Interop.Error.EINVAL)
                {
                    flags = 0;
                    if (TryCloneFile(sourceFullPath, destFullPath, flags, out error)) return true;
                }
            }

            // Check if it's not supported, if files are on different filesystems, or if the destination file still exists.
            Debug.Assert(error != Interop.Error.EINVAL);
            if (error == Interop.Error.ENOTSUP || error == Interop.Error.EXDEV || error == Interop.Error.EEXIST)
            {
                // Fall back to normal copy.
                return false;
            }

            // Throw the appropriate exception.
            Debug.Assert(error != Interop.Error.SUCCESS); // We shouldn't fail with success.
            throw Interop.GetExceptionForIoErrno(error.Info(), destFullPath);
        }
    }
}
