// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoClone
    {
        // PosTest1: Verify method Clone
        [Fact]
        public void TestClone()
        {
            NumberFormatInfo nfi1 = new NumberFormatInfo();
            nfi1.CurrencyDecimalSeparator = "testStr";
            NumberFormatInfo nfi2 = (NumberFormatInfo)nfi1.Clone();
            Assert.NotEqual(nfi1.GetHashCode(), nfi2.GetHashCode());
            Assert.NotEqual(nfi1, nfi2);
            Assert.Equal(nfi1.CurrencyDecimalDigits, nfi2.CurrencyDecimalDigits);
            Assert.Equal(nfi1.CurrencyDecimalSeparator, nfi2.CurrencyDecimalSeparator);
            Assert.Equal(nfi1.CurrencyGroupSizes, nfi2.CurrencyGroupSizes);
            Assert.Equal(nfi1.CurrencyGroupSeparator, nfi2.CurrencyGroupSeparator);
            Assert.Equal(nfi1.CurrencyNegativePattern, nfi2.CurrencyNegativePattern);
            Assert.Equal(nfi1.CurrencyPositivePattern, nfi2.CurrencyPositivePattern);
            Assert.Equal(nfi1.IsReadOnly, nfi2.IsReadOnly);
        }
    }
}
