// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Write : FileSystemTest
    {
        [Fact]
        public void NullArrayThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Write(null, 0, 1));
            }
        }

        [Fact]
        public void NegativeOffsetThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Write(new byte[1], -1, 1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Write(null, -1, 1));
            }
        }

        [Fact]
        public void NegativeCountThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => fs.Write(new byte[1], 0, -1));

                // offset is checked before count
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Write(new byte[1], -1, -1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Write(null, -1, -1));
            }
        }

        [Fact]
        public void ArrayOutOfBoundsThrows()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                // offset out of bounds
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[1], 1, 1));

                // offset out of bounds for 0 count Write
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[1], 2, 0));

                // offset out of bounds even for 0 length buffer
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[0], 1, 0));

                // combination offset and count out of bounds
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[2], 1, 2));

                // edges
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[0], int.MaxValue, 0));
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[0], int.MaxValue, int.MaxValue));
            }
        }

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
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[2], 1, 2));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => fs.Write(new byte[1], 0, -1));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Write(new byte[1], -1, -1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Write(null, -1, -1));
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
                Assert.Throws<ArgumentException>(null, () => fs.Write(new byte[2], 1, 2));

                // count is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => fs.Write(new byte[1], 0, -1));

                // offset is checked prior
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.Write(new byte[1], -1, -1));

                // array is checked first
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.Write(null, -1, -1));
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
