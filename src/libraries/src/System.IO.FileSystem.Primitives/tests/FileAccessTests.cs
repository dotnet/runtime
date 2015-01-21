// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public static class FileAccessTests
    {
        [Fact]
        public static void ValueTest()
        {
            Assert.Equal(1, (int)FileAccess.Read);
            Assert.Equal(2, (int)FileAccess.Write);
            Assert.Equal(3, (int)FileAccess.ReadWrite);
        }
    }
}