// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class Junctions : BaseSymbolicLinks
    {
        protected DirectoryInfo CreateJunction(string junctionPath, string targetPath)
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

            FileSystemInfo? actualTargetInfo = junctionInfo.ResolveLinkTarget(returnFinalTarget);
            Assert.True(actualTargetInfo is DirectoryInfo);
            Assert.Equal(targetPath, actualTargetInfo.FullName);
            Assert.Equal(targetPath, junctionInfo.LinkTarget);
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
            _ = CreateJunction(middleJunctionPath, targetPath);
            DirectoryInfo firstJunctionInfo = CreateJunction(firstJunctionPath, middleJunctionPath);

            string expectedTargetPath = returnFinalTarget ? targetPath : middleJunctionPath;

            FileSystemInfo? actualTargetInfo = firstJunctionInfo.ResolveLinkTarget(returnFinalTarget);

            Assert.True(actualTargetInfo is DirectoryInfo);
            Assert.Equal(expectedTargetPath, actualTargetInfo.FullName);

            // Always the immediate target
            Assert.Equal(middleJunctionPath, firstJunctionInfo.LinkTarget);
        }
    }
}
