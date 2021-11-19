// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    public class File_SymbolicLinks : BaseSymbolicLinks_FileSystem
    {
        protected override bool IsDirectoryTest => false;

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

        protected override FileSystemInfo CreateSymbolicLink(string path, string pathToTarget) =>
            File.CreateSymbolicLink(path, pathToTarget);

        protected override FileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget) =>
            File.ResolveLinkTarget(linkPath, returnFinalTarget);

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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UnsupportedLink_ReturnsNull()
        {
            string unsupportedLinkPath = GetAppExecLinkPath();
            if (unsupportedLinkPath is null)
            {
                return;
            }

            Assert.Null(File.ResolveLinkTarget(unsupportedLinkPath, false));
            Assert.Null(File.ResolveLinkTarget(unsupportedLinkPath, true));
        }


        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CreateSymbolicLink_PathToTarget_RelativeToLinkPath()
        {
            RemoteExecutor.Invoke(() => CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(false)).Dispose();
            RemoteExecutor.Invoke(() => CreateSymbolicLink_PathToTarget_RelativeToLinkPath_Internal(true)).Dispose();
        }
    }
}
