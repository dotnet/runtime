// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class FileStream_ctor_str_fm : FileSystemTest
    {
        protected virtual FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }

        [Fact]
        public void NullPathThrows()
        {
            Assert.Throws<ArgumentNullException>(() => CreateFileStream(null, FileMode.Open));
        }

        [Fact]
        public void EmptyPathThrows()
        {
            Assert.Throws<ArgumentException>(() => CreateFileStream(String.Empty, FileMode.Open));
        }

        [Fact]
        public void DirectoryThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() => CreateFileStream(".", FileMode.Open));
        }

        [Fact]
        public void InvalidModeThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("mode", () => CreateFileStream(GetTestFilePath(), ~FileMode.Open));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingFile_ThrowsFileNotFound(char trailingChar)
        {
            string path = GetTestFilePath() + trailingChar;
            Assert.Throws<FileNotFoundException>(() => CreateFileStream(path, FileMode.Open));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingDirectory_ThrowsDirectoryNotFound(char trailingChar)
        {
            string path = Path.Combine(GetTestFilePath(), "file" + trailingChar);
            Assert.Throws<DirectoryNotFoundException>(() => CreateFileStream(path, FileMode.Open));
        }

        [Fact]
        public void FileModeCreate()
        {
            using (CreateFileStream(GetTestFilePath(), FileMode.Create))
            { }
        }

        [Fact]
        public void FileModeCreateExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                // Ensure that the file was re-created
                Assert.Equal(0L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeCreateNew()
        {
            using (CreateFileStream(GetTestFilePath(), FileMode.CreateNew))
            { }
        }

        [Fact]
        public void FileModeCreateNewExistingThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.CreateNew))
            {
                fs.WriteByte(0);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }

            Assert.Throws<IOException>(() => CreateFileStream(fileName, FileMode.CreateNew));
        }

        [Fact]
        public void FileModeOpenThrows()
        {
            string fileName = GetTestFilePath();
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => CreateFileStream(fileName, FileMode.Open));
            Assert.Equal(fileName, fnfe.FileName);
        }

        [Fact]
        public void FileModeOpenExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Open))
            {
                // Ensure that the file was re-opened
                Assert.Equal(1L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeOpenOrCreate()
        {
            using (CreateFileStream(GetTestFilePath(), FileMode.OpenOrCreate))
            {}
        }

        [Fact]
        public void FileModeOpenOrCreateExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.OpenOrCreate))
            {
                // Ensure that the file was re-opened
                Assert.Equal(1L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeTruncateThrows()
        {
            string fileName = GetTestFilePath();
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => CreateFileStream(fileName, FileMode.Truncate));
            Assert.Equal(fileName, fnfe.FileName);
        }

        [Fact]
        public void FileModeTruncateExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Truncate))
            {
                // Ensure that the file was re-opened and truncated
                Assert.Equal(0L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Fact]
        public virtual void FileModeAppend()
        {
            using (FileStream fs = CreateFileStream(GetTestFilePath(), FileMode.Append))
            {
                Assert.Equal(false, fs.CanRead);
                Assert.Equal(true, fs.CanWrite);
            }
        }

        [Fact]
        public virtual void FileModeAppendExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Append))
            {
                // Ensure that the file was re-opened and position set to end
                Assert.Equal(1L, fs.Length);
                Assert.Equal(1L, fs.Position);
                Assert.False(fs.CanRead);
                Assert.True(fs.CanSeek);
                Assert.True(fs.CanWrite);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Throws<NotSupportedException>(() => fs.ReadByte());
            }
        }
    }
}
