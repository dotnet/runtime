// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public static class FileAttributesTests
    {
        [Fact]
        public static void ValueTest()
        {
            Assert.Equal(0x0001, (int)FileAttributes.ReadOnly);
            Assert.Equal(0x0002, (int)FileAttributes.Hidden);
            Assert.Equal(0x0004, (int)FileAttributes.System);
            Assert.Equal(0x0010, (int)FileAttributes.Directory);
            Assert.Equal(0x0020, (int)FileAttributes.Archive);
            Assert.Equal(0x0040, (int)FileAttributes.Device);
            Assert.Equal(0x0080, (int)FileAttributes.Normal);
            Assert.Equal(0x0100, (int)FileAttributes.Temporary);
            Assert.Equal(0x0200, (int)FileAttributes.SparseFile);
            Assert.Equal(0x0400, (int)FileAttributes.ReparsePoint);
            Assert.Equal(0x0800, (int)FileAttributes.Compressed);
            Assert.Equal(0x1000, (int)FileAttributes.Offline);
            Assert.Equal(0x2000, (int)FileAttributes.NotContentIndexed);
            Assert.Equal(0x4000, (int)FileAttributes.Encrypted);
            Assert.Equal(0x8000, (int)FileAttributes.IntegrityStream);
            Assert.Equal(0x20000, (int)FileAttributes.NoScrubData);
        }
    
    }
}
