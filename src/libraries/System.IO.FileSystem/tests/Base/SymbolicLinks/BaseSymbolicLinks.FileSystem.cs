// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    // Contains test methods that can be used for FileInfo, DirectoryInfo, File or Directory.
    public abstract class BaseSymbolicLinks_FileSystem : BaseSymbolicLinks
    {
        protected abstract bool IsDirectoryTest { get; }

        /// <summary>Creates a new file or directory depending on the implementing class.
        /// If createOpposite is true, creates a directory if the implementing class is for File or FileInfo, or
        /// creates a file if the implementing class is for Directory or DirectoryInfo.</summary>
        protected abstract void CreateFileOrDirectory(string path, bool createOpposite = false);

        protected abstract void AssertIsCorrectTypeAndDirectoryAttribute(FileSystemInfo linkInfo);

        protected abstract void AssertLinkExists(FileSystemInfo linkInfo);

        /// <summary>Calls the actual public API for creating a symbolic link.</summary>
        protected abstract FileSystemInfo CreateSymbolicLink(string path, string pathToTarget);

        private void CreateSymbolicLink_Opposite(string path, string pathToTarget)
        {
            if (IsDirectoryTest)
            {
                File.CreateSymbolicLink(path, pathToTarget);
            }
            else
            {
                Directory.CreateSymbolicLink(path, pathToTarget);
            }
        }

        /// <summary>Calls the actual public API for resolving the symbolic link target.</summary>
        protected abstract FileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget);

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

        protected void ResolveLinkTarget_Throws_NotExists_Internal<T>() where T : Exception
        {
            string path = GetRandomFilePath();
            Assert.Throws<T>(() => ResolveLinkTarget(path, returnFinalTarget: false));
            Assert.Throws<T>(() => ResolveLinkTarget(path, returnFinalTarget: true));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveLinkTarget_ReturnsNull_NotALink(bool returnFinalTarget)
        {
            string path = GetRandomFilePath();
            CreateFileOrDirectory(path);
            Assert.Null(ResolveLinkTarget(path, returnFinalTarget));
        }

        [Theory]
        [MemberData(nameof(ResolveLinkTarget_PathToTarget_Data))]
        public void ResolveLinkTarget_Succeeds(string pathToTarget, bool returnFinalTarget)
        {
            string linkPath = GetRandomLinkPath();
            FileSystemInfo linkInfo = CreateSymbolicLink(linkPath, pathToTarget);
            AssertLinkExists(linkInfo);
            AssertIsCorrectTypeAndDirectoryAttribute(linkInfo);
            Assert.Equal(pathToTarget, linkInfo.LinkTarget);

            FileSystemInfo targetInfo = ResolveLinkTarget(linkPath, returnFinalTarget);
            Assert.NotNull(targetInfo);
            Assert.False(targetInfo.Exists);

            string expectedTargetFullName = Path.IsPathFullyQualified(pathToTarget) ?
                pathToTarget : Path.GetFullPath(Path.Join(Path.GetDirectoryName(linkPath), pathToTarget));

            Assert.Equal(expectedTargetFullName, targetInfo.FullName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveLinkTarget_FileSystemEntryExistsButIsNotALink(bool returnFinalTarget)
        {
            string path = GetRandomFilePath();
            CreateFileOrDirectory(path); // entry exists as a normal file, not as a link

            FileSystemInfo target = ResolveLinkTarget(path, returnFinalTarget);
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
                link1Target: link2Path,
                link2Path: link2Path,
                link2Target: filePath,
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
                link1Target: Path.Join(dirPath, "..", dirName, link2FileName),
                link2Path: link2Path,
                link2Target: Path.Join(dirPath, "..", dirName, fileName),
                filePath: filePath);
        }

        [Fact]
        public void ResolveLinkTarget_ReturnFinalTarget_Relative()
        {
            string link1Path = GetRandomLinkPath();
            string link2Path = GetRandomLinkPath();
            string filePath = GetRandomFilePath();

            string link2FileName = Path.GetFileName(link2Path);
            string fileName = Path.GetFileName(filePath);

            ResolveLinkTarget_ReturnFinalTarget(
                link1Path: link1Path,
                link1Target: link2FileName,
                link2Path: link2Path,
                link2Target: fileName,
                filePath: filePath);
        }

        [Fact]
        public void ResolveLinkTarget_ReturnFinalTarget_Relative_WithRedundantSegments()
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
                link1Target: Path.Join("..", dirName, link2FileName),
                link2Path: link2Path,
                link2Target: Path.Join("..", dirName, fileName),
                filePath: filePath);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(10, false)]
        [InlineData(20, false)]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(20, true)]
        public void ResolveLinkTarget_ReturnFinalTarget_ChainOfLinks_Succeeds(int length, bool relative)
        {
            string target = GetRandomFilePath();
            CreateFileOrDirectory(target);

            string tail = CreateChainOfLinks(target, length, relative);
            FileSystemInfo targetInfo = ResolveLinkTarget(tail, returnFinalTarget: true);
            Assert.Equal(target, targetInfo.FullName);
        }

        [Theory]
        // 100 is way beyond the limit (63 in Windows and 40 in Unix), we just want to make sure that a nice exception is thrown when its exceeded.
        // We also don't want to test for a very precise limit given that it is very inconsistent across Windows versions.
        [InlineData(100, false)]
        [InlineData(100, true)]
        public void ResolveLinkTarget_ReturnFinalTarget_ChainOfLinks_ExceedsLimit_Throws(int length, bool relative)
        {
            string target = GetRandomFilePath();
            CreateFileOrDirectory(target);

            string tail = CreateChainOfLinks(target, length, relative);
            Assert.Throws<IOException>(() => ResolveLinkTarget(tail, returnFinalTarget: true));
        }

        private string CreateChainOfLinks(string target, int length, bool relative)
        {
            string previousPath = target;

            for (int i = 0; i < length; i++)
            {
                string currentLinkPath = GetRandomLinkPath();
                CreateSymbolicLink(currentLinkPath, relative ? Path.GetFileName(previousPath) : previousPath);
                previousPath = currentLinkPath;
            }

            return previousPath;
        }

        [Fact]
        public void DetectSymbolicLinkCycle()
        {
            // link1 -> link2 -> link1 (cycle)

            string link2Path = GetRandomFilePath();
            string link1Path = GetRandomFilePath();

            FileSystemInfo link1Info = CreateSymbolicLink(link1Path, link2Path);
            FileSystemInfo link2Info = CreateSymbolicLink(link2Path, link1Path);

            // Can get targets without following symlinks
            FileSystemInfo link1Target = ResolveLinkTarget(link1Path, returnFinalTarget: false);
            FileSystemInfo link2Target = ResolveLinkTarget(link2Path, returnFinalTarget: false);

            // Cannot get target when following symlinks
            Assert.Throws<IOException>(() => ResolveLinkTarget(link1Path, returnFinalTarget: true));
            Assert.Throws<IOException>(() => ResolveLinkTarget(link2Path, returnFinalTarget: true));
        }

        [Fact]
        public void DetectLinkReferenceToSelf()
        {
            // link -> link (reference to itself)

            string linkPath = GetRandomFilePath();
            FileSystemInfo linkInfo = CreateSymbolicLink(linkPath, linkPath);

            // Can get target without following symlinks
            FileSystemInfo linkTarget = ResolveLinkTarget(linkPath, returnFinalTarget: false);

            // Cannot get target when following symlinks
            Assert.Throws<IOException>(() => ResolveLinkTarget(linkPath, returnFinalTarget: true));
        }

        [Fact]
        public void CreateSymbolicLink_WrongTargetType_Throws()
        {
            // dirLink -> file
            // fileLink -> dir

            string targetPath = GetRandomFilePath();
            CreateFileOrDirectory(targetPath, createOpposite: true); // The underlying file system entry needs to be different
            Assert.Throws<IOException>(() => CreateSymbolicLink(GetRandomFilePath(), targetPath));
        }

        [Fact]
        public void CreateSymbolicLink_WrongTargetType_Indirect_Throws()
        {
            // link-2 (dir) -> link-1 (file) -> file
            // link-2 (file) -> link-1 (dir) -> dir
            string targetPath = GetRandomFilePath();
            string firstLinkPath = GetRandomFilePath();
            string secondLinkPath = GetRandomFilePath();

            CreateFileOrDirectory(targetPath, createOpposite: true);
            CreateSymbolicLink_Opposite(firstLinkPath, targetPath);

            Assert.Throws<IOException>(() => CreateSymbolicLink(secondLinkPath, firstLinkPath));
        }

        [Fact]
        public void CreateSymbolicLink_CorrectTargetType_Indirect_Succeeds()
        {
            // link-2 (file) -> link-1 (file) -> file
            // link-2 (dir) -> link-1 (dir) -> dir
            string targetPath = GetRandomFilePath();
            string firstLinkPath = GetRandomFilePath();
            string secondLinkPath = GetRandomFilePath();

            CreateFileOrDirectory(targetPath, createOpposite: false);
            CreateSymbolicLink(firstLinkPath, targetPath);

            FileSystemInfo secondLinkInfo = CreateSymbolicLink(secondLinkPath, firstLinkPath);
            Assert.Equal(firstLinkPath, secondLinkInfo.LinkTarget);
            Assert.Equal(targetPath, secondLinkInfo.ResolveLinkTarget(true).FullName);
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
                AssertLinkExists(link);
            }
            else
            {
                Assert.True(link.Exists); // The target file or directory was created above, so we report Exists of the target for both
            }

            FileSystemInfo target = ResolveLinkTarget(linkPath, returnFinalTarget: false);
            AssertIsCorrectTypeAndDirectoryAttribute(target);
            Assert.True(Path.IsPathFullyQualified(target.FullName));
        }

        /// <summary>
        /// Creates and Resolves a chain of links.
        /// link1 -> link2 -> file
        /// </summary>
        private void ResolveLinkTarget_ReturnFinalTarget(string link1Path, string link1Target, string link2Path, string link2Target, string filePath)
        {
            Assert.True(Path.IsPathFullyQualified(link1Path));
            Assert.True(Path.IsPathFullyQualified(link2Path));
            Assert.True(Path.IsPathFullyQualified(filePath));

            CreateFileOrDirectory(filePath);

            // link2 to file
            FileSystemInfo link2Info = CreateSymbolicLink(link2Path, link2Target);
            Assert.True(link2Info.Exists);
            Assert.True(link2Info.Attributes.HasFlag(FileAttributes.ReparsePoint));
            AssertIsCorrectTypeAndDirectoryAttribute(link2Info);
            Assert.Equal(link2Target, link2Info.LinkTarget);

            // link1 to link2
            FileSystemInfo link1Info = CreateSymbolicLink(link1Path, link1Target);
            Assert.True(link1Info.Exists);
            Assert.True(link1Info.Attributes.HasFlag(FileAttributes.ReparsePoint));
            AssertIsCorrectTypeAndDirectoryAttribute(link1Info);
            Assert.Equal(link1Target, link1Info.LinkTarget);

            // link1: do not follow symlinks
            FileSystemInfo link1TargetInfo = ResolveLinkTarget(link1Path, returnFinalTarget: false);
            Assert.True(link1TargetInfo.Exists);
            AssertIsCorrectTypeAndDirectoryAttribute(link1TargetInfo);
            Assert.True(link1TargetInfo.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(link2Path, link1TargetInfo.FullName);
            Assert.Equal(link2Target, link1TargetInfo.LinkTarget);

            // link2: do not follow symlinks
            FileSystemInfo link2TargetInfo = ResolveLinkTarget(link2Path, returnFinalTarget: false);
            Assert.True(link2TargetInfo.Exists);
            AssertIsCorrectTypeAndDirectoryAttribute(link2TargetInfo);
            Assert.False(link2TargetInfo.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(filePath, link2TargetInfo.FullName);
            Assert.Null(link2TargetInfo.LinkTarget);

            // link1: follow symlinks
            FileSystemInfo finalTarget = ResolveLinkTarget(link1Path, returnFinalTarget: true);
            Assert.True(finalTarget.Exists);
            AssertIsCorrectTypeAndDirectoryAttribute(finalTarget);
            Assert.False(finalTarget.Attributes.HasFlag(FileAttributes.ReparsePoint));
            Assert.Equal(filePath, finalTarget.FullName);
        }

        protected void CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(bool createOpposite)
        {
            string tempCwd = GetRandomDirPath();
            Directory.CreateDirectory(tempCwd);
            Directory.SetCurrentDirectory(tempCwd);

            // Create a dummy file or directory in cwd.
            string fileOrDirectoryInCwd = GetRandomFileName();
            CreateFileOrDirectory(fileOrDirectoryInCwd, createOpposite);

            string oneLevelUpPath = Path.Combine(tempCwd, "one-level-up");
            Directory.CreateDirectory(oneLevelUpPath);
            string linkPath = Path.Combine(oneLevelUpPath, GetRandomLinkName());

            // Create a link with a similar Target Path to the one of our dummy file or directory.
            FileSystemInfo linkInfo = CreateSymbolicLink(linkPath, fileOrDirectoryInCwd);
            FileSystemInfo targetInfo = linkInfo.ResolveLinkTarget(returnFinalTarget: false);

            // Verify that Target is resolved and is relative to Link's directory and not to the cwd.
            Assert.False(targetInfo.Exists);
            Assert.Equal(Path.GetDirectoryName(linkInfo.FullName), Path.GetDirectoryName(targetInfo.FullName));
        }

        public static IEnumerable<object[]> ResolveLinkTarget_PathToTarget_Data
        {
            get
            {
                foreach (string path in PathToTargetData)
                {
                    yield return new object[] { path, false };
                    yield return new object[] { path, true };
                }
            }
        }

        internal static IEnumerable<string> PathToTargetData
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    //Non-rooted relative
                    yield return "foo";
                    yield return @".\foo";
                    yield return @"..\foo";
                    // Rooted relative
                    yield return @"\foo";
                    // Rooted absolute
                    yield return Path.Combine(Path.GetTempPath(), "foo");
                    // Extended DOS
                    yield return Path.Combine(@"\\?\", Path.GetTempPath(), "foo");
                    // UNC
                    yield return @"\\SERVER\share\path";
                }
                else
                {
                    //Non-rooted relative
                    yield return "foo";
                    yield return "./foo";
                    yield return "../foo";
                    // Rooted relative
                    yield return "/foo";
                    // Rooted absolute
                    Path.Combine(Path.GetTempPath(), "foo");
                }
            }
        }
    }
}
