// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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