// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_SymbolicLinks : BaseSymbolicLinks_FileSystemInfo
    {
        protected override bool IsDirectoryTest => false;

        protected override FileSystemInfo GetFileSystemInfo(string path) =>
            new FileInfo(path);

        protected override void CreateFileOrDirectory(string path, bool createOpposite = false)
        {
            if (!createOpposite)
            {
                File.Create(path).Dispose();
            }
            else
            {
                Directory.CreateDirectory(path);
            }
        }

        protected override void AssertIsCorrectTypeAndDirectoryAttribute(FileSystemInfo linkInfo)
        {
            if (linkInfo.Exists)
            {
                Assert.False(linkInfo.Attributes.HasFlag(FileAttributes.Directory));
            }
            Assert.True(linkInfo is FileInfo);
        }

        protected override void AssertLinkExists(FileSystemInfo link) =>
            Assert.True(link.Exists);

        [Fact]
        public void ResolveLinkTarget_Throws_NotExists() =>
            ResolveLinkTarget_Throws_NotExists_Internal<FileNotFoundException>();

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CreateSymbolicLink_PathToTarget_RelativeToLinkPath()
        {
            RemoteExecutor.Invoke(() => CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(false)).Dispose();
            RemoteExecutor.Invoke(() => CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(true)).Dispose();
        }
    }
}
