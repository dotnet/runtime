// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static partial void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // Attempt to clone the file:

            // Simplify the destination path (i.e. unlink all the links except the last one itself,
            // i.e. for /link1/link2, you could get /folder1/link2).
            string destFullPath_Full = Path.GetFullPath(destFullPath);
            string? destPathFolder = Path.GetDirectoryName(destFullPath_Full);
            string destPath;
            if (string.IsNullOrEmpty(destPathFolder))
            {
                destPath = destFullPath_Full;
            }
            else
            {
                try
                {
                    destPathFolder = ResolveLinkTargetString(destPathFolder, true, true);
                }
                catch
                {
                    //In case readlink fails
                    destPathFolder = null;
                }
                if (string.IsNullOrEmpty(destPathFolder))
                {
                    destPath = destFullPath_Full;
                }
                else
                {
                    destPath = Path.Combine(destPathFolder, Path.GetFileName(destFullPath));
                }
            }

            // Get the full path of the source path and verify that we're not copying the source file onto itself
            string fullSource = TryGetLinkTarget(sourceFullPath, destPath, overwrite) ?? sourceFullPath;

            // Start the file copy and prepare for finalization
            StartedCopyFileState startedCopyFile = StartCopyFile(fullSource, destPath, overwrite, openDst: false);

            // Attempt counter just in case we somehow loop infinite times e.g. on a
            // filesystem that doesn't actually delete files but pretends it does.
            // Declare error variable here since it can be used after some jumping around.
            int attempts = 0;
            int error;

            try
            {
                // Don't need to re-read the link on our first attempt
                bool failOnRereadDoesntChange = false;
                if (overwrite)
                {
                    // Ensure file is deleted on first try.
                    // Get a lock to the dest file for compat reasons, and then delete it.
                    using SafeFileHandle? dstHandle = OpenCopyFileDstHandle(destPath, true, startedCopyFile, false);
                    File.Delete(destPath);
                }
                goto tryAgain;

                // We may want to re-read the link to see if its path has changed
                tryAgainWithReadLink:
                if (++attempts >= 5) goto throwError;
                string fullSource2 = TryGetLinkTarget(sourceFullPath, destPath, overwrite) ?? sourceFullPath;
                if (fullSource != fullSource2)
                {
                    // Path has changed
                    startedCopyFile.Dispose();
                    startedCopyFile = StartCopyFile(fullSource, destPath, overwrite, openDst: false);
                }
                else if (failOnRereadDoesntChange)
                {
                    // Path hasn't changed and we want to throw the error we got earlier
                    goto throwError;
                }
                failOnRereadDoesntChange = false;

                // Attempt to clone the file
                tryAgain:
                unsafe
                {
                    if (Interop.@libc.copyfile(fullSource, destPath, null, Interop.@libc.COPYFILE_CLONE_FORCE) == 0)
                    {
                        return;
                    }
                }

                // Check the error
                error = Marshal.GetLastWin32Error();
                const int ENOTSUP = 45;
                const int EEXIST = 17;
                const int ENOENT = 2;
                bool directoryExist = false;
                if ((error == ENOTSUP && FileOrDirectoryExists(destPath)) || error == EEXIST)
                {
                    // This means the destination existed, try again with the destination deleted if appropriate
                    error = EEXIST;
                    if (Directory.Exists(destPath))
                    {
                        directoryExist = true;
                        goto throwError;
                    }
                    if (overwrite)
                    {
                        // Get a lock to the dest file for compat reasons, and then delete it.
                        using SafeFileHandle? dstHandle = OpenCopyFileDstHandle(destPath, true, startedCopyFile, false);
                        File.Delete(destPath);
                        goto tryAgainWithReadLink;
                    }
                }
                else if (error == ENOTSUP)
                {
                    // This probably means cloning is not supported, try the standard implementation
                    goto fallback;
                }
                else if (error == ENOENT)
                {
                    // This can happen if the source is a symlink and it has been changed to a different file, and the first has been deleted or renamed, for example.
                    // failOnRereadDoesntChange means we want to fail if the link didn't change, indicating the source actually doesn't exist.
                    failOnRereadDoesntChange = true;
                    goto tryAgainWithReadLink;
                }

                // Throw an appropriate error
                throwError:
                if (directoryExist)
                {
                    throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath));
                }
                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(error));

                // Fallback to the standard unix implementation for when cloning is not supported
                fallback:

                // Open the dst handle
                startedCopyFile.dst = OpenCopyFileDstHandle(destFullPath, overwrite, startedCopyFile, true);

                // Copy the file using the standard unix implementation
                StandardCopyFile(startedCopyFile);
            }
            finally
            {
                startedCopyFile.Dispose();
            }

            // Attempts to read the path's link target, or returns null even if the path doesn't exist rather than throwing.
            // Throws an error if 'path' is at any point equal to 'destPath', since it means we're copying onto itself.
            // Throws an error if 'path' has too many symbolic link levels.
            static string? TryGetLinkTarget(string path, string destPath, bool overwrite)
            {
                if (path == destPath)
                {
                    // Throw an appropriate error
                    if (overwrite) throw new IOException(SR.Format(SR.IO_SharingViolation_File, destPath));
                    else throw new IOException(SR.Format(SR.IO_FileExists_Name, destPath));
                }
                string? currentTarget = null;
                for (int i = 0; i < MaxFollowedLinks; i++)
                {
                    string? newTarget;
                    try
                    {
                        // Attempt to unlink the current link
                        newTarget = ResolveLinkTargetString(currentTarget ?? path, false, false);
                    }
                    catch
                    {
                        // This means path's target doesn't exist, stop unlinking
                        return currentTarget;
                    }

                    // Check the new target path
                    if (newTarget == destPath)
                    {
                        // Throw an appropriate error
                        if (overwrite) throw new IOException(SR.Format(SR.IO_SharingViolation_File, destPath));
                        else throw new IOException(SR.Format(SR.IO_FileExists_Name, destPath));
                    }

                    // Store the unlinked path, otherwise return our current path
                    if (string.IsNullOrEmpty(newTarget))
                    {
                        return currentTarget;
                    }
                    else
                    {
                        currentTarget = newTarget;
                    }
                }

                // If we get here, we've gone through MaxFollowedLinks iterations
                throw new IOException(SR.Format(SR.IO_TooManySymbolicLinkLevels, path));
            }

            // Checks if a file or directory exists without caring which it was
            static bool FileOrDirectoryExists(string path)
            {
                return Interop.Sys.Stat(path, out _) >= 0;
            }
        }
    }
}
