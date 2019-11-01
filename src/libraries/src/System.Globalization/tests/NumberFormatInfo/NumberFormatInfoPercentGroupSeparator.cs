// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentGroupSeparator
    {
        [Fact]
        public void PercentGroupSeparator_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(",", NumberFormatInfo.InvariantInfo.PercentGroupSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        [InlineData("")]
        public void PercentGroupSeparator_Set_GetReturnsExpected(string newPercentGroupSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentGroupSeparator = newPercentGroupSeparator;
            Assert.Equal(newPercentGroupSeparator, format.PercentGroupSeparator);
        }

        [Fact]
        public void PercentGroupSeparator_SetNull_ThrowsArgumentNullException()
        {
            var format = new NumberFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", "PercentGroupSeparator", () => format.PercentGroupSeparator = null);
        }

        [Fact]
        public void PercentGroupSeparator_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentGroupSeparator = "string");
        }
    }
}
