// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static partial void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            // Open src and dest file handles.
            // ! because OpenCopyFileDstHandle doesn't return null when openNewFile is true
            using SafeFileHandle src = SafeFileHandle.OpenReadOnly(sourceFullPath, FileOptions.None, out var srcFileStatus);
            using SafeFileHandle dst = OpenCopyFileDstHandle(destFullPath, overwrite, SafeFileHandle.GetFileMode(srcFileStatus), true)!;

            // Copy the file in a way that works on all Unix Operating Systems.
            // Note: the fallback code in FileSystem.CopyFile.OSX.cs needs to be kept in sync with this.
            Interop.CheckIo(Interop.Sys.CopyFile(src, dst, srcFileStatus.Size));
        }
    }
}
