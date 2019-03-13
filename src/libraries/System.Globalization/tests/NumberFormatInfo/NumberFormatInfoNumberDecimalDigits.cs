// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberDecimalDigits
    {
        public static IEnumerable<object[]> NumberDecimalDigits_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 2, 2 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 2, 3 };
        }

        [Theory]
        [MemberData(nameof(NumberDecimalDigits_TestData))]
        public void NumberDecimalDigits_Get_ReturnsExpected(NumberFormatInfo format, int expectedWindows, int expectedIcu)
        {
            int expected = PlatformDetection.IsWindows ? expectedWindows : expectedIcu;
            Assert.Equal(expected, format.NumberDecimalDigits);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        public void NumberDecimalDigits_Set_GetReturnsExpected(int newNumberDecimalDigits)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberDecimalDigits = newNumberDecimalDigits;
            Assert.Equal(newNumberDecimalDigits, format.NumberDecimalDigits);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void NumberDecimalDigits_SetInvalid_ThrowsArgumentOutOfRangeException(int value)
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", "NumberDecimalDigits", () => format.NumberDecimalDigits = value);
        }
        
        [Fact]
        public void NumberDecimalDigits_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberDecimalDigits = 1);
        }
    }
}
