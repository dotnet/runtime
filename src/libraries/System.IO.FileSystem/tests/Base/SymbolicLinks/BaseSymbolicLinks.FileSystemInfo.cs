// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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

        protected override FileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget = false)
            => GetFileSystemInfo(linkPath).ResolveLinkTarget(returnFinalTarget);

        [Fact]
        public void LinkTarget_ReturnsNull_NotExists()
        {
            FileSystemInfo info = GetFileSystemInfo(GetRandomLinkPath());
            Assert.Null(info.LinkTarget);
        }

        [Fact]
        public void LinkTarget_ReturnsNull_NotALink()
        {
            string path = GetTestFilePath();
            CreateFileOrDirectory(path);
            FileSystemInfo info = GetFileSystemInfo(path);

            Assert.True(info.Exists);
            Assert.Null(info.LinkTarget);
        }

        [Theory]
        [MemberData(nameof(LinkTarget_PathToTarget_Data))]
        public void LinkTarget_Succeeds(string pathToTarget)
        {
            FileSystemInfo linkInfo = CreateSymbolicLink(GetRandomLinkPath(), pathToTarget);

            AssertLinkExists(linkInfo);
            AssertLinkTargetEquals(pathToTarget, linkInfo.LinkTarget);
        }

        public static IEnumerable<object[]> LinkTarget_PathToTarget_Data
        {
            get
            {
                foreach (string path in PathToTargetData)
                {
                    yield return new object[] { path };
                }
            }
        }
    }
}
