// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.Equal(strCurrencySymbol, "\u00A5");
        }
    }
}