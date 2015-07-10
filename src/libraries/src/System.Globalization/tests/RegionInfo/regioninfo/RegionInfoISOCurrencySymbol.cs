// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoISOCurrencySymbol
    {
        // PosTest1:Return the property ISOCurrencySymbol in RegionInfo object 1
        [Fact]
        public void PosTest1()
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            Assert.Equal("USD", strISOCurrency);
        }

        // PosTest2:Return the property ISOCurrencySymbol in RegionInfo object 2
        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            Assert.Equal("CNY", strISOCurrency);
        }

        // PosTest3:Return the property ISOCurrencySymbol in RegionInfo object 3
        [Fact]
        public void PosTest3()
        {
            RegionInfo regionInfo = new RegionInfo("de-DE");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            Assert.Equal("EUR", strISOCurrency);
        }

        // PosTest4:Return the property ISOCurrencySymbol in RegionInfo object 4
        [Fact]
        public void PosTest4()
        {
            RegionInfo regionInfo = new RegionInfo("it-IT");
            string strISOCurrency = regionInfo.ISOCurrencySymbol;
            Assert.Equal("EUR", strISOCurrency);
        }
    }
}
