// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
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

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/53021", TestPlatforms.Browser)]
        public void WriteToReadOnlyFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                // Operation succeeds when being run by the Unix superuser
                if (PlatformDetection.IsSuperUser)
                {
                    Write(path, "text");
                    Assert.Equal("text", Read(path));
                }
                else
                    Assert.Throws<UnauthorizedAccessException>(() => Write(path, "text"));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
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
