// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentDecimalDigits
    {
        public static IEnumerable<object[]> PercentDecimalDigits_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 2, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 2, 3 };
        }

        [Theory]
        [MemberData(nameof(PercentDecimalDigits_TestData))]
        public void PercentDecimalDigits_Get_ReturnsExpected(NumberFormatInfo format, int expectedNls, int expectedIcu)
        {
            int expected = PlatformDetection.IsNlsGlobalization ? expectedNls : expectedIcu;
            Assert.Equal(expected, format.PercentDecimalDigits);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        public void PercentDecimalDigits_Set_GetReturnsExpected(int newPercentDecimalDigits)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentDecimalDigits = newPercentDecimalDigits;
            Assert.Equal(newPercentDecimalDigits, format.PercentDecimalDigits);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void PercentDecimalDigits_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "PercentDecimalDigits", () => format.PercentDecimalDigits = value);
        }


        [Fact]
        public void PercentDecimalDigits_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentDecimalDigits = 1);
        }
    }
}
