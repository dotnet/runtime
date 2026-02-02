// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase
    {
        protected void VerifyPathsAreHardLinked(string path1, string path2)
        {
            Assert.True(File.Exists(path1));
            Assert.True(File.Exists(path2));

            using SafeFileHandle handle1 = Interop.Kernel32.CreateFile(
                path1,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS | Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT);

            using SafeFileHandle handle2 = Interop.Kernel32.CreateFile(
                path2,
                Interop.Kernel32.GenericOperations.GENERIC_READ,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS | Interop.Kernel32.FileOperations.FILE_FLAG_OPEN_REPARSE_POINT);

            if (!Interop.Kernel32.GetFileInformationByHandle(handle1, out Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInfo1))
            {
                throw new IOException($"Failed to get file information for {path1}");
            }

            if (!Interop.Kernel32.GetFileInformationByHandle(handle2, out Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInfo2))
            {
                throw new IOException($"Failed to get file information for {path2}");
            }

            Assert.Equal(fileInfo1.dwVolumeSerialNumber, fileInfo2.dwVolumeSerialNumber);
            ulong fileIndex1 = ((ulong)fileInfo1.nFileIndexHigh << 32) | fileInfo1.nFileIndexLow;
            ulong fileIndex2 = ((ulong)fileInfo2.nFileIndexHigh << 32) | fileInfo2.nFileIndexLow;
            Assert.Equal(fileIndex1, fileIndex2);
        }
    }
}
