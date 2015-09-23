// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencySymbol
    {
        // PosTest1: Verify value of property CurrencySymbol for specific locales
        [Theory]
        [InlineData("en-US", "$")]
        [InlineData("en-GB", "\x00a3")] // pound
        [InlineData("", "\x00a4")] // international
        public void PosTest1(string localeName, string expectedCurrencySymbol)
        {
            CultureInfo myCulture = new CultureInfo(localeName);
            Assert.Equal(expectedCurrencySymbol, myCulture.NumberFormat.CurrencySymbol);
        }
    }
}
