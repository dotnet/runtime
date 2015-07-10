// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoToString
    {
        // PosTest1:Invoke the method ToString in RegionInfo object 1
        [Fact]
        public void PosTest1()
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strVal = regionInfo.ToString();
            Assert.Equal(regionInfo.Name, strVal);
        }

        // PosTest2:Invoke the method ToString in RegionInfo object 2
        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strVal = regionInfo.ToString();
            Assert.Equal(regionInfo.Name, strVal);
        }

        // PosTest3:Invoke the method ToString in RegionInfo object 3
        [Fact]
        public void PosTest3()
        {
            RegionInfo regionInfo = new RegionInfo("en-IE");
            string strVal = regionInfo.ToString();
            Assert.Equal(regionInfo.Name, strVal);
        }
    }
}