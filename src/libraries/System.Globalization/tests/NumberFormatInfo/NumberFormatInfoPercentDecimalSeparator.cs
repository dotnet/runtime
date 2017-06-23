// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentDecimalSeparator
    {
        [Fact]
        public void PercentDecimalSeparator_Get_InvariantInfo()
        {
            Assert.Equal(".", NumberFormatInfo.InvariantInfo.PercentDecimalSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        public void PercentDecimalSeparator_Set(string newPercentDecimalSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentDecimalSeparator = newPercentDecimalSeparator;
            Assert.Equal(newPercentDecimalSeparator, format.PercentDecimalSeparator);
        }

        [Fact]
        public void PercentDecimalSeparator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PercentDecimalSeparator", () => new NumberFormatInfo().PercentDecimalSeparator = null);
            AssertExtensions.Throws<ArgumentException>(null, () => new NumberFormatInfo().PercentDecimalSeparator = "");
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentDecimalSeparator = "string");
        }
    }
}
