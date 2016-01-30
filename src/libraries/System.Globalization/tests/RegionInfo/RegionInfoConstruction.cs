// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoConstruction
    {
        [Fact]
        public void CreateRegionInfoUsingRegionName()
        {
            RegionInfo ri = new RegionInfo("US");
            ri = new RegionInfo("US");
            ri = new RegionInfo("IT");
            ri = new RegionInfo("SA");
            ri = new RegionInfo("JP");
            ri = new RegionInfo("CN");
            ri = new RegionInfo("TW");
        }
    }
}
