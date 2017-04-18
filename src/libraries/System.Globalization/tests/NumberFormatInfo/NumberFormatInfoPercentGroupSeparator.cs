// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoPercentGroupSeparator
    {
        [Fact]
        public void PercentGroupSeparator_Get_InvariantInfo()
        {
            Assert.Equal(",", NumberFormatInfo.InvariantInfo.PercentGroupSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        [InlineData("")]
        public void PercentGroupSeparator_Set(string newPercentGroupSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.PercentGroupSeparator = newPercentGroupSeparator;
            Assert.Equal(newPercentGroupSeparator, format.PercentGroupSeparator);
        }

        [Fact]
        public void PercentGroupSeparator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("PercentGroupSeparator", () => new NumberFormatInfo().PercentGroupSeparator = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.PercentGroupSeparator = "string");
        }
    }
}
