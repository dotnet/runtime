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
            // Attempt to clone the file:

            // Helper function to throw an error for copying onto self
            [StackTraceHidden]
            void CopyOntoSelfError()
            {
                // Throw an appropriate error
                if (overwrite) throw new IOException(SR.Format(SR.IO_SharingViolation_File, destFullPath));
                else throw new IOException(SR.Format(SR.IO_FileExists_Name, destFullPath));
            }

            // Helper function to throw an error when the destination exists and overwrite = false.
            [StackTraceHidden]
            static void DestinationExistsError()
            {
                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EEXIST));
            }

            // Fail fast for blatantly copying onto self
            if (sourceFullPath == destFullPath)
            {
                CopyOntoSelfError();
            }

            // Start the file copy and prepare for finalization
            StartedCopyFileState startedCopyFile = StartCopyFile(sourceFullPath, destFullPath, overwrite, openDst: false);

            // Ensure we dispose startedCopyFile.
            try
            {
                // Read filestatus of destination file to determine how we continue
                int destError = Interop.Sys.Stat(destFullPath, out var destStat);

                // Counter to count the amount of times we have repeated. Used in case EEXIST is thrown by clonefile, indicating the
                // file was re-created before we copy. Not a problematic error, but we do not want to retry an infinite amount of times.
                int repeats = 0;
                tryAgain:

                // Interpret the error from stat
                if (destError != 0)
                {
                    // Some error, let's see what it is:
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();

                    if (error.Error == Interop.Error.ENOENT)
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
                        DestinationExistsError();
                    }
                    if (startedCopyFile.fileDev != destStat.Dev)
                    {
                        // On different device
                        goto tryFallback;
                    }
                    else if (startedCopyFile.fileIno == destStat.Ino)
                    {
                        // Copying onto itself
                        CopyOntoSelfError();
                    }
                    else
                    {
                        goto tryDelete;
                    }
                }

                // Try deleting destination:
                tryDelete:
                {
                    // Delete the destination. This should fail on directories. And update the mode.
                    // Get a lock to the dest file for compat reasons, and then delete it.
                    using SafeFileHandle? dstHandle = OpenCopyFileDstHandle(destFullPath, true, startedCopyFile, false);
                    File.Delete(destFullPath);
                }

                // Try clonefile:
                tryCloneFile:
                {
                    if (Interop.@libc.clonefile(sourceFullPath, destFullPath, 0) == 0)
                    {
                        // Success
                        return;
                    }

                    // Read error
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();

                    // Check error
                    if (error.Error == Interop.Error.EEXIST)
                    {
                        if (!overwrite)
                        {
                            // Throw an error if we're not overriding
                            DestinationExistsError();
                        }

                        // Destination existed, try again (up a certain number of times).
                        if (++repeats < 5)
                        {
                            goto tryAgain;
                        }
                    }

                    // Try fallback
                    // goto tryFallback;
                }

                // Try fallback:
                tryFallback:
                {
                    // Open the dst handle
                    startedCopyFile.dst = OpenCopyFileDstHandle(destFullPath, overwrite, startedCopyFile, true);

                    // Copy the file using the standard unix implementation
                    StandardCopyFile(startedCopyFile);
                }
            }
            finally
            {
                startedCopyFile.Dispose();
            }
        }
    }
}
