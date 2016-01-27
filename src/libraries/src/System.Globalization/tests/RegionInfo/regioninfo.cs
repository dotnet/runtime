// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoTest
    {
        [Fact]
        public void Test1()
        {
            RegionInfo riUS = new RegionInfo("en-US");
            RegionInfo riUK = new RegionInfo("en-GB");
            Assert.NotEqual(riUK, riUS);
        }

        [Fact]
        public void Test2()
        {
            RegionInfo riUS = new RegionInfo("en-US");
            RegionInfo riUK = new RegionInfo("en-GB");
            Assert.NotEqual(riUS.GetHashCode(), riUK.GetHashCode());
        }

        [Fact]
        public void Test3()
        {
            RegionInfo riUS = new RegionInfo("en-US");
            Assert.Equal(riUS.ToString(), "US");
        }

        [Fact]
        public void Test4()
        {
            RegionInfo riUS = new RegionInfo("en-US");
            Assert.Equal("United States", riUS.DisplayName);
        }
    }
}
