// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoEquals
    {
        [Fact]
        public void PosTest1()
        {
            RegionInfo regionInfo1 = new RegionInfo("en-US");
            RegionInfo regionInfo2 = new RegionInfo("en-US");
            Assert.True(regionInfo1.Equals(regionInfo2));
        }

        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo1 = new RegionInfo("en-US");
            RegionInfo regionInfo2 = new RegionInfo("zh-CN");
            Assert.False(regionInfo1.Equals(regionInfo2));
        }

        [Fact]
        public void PosTest3()
        {
            RegionInfo regionInfo1 = new RegionInfo("US");
            RegionInfo regionInfo2 = new RegionInfo("en-US");
            Assert.True(regionInfo1.Equals(regionInfo2));
        }

        // PosTest4:RegionInfo object compared with not RegionInfo object
        [Fact]
        public void PosTest4()
        {
            RegionInfo regionInfo1 = new RegionInfo("en-US");
            object objVal = new object();
            Assert.False(regionInfo1.Equals(objVal));
        }
    }
}
