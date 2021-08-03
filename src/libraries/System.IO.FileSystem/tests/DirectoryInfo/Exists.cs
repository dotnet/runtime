// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public partial class DirectoryInfo_Exists : FileSystemTest
    {
        [Fact]
        public void InitializeExistsBeforeCreation()
        {
            string dirName = GetTestFilePath();
            DirectoryInfo di = new DirectoryInfo(dirName);
            // don't check it, data has not yet been init'ed
            Directory.CreateDirectory(dirName);
            // data will be init'ed at the time of calling exists
            Assert.True(di.Exists);
        }

        [Fact]
        public void InitializeExistsAfterCreation()
        {
            string dirName = GetTestFilePath();
            DirectoryInfo di = new DirectoryInfo(dirName);

            Assert.False(di.Exists);
            Directory.CreateDirectory(dirName);

            // data should be stale
            Assert.False(di.Exists);

            // force refresh
            di.Refresh();
            Assert.True(di.Exists);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't read root in appcontainer
        public void Root()
        {
            Assert.True(new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory())).Exists);
        }

        [Fact]
        public void DotPath()
        {
            Assert.True(new DirectoryInfo(Path.Combine(TestDirectory, ".")).Exists);
        }

        [Fact]
        public void DotDotPath()
        {
            Assert.True(new DirectoryInfo(Path.Combine(TestDirectory, Path.GetRandomFileName(), "..")).Exists);
        }

        [Fact]
        public void NonExistentDirectories()
        {
            Assert.False(new DirectoryInfo("Da drar vi til fjells").Exists);
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingDirectory(char trailingChar)
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(Path.Combine(path, "file" + trailingChar));
            Assert.False(info.Exists);
        }

        [Fact]
        [PlatformSpecific(CaseInsensitivePlatforms)]
        public void CaseInsensitivity()
        {
            Assert.True(new DirectoryInfo(TestDirectory.ToUpperInvariant()).Exists);
            Assert.True(new DirectoryInfo(TestDirectory.ToLowerInvariant()).Exists);
        }

        [Fact]
        [PlatformSpecific(CaseSensitivePlatforms)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52857", TestPlatforms.MacCatalyst)]
        public void CaseSensitivity()
        {
            Assert.False(new DirectoryInfo(TestDirectory.ToUpperInvariant()).Exists);
            Assert.False(new DirectoryInfo(TestDirectory.ToLowerInvariant()).Exists);
        }

        [Fact]
        public void TrueForCreatedDirectory()
        {
            DirectoryInfo di = Directory.CreateDirectory(GetTestFilePath());
            Assert.True(di.Exists);
        }

        [Fact]
        public void TrueForNewDirectoryInfo()
        {
            string dirName = GetTestFilePath();
            Directory.CreateDirectory(dirName);

            DirectoryInfo di = new DirectoryInfo(dirName);
            Assert.True(di.Exists);
        }

        [Fact]
        public void FalseForFile()
        {
            string fileName = GetTestFilePath();
            File.Create(fileName).Dispose();
            DirectoryInfo di = new DirectoryInfo(fileName);
            Assert.False(di.Exists);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]  // Uses P/Invokes
        public void FalseForNonRegularFile()
        {
            string fileName = GetTestFilePath();
            Assert.Equal(0, mkfifo(fileName, 0));
            DirectoryInfo di = new DirectoryInfo(fileName);
            Assert.False(di.Exists);
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymlinkToNewDirectoryInfo()
        {
            string path = GetTestFilePath();
            new DirectoryInfo(path).Create();

            string linkPath = GetTestFilePath();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true));

            Assert.True(new DirectoryInfo(path).Exists);
            Assert.True(new DirectoryInfo(linkPath).Exists);
        }

        [ConditionalFact(nameof(CanCreateSymbolicLinks))]
        public void SymLinksMayExistIndependentlyOfTarget()
        {
            var path = GetTestFilePath();
            var linkPath = GetTestFilePath();

            var pathFI = new DirectoryInfo(path);
            var linkPathFI = new DirectoryInfo(linkPath);

            pathFI.Create();
            Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true));

            // Both the symlink and the target exist
            pathFI.Refresh();
            linkPathFI.Refresh();
            Assert.True(pathFI.Exists, "path should exist");
            Assert.True(linkPathFI.Exists, "linkPath should exist");

            // Delete the target.  The symlink should still exist, but on Unix it'll now
            // be considered a file and won't exist as a directory.
            pathFI.Delete();
            pathFI.Refresh();
            Assert.False(pathFI.Exists, "path should now not exist");
            linkPathFI.Refresh();
            if (OperatingSystem.IsWindows())
            {
                Assert.True(linkPathFI.Exists, "linkPath directory should still exist");
                Assert.False(File.Exists(linkPath), "linkPath file should not exist");

                Directory.Delete(linkPath);
            }
            else
            {
                Assert.False(linkPathFI.Exists, "linkPath directory should no longer exist");
                Assert.True(File.Exists(linkPath), "linkPath file should now exist");

                File.Delete(linkPath);
            }

            linkPathFI.Refresh();
            Assert.False(linkPathFI.Exists, "linkPath should no longer exist");
        }
    }
}
