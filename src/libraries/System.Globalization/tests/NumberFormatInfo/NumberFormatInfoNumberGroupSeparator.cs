// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberGroupSeparator
    {
        [Fact]
        public void NumberGroupSeparator_Get_InvariantInfo()
        {
            Assert.Equal(",", NumberFormatInfo.InvariantInfo.NumberGroupSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        [InlineData("")]
        public void NumberGroupSeparator_Set(string newNumberGroupSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberGroupSeparator = newNumberGroupSeparator;
            Assert.Equal(newNumberGroupSeparator, format.NumberGroupSeparator);
        }

        [Fact]
        public void NumberGroupSeparator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("NumberGroupSeparator", () => new NumberFormatInfo().NumberGroupSeparator = null);
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberGroupSeparator = "string");
        }
    }
}
