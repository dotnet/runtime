// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase
    {
        protected void AssertPathsAreHardLinked(string path1, string path2)
        {
            Assert.Equal(GetFileId(path1), GetFileId(path2));

            static (uint dwVolumeSerialNumber, uint nFileIndexHigh, uint nFileIndexLow) GetFileId(string path)
            {
                using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                    path,
                    Interop.Kernel32.GenericOperations.GENERIC_READ,
                    FileShare.ReadWrite | FileShare.Delete,
                    FileMode.Open,
                    Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS | Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT);

                if (!Interop.Kernel32.GetFileInformationByHandle(handle, out Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInfo))
                {
                    throw new IOException($"Failed to get file information for {path}");
                }

                return (fileInfo.dwVolumeSerialNumber, fileInfo.nFileIndexHigh, fileInfo.nFileIndexLow);
            }
        }
    }
}
