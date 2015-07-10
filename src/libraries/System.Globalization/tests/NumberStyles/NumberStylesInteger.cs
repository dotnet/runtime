// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesInteger
    {
        // PosTest1: Verify value of NumberStyles.Integer
        [Fact]
        public void TestInteger()
        {
            int AllowLeadingWhite = 0x00000001,
                AllowTrailingWhite = 0x00000002,
                AllowLeadingSign = 0x00000004;

            int expected = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign;
            int actual = (int)NumberStyles.Integer;

            Assert.Equal(expected, actual);
        }
    }
}
