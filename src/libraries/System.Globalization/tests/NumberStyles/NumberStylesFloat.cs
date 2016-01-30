// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesFloat
    {
        // PosTest1: Verify value of NumberStyles.Float
        [Fact]
        public void TestFloat()
        {
            int AllowLeadingWhite = 0x00000001,
                AllowTrailingWhite = 0x00000002,
                AllowLeadingSign = 0x00000004,
                AllowDecimalPoint = 0x00000020,
                AllowExponent = 0x00000080;

            int expected = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign |
                           AllowDecimalPoint | AllowExponent;

            int actual = (int)NumberStyles.Float;
            Assert.Equal(expected, actual);
        }
    }
}
