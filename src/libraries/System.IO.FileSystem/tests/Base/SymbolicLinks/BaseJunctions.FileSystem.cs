// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public abstract class BaseJunctions_FileSystem : BaseSymbolicLinks
    {
        protected DirectoryInfo CreateJunction(string junctionPath, string targetPath)
        {
            Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));
            DirectoryInfo junctionInfo = new(junctionPath);
            return junctionInfo;
        }

        protected abstract DirectoryInfo CreateDirectory(string path);

        protected abstract FileSystemInfo ResolveLinkTarget(string junctionPath, bool returnFinalTarget);

        [Fact]
        public void Junction_ResolveLinkTarget()
        {
            string junctionPath = GetRandomLinkPath();
            string targetPath = GetRandomDirPath();

            CreateDirectory(targetPath);
            DirectoryInfo junctionInfo = CreateJunction(junctionPath, targetPath);

            FileSystemInfo? actualTargetInfo = ResolveLinkTarget(junctionPath, returnFinalTarget: false);
            Assert.True(actualTargetInfo is DirectoryInfo);
            Assert.Equal(targetPath, actualTargetInfo.FullName);
            Assert.Equal(targetPath, junctionInfo.LinkTarget);
        }
    }
}
