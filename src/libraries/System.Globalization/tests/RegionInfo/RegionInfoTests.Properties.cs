// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoPropertyTests
    {
        [Fact]
        public void CurrentRegion()
        {
            Assert.Equal(RegionInfo.CurrentRegion, new RegionInfo(CultureInfo.CurrentCulture.Name));
            Assert.Same(RegionInfo.CurrentRegion, RegionInfo.CurrentRegion);
        }

        [Theory]
        [InlineData("en-US", "United States")]
        public void DisplayName(string name, string expected)
        {
            Assert.Equal(expected, new RegionInfo(name).DisplayName);
        }

        [Theory]
        [InlineData("en-US", new string[] { "United States" })]
        [InlineData("US", new string[] { "United States" })]
        [InlineData("zh-CN", new string[] { "China", "People's Republic of China" })]
        [InlineData("CN", new string[] { "China", "People's Republic of China" })]
        public void EnglishName(string name, string[] expected)
        {
            string result = new RegionInfo(name).EnglishName;
            Assert.True(expected.Contains(result));
        }

        [Theory]
        [InlineData("en-US", false)]
        [InlineData("zh-CN", true)]
        public void IsMetric(string name, bool expected)
        {
            Assert.Equal(expected, new RegionInfo(name).IsMetric);
        }

        [Theory]
        [InlineData("en-US", "USD")]
        [InlineData("zh-CN", "CNY")]
        [InlineData("de-DE", "EUR")]
        [InlineData("it-IT", "EUR")]
        public void ISOCurrencySymbol(string name, string expected)
        {
            Assert.Equal(expected, new RegionInfo(name).ISOCurrencySymbol);
        }

        [Theory]
        [InlineData("en-US", new string[] { "$" })]
        [InlineData("zh-CN", new string[] { "\u00A5", "\uffe5" })] // \u00A5 is Latin-1 Supplement(Windows), \uffe5 is Halfwidth and Fullwidth Forms(ICU)
        public void CurrencySymbol(string name, string[] expected)
        {
            string result = new RegionInfo(name).CurrencySymbol;
            Assert.True(expected.Contains(result));
        }

        [Theory]
        [InlineData("en-US", "US")]
        [InlineData("zh-CN", "CN")]
        [InlineData("de-DE", "DE")]
        [InlineData("it-IT", "IT")]
        public void TwoLetterISORegionName(string name, string expected)
        {
            Assert.Equal(expected, new RegionInfo(name).TwoLetterISORegionName);
        }
    }
}
