// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentDecimalSeparator
    {
        [Fact]
        public void PercentDecimalSeparator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(".", NumberFormatInfo.InvariantInfo.PercentDecimalSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        public void PercentDecimalSeparator_Set_GetReturnsExpected(string newPercentDecimalSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentDecimalSeparator = newPercentDecimalSeparator;
            Assert.Equal(newPercentDecimalSeparator, format.PercentDecimalSeparator);
        }

        [Fact]
        public void PercentDecimalSeparator_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "PercentDecimalSeparator", () => format.PercentDecimalSeparator = null);
        }

        [Fact]
        public void PercentDecimalSeparator_SetEmpty_ThrowsArgumentException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", null, () => format.PercentDecimalSeparator = "");
        }

        [Fact]
        public void PercentDecimalSeparator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentDecimalSeparator = "string");
        }
    }
}
