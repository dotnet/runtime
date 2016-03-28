// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public sealed class Directory_SetCurrentDirectory : RemoteExecutorTestBase
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
        public void SetToValidOtherDirectory()
        {
            RemoteInvoke(() =>
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(TestDirectory);
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Assert.Equal(TestDirectory, Directory.GetCurrentDirectory());
                    }
                }
                finally
                {
                    Directory.SetCurrentDirectory(currentDirectory);
                }
                Assert.Equal(currentDirectory, Directory.GetCurrentDirectory());
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void SetToNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            RemoteInvoke(() =>
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Assert.Throws<DirectoryNotFoundException>(() => Directory.SetCurrentDirectory(GetTestFilePath()));
                    }
                    else
                    {
                        Assert.Throws<FileNotFoundException>(() => Directory.SetCurrentDirectory(GetTestFilePath()));
                    }
                    Assert.Equal(currentDirectory, Directory.GetCurrentDirectory());
                }
                finally
                {
                    Directory.SetCurrentDirectory(currentDirectory);
                }
                return SuccessExitCode;
            }).Dispose();
        }
    }
}
