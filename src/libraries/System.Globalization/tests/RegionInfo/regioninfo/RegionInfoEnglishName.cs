// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoEnglishName
    {
        // PosTest1:Return the property EngLishName in RegionInfo object 1
        [Fact]
        public void PosTest1()
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strVal = regionInfo.EnglishName;
            Assert.Equal("United States", strVal);
        }

        // PosTest2:Return the property EngLishName in RegionInfo object 2
        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo = new RegionInfo("US");
            string strVal = regionInfo.EnglishName;
            Assert.Equal("United States", strVal);
        }

        // PosTest3:Return the property EngLishName in RegionInfo object 3
        [Fact]
        public void PosTest3()
        {
            RegionInfo regionInfo = new RegionInfo("CN");
            string strVal = regionInfo.EnglishName;
            Assert.True(strVal == "China" || strVal == "People's Republic of China");
        }

        // PosTest4:Return the property EngLishName in RegionInfo object 4
        [Fact]
        public void PosTest4()
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strVal = regionInfo.EnglishName;
            Assert.True(strVal == "China" || strVal == "People's Republic of China");
        }
    }
}
