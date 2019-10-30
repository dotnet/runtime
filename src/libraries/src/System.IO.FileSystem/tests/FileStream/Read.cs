// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Read : FileSystemTest
    {
        [Fact]
        public void NullArrayThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Read(null, 0, 1));
            }
        }

        [Fact]
        public void NegativeReadRootThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                new FileStream(Path.GetPathRoot(Directory.GetCurrentDirectory()), FileMode.Open, FileAccess.Read));
        }

        [Fact]
        public void NegativeOffsetThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Read(new byte[1], -1, 1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Read(null, -1, 1));
            }
        }

        [Fact]
        public void NegativeCountThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => fs.Read(new byte[1], 0, -1));

                // offset is checked before count
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Read(new byte[1], -1, -1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Read(null, -1, -1));
            }
        }

        [Fact]
        public void ArrayOutOfBoundsThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[1], 1, 1));

                // offset out of bounds for 0 count read
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[0], 1, 0));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[2], 1, 2));

                // edges
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[0], int.MaxValue, 0));
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[0], int.MaxValue, int.MaxValue));
            }
        }

        [Fact]
        public void ReadDisposedThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                fs.Dispose();
                Assert.Throws<ObjectDisposedException>(() => fs.Read(new byte[1], 0, 1));
                // even for noop read
                Assert.Throws<ObjectDisposedException>(() => fs.Read(new byte[1], 0, 0));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[2], 1, 2));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => fs.Read(new byte[1], 0, -1));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Read(new byte[1], -1, -1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Read(null, -1, -1));
            }
        }

        [Fact]
        public void WriteOnlyThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => fs.Read(new byte[1], 0, 1));

                fs.Dispose();
                // Disposed checking happens first
                Assert.Throws<ObjectDisposedException>(() => fs.Read(new byte[1], 0, 1));

                // out of bounds checking happens first
                Assert.Throws<ArgumentException>(null, () => fs.Read(new byte[2], 1, 2));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => fs.Read(new byte[1], 0, -1));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Read(new byte[1], -1, -1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Read(null, -1, -1));
            }
        }

        [Fact]
        public void NoopReadsSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Equal(0, fs.Read(new byte[0], 0, 0));
                Assert.Equal(0, fs.Read(new byte[1], 0, 0));
                // even though offset is out of bounds of array, this is still allowed
                // for the last element
                Assert.Equal(0, fs.Read(new byte[1], 1, 0));
                Assert.Equal(0, fs.Read(new byte[2], 1, 0));
            }
        }

        [Fact]
        public void EmptyFileReadsSucceed()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                byte[] buffer = new byte[TestBuffer.Length];

                // use a recognizable pattern
                TestBuffer.CopyTo(buffer, 0);

                Assert.Equal(0, fs.Read(buffer, 0, 1));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, fs.Read(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, fs.Read(buffer, buffer.Length - 1, 1));
                Assert.Equal(TestBuffer, buffer);

                Assert.Equal(0, fs.Read(buffer, buffer.Length / 2, buffer.Length - buffer.Length / 2));
                Assert.Equal(TestBuffer, buffer);
            }
        }

        [Fact]
        public void ReadExistingFile()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                fs.Write(TestBuffer, 0, TestBuffer.Length);
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[TestBuffer.Length];
                Assert.Equal(TestBuffer.Length, fs.Read(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer);

                // read with too large buffer at front of buffer
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, fs.Read(buffer, 0, buffer.Length));
                Assert.Equal(TestBuffer, buffer.Take(TestBuffer.Length));
                // Remainder of buffer should be untouched.
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length], buffer.Skip(TestBuffer.Length));

                // read with too large buffer in middle of buffer
                fs.Position = 0;
                buffer = new byte[TestBuffer.Length * 2];
                Assert.Equal(TestBuffer.Length, fs.Read(buffer, 2, buffer.Length - 2));
                Assert.Equal(TestBuffer, buffer.Skip(2).Take(TestBuffer.Length));
                // Remainder of buffer should be untouched.
                Assert.Equal(new byte[2], buffer.Take(2));
                Assert.Equal(new byte[buffer.Length - TestBuffer.Length - 2], buffer.Skip(2 + TestBuffer.Length));
            }
        }
    }
}
