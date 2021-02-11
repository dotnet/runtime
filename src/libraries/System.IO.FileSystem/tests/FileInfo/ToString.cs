// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
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
