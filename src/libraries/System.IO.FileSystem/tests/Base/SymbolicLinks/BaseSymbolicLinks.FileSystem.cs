// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace System.IO.Tests
{
    // Contains test methods that can be used for FileInfo, DirectoryInfo, File or Directory.
    public abstract class BaseSymbolicLinks_FileSystem : BaseSymbolicLinks
    {
        /// <summary>Creates a new file or directory depending on the implementing class.
        /// If createOpposite is true, creates a directory if the implementing class is for File or FileInfo, or
        /// creates a file if the implementing class is for Directory or DirectoryInfo.</summary>
        protected abstract void CreateFileOrDirectory(string path, bool createOpposite = false);
        protected abstract void AssertIsCorrectTypeAndDirectoryAttribute(FileSystemInfo fsi);
        protected abstract void AssertLinkExists(FileSystemInfo link);
        protected abstract void AssertExistsWhenNoTarget(FileSystemInfo link);
        /// <summary>Calls the actual public API for creating a symbolic link.</summary>
        protected abstract FileSystemInfo CreateSymbolicLink(string path, string pathToTarget);
        /// <summary>Calls the actual public API for resolving the symbolic link target.</summary>
        protected abstract FileSystemInfo ResolveLinkTarget(string linkPath, string? expectedLinkTarget, bool returnFinalTarget = false);

        [Fact]
        public void CreateSymbolicLink_NullPathToTarget()
        {
            Assert.Throws<ArgumentNullException>(() => CreateSymbolicLink(GetRandomFilePath(), pathToTarget: null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        public void CreateSymbolicLink_InvalidPathToTarget(string pathToTarget)
        {
            Assert.Throws<ArgumentException>(() => CreateSymbolicLink(GetRandomFilePath(), pathToTarget));
        }

        [Fact]
        public void CreateSymbolicLink_RelativeTargetPath_TargetExists()
        {
            // /path/to/link -> /path/to/existingtarget

            string linkPath = GetRandomLinkPath();
            string existentTarget = GetRandomFileName();
            string targetPath = Path.Join(Path.GetDirectoryName(linkPath), existentTarget);
            VerifySymbolicLinkAndResolvedTarget(
                linkPath: linkPath,
                expectedLinkTarget: existentTarget,
                targetPath: targetPath);
        }

        [Fact]
        public void CreateSymbolicLink_RelativeTargetPath_TargetExists_WithRedundantSegments()
        {
            // /path/to/link -> /path/to/../to/existingtarget

            string linkPath = GetRandomLinkPath();
            string fileName = GetRandomFileName();
            string dirPath = Path.GetDirectoryName(linkPath);
            string dirName = Path.GetFileName(dirPath);
            string targetPath = Path.Join(dirPath, fileName);
            string existentTarget = Path.Join("..", dirName, fileName);
            VerifySymbolicLinkAndResolvedTarget(
                linkPath: linkPath,
                expectedLinkTarget: existentTarget,
                targetPath: targetPath);
        }

        [Fact]
        public void CreateSymbolicLink_AbsoluteTargetPath_TargetExists()
        {
            // /path/to/link -> /path/to/existingtarget

            string linkPath = GetRandomLinkPath();
            string targetPath = GetRandomFilePath();
            VerifySymbolicLinkAndResolvedTarget(
                linkPath: linkPath,
                expectedLinkTarget: targetPath,
                targetPath: targetPath);
        }

        [Fact]
        public void CreateSymbolicLink_AbsoluteTargetPath_TargetExists_WithRedundantSegments()
        {
            // /path/to/link -> /path/to/../to/existingtarget

            string linkPath = GetRandomLinkPath();
            string fileName = GetRandomFileName();
            string dirPath = Path.GetDirectoryName(linkPath);
            string dirName = Path.GetFileName(dirPath);
            string targetPath = Path.Join(dirPath, fileName);
            string existentTarget = Path.Join(dirPath, "..", dirName, fileName);
            VerifySymbolicLinkAndResolvedTarget(
                linkPath: linkPath,
                expectedLinkTarget: existentTarget,
                targetPath: targetPath);
        }

        [Fact]
        public void CreateSymbolicLink_RelativeTargetPath_NonExistentTarget()
        {
            // /path/to/link -> /path/to/nonexistenttarget

            string linkPath = GetRandomLinkPath();
            string nonExistentTarget = GetRandomFileName();
            VerifySymbolicLinkAndResolvedTarget(
                linkPath: linkPath,
                expectedLinkTarget: nonExistentTarget,
                targetPath: null); // do not create target
        }

        [Fact]
        public void CreateSymbolicLink_AbsoluteTargetPath_NonExistentTarget()
        {
            // /path/to/link -> /path/to/nonexistenttarget

            string linkPath = GetRandomLinkPath();
            string nonExistentTarget = GetRandomFilePath();
            VerifySymbolicLinkAndResolvedTarget(
                linkPath: linkPath,
                expectedLinkTarget: nonExistentTarget,
                targetPath: null); // do not create target
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveLinkTarget_FileSystemEntryExistsButIsNotALink(bool returnFinalTarget)
        {
            string path = GetRandomFilePath();
            CreateFileOrDirectory(path); // entry exists as a normal file, not as a link

            FileSystemInfo target = ResolveLinkTarget(path, expectedLinkTarget: null, returnFinalTarget);
            Assert.Null(target);
        }

        [Fact]
        public void ResolveLinkTarget_ReturnFinalTarget_Absolute()
        {
            string link1Path = GetRandomLinkPath();
            string link2Path = GetRandomLinkPath();
            string filePath = GetRandomFilePath();

            ResolveLinkTarget_ReturnFinalTarget(
                link1Path: link1Path,
                expectedLink1Target: link2Path,
                link2Path: link2Path,
                expectedLink2Target: filePath,
                filePath: filePath);
        }

        [Fact]
        public void ResolveLinkTarget_ReturnFinalTarget_Absolute_WithRedundantSegments()
        {
            string link1Path = GetRandomLinkPath();
            string link2Path = GetRandomLinkPath();
            string filePath = GetRandomFilePath();

            string dirPath = Path.GetDirectoryName(filePath);
            string dirName = Path.GetFileName(dirPath);

            string link2FileName = Path.GetFileName(link2Path);
            string fileName = Path.GetFileName(filePath);

            ResolveLinkTarget_ReturnFinalTarget(
                link1Path: link1Path,
                expectedLink1Target: Path.Join(dirPath, "..", dirName, link2FileName),
                link2Path: link2Path,
                expectedLink2Target: Path.Join(dirPath, "..", dirName, fileName),
                filePath: filePath);
        }

        [Fact]
        public void ResolveLinkTarget_ReturnFinalTarget_Relative()
        {
            string link1Path = GetRandomLinkPath();
            string link2Path =  GetRandomLinkPath();
            string filePath = GetRandomFilePath();

            string link2FileName = Path.GetFileName(link2Path);
            string fileName = Path.GetFileName(filePath);

            ResolveLinkTarget_ReturnFinalTarget(
                link1Path: link1Path,
                expectedLink1Target: link2FileName,
                link2Path: link2Path,
                expectedLink2Target: fileName,
                filePath: filePath);
        }

        [Fact]
        public void ResolveLinkTarget_ReturnFinalTarget_Relative_WithRedundantSegments()
        {
            string link1Path = GetRandomLinkPath();
            string link2Path =  GetRandomLinkPath();
            string filePath = GetRandomFilePath();

            string dirPath = Path.GetDirectoryName(filePath);
            string dirName = Path.GetFileName(dirPath);

            string link2FileName = Path.GetFileName(link2Path);
            string fileName = Path.GetFileName(filePath);

            ResolveLinkTarget_ReturnFinalTarget(
                link1Path: link1Path,
                expectedLink1Target: Path.Join("..", dirName, link2FileName),
                link2Path: link2Path,
                expectedLink2Target: Path.Join("..", dirName, fileName),
                filePath: filePath);
        }

        [Fact]
        public void DetectSymbolicLinkCycle()
        {
            // link1 -> link2
            //   ^        /
            //    \______/

            string link2Path = GetRandomFilePath();
            string link1Path = GetRandomFilePath();

            FileSystemInfo link1Info = CreateSymbolicLink(link1Path, link2Path);
            FileSystemInfo link2Info = CreateSymbolicLink(link2Path, link1Path);

            // Can get targets without following symlinks
            FileSystemInfo link1Target = ResolveLinkTarget(link1Path, expectedLinkTarget: link2Path);
            FileSystemInfo link2Target = ResolveLinkTarget(link2Path, expectedLinkTarget: link1Path);

            // Cannot get target when following symlinks
            Assert.Throws<IOException>(() => ResolveLinkTarget(link1Path, expectedLinkTarget: link2Path, returnFinalTarget: true));
            Assert.Throws<IOException>(() => ResolveLinkTarget(link2Path, expectedLinkTarget: link1Path, returnFinalTarget: true));
        }

        [Fact]
        public void CreateSymbolicLink_WrongTargetType()
        {
            // dirLink -> file
            // fileLink -> dir

            string targetPath = GetRandomFilePath();
            CreateFileOrDirectory(targetPath, createOpposite: true); // The underlying file system entry needs to be different
            Assert.Throws<IOException>(() => CreateSymbolicLink(GetRandomFilePath(), targetPath));
        }

        protected void ResolveLinkTarget_LinkDoesNotExist_Internal<T>() where T : Exception
        {
            // ? -> ?

            string path = GetRandomFilePath();
            Assert.Throws<T>(() => ResolveLinkTarget(path, expectedLinkTarget: null));
            Assert.Throws<T>(() => ResolveLinkTarget(path, expectedLinkTarget: null, returnFinalTarget: true));
        }

        private void VerifySymbolicLinkAndResolvedTarget(string linkPath, string expectedLinkTarget, string targetPath = null)
        {
            // linkPath -> expectedLinkTarget (created in targetPath if not null)

            if (targetPath != null)
            {
                CreateFileOrDirectory(targetPath);
            }

            FileSystemInfo link = CreateSymbolicLink(linkPath, expectedLinkTarget);
            if (targetPath == null)
            {
                // Behavior different between files and directories when target does not exist
                AssertExistsWhenNoTarget(link);
            }
            else
            {
                Assert.True(link.Exists); // The target file or directory was created above, so we report Exists of the target for both
            }

            FileSystemInfo target = ResolveLinkTarget(linkPath, expectedLinkTarget);
            AssertIsCorrectTypeAndDirectoryAttribute(target);
            Assert.True(Path.IsPathFullyQualified(target.FullName));
        }

        private void ResolveLinkTarget_ReturnFinalTarget(string link1Path, string expectedLink1Target, string link2Path, string expectedLink2Target, string filePath)
        {
            // link1Path -> expectedLink1Target (created in link2Path) -> expectedLink2Target (created in filePath)

            CreateFileOrDirectory(filePath);

            // link2 to target
            FileSystemInfo link2 = CreateSymbolicLink(link2Path, expectedLink2Target);
            Assert.True(link2.Exists);
            Assert.True(link2.Attributes.HasFlag(FileAttributes.ReparsePoint));
            AssertIsCorrectTypeAndDirectoryAttribute(link2);
            Assert.Equal(link2.LinkTarget, expectedLink2Target);

            // link1 to link2
            FileSystemInfo link1 = CreateSymbolicLink(link1Path, expectedLink1Target);
            Assert.True(link1.Exists);
            Assert.True(link1.Attributes.HasFlag(FileAttributes.ReparsePoint));
            AssertIsCorrectTypeAndDirectoryAttribute(link1);
            Assert.Equal(link1.LinkTarget, expectedLink1Target);

            // link1: do not follow symlinks
            FileSystemInfo link1Target = ResolveLinkTarget(link1Path, expectedLink1Target);
            Assert.True(link1Target.Exists);
            AssertIsCorrectTypeAndDirectoryAttribute(link1Target);
            Assert.True(link1Target.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(link1Target.FullName, link2Path);
            Assert.Equal(link1Target.LinkTarget, expectedLink2Target);

            // link2: do not follow symlinks
            FileSystemInfo link2Target = ResolveLinkTarget(link2Path, expectedLink2Target);
            Assert.True(link2Target.Exists);
            AssertIsCorrectTypeAndDirectoryAttribute(link2Target);
            Assert.False(link2Target.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(link2Target.FullName, filePath);
            Assert.Null(link2Target.LinkTarget);

            // link1: follow symlinks
            FileSystemInfo finalTarget = ResolveLinkTarget(link1Path, expectedLinkTarget: expectedLink1Target, returnFinalTarget: true);
            Assert.True(finalTarget.Exists);
            AssertIsCorrectTypeAndDirectoryAttribute(finalTarget);
            Assert.False(finalTarget.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(finalTarget.FullName, filePath);
        }
    }
}