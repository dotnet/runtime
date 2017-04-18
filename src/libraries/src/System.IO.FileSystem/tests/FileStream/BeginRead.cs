// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_BeginRead : FileSystemTest
    {
        [Fact]
        public void BeginReadThrowsForNullArray()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                AssertExtensions.Throws<ArgumentNullException>("array", () => fs.BeginRead(null, 0, 0, null, null));
            }
        }

        [Fact]
        public void BeginReadThrowsForNegativeOffset()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => fs.BeginRead(new byte[0], -1, 0, null, null));
            }
        }

        [Fact]
        public void BeginReadThrowsForNegativeNumBytes()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("numBytes", () => fs.BeginRead(new byte[0], 0, -1, null, null));
            }
        }

        [Theory
            InlineData(0, 0, 1)
            InlineData(0, 1, 0)
            InlineData(1, 0, 2)
            InlineData(1, 1, 1)
            ]
        public void BeginReadThrowsForBadOffset(int arraySize, int offset, int numBytes)
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<ArgumentException>(() => fs.BeginRead(new byte[arraySize], offset, numBytes, null, null));
            }
        }

        [Fact]
        public void BeginReadThrowsForClosed()
        {
            FileStream fs;
            using (fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
            }

            Assert.Throws<ObjectDisposedException>(() => fs.BeginRead(new byte[0], 0, 0, null, null));
        }

        [Fact]
        public void BeginReadThrowsForWriteOnly()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.Write))
            {
                Assert.Throws<NotSupportedException>(() => fs.BeginRead(new byte[0], 0, 0, null, null));
            }
        }
    }
}
