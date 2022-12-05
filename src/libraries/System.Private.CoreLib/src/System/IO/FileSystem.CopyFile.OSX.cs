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
            //Attempt to clone the file:

            //Get the full path of the source path
            string fullSource = TryGetLinkTarget(sourceFullPath) ?? sourceFullPath;

            //Start the file copy and prepare for finalization
            StartedCopyFileState startedCopyFile = StartCopyFile(fullSource, destFullPath, overwrite, openDst: false);

            //Attempt counter just in case we somehow loop infinite times e.g. on a
            //filesystem that doesn't actually delete files but pretends it does.
            //Declare error variable here since it can be used after some jumping around.
            int attempts = 0;
            int error;

            try
            {
                //Don't need to re-read the link on our first attempt
                bool failOnRereadDoesntChange = false;
                if (overwrite)
                {
                    //Ensure file is deleted on first try.
                    //Get a lock to the dest file for compat reasons, and then delete it.
                    using SafeFileHandle? dstHandle = OpenCopyFileDstHandle(destFullPath, true, startedCopyFile, false);
                    File.Delete(destFullPath);
                }
                goto tryAgain;

                //We may want to re-read the link to see if its path has changed
                tryAgainWithReadLink:
                if (++attempts >= 5) goto throwError;
                string fullSource2 = TryGetLinkTarget(sourceFullPath) ?? sourceFullPath;
                if (fullSource != fullSource2)
                {
                    //Path has changed
                    startedCopyFile.Dispose();
                    startedCopyFile = StartCopyFile(fullSource, destFullPath, overwrite, openDst: false);
                }
                else if (failOnRereadDoesntChange)
                {
                    //Path hasn't changed and we want to throw the error we got earlier
                    goto throwError;
                }
                failOnRereadDoesntChange = false;

                //Attempt to clone the file
                tryAgain:
                unsafe
                {
                    if (Interop.@libc.copyfile(fullSource, destFullPath, null, Interop.@libc.COPYFILE_CLONE_FORCE) == 0)
                    {
                        return;
                    }
                }

                //Check the error
                error = Marshal.GetLastWin32Error();
                const int ENOTSUP = 45;
                const int EEXIST = 17;
                const int ENOENT = 2;
                bool directoryExist = false;
                if ((error == ENOTSUP && FileOrDirectoryExists(destFullPath)) || error == EEXIST)
                {
                    //This means the destination existed, try again with the destination deleted if appropriate
                    error = EEXIST;
                    if (Directory.Exists(destFullPath))
                    {
                        directoryExist = true;
                        goto throwError;
                    }
                    if (overwrite)
                    {
                        //Get a lock to the dest file for compat reasons, and then delete it.
                        using SafeFileHandle dstHandle = OpenCopyFileDstHandle(destFullPath, true, startedCopyFile, false);
                        File.Delete(destFullPath);
                        goto tryAgainWithReadLink;
                    }
                }
                else if (error == ENOTSUP)
                {
                    //This probably means cloning is not supported, try the standard implementation
                    goto fallback;
                }
                else if (error == ENOENT)
                {
                    //This can happen if the source is a symlink and it has been changed to a different file, and the first has been deleted or renamed, for example.
                    //failOnRereadDoesntChange means we want to fail if the link didn't change, indicating the source actually doesn't exist.
                    failOnRereadDoesntChange = true;
                    goto tryAgainWithReadLink;
                }

                //Throw an appropriate error
                throwError:
                if (directoryExist)
                {
                    throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath));
                }
                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(error));

                //Fallback to the standard unix implementation for when cloning is not supported
                fallback:

                //Open the dst handle
                startedCopyFile.dst = OpenCopyFileDstHandle(destFullPath, overwrite, startedCopyFile, true);

                //Copy the file using the standard unix implementation
                StandardCopyFile(startedCopyFile);
            }
            finally
            {
                startedCopyFile.Dispose();
            }

            //Attempts to read the path's link target, or returns null even if the path doesn't exist
            static string? TryGetLinkTarget(string path)
            {
                try
                {
                    return ResolveLinkTargetString(path, true, false);
                }
                catch
                {
                    return null;
                }
            }

            //Checks if a file or directory exists without caring which it was
            static bool FileOrDirectoryExists(string path)
            {
                return Interop.Sys.Stat(path, out _) >= 0;
            }
        }
    }
}
