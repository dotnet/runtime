// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoCurrencySymbol
    {
        // PosTest1:Return the CurrencySymbol property in RegionInfo object 1
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCulture = new CultureInfo("en-US");
            RegionInfo regionInfo = new RegionInfo(myCulture.Name);
            string strCurrencySymbol = regionInfo.CurrencySymbol;
            Assert.Equal("$", strCurrencySymbol);
        }

        // PosTest2:Return the CurrencySymbol property in RegionInfo object 2
        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strCurrencySymbol = regionInfo.CurrencySymbol;
            Assert.True(strCurrencySymbol.Equals("\u00A5") || strCurrencySymbol.Equals("\uffe5"));
            // \u00A5 is Latin-1 Supplement (Windows), \uffe5 is Halfwidth and Fullwidth Forms (ICU)
            // String.Normalize(NormalizationForm.FormKD) could also be used if it was ported
        }
    }
}
