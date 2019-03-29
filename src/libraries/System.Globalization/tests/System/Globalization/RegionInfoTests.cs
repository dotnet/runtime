// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoPropertyTests
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

        [Fact]
        public void CurrentRegion()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                RegionInfo ri = new RegionInfo(new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName);
                Assert.True(RegionInfo.CurrentRegion.Equals(ri) || RegionInfo.CurrentRegion.Equals(new RegionInfo(CultureInfo.CurrentCulture.Name)));
                Assert.Same(RegionInfo.CurrentRegion, RegionInfo.CurrentRegion);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Theory]
        [InlineData("en-US", "United States")]
        public void DisplayName(string name, string expected)
        {
            RemoteExecutor.Invoke((string _name, string _expected) =>
            {
                CultureInfo.CurrentUICulture = new CultureInfo(_name);
                Assert.Equal(_expected, new RegionInfo(_name).DisplayName);

                return RemoteExecutor.SuccessExitCode;
            }, name, expected).Dispose();
        }

        [Theory]
        [InlineData("GB", "United Kingdom")]
        [InlineData("SE", "Sverige")]
        [InlineData("FR", "France")]
        public void NativeName(string name, string expected)
        {
            Assert.Equal(expected, new RegionInfo(name).NativeName);
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

        public static IEnumerable<object[]> RegionInfo_TestData()
        {
            yield return new object[] { 0x409, 244, "US Dollar", "US Dollar", "\u0055\u0053\u0020\u0044\u006f\u006c\u006c\u0061\u0072", "USA", "USA" };
            yield return new object[] { 0x411, 122, "Japanese Yen", "Japanese Yen", PlatformDetection.IsWindows ? "\u5186" : "\u65e5\u672c\u5186", "JPN", "JPN" };
            yield return new object[] { 0x804, 45, "Chinese Yuan", "PRC Yuan Renminbi", "\u4eba\u6c11\u5e01", "CHN", "CHN" };
            yield return new object[] { 0x401, 205, "Saudi Riyal", "Saudi Riyal", PlatformDetection.IsWindows ?
                                                    "\u0631\u064a\u0627\u0644\u00a0\u0633\u0639\u0648\u062f\u064a" :
                                                    "\u0631\u064a\u0627\u0644\u0020\u0633\u0639\u0648\u062f\u064a",
                                                    "SAU", "SAU" };
            yield return new object[] { 0x412, 134, "South Korean Won", "Korean Won", PlatformDetection.IsWindows ? "\uc6d0" : "\ub300\ud55c\ubbfc\uad6d\u0020\uc6d0", "KOR", "KOR" };
            yield return new object[] { 0x40d, 117, "Israeli New Shekel", "Israeli New Sheqel",
                                                    PlatformDetection.IsWindows || PlatformDetection.ICUVersion.Major >= 58 ? "\u05e9\u05e7\u05dc\u0020\u05d7\u05d3\u05e9" : "\u05e9\u05f4\u05d7", "ISR", "ISR" };
        }

        [Theory]
        [MemberData(nameof(RegionInfo_TestData))]
        public void MiscTest(int lcid, int geoId, string currencyEnglishName, string alternativeCurrencyEnglishName, string currencyNativeName, string threeLetterISORegionName, string threeLetterWindowsRegionName)
        {
            RegionInfo ri = new RegionInfo(lcid); // create it with lcid
            Assert.Equal(geoId, ri.GeoId);
            Assert.True(currencyEnglishName.Equals(ri.CurrencyEnglishName) ||
                        alternativeCurrencyEnglishName.Equals(ri.CurrencyEnglishName), "Wrong currency English Name");
            Assert.Equal(currencyNativeName, ri.CurrencyNativeName);
            Assert.Equal(threeLetterISORegionName, ri.ThreeLetterISORegionName);
            Assert.Equal(threeLetterWindowsRegionName, ri.ThreeLetterWindowsRegionName);
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
