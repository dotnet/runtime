// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowParentheses
    {
        // PosTest1: Verify value of NumberStyles.AllowParentheses
        [Fact]
        public void TestAllowParentheses()
        {
            int expected = 0x00000010;
            int actual = (int)NumberStyles.AllowParentheses;
            Assert.Equal(expected, actual);
        }
    }
}
