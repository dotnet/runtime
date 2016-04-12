// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class FileStream_WriteByte : FileSystemTest
    {
        [Fact]
        public void ReadDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.WriteByte(0));
            }
        }

        [Fact]
        public void ReadOnlyThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => fs.WriteByte(0));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() => fs.WriteByte(0));
            }
        }

        [Fact]
        public void WriteFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                foreach(byte b in TestBuffer)
                {
                    fs.WriteByte(b);
                }
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, fs.Read(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public void WriteFile_PastBufferSize()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                for (int written = 0; written < TestBuffer.Length * 1000; written++)
                {
                    fs.WriteByte(TestBuffer[written % TestBuffer.Length]);
                }
            }
            byte[] buffer = File.ReadAllBytes(fileName);
            int offset = 0;
            while (offset < buffer.Length)
            {
                for (int i = 0; i < TestBuffer.Length; i++)
                    Assert.Equal(TestBuffer[i], buffer[i + offset]);
                offset += TestBuffer.Length;
            }
        }
    }
}
