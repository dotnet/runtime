// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_ctor_str_fm : FileSystemTest
    {
        [Fact]
        public void NullPathThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new FileStream(null, FileMode.Open));
        }

        [Fact]
        public void EmptyPathThrows()
        {
            Assert.Throws<ArgumentException>(() => new FileStream(String.Empty, FileMode.Open));
        }

        [Fact]
        public void DirectoryThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() => new FileStream(".", FileMode.Open));
        }

        [Fact]
        public void InvalidModeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStream(GetTestFilePath(), ~FileMode.Open));
        }

        [Fact]
        public void FileModeCreate()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            { }
        }

        [Fact]
        public void FileModeCreateExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                // Ensure that the file was re-created
                Assert.Equal(0L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(true, fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeCreateNew()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.CreateNew))
            { }
        }

        [Fact]
        public void FileModeCreateNewExistingThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.CreateNew))
            {
                fs.WriteByte(0);
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(true, fs.CanWrite);
            }

            Assert.Throws<IOException>(() => new FileStream(fileName, FileMode.CreateNew));
        }

        [Fact]
        public void FileModeOpenThrows()
        {
            Assert.Throws<FileNotFoundException>(() => new FileStream(GetTestFilePath(), FileMode.Open));
        }

        [Fact]
        public void FileModeOpenExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                // Ensure that the file was re-opened
                Assert.Equal(1L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(true, fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeOpenOrCreate()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.OpenOrCreate))
            {}
        }

        [Fact]
        public void FileModeOpenOrCreateExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                // Ensure that the file was re-opened
                Assert.Equal(1L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(true, fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeTruncateThrows()
        {
            Assert.Throws<FileNotFoundException>(() => new FileStream(GetTestFilePath(), FileMode.Truncate));
        }

        [Fact]
        public void FileModeTruncateExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Truncate))
            {
                // Ensure that the file was re-opened and truncated
                Assert.Equal(0L, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(true, fs.CanWrite);
            }
        }

        [Fact]
        public void FileModeAppend()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Append))
            { }
        }

        [Fact]
        public void FileModeAppendExisting()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Append))
            {
                // Ensure that the file was re-opened and position set to end
                Assert.Equal(1L, fs.Length);
                Assert.Equal(1L, fs.Position);
                Assert.Equal(false, fs.CanRead);
                Assert.Equal(true, fs.CanSeek);
                Assert.Equal(true, fs.CanWrite);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Throws<NotSupportedException>(() => fs.ReadByte());
            }
        }
    }
}
