// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
