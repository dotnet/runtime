// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Write : FileSystemTest
    {
        [Fact]
        public void WriteDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Write(new byte[1], 0, 1));
                // even for noop Write
                Assert.Throws<ObjectDisposedException>(() => fs.Write(new byte[1], 0, 0));

                // out of bounds checking happens first
                Assert.Throws<ArgumentOutOfRangeException>(() => fs.Write(new byte[2], 1, 2));
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
                Assert.Throws<NotSupportedException>(() => fs.Write(new byte[1], 0, 1));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() => fs.Write(new byte[1], 0, 1));

                // out of bounds checking happens first
                Assert.Throws<ArgumentOutOfRangeException>(() => fs.Write(new byte[2], 1, 2));
            }
        }

        [Fact]
        public void NoopWritesSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(new byte[0], 0, 0);
                fs.Write(new byte[1], 0, 0);
                // even though offset is out of bounds of array, this is still allowed
                // for the last element
                fs.Write(new byte[1], 1, 0);
                fs.Write(new byte[2], 1, 0);
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Fact]
        public void SimpleWrite()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                fs.Position = 0;
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, fs.Read(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);
            }
        }
    }
}
