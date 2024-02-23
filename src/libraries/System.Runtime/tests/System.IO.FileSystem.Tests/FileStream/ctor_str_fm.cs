// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_ctor_str_fm : FileSystemTest
    {
        protected virtual FileStream CreateFileStream(string path, FileMode mode)
        {
            return new FileStream(path, mode);
        }

        protected virtual string GetExpectedParamName(string paramName) => paramName;

        [Fact]
        public void NullPathThrows()
        {
            Assert.Throws<ArgumentNullException>(() => CreateFileStream(null, FileMode.Open));
        }

        [Fact]
        public void EmptyPathThrows()
        {
            Assert.Throws<ArgumentException>(() => CreateFileStream(string.Empty, FileMode.Open));
        }

        [Fact]
        public void DirectoryThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() => CreateFileStream(".", FileMode.Open));
        }

        [Fact]
        public void InvalidModeThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                GetExpectedParamName("mode"),
                () => CreateFileStream(GetTestFilePath(), ~FileMode.Open));
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

        public static TheoryData<string> StreamSpecifiers
        {
            get
            {
                TheoryData<string> data = new TheoryData<string>();
                data.Add("");

                if (PlatformDetection.IsWindows)
                {
                    data.Add("::$DATA");        // Same as default stream (e.g. main file)
                    data.Add(":bar");           // $DATA isn't necessary
                    data.Add(":bar:$DATA");     // $DATA can be explicitly specified
                }

                return data;
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeCreate(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (CreateFileStream(fileName, FileMode.Create))
            {
                Assert.True(File.Exists(fileName));
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeCreateExisting(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                // Ensure that the file was re-created
                Assert.Equal(0, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeCreateNew(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (CreateFileStream(fileName, FileMode.CreateNew))
            {
                Assert.True(File.Exists(fileName));
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeCreateNewExistingThrows(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.CreateNew))
            {
                fs.WriteByte(0);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }

            Assert.Throws<IOException>(() => CreateFileStream(fileName, FileMode.CreateNew));
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeOpenThrows(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => CreateFileStream(fileName, FileMode.Open));
            Assert.Equal(fileName, fnfe.FileName);
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeOpenExisting(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Open))
            {
                // Ensure that the file was re-opened
                Assert.Equal(1, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeOpenOrCreate(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (CreateFileStream(fileName, FileMode.OpenOrCreate))
            {
                Assert.True(File.Exists(fileName));
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeOpenOrCreateExisting(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.OpenOrCreate))
            {
                // Ensure that the file was re-opened
                Assert.Equal(1, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeTruncateThrows(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => CreateFileStream(fileName, FileMode.Truncate));
            Assert.Equal(fileName, fnfe.FileName);
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public void FileModeTruncateExisting(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Truncate))
            {
                // Ensure that the file was re-opened and truncated
                Assert.Equal(0, fs.Length);
                Assert.Equal(0L, fs.Position);
                Assert.True(fs.CanRead);
                Assert.True(fs.CanWrite);
            }
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public virtual void FileModeAppend(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Append))
            {
                Assert.False(fs.CanRead);
                Assert.True(fs.CanWrite);

                fs.Write("abcde"u8);
                Assert.Equal(5, fs.Length);
                Assert.Equal(5, fs.Position);
                Assert.Equal(1, fs.Seek(1, SeekOrigin.Begin));

                fs.Write("xyz"u8);
                Assert.Equal(4, fs.Position);
                Assert.Equal(5, fs.Length);
            }

            Assert.Equal("axyze", File.ReadAllText(fileName));
        }

        [Theory, MemberData(nameof(StreamSpecifiers))]
        public virtual void FileModeAppendExisting(string streamSpecifier)
        {
            string fileName = GetTestFilePath() + streamSpecifier;
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create))
            {
                fs.WriteByte((byte)'s');
            }

            string initialContents = File.ReadAllText(fileName);
            using (FileStream fs = CreateFileStream(fileName, FileMode.Append))
            {
                // Ensure that the file was re-opened and position set to end
                Assert.Equal(1, fs.Length);

                long position = fs.Position;
                Assert.Equal(fs.Length, position);

                Assert.False(fs.CanRead);
                Assert.True(fs.CanSeek);
                Assert.True(fs.CanWrite);

                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Throws<IOException>(() => fs.Seek(0, SeekOrigin.Begin));
                Assert.Throws<NotSupportedException>(() => fs.ReadByte());

                fs.Write("abcde"u8);
                Assert.Equal(position + 5, fs.Position);

                Assert.Equal(position, fs.Seek(position, SeekOrigin.Begin));
                Assert.Equal(position + 1, fs.Seek(1, SeekOrigin.Current));
                fs.Write("xyz"u8);
            }

            Assert.Equal(initialContents + "axyze", File.ReadAllText(fileName));
        }
    }
}
