// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeStylesTests
    {
        [Theory]
        [InlineData(DateTimeStyles.AllowInnerWhite, 0x00000004)]
        [InlineData(DateTimeStyles.AssumeLocal, 0x00000020)]
        [InlineData(DateTimeStyles.AllowWhiteSpaces, DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite)]
        [InlineData(DateTimeStyles.AllowTrailingWhite, 0x00000002)]
        [InlineData(DateTimeStyles.AdjustToUniversal, 0x00000010)]
        [InlineData(DateTimeStyles.AllowLeadingWhite, 0x00000001)]
        [InlineData(DateTimeStyles.None, 0x00000000)]
        [InlineData(DateTimeStyles.AssumeUniversal, 0x00000040)]
        [InlineData(DateTimeStyles.NoCurrentDateDefault, 0x00000008)]
        [InlineData(DateTimeStyles.RoundtripKind, 0x00000080)]
        public void EnumNames_MatchExpectedValues(DateTimeStyles style, ulong expected)
        {
            Assert.Equal(expected, (ulong)style);
        }
    }
}
