// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberNegativePattern
    {
        public static IEnumerable<object[]> NumberNegativePattern_TestData()
        {
            yield return new object[] { NumberFormatInfo.InvariantInfo, 1 };
            yield return new object[] { CultureInfo.GetCultureInfo("en-US").NumberFormat, 1 };
        }

        [Theory]
        [MemberData(nameof(NumberNegativePattern_TestData))]
        public void NumberNegativePattern_Get(NumberFormatInfo format, int expected)
        {
            Assert.Equal(expected, format.NumberNegativePattern);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public void NumberNegativePattern_Set(int newNumberNegativePattern)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberNegativePattern = newNumberNegativePattern;
            Assert.Equal(newNumberNegativePattern, format.NumberNegativePattern);
        }

        [Fact]
        public void NumberNegativePattern_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("NumberNegativePattern", () => new NumberFormatInfo().NumberNegativePattern = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("NumberNegativePattern", () => new NumberFormatInfo().NumberNegativePattern = 5);

            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberNegativePattern = 1);
        }
    }
}
