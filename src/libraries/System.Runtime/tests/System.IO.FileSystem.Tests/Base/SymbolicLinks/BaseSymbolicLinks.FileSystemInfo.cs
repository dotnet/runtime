// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    // Contains test methods that can be used for FileInfo and DirectoryInfo.
    public abstract class BaseSymbolicLinks_FileSystemInfo : BaseSymbolicLinks_FileSystem
    {
        // Creates and returns FileSystemInfo instance by calling either the DirectoryInfo or FileInfo constructor and passing the path.
        protected abstract FileSystemInfo GetFileSystemInfo(string path);

        protected override FileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
        {
            FileSystemInfo link = GetFileSystemInfo(path);
            link.CreateAsSymbolicLink(pathToTarget);
            return link;
        }

        protected override FileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget)
            => GetFileSystemInfo(linkPath).ResolveLinkTarget(returnFinalTarget);

        private void Delete(string path)
        {
            if (IsDirectoryTest)
            {
                Directory.Delete(path);
            }
            else
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LinkTarget_ReturnsNull_NotExists()
        {
            FileSystemInfo info = GetFileSystemInfo(GetRandomLinkPath());
            Assert.Null(info.LinkTarget);
        }

        [Fact]
        public void LinkTarget_ReturnsNull_NotALink()
        {
            string path = GetRandomFilePath();
            CreateFileOrDirectory(path);
            FileSystemInfo info = GetFileSystemInfo(path);

            Assert.True(info.Exists);
            Assert.Null(info.LinkTarget);
        }

        [Theory]
        [MemberData(nameof(SymbolicLink_LinkTarget_PathToTarget_Data))]
        public void LinkTarget_Succeeds(string pathToTarget)
        {
            FileSystemInfo linkInfo = CreateSymbolicLink(GetRandomLinkPath(), pathToTarget);

            AssertLinkExists(linkInfo);
            Assert.Equal(pathToTarget, linkInfo.LinkTarget);
        }

        [Fact]
        public void LinkTarget_RefreshesCorrectly()
        {
            string path = GetRandomLinkPath();
            string pathToTarget = GetRandomFilePath();
            CreateFileOrDirectory(pathToTarget);
            FileSystemInfo linkInfo = CreateSymbolicLink(path, pathToTarget);
            Assert.Equal(pathToTarget, linkInfo.LinkTarget);

            Delete(path);
            Assert.Equal(pathToTarget, linkInfo.LinkTarget);

            linkInfo.Refresh();
            Assert.Null(linkInfo.LinkTarget);

            string newPathToTarget = GetRandomFilePath();
            CreateFileOrDirectory(newPathToTarget);
            FileSystemInfo newLinkInfo = CreateSymbolicLink(path, newPathToTarget);

            linkInfo.Refresh();
            Assert.Equal(newPathToTarget, linkInfo.LinkTarget);
            Assert.Equal(newLinkInfo.LinkTarget, linkInfo.LinkTarget);
        }
    }
}
