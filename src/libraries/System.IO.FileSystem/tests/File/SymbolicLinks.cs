// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    public class File_SymbolicLinks : BaseSymbolicLinks_FileSystem
    {
        protected override void CreateFileOrDirectory(string path, bool createOpposite = false)
        {
            if (!createOpposite)
            {
                File.Create(path).Dispose();
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }

        protected override FileSystemInfo CreateSymbolicLink(string path, string pathToTarget) =>
            File.CreateSymbolicLink(path, pathToTarget);

        protected override FileSystemInfo ResolveLinkTarget(string linkPath, string? expectedLinkTarget, bool returnFinalTarget = false) =>
            File.ResolveLinkTarget(linkPath, returnFinalTarget);

        protected override void AssertIsCorrectTypeAndDirectoryAttribute(FileSystemInfo fsi)
        {
            if (fsi.Exists)
            {
                Assert.False(fsi.Attributes.HasFlag(FileAttributes.Directory));
            }
            Assert.True(fsi is FileInfo);
        }

        protected override void AssertLinkExists(FileSystemInfo link) =>
            Assert.True(link.Exists); // For file symlinks, we return the exists info from the actual link, not the target

        protected override void AssertExistsWhenNoTarget(FileSystemInfo link) =>
            Assert.True(link.Exists);

        [Fact]
        public void ResolveLinkTarget_LinkDoesNotExist() =>
            ResolveLinkTarget_LinkDoesNotExist_Internal<FileNotFoundException>();
    }
}
