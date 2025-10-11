// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Seek : FileSystemTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void SeekNegativePositionThrowsWithClearMessage(int bufferSize)
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                
                // Seek to negative position from Begin should throw IOException with clear message
                IOException ex = Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Begin));
                Assert.Contains("before the beginning", ex.Message, StringComparison.OrdinalIgnoreCase);
                
                // Seek to negative position from Current should throw IOException with clear message
                fs.Position = 5;
                ex = Assert.Throws<IOException>(() => fs.Seek(-10, SeekOrigin.Current));
                Assert.Contains("before the beginning", ex.Message, StringComparison.OrdinalIgnoreCase);
                
                // Seek to negative position from End should throw IOException with clear message
                ex = Assert.Throws<IOException>(() => fs.Seek(-fs.Length - 1, SeekOrigin.End));
                Assert.Contains("before the beginning", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void SeekAppendModifyThrows(int bufferSize)
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read, bufferSize))
            {
                long length = fs.Length;
                Assert.Throws<IOException>(() => fs.Seek(length - 1, SeekOrigin.Begin));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.Current));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-1, SeekOrigin.End));
                Assert.Equal(length, fs.Position);

                Assert.Throws<IOException>(() => fs.Seek(0, SeekOrigin.Begin));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-length, SeekOrigin.Current));
                Assert.Equal(length, fs.Position);
                Assert.Throws<IOException>(() => fs.Seek(-length, SeekOrigin.End));
                Assert.Equal(length, fs.Position);

                Assert.Throws<IOException>(() => fs.Position = length - 1);
                Assert.Equal(length, fs.Position);

                fs.Write(TestBuffer);
                Assert.Equal(length, fs.Seek(length, SeekOrigin.Begin));
            }
        }
    }
}
