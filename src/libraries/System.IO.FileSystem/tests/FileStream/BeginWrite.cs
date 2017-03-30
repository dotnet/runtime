// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_BeginWrite : FileSystemTest
    {
        [Fact]
        public void BeginWriteThrowsForNullArray()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<ArgumentNullException>("array", () => fs.BeginWrite(null, 0, 0, null, null));
            }
        }

        [Fact]
        public void BeginWriteThrowsForNegativeOffset()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => fs.BeginWrite(new byte[0], -1, 0, null, null));
            }
        }

        [Fact]
        public void BeginWriteThrowsForNegativeNumBytes()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<ArgumentOutOfRangeException>("numBytes", () => fs.BeginWrite(new byte[0], 0, -1, null, null));
            }
        }

        [Theory
            InlineData(0, 0, 1)
            InlineData(0, 1, 0)
            InlineData(1, 0, 2)
            InlineData(1, 1, 1)
            ]
        public void BeginWriteThrowsForBadOffset(int arraySize, int offset, int numBytes)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<ArgumentException>(() => fs.BeginWrite(new byte[arraySize], offset, numBytes, null, null));
            }
        }

        [Fact]
        public void BeginWriteThrowsForClosed()
        {
            FileStream fs;
            using (fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
            }

            Assert.Throws<ObjectDisposedException>(() => fs.BeginWrite(new byte[0], 0, 0, null, null));
        }

        [Fact]
        public void BeginWriteThrowsForReadOnly()
        {
            string testPath = GetTestFilePath();
            using (FileStream fs = new FileStream(testPath, FileMode.Create, FileAccess.Write))
            {
            }

            using (FileStream fs = new FileStream(testPath, FileMode.Open, FileAccess.Read))
            {
                Assert.Throws<NotSupportedException>(() => fs.BeginWrite(new byte[0], 0, 0, null, null));
            }
        }
    }
}
