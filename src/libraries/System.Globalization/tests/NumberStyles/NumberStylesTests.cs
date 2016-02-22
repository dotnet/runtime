// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesTests
    {
        [Theory]
        [InlineData(NumberStyles.AllowCurrencySymbol, 0x00000100)]
        [InlineData(NumberStyles.AllowDecimalPoint, 0x00000020)]
        [InlineData(NumberStyles.AllowExponent, 0x00000080)]
        [InlineData(NumberStyles.AllowHexSpecifier, 0x00000200)]
        [InlineData(NumberStyles.AllowLeadingSign, 0x00000004)]
        [InlineData(NumberStyles.AllowLeadingWhite, 0x00000001)]
        [InlineData(NumberStyles.AllowParentheses, 0x00000010)]
        [InlineData(NumberStyles.AllowThousands, 0x00000040)]
        [InlineData(NumberStyles.AllowTrailingSign, 0x00000008)]
        [InlineData(NumberStyles.AllowTrailingWhite, 0x00000002)]
        [InlineData(NumberStyles.Any, NumberStyles.AllowExponent | NumberStyles.Currency)]
        [InlineData(NumberStyles.Currency, NumberStyles.AllowParentheses | NumberStyles.Number | NumberStyles.AllowCurrencySymbol)]
        [InlineData(NumberStyles.Float, NumberStyles.Integer | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent)]
        [InlineData(NumberStyles.HexNumber, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowHexSpecifier)]
        [InlineData(NumberStyles.Integer, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign)]
        [InlineData(NumberStyles.None, 0x00000000)]
        [InlineData(NumberStyles.Number, NumberStyles.Integer | NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands)]
        public void EnumNames_MatchExpectedValues(NumberStyles style, int expected)
        {
            Assert.Equal(expected, (int)style);
        }
    }
}
