// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoCtor1
    {
        // PosTest1:Initialize the RegionInfo 1
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultur = new CultureInfo("en-US");
            string culture = myCultur.Name;
            RegionInfo myRegInfo = new RegionInfo(culture);
            Assert.True(myRegInfo.Name == "en-US" || myRegInfo.Name == "US");
        }

        // PosTest2:Initialize the RegionInfo 2
        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultur = new CultureInfo("zh-CN");
            string culture = myCultur.Name;
            RegionInfo myRegInfo = new RegionInfo(culture);
            Assert.True(myRegInfo.Name == "zh-CN" || myRegInfo.Name == "CN");
        }

        // PosTest3:Initialize the RegionInfo 3
        [Fact]
        public void PosTest3()
        {
            CultureInfo myCultur = new CultureInfo("en-IE");
            string culture = myCultur.Name;
            RegionInfo myRegInfo = new RegionInfo(culture);
            Assert.True(myRegInfo.Name == "en-IE" || myRegInfo.Name == "IE");
        }

        // PosTest4:Initialize the RegionInfo 4
        [Fact]
        public void PosTest4()
        {
            CultureInfo myCultur = new CultureInfo("en-GB");
            string culture = myCultur.Name;
            RegionInfo myRegInfo = new RegionInfo(culture);
            Assert.True(myRegInfo.Name == "en-GB" || myRegInfo.Name == "GB");
        }

        // NegTest1:culture specifies neutral culture 1
        [Fact]
        public void NegTest1()
        {
            CultureInfo myCultur = new CultureInfo("en");
            string culture = myCultur.Name;
            Assert.Throws<ArgumentException>(() =>
            {
                RegionInfo myRegInfo = new RegionInfo(culture);
            });
        }
    }
}
