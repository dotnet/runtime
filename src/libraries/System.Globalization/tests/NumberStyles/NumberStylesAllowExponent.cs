// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowExponent
    {
        // PosTest1: Verify value of NumberStyles.AllowExponent
        [Fact]
        public void TestAllowExponent()
        {
            int expected = 0x00000080;
            int actual = (int)NumberStyles.AllowExponent;
            Assert.Equal(expected, actual);
        }
    }
}
