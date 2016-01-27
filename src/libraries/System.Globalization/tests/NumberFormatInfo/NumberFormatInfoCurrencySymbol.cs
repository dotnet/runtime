// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
