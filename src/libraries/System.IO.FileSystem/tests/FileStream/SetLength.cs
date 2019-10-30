// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_SetLength : FileSystemTest
    {
        [Fact]
        public void InvalidLengths()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.SetLength(-1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.SetLength(long.MinValue));
            }
        }

        [Fact]
        public void SetLengthDisposedThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.SetLength(0));
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.SetLength(-1));
            }
        }

        [Fact]
        public void SetLengthAccessNoWriteThrows()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => fs.SetLength(1));
                Assert.Throws<NotSupportedException>(() => fs.SetLength(0));
                // no change should still throw
                Assert.Throws<NotSupportedException>(() => fs.SetLength(fs.Length));
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.SetLength(-1));
                // disposed check happens first
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.SetLength(0));
            }
        }

        [Fact]
        public void SetLengthUnseekableThrows()
        {
            using (FileStream fs = new UnseekableFileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => fs.Seek(1, SeekOrigin.Begin));
                // no fast path
                Assert.Throws<NotSupportedException>(() => fs.Seek(fs.Position, SeekOrigin.Begin));
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentException>("origin", null, () => fs.Seek(0, ~SeekOrigin.Begin));
                // dispose checking happens first
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Seek(fs.Position, SeekOrigin.Begin));
            }
        }

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
            }
        }

        [Fact]
        public void SetLengthSame()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                long length = fs.Length, position = fs.Position;
                fs.SetLength(length);
                Assert.Equal(length, fs.Length);
                Assert.Equal(position, fs.Position);
            }
        }

        [Fact]
        public void SetLengthLonger()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                long length = fs.Length, position = fs.Position;
                fs.SetLength(length * 2);
                Assert.Equal(length * 2, fs.Length);
                Assert.Equal(position, fs.Position);  // should not change
            }
        }

        [Fact]
        public void SetLengthShorter()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                long length = fs.Length;
                fs.SetLength(length / 2);
                Assert.Equal(length / 2, fs.Length);
                // file pointer should move as well when truncating
                Assert.Equal(length / 2, fs.Position);

                fs.SetLength(0);
                Assert.Equal(0, fs.Length);
                // file pointer should move as well when truncating
                Assert.Equal(0, fs.Position);
            }
        }
    }
}
