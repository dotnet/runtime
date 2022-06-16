// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class File_ReadWriteAllText : FileSystemTest
    {
        #region Utilities

        protected virtual bool IsAppend {  get; }

        protected virtual void Write(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        protected virtual void Write(string path, string content, Encoding encoding)
        {
            File.WriteAllText(path, content, encoding);
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

        [Theory]
        [InlineData(200, 100)]
        [InlineData(50_000, 40_000)] // tests a different code path than the line above
        public void AppendOrOverwrite(int linesSizeLength, int overwriteLinesLength)
        {
            string path = GetTestFilePath();
            string lines = new string('c', linesSizeLength);
            string overwriteLines = new string('b', overwriteLinesLength);

            Write(path, lines);
            Write(path, overwriteLines);

            if (IsAppend)
            {
                Assert.Equal(lines + overwriteLines, Read(path));
            }
            else
            {
                Assert.DoesNotContain("Append", GetType().Name); // ensure that all "Append" types override this property

                Assert.Equal(overwriteLines, Read(path));
            }
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

        public static IEnumerable<object[]> OutputIsTheSameAsForStreamWriter_Args()
        {
            string longText = new string('z', 50_000);
            foreach (Encoding encoding in new[] { Encoding.Unicode , new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) })
            {
                foreach (string text in new[] { null, string.Empty, " ", "shortText", longText })
                {
                    yield return new object[] { text, encoding };
                }
            }
        }

        [Theory]
        [MemberData(nameof(OutputIsTheSameAsForStreamWriter_Args))]
        public void OutputIsTheSameAsForStreamWriter(string content, Encoding encoding)
        {
            string filePath = GetTestFilePath();
            Write(filePath, content, encoding); // it uses System.IO.File APIs

            string swPath = GetTestFilePath();
            using (StreamWriter sw = new StreamWriter(swPath, IsAppend, encoding))
            {
                sw.Write(content);
            }

            Assert.Equal(File.ReadAllText(swPath, encoding), File.ReadAllText(filePath, encoding));
            Assert.Equal(File.ReadAllBytes(swPath), File.ReadAllBytes(filePath)); // ensure Preamble was stored
        }

        [Theory]
        [MemberData(nameof(OutputIsTheSameAsForStreamWriter_Args))]
        public void OutputIsTheSameAsForStreamWriter_Overwrite(string content, Encoding encoding)
        {
            string filePath = GetTestFilePath();
            string swPath = GetTestFilePath();

            for (int i = 0; i < 2; i++)
            {
                Write(filePath, content, encoding); // it uses System.IO.File APIs

                using (StreamWriter sw = new StreamWriter(swPath, IsAppend, encoding))
                {
                    sw.Write(content);
                }
            }

            Assert.Equal(File.ReadAllText(swPath, encoding), File.ReadAllText(filePath, encoding));
            Assert.Equal(File.ReadAllBytes(swPath), File.ReadAllBytes(filePath)); // ensure Preamble was stored once
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
