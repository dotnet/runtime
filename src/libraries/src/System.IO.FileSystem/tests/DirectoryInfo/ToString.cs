// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_ToString : FileSystemTest
    {
        [Fact]
        public void ValidDirectory()
        {
            string path = GetTestFilePath();
            var info = new DirectoryInfo(path);
            Assert.Equal(Path.GetFileName(path), info.ToString());
        }

        [Fact]
        public void RootOfCurrentDrive()
        {
            string path = Path.GetPathRoot(TestDirectory);
            var info = new DirectoryInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void ParentDirectory()
        {
            string path = "..";
            var info = new DirectoryInfo(path);
            Assert.Equal(Path.GetFileName(Directory.GetParent(Directory.GetCurrentDirectory()).Name), info.ToString());
        }

        [Fact]
        public void CurrentDirectory()
        {
            string path = ".";
            var info = new DirectoryInfo(path);
            Assert.Equal(Path.GetFileName(Directory.GetCurrentDirectory()), info.ToString());
        }
    }
}
