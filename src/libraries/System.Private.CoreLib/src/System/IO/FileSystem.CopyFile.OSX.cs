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
                if (overwrite)
                {
                    throw new IOException(SR.Format(SR.IO_SharingViolation_File, destFullPath));
                }

                throw new IOException(SR.Format(SR.IO_FileExists_Name, destFullPath));
            }

            // Start the copy
            (long fileLength, long fileDev, long fileIno, SafeFileHandle src, SafeFileHandle? dst) startedCopyFile = StartCopyFile(sourceFullPath, destFullPath, overwrite, openDst: false);
            try
            {
                // Read FileStatus of destination file to determine how to continue
                int destError = Interop.Sys.Stat(destFullPath, out var destStat);

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
                        throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EEXIST));
                    }
                    if (startedCopyFile.fileDev != destStat.Dev)
                    {
                        // On different device
                        goto tryFallback;
                    }
                    else if (startedCopyFile.fileIno == destStat.Ino)
                    {
                        // Copying onto itself
                        throw new IOException(SR.Format(SR.IO_SharingViolation_File, destFullPath));
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
                    startedCopyFile.dst = OpenCopyFileDstHandle(destFullPath, overwrite, startedCopyFile, openNewFile: true);

                    // Copy the file using the standard unix implementation
                    // dst! because dst is not null if OpenCopyFileDstHandle's openNewFile is true.
                    StandardCopyFile(startedCopyFile.src, startedCopyFile.dst!, startedCopyFile.fileLength);
                }
            }
            finally
            {
                startedCopyFile.src?.Dispose();
                startedCopyFile.dst?.Dispose();
            }
        }
    }
}
