// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Position : FileSystemTest
    {
        [Fact]
        public void GetPositionDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Position);
            }
        }

        [Fact]
        public void GetPositionUnseekableThrows()
        {
            using (FileStream fs = new UnseekableFileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => fs.Position);
                // dispose checking happens first
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Position);
            }
        }

        [Fact]
        public void SetPositionInvalidThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.Position = -1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.Position = long.MinValue);
            }
        }

        [Fact]
        public void SetPositionDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Position = 1);
                // no fast path
                Assert.Throws<ObjectDisposedException>(() => fs.Position = fs.Position);
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.Position = -1);
            }
        }

        [Fact]
        public void SetPositionUnseekableThrows()
        {
            using (FileStream fs = new UnseekableFileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Throws<NotSupportedException>(() => fs.Position = 1);
                // no fast path
                Assert.Throws<NotSupportedException>(() => fs.Position = fs.Position);
                // parameter checking happens first
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => fs.Position = -1);
                // dispose checking happens first
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Position = 1);
            }
        }

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
        public void ValidPositions()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                fs.Position = 0;
                Assert.Equal(0, fs.Position);

                // End
                fs.Position = fs.Length;
                Assert.Equal(fs.Length, fs.Position);

                // Middle
                fs.Position = fs.Length / 2;
                Assert.Equal(fs.Length / 2, fs.Position);

                // Make sure that a noop change doesn't modify position
                fs.Position = fs.Position;
                Assert.Equal(fs.Length / 2, fs.Position);

                // Past the end, making sure the file length doesn't change by doing so
                long length = fs.Length;
                fs.Position = length * 2;
                Assert.Equal(length, fs.Length);
                Assert.Equal(length * 2, fs.Position);
            }
        }

        [Fact]
        public void PositionPastEnd()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);

                // Ensure our test is set up correctly
                Assert.Equal(TestBuffer.Length, fs.Length);
                Assert.Equal(TestBuffer.Length, fs.Position);

                // Move past end
                fs.Position = fs.Length + 1;
                Assert.Equal(TestBuffer.Length + 1, fs.Position);
                // Length is not updated until a write
                Assert.Equal(TestBuffer.Length, fs.Length);

                // At end of stream
                Assert.Equal(-1, fs.ReadByte());
                // Read should not update position or length
                Assert.Equal(TestBuffer.Length + 1, fs.Position);
                Assert.Equal(TestBuffer.Length, fs.Length);

                // Move back one, still at end of stream since length hasn't changed
                fs.Position--;
                Assert.Equal(-1, fs.ReadByte());
                // Read should not update position or length
                Assert.Equal(TestBuffer.Length, fs.Position);
                Assert.Equal(TestBuffer.Length, fs.Length);

                // Move past end
                fs.Position = fs.Length + 1;
                fs.WriteByte(0x2A);
                // Writing a single byte should update length by 2 (filling gap with zero).
                Assert.Equal(TestBuffer.Length + 2, fs.Position);
                Assert.Equal(TestBuffer.Length + 2, fs.Length);
                // Validate zero fill
                fs.Position -= 2;
                Assert.Equal(0, fs.ReadByte());
                // Validate written value
                Assert.Equal(0x2A, fs.ReadByte());
            }
        }
    }
}
