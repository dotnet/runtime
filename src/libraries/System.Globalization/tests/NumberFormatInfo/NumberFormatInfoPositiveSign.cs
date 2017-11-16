// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPositiveSign
    {
        public static IEnumerable<object[]> PositiveSign_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, "+" };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, "+" };
        }

        [Theory]
        [MemberData(nameof(PositiveSign_TestData))]
        public void PositiveSign_Get(NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, format.PositiveSign);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("   ")]
        [InlineData("")]
        public void PositiveSign_Set(string newPositiveSign)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PositiveSign = newPositiveSign;
            Assert.Equal(newPositiveSign, format.PositiveSign);
        }

        [Fact]
        public void PositiveSign_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PositiveSign", () => new NumberFormatInfo().PositiveSign = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PositiveSign = "");
        }
    }
}
