// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public static class FileModeTests
    {
        [Fact]
        public static void ValueTest()
        {
            Assert.Equal(1, (int)FileMode.CreateNew);
            Assert.Equal(2, (int)FileMode.Create);
            Assert.Equal(3, (int)FileMode.Open);
            Assert.Equal(4, (int)FileMode.OpenOrCreate);
            Assert.Equal(5, (int)FileMode.Truncate);
            Assert.Equal(6, (int)FileMode.Append);
        }
    }
}