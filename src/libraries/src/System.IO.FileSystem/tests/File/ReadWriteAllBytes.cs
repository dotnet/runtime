// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_ReadWriteAllBytes : FileSystemTest
    {
        [Fact]
        public void NullParameters()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllBytes(null, new byte[0]));
            Assert.Throws<ArgumentNullException>(() => File.WriteAllBytes(path, null));
            Assert.Throws<ArgumentNullException>(() => File.ReadAllBytes(null));
        }

        [Fact]
        public void InvalidParameters()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentException>(() => File.WriteAllBytes(string.Empty, new byte[0]));
            Assert.Throws<ArgumentException>(() => File.ReadAllBytes(string.Empty));
        }

        [Fact]
        public void Read_FileNotFound()
        {
            string path = GetTestFilePath();
            Assert.Throws<FileNotFoundException>(() => File.ReadAllBytes(path));
        }

        [Fact]
        public void EmptyContentCreatesFile()
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[0]);
            Assert.True(File.Exists(path));
            Assert.Empty(File.ReadAllText(path));
            File.Delete(path);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void ValidWrite(int size)
        {
            string path = GetTestFilePath();
            byte[] buffer = Encoding.UTF8.GetBytes(new string('c', size));
            File.WriteAllBytes(path, buffer);
            Assert.Equal(buffer, File.ReadAllBytes(path));
            File.Delete(path);
        }

        [Fact]
        [OuterLoop]
        public void ReadFileOver2GB()
        {
            string path = GetTestFilePath();
            int split = 10;
            string toWrite = new string('a', Int32.MaxValue / split);
            using (var writer = new StreamWriter(File.Create(path)))
            {
                for (int i = 0; i < (split + 1); i++)
                {
                    try
                    {
                        writer.Write(toWrite);
                        writer.Flush();
                    }
                    catch (OutOfMemoryException)
                    {
                        split /= 2;
                        toWrite = new string('a', Int32.MaxValue / split);
                    }
                }
            }
            // File is too large for ReadAllBytes at once
            Assert.Throws<IOException>(() => File.ReadAllBytes(path));
        }

        [Fact]
        public void Overwrite()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes(new string('c', 100));
            byte[] overwriteBytes = Encoding.UTF8.GetBytes(new string('b', 50));
            File.WriteAllBytes(path, bytes);
            File.WriteAllBytes(path, overwriteBytes);
            Assert.Equal(overwriteBytes, File.ReadAllBytes(path));
        }

        [Fact]
        public void OpenFile_ThrowsIOException()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes(new string('c', 100));
            using (File.Create(path))
            {
                Assert.Throws<IOException>(() => File.WriteAllBytes(path, bytes));
                Assert.Throws<IOException>(() => File.ReadAllBytes(path));
            }
        }

        [Fact]
        public void WriteToReadOnlyFile_UnauthException()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            Assert.Throws<UnauthorizedAccessException>(() => File.WriteAllBytes(path, Encoding.UTF8.GetBytes("text")));
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }
}
