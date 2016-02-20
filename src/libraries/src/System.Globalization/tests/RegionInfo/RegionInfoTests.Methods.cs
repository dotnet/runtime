// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoMethodTests
    {
        [Theory]
        [InlineData("US", "US")]
        [InlineData("IT", "IT")]
        [InlineData("IE", "IE")]
        [InlineData("SA", "SA")]
        [InlineData("JP", "JP")]
        [InlineData("CN", "CN")]
        [InlineData("TW", "TW")]
        [InlineData("en-GB", "GB")]
        [InlineData("en-IE", "IE")]
        [InlineData("en-US", "US")]
        [InlineData("zh-CN", "CN")]
        public void Ctor(string name, string expectedName)
        {
            var regionInfo = new RegionInfo(name);
            Assert.Equal(expectedName, regionInfo.Name);
            Assert.Equal(regionInfo.Name, regionInfo.ToString());
        }

        [Fact]
        public void Ctor_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new RegionInfo(null)); // Culture is null
            Assert.Throws<ArgumentException>(() => new RegionInfo("")); // Culture is non-existent
            Assert.Throws<ArgumentException>(() => new RegionInfo("no-such-culture")); // Culture is non-existent
            Assert.Throws<ArgumentException>(() => new RegionInfo("en")); // Culture is neutral
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("en-US"), true };
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("US"), true };
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("en-GB"), false };
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("zh-CN"), false };
            yield return new object[] { new RegionInfo("en-US"), new object(), false };
        }

        [Theory]
        [MemberData("Equals_TestData")]
        public void Equals(RegionInfo regionInfo1, object obj, bool expected)
        {
            Assert.Equal(expected, regionInfo1.Equals(obj));
            Assert.Equal(regionInfo1.GetHashCode(), regionInfo1.GetHashCode());
            if (obj is RegionInfo)
            {
                Assert.Equal(expected, regionInfo1.GetHashCode().Equals(obj.GetHashCode()));
            }
        }
    }
}
