// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowDecimalPoint
    {
        // PosTest1: Verify value of NumberStyles.AllowDecimalPoint
        [Fact]
        public void TestAllowDecimalPoint()
        {
            int expected = 0x00000020;
            int actual = (int)NumberStyles.AllowDecimalPoint;
            Assert.Equal(expected, actual);
        }
    }
}
