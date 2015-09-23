// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoIsMetric
    {
        // PosTest1:Return the property IsMetric in RegionInfo object 1
        [Fact]
        public void PosTest1()
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            Assert.False(regionInfo.IsMetric);
        }

        // PosTest2:Return the property IsMetric in RegionInfo object 2
        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            Assert.True(regionInfo.IsMetric);
        }
    }
}