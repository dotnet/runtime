// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;

namespace System.IO.Tests
{
    public class Directory_Delete_str : FileSystemTest
    {
        static bool IsBindMountSupported => OperatingSystem.IsLinux() && !PlatformDetection.IsInContainer;

        static bool IsBindMountSupportedAndPrivilegedProcess => IsBindMountSupported && PlatformDetection.IsPrivilegedProcess;

        static bool IsRemoteExecutorSupportedAndUsingNewNormalization => RemoteExecutor.IsSupported && UsingNewNormalization;

        static bool IsRemoteExecutorSupportedAndLongPathsAreNotBlockedAndUsingNewNormalization => RemoteExecutor.IsSupported && LongPathsAreNotBlocked && UsingNewNormalization;

        #region Utilities

        protected virtual void Delete(string path)
        {
            Directory.Delete(path);
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
        public void ShouldThrowIOExceptionIfContainedFileInUse()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            using (File.Create(Path.Combine(testDir.FullName, GetTestFileName())))
            {
                Assert.Throws<IOException>(() => Delete(testDir.FullName));
            }
            Assert.True(testDir.Exists);
        }

        [Fact]
        public void ShouldThrowIOExceptionForDirectoryWithFiles()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            File.Create(Path.Combine(testDir.FullName, GetTestFileName())).Dispose();
            Assert.Throws<IOException>(() => Delete(testDir.FullName));
            Assert.True(testDir.Exists);
        }

        [Fact]
        public void DirectoryWithSubdirectories()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.CreateSubdirectory(GetTestFileName());
            Assert.Throws<IOException>(() => Delete(testDir.FullName));
            Assert.True(testDir.Exists);
        }

        [Fact]
        [OuterLoop]
        public void DeleteRoot()
        {
            Assert.Throws<IOException>(() => Delete(Path.GetPathRoot(Directory.GetCurrentDirectory())));
        }

        [Fact]
        public void PositiveTest()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Delete(testDir.FullName);
            Assert.False(testDir.Exists);
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingFile_ThrowsDirectoryNotFound(char trailingChar)
        {
            string path = GetTestFilePath() + trailingChar;
            Assert.Throws<DirectoryNotFoundException>(() => Delete(path));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingDirectory_ThrowsDirectoryNotFound(char trailingChar)
        {
            string path = Path.Combine(GetTestFilePath(), "file" + trailingChar);
            Assert.Throws<DirectoryNotFoundException>(() => Delete(path));
        }

        [Fact]
        public void ShouldThrowIOExceptionDeletingCurrentDirectory()
        {
            Assert.Throws<IOException>(() => Delete(Directory.GetCurrentDirectory()));
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void DeletingSymLinkDoesntDeleteTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetRandomLinkPath();

            Directory.CreateDirectory(path);
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true));

            // Both the symlink and the target exist
            Assert.True(Directory.Exists(path), "path should exist");
            Assert.True(Directory.Exists(linkPath), "linkPath should exist");

            // Delete the symlink
            Directory.Delete(linkPath);

            // Target should still exist
            Assert.True(Directory.Exists(path), "path should still exist");
            Assert.False(Directory.Exists(linkPath), "linkPath should no longer exist");
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndUsingNewNormalization))]
        public void ExtendedDirectoryWithSubdirectories()
        {
            RemoteExecutor.Invoke(() =>
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
                DirectoryInfo testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + GetTestFilePath());
                testDir.CreateSubdirectory(GetTestFileName());
                Assert.Throws<IOException>(() => Delete(testDir.FullName));
                Assert.True(testDir.Exists);
            }).Dispose();
        }

        [ConditionalFact(nameof(IsRemoteExecutorSupportedAndLongPathsAreNotBlockedAndUsingNewNormalization))]
        public void LongPathExtendedDirectory()
        {
            RemoteExecutor.Invoke(() =>
            {
                Directory.SetCurrentDirectory(Path.GetTempPath());
                DirectoryInfo testDir = Directory.CreateDirectory(IOServices.GetPath(IOInputs.ExtendedPrefix + TestDirectory, characterCount: 500));
                Delete(testDir.FullName);
                Assert.False(testDir.Exists);
            }).Dispose();
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting readonly directory throws IOException
        public void WindowsDeleteReadOnlyDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.Attributes = FileAttributes.ReadOnly;
            Assert.Throws<IOException>(() => Delete(testDir.FullName));
            Assert.True(testDir.Exists);
            testDir.Attributes = FileAttributes.Normal;
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting extended readonly directory throws IOException
        public void WindowsDeleteExtendedReadOnlyDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + GetTestFilePath());
            testDir.Attributes = FileAttributes.ReadOnly;
            Assert.Throws<IOException>(() => Delete(testDir.FullName));
            Assert.True(testDir.Exists);
            testDir.Attributes = FileAttributes.Normal;
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Deleting readOnly directory succeeds
        public void UnixDeleteReadOnlyDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.Attributes = FileAttributes.ReadOnly;
            Delete(testDir.FullName);
            Assert.False(testDir.Exists);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting hidden directory succeeds
        public void WindowsShouldBeAbleToDeleteHiddenDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.Attributes = FileAttributes.Hidden;
            Delete(testDir.FullName);
            Assert.False(testDir.Exists);
        }

        [ConditionalFact(nameof(UsingNewNormalization))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Deleting extended hidden directory succeeds
        public void WindowsShouldBeAbleToDeleteExtendedHiddenDirectory()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(IOInputs.ExtendedPrefix + GetTestFilePath());
            testDir.Attributes = FileAttributes.Hidden;
            Delete(testDir.FullName);
            Assert.False(testDir.Exists);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Deleting hidden directory succeeds
        public void UnixShouldBeAbleToDeleteHiddenDirectory()
        {
            string testDir = "." + GetTestFileName();
            Directory.CreateDirectory(Path.Combine(TestDirectory, testDir));
            Assert.True(0 != (new DirectoryInfo(Path.Combine(TestDirectory, testDir)).Attributes & FileAttributes.Hidden));
            Delete(Path.Combine(TestDirectory, testDir));
            Assert.False(Directory.Exists(testDir));
        }

        [ConditionalFact(nameof(IsBindMountSupportedAndPrivilegedProcess))]
        [OuterLoop("Needs sudo access")]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void Unix_NotFoundDirectory_ReadOnlyVolume()
        {
            ReadOnly_FileSystemHelper(readOnlyDirectory =>
            {
                Assert.Throws<DirectoryNotFoundException>(() => Delete(Path.Combine(readOnlyDirectory, "DoesNotExist")));
            });
        }
        #endregion
    }

    public partial class Directory_Delete_str_bool : Directory_Delete_str
    {
        #region Utilities

        protected override void Delete(string path)
        {
            Directory.Delete(path, false);
        }

        protected virtual void Delete(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        #endregion

        [Fact]
        public void RecursiveDelete()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            File.Create(Path.Combine(testDir.FullName, GetTestFileName())).Dispose();
            testDir.CreateSubdirectory(GetTestFileName());
            Delete(testDir.FullName, true);
            Assert.False(testDir.Exists);
        }

        [Fact]
        public void RecursiveDeleteWithTrailingSlash()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Delete(testDir.FullName + Path.DirectorySeparatorChar, true);
            Assert.False(testDir.Exists);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("This test is very slow.")]
        public void RecursiveDelete_DeepNesting()
        {
            // Create a 2000 level deep directory and recursively delete from the root.
            // This number can be dropped if we find it problematic on low memory machines
            // and/or we can look at skipping in such environments.
            //
            // On debug we were overflowing the stack with directories that were under 1000
            // levels deep. Testing on a 32GB box I consistently fell over around 1300.
            // With optimizations to the Delete helper I was able to raise this to around 3200.
            // Release binaries don't stress the stack nearly as much (10K+ is doable, but can
            // take 5 minutes on an SSD).

            string rootDirectory = GetTestFilePath();
            StringBuilder sb = new StringBuilder(5000);
            sb.Append(rootDirectory);
            for (int i = 0; i < 2000; i++)
            {
                sb.Append(@"\a");
            }
            string path = sb.ToString();
            Directory.CreateDirectory(path);
            Delete(rootDirectory, recursive: true);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Recursive delete throws IOException if directory contains in-use file
        public void RecursiveDelete_ShouldThrowIOExceptionIfContainedFileInUse()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            using (File.Create(Path.Combine(testDir.FullName, GetTestFileName())))
            {
                Assert.Throws<IOException>(() => Delete(testDir.FullName, true));
            }
            Assert.True(testDir.Exists);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void RecursiveDeletingDoesntFollowLinks()
        {
            var target = GetTestFilePath();
            Directory.CreateDirectory(target);

            var fileInTarget = Path.Combine(target, GetTestFileName());
            File.WriteAllText(fileInTarget, "");

            var linkParent = GetTestFilePath();
            Directory.CreateDirectory(linkParent);

            var linkPath = Path.Combine(linkParent, GetTestFileName());
            Assert.NotNull(Directory.CreateSymbolicLink(linkPath, target));

            // Both the symlink and the target exist
            Assert.True(Directory.Exists(target), "target should exist");
            Assert.True(Directory.Exists(linkPath), "linkPath should exist");
            Assert.True(File.Exists(fileInTarget), "fileInTarget should exist");

            // Delete the parent folder of the symlink.
            Delete(linkParent, true);

            // Target should still exist
            Assert.True(Directory.Exists(target), "target should still exist");
            Assert.False(Directory.Exists(linkPath), "linkPath should no longer exist");
            Assert.True(File.Exists(fileInTarget), "fileInTarget should exist");
        }
    }
}
