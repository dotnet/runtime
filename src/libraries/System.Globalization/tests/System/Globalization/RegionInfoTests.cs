// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Tests;
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void CurrentRegion_Unix()
        {
            using (new ThreadCultureChange("en-US"))
            {
                RegionInfo ri = new RegionInfo(new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName);
                Assert.True(RegionInfo.CurrentRegion.Equals(ri) || RegionInfo.CurrentRegion.Equals(new RegionInfo(CultureInfo.CurrentCulture.Name)));
                Assert.Same(RegionInfo.CurrentRegion, RegionInfo.CurrentRegion);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CurrentRegion_Windows()
        {
            RemoteExecutor.Invoke(() =>
            {
                RegionInfo ri = RegionInfo.CurrentRegion;
                CultureInfo.CurrentCulture.ClearCachedData(); // clear the current region cached data

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ja-JP");

                // Changing the current culture shouldn't affect the default current region as we get it from Windows settings.
                Assert.Equal(ri.TwoLetterISORegionName, RegionInfo.CurrentRegion.TwoLetterISORegionName);
            }).Dispose();
        }


        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        // We are testing with "no" as it match a neutral culture name. We want ensure this not conflict with region name.
        [InlineData("no")]
        [InlineData("No")]
        [InlineData("NO")]
        public void ValidateUsingCasedRegionName(string regionName)
        {
            RemoteExecutor.Invoke(name =>
            {
                // It is important to do this test in the following order because we have internal cache for regions.
                // creating the region with the original input name should be the first to do to ensure not cached before.
                string resultedName = new RegionInfo(name).Name;
                string expectedName = new RegionInfo(name.ToUpperInvariant()).Name;
                Assert.Equal(expectedName, resultedName);
            }, regionName).Dispose();
        }

        [Theory]
        [InlineData("en-US", "United States")]
        [OuterLoop("May fail on machines with multiple language packs installed")] // see https://github.com/dotnet/runtime/issues/30132
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45951", TestPlatforms.Browser)]
        public void DisplayName(string name, string expected)
        {
            using (new ThreadCultureChange(null, new CultureInfo(name)))
            {
                Assert.Equal(expected, new RegionInfo(name).DisplayName);
            }
        }

        public static IEnumerable<object[]> NativeName_TestData()
        {
            if (PlatformDetection.IsNotUsingLimitedCultures)
            {
                yield return new object[] { "GB", "United Kingdom" };
                yield return new object[] { "SE", "Sverige" };
                yield return new object[] { "FR", "France" };
            }
            else
            {
                // Browser's ICU doesn't contain RegionInfo.NativeName
                yield return new object[] { "GB", "GB" };
                yield return new object[] { "SE", "SE" };
                yield return new object[] { "FR", "FR" };
            }
        }

        [Theory]
        [MemberData(nameof(NativeName_TestData))]
        public void NativeName(string name, string expected)
        {
            Assert.Equal(expected, new RegionInfo(name).NativeName);
        }

        public static IEnumerable<object[]> EnglishName_TestData()
        {
            if (PlatformDetection.IsNotUsingLimitedCultures)
            {
                yield return new object[] { "en-US", new string[] { "United States" } };
                yield return new object[] { "US", new string[] { "United States" } };
                yield return new object[] { "zh-CN", new string[] { "China", "People's Republic of China", "China mainland" }};
                yield return new object[] { "CN", new string[] { "China", "People's Republic of China", "China mainland" } };
            }
            else
            {
                // Browser's ICU doesn't contain RegionInfo.EnglishName
                yield return new object[] { "en-US", new string[] { "US" } };
                yield return new object[] { "US", new string[] { "US" } };
                yield return new object[] { "zh-CN", new string[] { "CN" }};
                yield return new object[] { "CN", new string[] { "CN" } };
            }
        }

        [Theory]
        [MemberData(nameof(EnglishName_TestData))]
        public void EnglishName(string name, string[] expected)
        {
            string result = new RegionInfo(name).EnglishName;
            Assert.True(expected.Contains(result), $"RegionInfo.EnglishName({name})='{result}' not found in the possible names ({String.Join(", ", expected)}) ");
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

        [Fact]
        public void CurrencySymbol()
        {
            Assert.Equal("$", new RegionInfo("en-US").CurrencySymbol);
            Assert.Contains(new RegionInfo("zh-CN").CurrencySymbol, new string[] { "\u00A5", "\uffe5" });
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
            yield return new object[] { 0x409, 244, "US Dollar", "USD", "US Dollar", "\u0055\u0053\u0020\u0044\u006f\u006c\u006c\u0061\u0072", "USA", "USA" };
            yield return new object[] { 0x411, 122, "Japanese Yen", "JPY", "Japanese Yen", PlatformDetection.IsNlsGlobalization ? "\u5186" : "\u65e5\u672c\u5186", "JPN", "JPN" };
            yield return new object[] { 0x804, 45, "Chinese Yuan", "CNY", "PRC Yuan Renminbi", "\u4eba\u6c11\u5e01", "CHN", "CHN" };
            yield return new object[] { 0x401, 205, "Saudi Riyal", "SAR", "Saudi Riyal", PlatformDetection.IsNlsGlobalization ?
                                                    "\u0631\u064a\u0627\u0644\u00a0\u0633\u0639\u0648\u062f\u064a" :
                                                    "\u0631\u064a\u0627\u0644\u0020\u0633\u0639\u0648\u062f\u064a",
                                                    "SAU", "SAU" };
            yield return new object[] { 0x412, 134, "South Korean Won", "KRW", "Korean Won", PlatformDetection.IsNlsGlobalization ? "\uc6d0" : "\ub300\ud55c\ubbfc\uad6d\u0020\uc6d0", "KOR", "KOR" };
            yield return new object[] { 0x40d, 117, "Israeli New Shekel", "ILS", "Israeli New Sheqel",
                                                    PlatformDetection.IsNlsGlobalization || PlatformDetection.ICUVersion.Major >= 58 ? "\u05e9\u05e7\u05dc\u0020\u05d7\u05d3\u05e9" : "\u05e9\u05f4\u05d7", "ISR", "ISR" };
        }

        [Theory]
        [MemberData(nameof(RegionInfo_TestData))]
        public void MiscTest(int lcid, int geoId, string currencyEnglishName, string currencyShortName, string alternativeCurrencyEnglishName, string currencyNativeName, string threeLetterISORegionName, string threeLetterWindowsRegionName)
        {
            RegionInfo ri = new RegionInfo(lcid); // create it with lcid
            Assert.Equal(geoId, ri.GeoId);

            if (PlatformDetection.IsUsingLimitedCultures)
            {
                Assert.Equal(currencyShortName, ri.CurrencyEnglishName);
                Assert.Equal(currencyShortName, ri.CurrencyNativeName);
            }
            else
            {
                Assert.True(currencyEnglishName.Equals(ri.CurrencyEnglishName) ||
                            alternativeCurrencyEnglishName.Equals(ri.CurrencyEnglishName), "Wrong currency English Name");
                Assert.Equal(currencyNativeName, ri.CurrencyNativeName);
            }
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
        public void EqualsTest(RegionInfo regionInfo1, object obj, bool expected)
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
