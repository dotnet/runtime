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
        [InlineData("US", "US", "US")]
        [InlineData("IT", "IT", "IT")]
        [InlineData("IE", "IE", "IE")]
        [InlineData("SA", "SA", "SA")]
        [InlineData("JP", "JP", "JP")]
        [InlineData("CN", "CN", "CN")]
        [InlineData("TW", "TW", "TW")]
        [InlineData("en-GB", "GB", "en-GB")]
        [InlineData("en-IE", "IE", "en-IE")]
        [InlineData("en-US", "US", "en-US")]
        [InlineData("zh-CN", "CN", "zh-CN")]
        public void Ctor(string name, string expectedName, string windowsDesktopName)
        {
            var regionInfo = new RegionInfo(name);
            Assert.True(windowsDesktopName.Equals(regionInfo.Name) || expectedName.Equals(regionInfo.Name));
            Assert.Equal(regionInfo.Name, regionInfo.ToString());
        }

        [Fact]
        public void Ctor_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new RegionInfo(null));
        }

        [Fact]
        public void Ctor_EmptyName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("name", null, () => new RegionInfo(""));
        }

        [Theory]
        [InlineData("no-such-culture")]
        [InlineData("en")]
        public void Ctor_InvalidName_ThrowsArgumentException(string name)
        {
            AssertExtensions.Throws<ArgumentException>("name", () => new RegionInfo(name));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("en-US"), true };
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("en-GB"), false };
            yield return new object[] { new RegionInfo("en-US"), new RegionInfo("zh-CN"), false };
            yield return new object[] { new RegionInfo("en-US"), new object(), false };
            yield return new object[] { new RegionInfo("en-US"), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
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
