// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
