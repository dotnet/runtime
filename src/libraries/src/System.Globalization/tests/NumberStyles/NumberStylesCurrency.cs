// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesCurrency
    {
        // PosTest1: Verify value of NumberStyles.Currency
        [Fact]
        public void TestCurrency()
        {
            int AllowLeadingWhite = 0x00000001,
                AllowTrailingWhite = 0x00000002,
                AllowLeadingSign = 0x00000004,
                AllowTrailingSign = 0x00000008,
                AllowParentheses = 0x00000010,
                AllowDecimalPoint = 0x00000020,
                AllowThousands = 0x00000040,
                AllowCurrencySymbol = 0x00000100;

            int expected = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                           AllowParentheses | AllowDecimalPoint | AllowThousands | AllowCurrencySymbol;

            int actual = (int)NumberStyles.Currency;
            Assert.Equal(expected, actual);
        }
    }
}
