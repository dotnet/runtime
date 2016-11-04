// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoMiscTests
    {
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
                                                    PlatformDetection.IsWindows ? "\u05e9\u05e7\u05dc\u0020\u05d7\u05d3\u05e9" : "\u05e9\u05f4\u05d7", "ISR", "ISR" };
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

        [Fact]
        public void NegativeTest()
        {
            Assert.Throws<System.ArgumentException>("name", () => new RegionInfo(""));
        }
     }
}
