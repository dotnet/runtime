// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentDecimalDigits
    {
        [Fact]
        public void PercentDecimalDigits_Get_InvariantInfo()
        {
            Assert.Equal(2, NumberFormatInfo.InvariantInfo.PercentDecimalDigits);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        public void PercentDecimalDigits_Set(int newPercentDecimalDigits)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentDecimalDigits = newPercentDecimalDigits;
            Assert.Equal(newPercentDecimalDigits, format.PercentDecimalDigits);
        }

        [Fact]
        public void PercentDecimalDigits_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("PercentDecimalDigits", () => new NumberFormatInfo().PercentDecimalDigits = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("PercentDecimalDigits", () => new NumberFormatInfo().PercentDecimalDigits = 100);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentDecimalDigits = 1);
        }
    }
}
