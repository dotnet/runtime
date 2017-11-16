// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.IO.Tests
{
    public class File_Delete : FileSystemTest
    {
        #region Utilities

        public virtual void Delete(string path)
        {
            File.Delete(path);
        }

        public virtual FileInfo Create(string path)
        {
            var ret = new FileInfo(path);
            ret.Create().Dispose();
            return ret;
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullParameters()
        {
            Assert.Throws<ArgumentNullException>(() => Delete(null));
        }

        [Fact]
        public void InvalidParameters()
        {
            Assert.Throws<ArgumentException>(() => Delete(string.Empty));
        }

        [Fact]
        public void DeleteDot_ThrowsUnauthorizedAccessException()
        {
            Assert.Throws<UnauthorizedAccessException>(() => Delete("."));
        }

        [Fact]
        public void ShouldBeAbleToDeleteHiddenFile()
        {
            FileInfo testFile = Create(GetTestFilePath());
            testFile.Attributes = FileAttributes.Hidden;
            Delete(testFile.FullName);
            Assert.False(testFile.Exists);
        }

        [Fact]
        public void DeleteNonEmptyFile()
        {
            FileInfo testFile = Create(GetTestFilePath());
            File.WriteAllText(testFile.FullName, "This is content");
            Delete(testFile.FullName);
            Assert.False(testFile.Exists);
        }

        [Fact]
        public void PositiveTest()
        {
            FileInfo testFile = Create(GetTestFilePath());
            Delete(testFile.FullName);
            Assert.False(testFile.Exists);
        }

        [Fact]
        public void NonExistentFile()
        {
            Delete(GetTestFilePath());
        }

        [Fact]
        public void ShouldThrowIOExceptionDeletingDirectory()
        {
            Assert.Throws<UnauthorizedAccessException>(() => Delete(TestDirectory));
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void DeletingSymLinkDoesntDeleteTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();

            File.Create(path).Dispose();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: false));

            // Both the symlink and the target exist
            Assert.True(File.Exists(path), "path should exist");
            Assert.True(File.Exists(linkPath), "linkPath should exist");

            // Delete the symlink
            File.Delete(linkPath);

            // Target should still exist
            Assert.True(File.Exists(path), "path should still exist");
            Assert.False(File.Exists(linkPath), "linkPath should no longer exist");
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting non-existent path throws
        public void Windows_NonExistentPath_Throws_DirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() => Delete(Path.Combine(TestDirectory, GetTestFileName(), "C")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Deleting non-existent path doesn't throw
        public void Unix_NonExistentPath_Nop()
        {
            Delete(Path.Combine(TestDirectory, GetTestFileName(), "C"));
        }

        [Fact]
        [OuterLoop("Needs sudo access")]
        [PlatformSpecific(TestPlatforms.Linux)]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void Unix_NonExistentPath_ReadOnlyVolume()
        {
            if (PlatformDetection.IsRedHatFamily6)
                return; // [ActiveIssue(https://github.com/dotnet/corefx/issues/21920)]

            ReadOnly_FileSystemHelper(readOnlyDirectory =>
            {
                Delete(Path.Combine(readOnlyDirectory, "DoesNotExist"));
            });
        }

        [Fact]
        [OuterLoop("Needs sudo access")]
        [PlatformSpecific(TestPlatforms.Linux)]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void Unix_ExistingDirectory_ReadOnlyVolume()
        {
            if (PlatformDetection.IsRedHatFamily6)
                return; // [ActiveIssue(https://github.com/dotnet/corefx/issues/21920)]

            ReadOnly_FileSystemHelper(readOnlyDirectory =>
            {
                Assert.Throws<IOException>(() => Delete(Path.Combine(readOnlyDirectory, "subdir")));
            }, subDirectoryName: "subdir");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting already-open file throws
        public void Windows_File_Already_Open_Throws_IOException()
        {
            string path = GetTestFilePath();
            using (File.Create(path))
            {
                Assert.Throws<IOException>(() => Delete(path));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Deleting already-open file allowed
        public void Unix_File_Already_Open_Allowed()
        {
            string path = GetTestFilePath();
            using (File.Create(path))
            {
                Delete(path);
                Assert.False(File.Exists(path));
            }
            Assert.False(File.Exists(path));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting readonly file throws
        public void WindowsDeleteReadOnlyFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            Assert.Throws<UnauthorizedAccessException>(() => Delete(path));
            Assert.True(File.Exists(path));
            File.SetAttributes(path, FileAttributes.Normal);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Deleting readonly file allowed
        public void UnixDeleteReadOnlyFile()
        {
            FileInfo testFile = Create(GetTestFilePath());
            testFile.Attributes = FileAttributes.ReadOnly;
            Delete(testFile.FullName);
            Assert.False(testFile.Exists);
        }

        #endregion
    }
}
