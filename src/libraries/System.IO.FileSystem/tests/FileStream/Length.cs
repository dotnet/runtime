// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Length : FileSystemTest
    {
        [Fact]
        public void GetLengthDisposedThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Length);
            }
        }

        [Fact]
        public void GetLengthUnseekableThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new UnseekableFileStream(fileName, FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => fs.Length);
                // dispose checking happens first
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Length);
            }
        }

        [Fact]
        public void EmptyFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                Assert.Equal(0, fs.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                Assert.Equal(0, fs.Length);
            }
        }

        [Fact]
        public void NonEmptyFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
                Assert.Equal(TestBuffer.Length, fs.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                Assert.Equal(TestBuffer.Length, fs.Length);
            }

        }
    }
}
