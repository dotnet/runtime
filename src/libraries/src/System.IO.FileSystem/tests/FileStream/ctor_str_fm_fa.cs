// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_ctor_str_fm_fa : FileStream_ctor_str_fm
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
        {
            // Run the path/mode tests against this constructor
            return CreateFileStream(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite);
        }

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
        {
            return new FileStream(path, mode, access);
        }

        [Fact]
        public void InvalidAccessThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("access", () => CreateFileStream(GetTestFilePath(), FileMode.Create, ~FileAccess.Read));
        }

        [Fact]
        public void InvalidFileModeAccessReadThrows()
        {
            FileMode[] invalidModes = { FileMode.Append, FileMode.Create, FileMode.CreateNew, FileMode.Truncate };
            foreach (FileMode invalidMode in invalidModes)
            {
                Assert.Throws<ArgumentException>(() => CreateFileStream(GetTestFilePath(), invalidMode, FileAccess.Read));
                // ArgumentException.ParamName is not set since two parameters disagree.
            }
        }

        [Fact]
        public void InvalidFileModeAppendWithWriteThrows()
        {
            Assert.Throws<ArgumentException>(() => CreateFileStream(GetTestFilePath(), FileMode.Append, FileAccess.Read));
            Assert.Throws<ArgumentException>(() => CreateFileStream(GetTestFilePath(), FileMode.Append, FileAccess.ReadWrite));
            // ArgumentException.ParamName is not set since two parameters disagree.
        }


        [Fact]
        public void FileAccessRead()
        {
            // create the file first since we can't create with only read permissions
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.WriteByte(0);
            }

            using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.Equal(false, fs.CanWrite);
                Assert.Throws<NotSupportedException>(() => fs.WriteByte(0));
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(0, fs.ReadByte());
            }
        }

        [Fact]
        public void FileAccessWrite()
        {
            using (FileStream fs = CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Equal(true, fs.CanWrite);
                fs.WriteByte(0); // should not throw
                Assert.Equal(false, fs.CanRead);
                Assert.Throws<NotSupportedException>(() => fs.ReadByte());
            }
        }

        [Fact]
        public void FileAccessReadWrite()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
            {
                fs.WriteByte(0); // should not throw
            }

            // Reopen the file so that we can test read without taking a dependency on other API like seek
            using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                Assert.Equal(true, fs.CanRead);
                Assert.Equal(0, fs.ReadByte());
                Assert.Equal(true, fs.CanWrite);
                fs.WriteByte(0); // should not throw
            }
        }
    }
}
