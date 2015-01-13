// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public static class FileShareTests
    {
        [Fact]
        public static void ValueTest()
        {
            Assert.Equal(0, (int)FileShare.None);
            Assert.Equal(1, (int)FileShare.Read);
            Assert.Equal(2, (int)FileShare.Write);
            Assert.Equal(3, (int)FileShare.ReadWrite);
            Assert.Equal(4, (int)FileShare.Delete);
            Assert.Equal(0x10, (int)FileShare.Inheritable);
        }
    }
}