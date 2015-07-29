// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_ToString : FileSystemTest
    {
        [Fact]
        public void ValidFile()
        {
            string path = GetTestFilePath();
            var info = new FileInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void RootDriveFiles()
        {
            string path = Path.Combine(Path.GetPathRoot(TestDirectory), GetTestFileName());
            var info = new FileInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void WithExtension()
        {
            string path = Path.Combine(GetTestFilePath(), "file.txt");
            var info = new FileInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void AlternateDirectoryDividerChar()
        {
            string path = Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "Directory", "File").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var info = new FileInfo(path);
            Assert.Equal(path, info.ToString());
        }

        [Fact]
        public void UNCShare()
        {
            string path = new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Directory", "File");
            var info = new FileInfo(path);
            Assert.Equal(path, info.ToString());
        }
    }
}
