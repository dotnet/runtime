// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class File_ReadWriteAllLines_Enumerable : FileSystemTest
    {
        #region Utilities

        protected virtual void Write(string path, string[] content)
        {
            File.WriteAllLines(path, (IEnumerable<string>)content);
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40065", TestPlatforms.Browser)]
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

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        [Fact]
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
                    Write(path, new string[] { "text" });
                    Assert.Equal(new string[] { "text" }, Read(path));
                }
                else
                    Assert.Throws<UnauthorizedAccessException>(() => Write(path, new string[] { "text" }));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public void DisposingEnumeratorClosesFile()
        {
            string path = GetTestFilePath();
            Write(path, new[] { "line1", "line2", "line3" });

            IEnumerable<string> readLines = File.ReadLines(path);
            using (IEnumerator<string> e1 = readLines.GetEnumerator())
            using (IEnumerator<string> e2 = readLines.GetEnumerator())
            {
                Assert.Same(readLines, e1);
                Assert.NotSame(e1, e2);
            }

            // File should be closed deterministically; this shouldn't throw.
            File.OpenWrite(path).Dispose();
        }

        #endregion
    }

    public class File_ReadWriteAllLines_Enumerable_Encoded : File_ReadWriteAllLines_Enumerable
    {
        protected override void Write(string path, string[] content)
        {
            File.WriteAllLines(path, (IEnumerable<string>)content, new UTF8Encoding(false));
        }

        protected override string[] Read(string path)
        {
            return File.ReadAllLines(path, new UTF8Encoding(false));
        }

        [Fact]
        public void NullEncoding()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllLines(path, (IEnumerable<string>)new string[] { "Text" }, null));
            Assert.Throws<ArgumentNullException>(() => File.ReadAllLines(path, null));
        }
    }

    public class File_ReadLines : File_ReadWriteAllLines_Enumerable
    {
        protected override string[] Read(string path)
        {
            return File.ReadLines(path).ToArray();
        }
    }

    public class File_ReadLines_Encoded : File_ReadWriteAllLines_Enumerable
    {
        protected override void Write(string path, string[] content)
        {
            File.WriteAllLines(path, (IEnumerable<string>)content, new UTF8Encoding(false));
        }

        protected override string[] Read(string path)
        {
            return File.ReadLines(path, new UTF8Encoding(false)).ToArray();
        }

        [Fact]
        public void NullEncoding()
        {
            string path = GetTestFilePath();
            Assert.Throws<ArgumentNullException>(() => File.WriteAllLines(path, (IEnumerable<string>)new string[] { "Text" }, null));
            Assert.Throws<ArgumentNullException>(() => File.ReadLines(path, null));
        }
    }

    public class File_ReadWriteAllLines_StringArray : FileSystemTest
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40065", TestPlatforms.Browser)]
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

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        [Fact]
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
                    Write(path, new string[] { "text" });
                    Assert.Equal(new string[] { "text" }, Read(path));
                }
                else
                    Assert.Throws<UnauthorizedAccessException>(() => Write(path, new string[] { "text" }));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public void DisposingEnumeratorClosesFile()
        {
            string path = GetTestFilePath();
            Write(path, new[] { "line1", "line2", "line3" });

            IEnumerable<string> readLines = File.ReadLines(path);
            using (IEnumerator<string> e1 = readLines.GetEnumerator())
            using (IEnumerator<string> e2 = readLines.GetEnumerator())
            {
                Assert.Same(readLines, e1);
                Assert.NotSame(e1, e2);
            }

            // File should be closed deterministically; this shouldn't throw.
            File.OpenWrite(path).Dispose();
        }

        #endregion
    }

    public class File_ReadWriteAllLines_StringArray_Encoded : File_ReadWriteAllLines_StringArray
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
}
