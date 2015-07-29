// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_ReadWriteAllLines : FileSystemTest
    {
        #region Utilities

        protected virtual void Write(string path, string[] content)
        {
            File.WriteAllLines(path, content);
        }

        protected virtual string[] Read(string path)
        {
            return File.ReadAllLines(path);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void InvalidPath()
        {
            Assert.Throws<ArgumentNullException>(() => Write(null, new string[] { "Text" }));
            Assert.Throws<ArgumentException>(() => Write(string.Empty, new string[] { "Text" }));
            Assert.Throws<ArgumentNullException>(() => Read(null));
            Assert.Throws<ArgumentException>(() => Read(string.Empty));
        }

        [Fact]
        public void NullLines()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => Write(path, null));

            Write(path, new string[] { null });
            Assert.Equal(new string[] {""}, Read(path));
        }

        [Fact]
        public void EmptyStringCreatesFile()
        {
            string path = GetTestFilePath();
            Write(path, new string[] { });
            Assert.True(File.Exists(path));
            Assert.Empty(Read(path));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void ValidWrite(int size)
        {
            string path = GetTestFilePath();
            string[] lines = new string[] { new string('c', size) };
            File.Create(path).Dispose();
            Write(path, lines);
            Assert.Equal(lines, Read(path));
        }

        [Fact]
        public virtual void Overwrite()
        {
            string path = GetTestFilePath();
            string[] lines = new string[] { new string('c', 200) };
            string[] overwriteLines = new string[] { new string('b', 100) };
            Write(path, lines);
            Write(path, overwriteLines);
            Assert.Equal(overwriteLines, Read(path));
        }

        [Fact]
        public void OpenFile_ThrowsIOException()
        {
            string path = GetTestFilePath();
            string[] lines = new string[] { new string('c', 200) };

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
            Assert.Throws<UnauthorizedAccessException>(() => Write(path, new string[] { "text" }));
            File.SetAttributes(path, FileAttributes.Normal);
        }

        #endregion
    }

    public class File_ReadWriteAllLines_Encoded : File_ReadWriteAllLines
    {
        protected override void Write(string path, string[] content)
        {
            File.WriteAllLines(path, content, new UTF8Encoding(false));
        }

        protected override string[] Read(string path)
        {
            return File.ReadAllLines(path, new UTF8Encoding(false));
        }

        [Fact]
        public void NullEncoding()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllLines(path, new string[] { "Text" }, null));
            Assert.Throws<ArgumentNullException>(() => File.ReadAllLines(path, null));
        }
    }

    public class File_ReadLines : File_ReadWriteAllLines
    {
        protected override string[] Read(string path)
        {
            return File.ReadLines(path).ToArray();
        }
    }

    public class File_ReadLines_Encoded : File_ReadWriteAllLines
    {
        protected override void Write(string path, string[] content)
        {
            File.WriteAllLines(path, content, new UTF8Encoding(false));
        }

        protected override string[] Read(string path)
        {
            return File.ReadLines(path, new UTF8Encoding(false)).ToArray();
        }

        [Fact]
        public void NullEncoding()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllLines(path, new string[] { "Text" }, null));
            Assert.Throws<ArgumentNullException>(() => File.ReadLines(path, null));
        }
    }
}