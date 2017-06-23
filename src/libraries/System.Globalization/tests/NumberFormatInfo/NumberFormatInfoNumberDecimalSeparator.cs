// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoNumberDecimalSeparator
    {
        [Fact]
        public void NumberDecimalSeparator_Get_InvariantInfo()
        {
            Assert.Equal(".", NumberFormatInfo.InvariantInfo.NumberDecimalSeparator);
        }

        [Theory]
        [InlineData("string")]
        [InlineData("    ")]
        public void NumberDecimalSeparator_Set(string newNumberDecimalSeparator)
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.NumberDecimalSeparator = newNumberDecimalSeparator;
            Assert.Equal(newNumberDecimalSeparator, format.NumberDecimalSeparator);
        }

        [Fact]
        public void NumberDecimalSeparator_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("NumberDecimalSeparator", () => new NumberFormatInfo().NumberDecimalSeparator = null);
            AssertExtensions.Throws<ArgumentException>(null, () => new NumberFormatInfo().NumberDecimalSeparator = "");
            Assert.Throws<InvalidOperationException>(() => NumberFormatInfo.InvariantInfo.NumberDecimalSeparator = "string");
        }
    }
}
