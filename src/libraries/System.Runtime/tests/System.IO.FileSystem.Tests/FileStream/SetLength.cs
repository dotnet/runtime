// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileStream_SetLength : FileSystemTest
    {
        [Fact]
        public void SetLengthAppendModifyThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Append))
            {
                long length = fs.Length;
                Assert.Throws<IOException>(() => fs.SetLength(length - 1));
                Assert.Equal(length, fs.Length);
                Assert.Throws<IOException>(() => fs.SetLength(0));
                Assert.Equal(length, fs.Length);

                fs.Write(TestBuffer);
                Assert.Equal(length + TestBuffer.Length, fs.Length);

                fs.SetLength(length);
                Assert.Equal(length, fs.Length);
            }
        }

        [Fact]
        public void SetLengthThrowsForUnseekableFileStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new UnseekableFileStream(fileName, FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => fs.SetLength(1));
            }
        }

        [Fact]
        public void GetLengthThrowsForUnseekableFileStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new UnseekableFileStream(fileName, FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => _ = fs.Length);
            }
        }
    }
}
