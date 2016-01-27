// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesNumber
    {
        // PosTest1: Verify value of NumberStyles.Number
        [Fact]
        public void TestNumber()
        {
            int AllowLeadingWhite = 0x00000001,
                AllowTrailingWhite = 0x00000002,
                AllowLeadingSign = 0x00000004,
                AllowTrailingSign = 0x00000008,
                AllowDecimalPoint = 0x00000020,
                AllowThousands = 0x00000040;

            int expected = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                           AllowDecimalPoint | AllowThousands;

            int actual = (int)NumberStyles.Number;
            Assert.Equal(expected, actual);
        }
    }
}
