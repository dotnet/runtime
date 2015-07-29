// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Directory : FileSystemTest
    {
        protected virtual string Directory(string path)
        {
            return new FileInfo(path).Directory.FullName;
        }

        [Fact]
        public void ValidCase()
        {
            var directory = Directory(Path.Combine(TestDirectory, "Test", "test2"));
            Assert.Equal(Path.Combine(TestDirectory, "Test"), directory);
        }

        [Fact]
        public void TrailingSlashes()
        {
            var directory = Directory(Path.Combine(TestDirectory, "a") + Path.DirectorySeparatorChar);
            Assert.Equal(Path.Combine(TestDirectory, "a"), directory);
        }

        [Fact]
        public void AltSeparatorCharInPath()
        {
            var directory = Directory(Path.Combine(TestDirectory, "File").Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            Assert.Equal(TestDirectory, directory);
        }

        [Fact]
        public virtual void Root_FileInfo_Returns_Null_Directory()
        {
            Assert.Null(new FileInfo(Path.GetPathRoot(TestDirectory)).Directory);
        }

        [Fact]
        public void DotsInPathAreValid()
        {
            var directory = Directory(Path.Combine(TestDirectory, "Test", "..", ".", "Test"));
            Assert.Equal(TestDirectory, directory);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void UNCShares()
        {
            var directory = Directory(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Directory", "File"));
            Assert.Equal(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Directory"), directory);
        }
    }

    public class FileInfo_DirectoryName : FileInfo_Directory
    {
        protected override string Directory(string path)
        {
            return new FileInfo(path).DirectoryName;
        }
    }
}
