// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberDecimalSeparator
    {
        [Fact]
        public void NumberDecimalSeparator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(".", NumberFormatInfo.InvariantInfo.NumberDecimalSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        public void NumberDecimalSeparator_Set_GetReturnsExpected(string newNumberDecimalSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberDecimalSeparator = newNumberDecimalSeparator;
            Assert.Equal(newNumberDecimalSeparator, format.NumberDecimalSeparator);
        }

        [Fact]
        public void NumberDecimalSeparator_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "NumberDecimalSeparator", () => format.NumberDecimalSeparator = null);
        }

        [Fact]
        public void NumberDecimalSeparator_SetEmpty_ThrowsArgumentException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", null, () => format.NumberDecimalSeparator = "");
        }

        [Fact]
        public void NumberDecimalSeparator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberDecimalSeparator = "string");
        }
    }
}
