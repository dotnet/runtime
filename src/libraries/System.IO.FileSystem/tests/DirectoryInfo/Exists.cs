// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public partial class DirectoryInfo_Exists : FileSystemTest
    {
        [Fact]
        public void InitializeExistsBeforeCreation()
        {
            string dirName = GetTestFilePath();
            DirectoryInfo di = new DirectoryInfo(dirName);
            // don't check it, data has not yet been init'ed
            Directory.CreateDirectory(dirName);
            // data will be init'ed at the time of calling exists
            Assert.True(di.Exists);
        }

        [Fact]
        public void InitializeExistsAfterCreation()
        {
            string dirName = GetTestFilePath();
            DirectoryInfo di = new DirectoryInfo(dirName);

            Assert.False(di.Exists);
            Directory.CreateDirectory(dirName);

            // data should be stale
            Assert.False(di.Exists);

            // force refresh
            di.Refresh();
            Assert.True(di.Exists);
        }

        [Fact]
        public void DotPath()
        {
            Assert.True(new DirectoryInfo(Path.Combine(TestDirectory, ".")).Exists);
        }

        [Fact]
        public void DotDotPath()
        {
            Assert.True(new DirectoryInfo(Path.Combine(TestDirectory, Path.GetRandomFileName(), "..")).Exists);
        }

        [Fact]
        public void NonExistentDirectories()
        {
            Assert.False(new DirectoryInfo("Da drar vi til fjells").Exists);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows | PlatformID.OSX)] // testing case-insensitivity
        public void CaseInsensitivity()
        {
            Assert.True(new DirectoryInfo(TestDirectory.ToUpperInvariant()).Exists);
            Assert.True(new DirectoryInfo(TestDirectory.ToLowerInvariant()).Exists);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Linux | PlatformID.FreeBSD)] // testing case-Sensitivity
        public void CaseSensitivity()
        {
            Assert.False(new DirectoryInfo(TestDirectory.ToUpperInvariant()).Exists);
            Assert.False(new DirectoryInfo(TestDirectory.ToLowerInvariant()).Exists);
        }

        [Fact]
        public void TrueForCreatedDirectory()
        {
            DirectoryInfo di = Directory.CreateDirectory(GetTestFilePath());
            Assert.True(di.Exists);
        }

        [Fact]
        public void TrueForNewDirectoryInfo()
        {
            string dirName = GetTestFilePath();
            Directory.CreateDirectory(dirName);

            DirectoryInfo di = new DirectoryInfo(dirName);
            Assert.True(di.Exists);
        }

        [Fact]
        public void FalseForFile()
        {
            string fileName = GetTestFilePath();
            File.Create(fileName).Dispose();
            DirectoryInfo di = new DirectoryInfo(fileName);
            Assert.False(di.Exists);
        }
    }
}
