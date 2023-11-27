// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class CultureInfoThreeLetterISOInfo
    {
        public static IEnumerable<object[]> RegionInfo_TestData()
        {
            yield return new object[] { 0x409, 244, "en-US", "USA", "eng" };
            yield return new object[] { 0x411, 122, "ja-JP", "JPN", "jpn" };
            yield return new object[] { 0x804, 45, "zh-CN", "CHN", "zho" };
            yield return new object[] { 0x401, 205, "ar-SA", "SAU", "ara" };
            yield return new object[] { 0x412, 134, "ko-KR", "KOR", "kor" };
            yield return new object[] { 0x40d, 117, "he-IL", "ISR", "heb" };
        }

        [Theory]
        [MemberData(nameof(RegionInfo_TestData))]
        public void MiscTest(int lcid, int geoId, string name, string threeLetterISORegionName, string threeLetterISOLanguageName)
        {
            RegionInfo ri = new RegionInfo(lcid); // create it with lcid
            Assert.Equal(geoId, ri.GeoId);
            Assert.Equal(threeLetterISORegionName, ri.ThreeLetterISORegionName);
            Assert.Equal(threeLetterISOLanguageName, new CultureInfo(name).ThreeLetterISOLanguageName);
        }
    }
}
