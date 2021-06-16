// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Position : FileSystemTest
    {
        [Fact]
        public void SetPositionAppendModify()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Append))
            {
                long length = fs.Length;
                Assert.Throws<IOException>(() => fs.Position = length - 1);
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Position = 0);
                Assert.Equal(length, fs.Position);

                fs.Position = length + 1;
                Assert.Equal(length + 1, fs.Position);
            }
        }

        [Fact]
        public void GetPositionThrowsForUnseekableFileStream()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new UnseekableFileStream(fileName, FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => _ = fs.Position);
            }
        }
    }
}
