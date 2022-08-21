// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Tests
{
    public sealed class Directory_SetCurrentDirectory : FileCleanupTestBase
    {
        [Fact]
        public void Null_Path_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Directory.SetCurrentDirectory(null));
        }

        [Fact]
        public void Empty_Path_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Directory.SetCurrentDirectory(string.Empty));
        }

        [Fact]
        public void SetToNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() => Directory.SetCurrentDirectory(GetTestFilePath()));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetToValidOtherDirectory()
        {
            RemoteExecutor.Invoke(() =>
            {
                Directory.SetCurrentDirectory(TestDirectory);

                if ((File.GetAttributes(Path.GetDirectoryName(TestDirectory)) & FileAttributes.ReparsePoint) != 0)
                {
                    // If the temp directory is symlink, setting the current directory to a symlinked path will result
                    // in GetCurrentDirectory returning the absolute path that followed the symlink. We can only verify
                    // the test directory name in that case.
                    Assert.Equal(Path.GetFileName(TestDirectory), Path.GetFileName(Directory.GetCurrentDirectory()));
                }
                else
                {
                    Assert.Equal(TestDirectory, Directory.GetCurrentDirectory());
                }

                Directory.SetCurrentDirectory(Path.GetTempPath());
            }).Dispose();
        }

        public sealed class Directory_SetCurrentDirectory_SymLink : FileSystemTest
        {
            private static bool CanCreateSymbolicLinksAndRemoteExecutorSupported => MountHelper.CanCreateSymbolicLinks && RemoteExecutor.IsSupported;

            [ConditionalFact(nameof(CanCreateSymbolicLinksAndRemoteExecutorSupported))]
            public void SetToPathContainingSymLink()
            {
                RemoteExecutor.Invoke(() =>
                {
                    var path = GetTestFilePath();
                    var linkPath = GetRandomLinkPath();

                    Directory.CreateDirectory(path);
                    Assert.True(MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: true));

                    // Both the symlink and the target exist
                    Assert.True(Directory.Exists(path), "path should exist");
                    Assert.True(Directory.Exists(linkPath), "linkPath should exist");

                    // Set Current Directory to symlink
                    Directory.SetCurrentDirectory(linkPath);
                    if (OperatingSystem.IsWindows())
                    {
                        Assert.Equal(linkPath, Directory.GetCurrentDirectory());
                    }
                    else
                    {
                        if ((File.GetAttributes(Path.GetDirectoryName(TestDirectory)) & FileAttributes.ReparsePoint) != 0)
                        {
                            // If the temp directory is symlink, setting the current directory to a symlinked path will result
                            // in GetCurrentDirectory returning the absolute path that followed the symlink. We can only verify
                            // the test directory name in that case.
                            Assert.Equal(Path.GetFileName(path), Path.GetFileName(Directory.GetCurrentDirectory()));
                        }
                        else
                        {
                            Assert.Equal(path, Directory.GetCurrentDirectory());
                        }
                    }

                    Directory.SetCurrentDirectory(Path.GetTempPath());
                }).Dispose();
            }
        }
    }
}
