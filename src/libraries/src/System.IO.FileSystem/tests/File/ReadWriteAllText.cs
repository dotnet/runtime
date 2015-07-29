// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_ReadWriteAllText : FileSystemTest
    {
        #region Utilities

        protected virtual void Write(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        protected virtual string Read(string path)
        {
            return File.ReadAllText(path);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void NullParameters()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => Write(null, "Text"));
            Assert.Throws<ArgumentNullException>(() => Read(null));
        }

        [Fact]
        public void NonExistentPath()
        {
            Assert.Throws<DirectoryNotFoundException>(() => Write(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), "Text"));
        }

        [Fact]
        public void NullContent_CreatesFile()
        {
            string path = GetTestFilePath();
            Write(path, null);
            Assert.Empty(Read(path));
        }

        [Fact]
        public void EmptyStringContent_CreatesFile()
        {
            string path = GetTestFilePath();
            Write(path, string.Empty);
            Assert.Empty(Read(path));
        }

        [Fact]
        public void InvalidParameters()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentException>(() => Write(string.Empty, "Text"));
            Assert.Throws<ArgumentException>(() => Read(""));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void ValidWrite(int size)
        {
            string path = GetTestFilePath();
            string toWrite = new string('c', size);

            File.Create(path).Dispose();
            Write(path, toWrite);
            Assert.Equal(toWrite, Read(path));
        }

        [Fact]
        public virtual void Overwrite()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);
            string overwriteLines = new string('b', 100);
            Write(path, lines);
            Write(path, overwriteLines);
            Assert.Equal(overwriteLines, Read(path));
        }

        [Fact]
        public void OpenFile_ThrowsIOException()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);

            using (File.Create(path))
            {
                Assert.Throws<IOException>(() => Write(path, lines));
                Assert.Throws<IOException>(() => Read(path));
            }
        }

        [Fact]
        public void Read_FileNotFound()
        {
            string path = GetTestFilePath();
            Assert.Throws<FileNotFoundException>(() => Read(path));
        }

        [Fact]
        public void WriteToReadOnlyFile_UnauthException()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            Assert.Throws<UnauthorizedAccessException>(() => Write(path, "text"));
            File.SetAttributes(path, FileAttributes.Normal);
        }

        #endregion
    }

    public class File_ReadWriteAllText_Encoded : File_ReadWriteAllText
    {
        protected override void Write(string path, string content)
        {
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        protected override string Read(string path)
        {
            return File.ReadAllText(path, new UTF8Encoding(false));
        }

        [Fact]
        public void NullEncoding()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllText(path, "Text", null));
            Assert.Throws<ArgumentNullException>(() => File.ReadAllText(path, null));
        }
    }
}