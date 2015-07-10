// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesNone
    {
        // PosTest1: Verify value of NumberStyles.None
        [Fact]
        public void TestNone()
        {
            int expected = 0x00000000;
            int actual = (int)NumberStyles.None;
            Assert.Equal(expected, actual);
        }
    }
}
