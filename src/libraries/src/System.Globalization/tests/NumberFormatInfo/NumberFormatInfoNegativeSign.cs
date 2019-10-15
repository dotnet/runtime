// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNegativeSign
    {
        public static IEnumerable<object[]> NegativeSign_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "-" };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, "-" };
        }

        [Theory]
        [MemberData(nameof(NegativeSign_TestData))]
        public void NegativeSign_Get_ReturnsExpected(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.NegativeSign);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void NegativeSign_Set_GetReturnsExpected(string newNegativeSign)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NegativeSign = newNegativeSign;
            Assert.Equal(newNegativeSign, format.NegativeSign);
        }

        [Fact]
        public void NegativeSign_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "NegativeSign", () => format.NegativeSign = null);
        }

        [Fact]
        public void NegativeSign_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NegativeSign = "");
        }
    }
}
