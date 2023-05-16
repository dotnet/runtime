// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static partial void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // Open the src file handle.
            using SafeFileHandle src = SafeFileHandle.OpenReadOnly(sourceFullPath, FileOptions.None, out var srcFileStatus);

            // Open the dst file handle.
            // Note: the code in FileSystem.CopyFile.OSX.cs needs to be kept in sync with this section.
            using SafeFileHandle dst = SafeFileHandle.Open(destFullPath, overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize: 0, unixCreateMode: null,
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

            // Copy the file in a way that works on all Unix Operating Systems.
            // Note: the fallback code in FileSystem.CopyFile.OSX.cs needs to be kept in sync with this.
            Interop.CheckIo(Interop.Sys.CopyFile(src, dst, srcFileStatus.Size));
        }
    }
}
