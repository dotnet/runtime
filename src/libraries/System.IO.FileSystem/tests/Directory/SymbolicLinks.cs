// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_SymbolicLinks : BaseSymbolicLinks_FileSystem
    {
        protected override void CreateFileOrDirectory(string path, bool createOpposite = false)
        {
            if (!createOpposite)
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                File.Create(path).Dispose();
            }
        }

        protected override FileSystemInfo CreateSymbolicLink(string path, string pathToTarget) =>
            Directory.CreateSymbolicLink(path, pathToTarget);

        protected override FileSystemInfo ResolveLinkTarget(string linkPath, string? expectedLinkTarget, bool returnFinalTarget = false) =>
            Directory.ResolveLinkTarget(linkPath, returnFinalTarget);

        protected override void AssertIsCorrectTypeAndDirectoryAttribute(FileSystemInfo fsi)
        {
            if (fsi.Exists)
            {
                Assert.True(fsi.Attributes.HasFlag(FileAttributes.Directory));
            }
            Assert.True(fsi is DirectoryInfo);
        }

        protected override void AssertLinkExists(FileSystemInfo link) =>
            Assert.False(link.Exists); // For directory symlinks, we return the exists info from the target

        protected override void AssertExistsWhenNoTarget(FileSystemInfo link)
        {
            if (PlatformDetection.IsWindows)
            {
                Assert.True(link.Exists);
            }
            else
            {
                // Unix implementation detail:
                // When the directory target does not exist FileStatus.GetExists returns false because:
                // - We check _exists (which whould be true because the link itself exists).
                // - We check InitiallyDirectory, which is the initial expected object type (which would be true).
                // - We check _directory (false because the target directory does not exist)
                Assert.False(link.Exists);
            }
        }

        [Fact]
        public void EnumerateDirectories_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Windows differentiates between dir symlinks and file symlinks
            int expected = OperatingSystem.IsWindows() ? 1 : 0;
            Assert.Equal(expected, Directory.EnumerateDirectories(testDirectory.FullName).Count());
        }

        [Fact]
        public void EnumerateFiles_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();

            // Windows differentiates between dir symlinks and file symlinks
            int expected = OperatingSystem.IsWindows() ? 0 : 1;
            Assert.Equal(expected, Directory.EnumerateFiles(testDirectory.FullName).Count());
        }

        [Fact]
        public void EnumerateFileSystemEntries_LinksWithCycles_ShouldNotThrow()
        {
            DirectoryInfo testDirectory = CreateDirectoryContainingSelfReferencingSymbolicLink();
            Assert.Single(Directory.EnumerateFileSystemEntries(testDirectory.FullName));
        }

        [Fact]
        public void ResolveLinkTarget_LinkDoesNotExist() =>
            ResolveLinkTarget_LinkDoesNotExist_Internal<DirectoryNotFoundException>();
    }
}
