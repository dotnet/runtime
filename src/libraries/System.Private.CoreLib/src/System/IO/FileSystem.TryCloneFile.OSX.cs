// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.IO
{
    internal static partial class FileSystem
    {
        private static bool TryCloneFile(string sourceFullPath, in Interop.Sys.FileStatus srcStat, string destFullPath, bool overwrite)
        {
            // Try to clone the file immediately, this will only succeed if the
            // destination doesn't exist, so we don't worry about locking for this one.
            int flags = Interop.@libc.CLONE_ACL;
            retry1:
            if (Interop.@libc.clonefile(sourceFullPath, destFullPath, flags) == 0)
            {
                // Success.
                return true;
            }
            Interop.Error error = Interop.Sys.GetLastError();

            // Some filesystems don't support ACLs, so may fail due to trying to copy ACLs.
            // This will disable them and allow trying again (a maximum of 1 time).
            if (flags != 0 && error == Interop.Error.EINVAL)
            {
                flags = 0;
                goto retry1;
            }

            // Try to delete the destination file if we're overwriting.
            if (error == Interop.Error.EEXIST && overwrite)
            {
                // Delete the destination. This should fail on directories. Get a lock to the dest file to ensure we don't copy onto it when
                // it's locked by something else, and then delete it. It should also fail if destination == source since it's already locked.
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

                // Try clonefile now we've deleted the destination file.
                retry2:
                if (Interop.@libc.clonefile(sourceFullPath, destFullPath, flags) == 0)
                {
                    // Success.
                    return true;
                }
                error = Interop.Sys.GetLastError();

                // Some filesystems don't support ACLs, so may fail due to trying to copy ACLs.
                // This will disable them and allow trying again (a maximum of 1 time).
                if (flags != 0 && error == Interop.Error.EINVAL)
                {
                    flags = 0;
                    goto retry2;
                }
            }

            // Check if it's not supported, if they're on different filesystems, the destination file still exists, or if we have unsupported flags still.
            // EINVAL is checked again here since clonefile is implemented by filesystem-specific drivers, therefore it could potentially fail with flags=0.
            if (error == Interop.Error.ENOTSUP || error == Interop.Error.EXDEV || error == Interop.Error.EEXIST || error == Interop.Error.EINVAL)
            {
                // Fall back to normal copy.
                return false;
            }

            // Throw the appropriate exception.
            Debug.Assert(error != Interop.Error.SUCCESS); // We shouldn't fail with success.
            throw Interop.GetExceptionForIoErrno(error.Info());
        }
    }
}
