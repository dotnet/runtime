// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    [ConditionalClass(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
    public class DirectoryInfo_SymbolicLinks : BaseSymbolicLinks_FileSystemInfo
    {
        protected override bool IsDirectoryTest => true;

        protected override FileSystemInfo GetFileSystemInfo(string path) =>
            new DirectoryInfo(path);

        protected override void CreateFileOrDirectory(string path, bool createOpposite = false)
        {
            if (!createOpposite)
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                File.Create(path).Dispose();
            }
        }

        protected override void AssertIsCorrectTypeAndDirectoryAttribute(FileSystemInfo linkInfo)
        {
            if (linkInfo.Exists)
            {
                Assert.True(linkInfo.Attributes.HasFlag(FileAttributes.Directory));
            }
            Assert.True(linkInfo is DirectoryInfo);
        }

        protected override void AssertLinkExists(FileSystemInfo link)
        {
            if (PlatformDetection.IsWindows)
            {
                Assert.True(link.Exists);
            }
            else
            {
                // Unix requires the target to be a directory that exists.
                Assert.False(link.Exists);
            }
        }

        [Fact]
        public void ResolveLinkTarget_Throws_NotExists() =>
            ResolveLinkTarget_Throws_NotExists_Internal<DirectoryNotFoundException>();

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CreateSymbolicLink_PathToTarget_RelativeToLinkPath()
        {
            RemoteExecutor.Invoke(() => CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(false)).Dispose();
            RemoteExecutor.Invoke(() => CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(true)).Dispose();
        }
    }
}
