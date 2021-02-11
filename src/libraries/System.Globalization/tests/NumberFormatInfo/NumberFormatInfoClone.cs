// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoClone
    {
        [Fact]
        public void Clone()
        {
            NumberFormatInfo format = new NumberFormatInfo();
            format.CurrencyDecimalSeparator = "string";

            NumberFormatInfo clone = (NumberFormatInfo)format.Clone();
            Assert.NotEqual(format.GetHashCode(), clone.GetHashCode());
            Assert.NotEqual(format, clone);
            Assert.NotSame(format, clone);

            Assert.Equal(format.CurrencyDecimalDigits, clone.CurrencyDecimalDigits);
            Assert.Equal(format.CurrencyDecimalSeparator, clone.CurrencyDecimalSeparator);
            Assert.Equal(format.CurrencyGroupSizes, clone.CurrencyGroupSizes);
            Assert.Equal(format.CurrencyGroupSeparator, clone.CurrencyGroupSeparator);
            Assert.Equal(format.CurrencyNegativePattern, clone.CurrencyNegativePattern);
            Assert.Equal(format.CurrencyPositivePattern, clone.CurrencyPositivePattern);
            Assert.Equal(format.IsReadOnly, clone.IsReadOnly);
        }
    }
}
