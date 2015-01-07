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
            ArgumentNullException ane = Assert.Throws<ArgumentNullException>(() => new FileStream(null, FileMode.Open));
            Assert.Equal("path", ane.ParamName);
        }

        [Fact]
        public void EmptyPathThrows()
        {
            ArgumentException ae = Assert.Throws<ArgumentException>(() => new FileStream(String.Empty, FileMode.Open));
            Assert.Equal("path", ae.ParamName);
        }

        [Fact]
        public void DirectoryThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() => new FileStream(".", FileMode.Open));
        }

        [Fact]
        public void InvalidModeThrows()
        {
            ArgumentOutOfRangeException aoore = Assert.Throws<ArgumentOutOfRangeException>(() => new FileStream(GetTestFilePath(), ~FileMode.Open));
            Assert.Equal("mode", aoore.ParamName);
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
            string fileName = GetTestFilePath();
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => new FileStream(fileName, FileMode.Open));
            Assert.Equal(fileName, fnfe.FileName);
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
            string fileName = GetTestFilePath();
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => new FileStream(fileName, FileMode.Truncate));
            Assert.Equal(fileName, fnfe.FileName);
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
