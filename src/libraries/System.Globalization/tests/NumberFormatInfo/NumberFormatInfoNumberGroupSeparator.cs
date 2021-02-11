// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberGroupSeparator
    {
        [Fact]
        public void NumberGroupSeparator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(",", NumberFormatInfo.InvariantInfo.NumberGroupSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        [InlineData("")]
        public void NumberGroupSeparator_Set_GetReturnsExpected(string newNumberGroupSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberGroupSeparator = newNumberGroupSeparator;
            Assert.Equal(newNumberGroupSeparator, format.NumberGroupSeparator);
        }

        [Fact]
        public void NumberGroupSeparator_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "NumberGroupSeparator", () => format.NumberGroupSeparator = null);
        }

        [Fact]
        public void NumberGroupSeparator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberGroupSeparator = "string");
        }
    }
}
