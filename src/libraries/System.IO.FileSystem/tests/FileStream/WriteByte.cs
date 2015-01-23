// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace System.IO.FileSystem.Tests
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
    }
}
