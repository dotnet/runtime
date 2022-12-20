// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalClass(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
    public class Junctions : BaseSymbolicLinks
    {
        private DirectoryInfo CreateJunction(string junctionPath, string targetPath)
        {
            Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));
            DirectoryInfo junctionInfo = new(junctionPath);
            return junctionInfo;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Junction_ResolveLinkTarget(bool returnFinalTarget)
        {
            string junctionPath = GetRandomLinkPath();
            string targetPath = GetRandomDirPath();

            Directory.CreateDirectory(targetPath);
            DirectoryInfo junctionInfo = CreateJunction(junctionPath, targetPath);

            FileSystemInfo? targetFromDirectoryInfo = junctionInfo.ResolveLinkTarget(returnFinalTarget);
            FileSystemInfo? targetFromDirectory = Directory.ResolveLinkTarget(junctionPath, returnFinalTarget);

            Assert.True(targetFromDirectoryInfo is DirectoryInfo);
            Assert.True(targetFromDirectory is DirectoryInfo);

            Assert.Equal(targetPath, junctionInfo.LinkTarget);

            Assert.Equal(targetPath, targetFromDirectoryInfo.FullName);
            Assert.Equal(targetPath, targetFromDirectory.FullName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Junction_ResolveLinkTarget_WithIndirection(bool returnFinalTarget)
        {
            string firstJunctionPath = GetRandomLinkPath();
            string middleJunctionPath = GetRandomLinkPath();
            string targetPath = GetRandomDirPath();

            Directory.CreateDirectory(targetPath);
            CreateJunction(middleJunctionPath, targetPath);
            DirectoryInfo firstJunctionInfo = CreateJunction(firstJunctionPath, middleJunctionPath);

            string expectedTargetPath = returnFinalTarget ? targetPath : middleJunctionPath;

            FileSystemInfo? targetFromDirectoryInfo = firstJunctionInfo.ResolveLinkTarget(returnFinalTarget);
            FileSystemInfo? targetFromDirectory = Directory.ResolveLinkTarget(firstJunctionPath, returnFinalTarget);

            Assert.True(targetFromDirectoryInfo is DirectoryInfo);
            Assert.True(targetFromDirectory is DirectoryInfo);

            // Always the immediate target
            Assert.Equal(middleJunctionPath, firstJunctionInfo.LinkTarget);

            Assert.Equal(expectedTargetPath, targetFromDirectoryInfo.FullName);
            Assert.Equal(expectedTargetPath, targetFromDirectory.FullName);
        }

        [Theory]
        [MemberData(nameof(Junction_ResolveLinkTarget_PathToTarget_Data))]
        public void Junction_ResolveLinkTarget_Succeeds(string pathToTarget, bool returnFinalTarget)
        {
            string linkPath = GetRandomLinkPath();
            FileSystemInfo linkInfo = CreateJunction(linkPath, pathToTarget);

            // Junctions are always created with absolute targets, even if a relative path is passed.
            string expectedTarget = Path.GetFullPath(pathToTarget);

            Assert.True(linkInfo.Exists);
            Assert.IsType<DirectoryInfo>(linkInfo);
            Assert.True(linkInfo.Attributes.HasFlag(FileAttributes.Directory));
            Assert.Equal(expectedTarget, linkInfo.LinkTarget);

            FileSystemInfo? targetFromDirectoryInfo = linkInfo.ResolveLinkTarget(returnFinalTarget);
            FileSystemInfo? targetFromDirectory = Directory.ResolveLinkTarget(linkPath, returnFinalTarget);

            Assert.NotNull(targetFromDirectoryInfo);
            Assert.NotNull(targetFromDirectory);

            Assert.False(targetFromDirectoryInfo.Exists);
            Assert.False(targetFromDirectory.Exists);


            Assert.Equal(expectedTarget, targetFromDirectoryInfo.FullName);
            Assert.Equal(expectedTarget, targetFromDirectory.FullName);
        }

        [Theory]
        [MemberData(nameof(Junction_LinkTarget_PathToTarget_Data))]
        public void Junction_LinkTarget_Succeeds(string pathToTarget)
        {
            FileSystemInfo linkInfo = CreateJunction(GetRandomLinkPath(), pathToTarget);
            Assert.True(linkInfo.Exists);
            Assert.Equal(Path.GetFullPath(pathToTarget), linkInfo.LinkTarget);
        }
    }
}
