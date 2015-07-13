// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoGetHashCode
    {
        // PosTest1:Get the hash code of the RegionInfo object 1
        [Fact]
        public void PosTest1()
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            int hashCode = regionInfo.GetHashCode();
            Assert.Equal(regionInfo.Name.GetHashCode(), hashCode);
        }

        // PosTest2:Get the hash code of the RegionInfo object 2
        [Fact]
        public void PosTest2()
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            int hashCode = regionInfo.GetHashCode();
            Assert.Equal(regionInfo.Name.GetHashCode(), hashCode);
        }
    }
}
