// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoDateTimeFormat
    {
        public static IEnumerable<object[]> DateTimeFormatInfo_Set_TestData()
        {
            DateTimeFormatInfo customDateTimeFormatInfo1 = new DateTimeFormatInfo();
            customDateTimeFormatInfo1.AMDesignator = "a.m.";
            customDateTimeFormatInfo1.MonthDayPattern = "MMMM-dd";
            customDateTimeFormatInfo1.ShortTimePattern = "HH|mm";
            yield return new object[] { "en-US", customDateTimeFormatInfo1 };

            DateTimeFormatInfo customDateTimeFormatInfo2 = new DateTimeFormatInfo();
            customDateTimeFormatInfo2.LongTimePattern = "H:mm:ss";
            yield return new object[] { "fi-FI", customDateTimeFormatInfo2 };
        }

        [Theory]
        [MemberData(nameof(DateTimeFormatInfo_Set_TestData))]
        public void DateTimeFormatInfo_Set(string name, DateTimeFormatInfo newDateTimeFormatInfo)
        {
            CultureInfo culture = new CultureInfo(name);
            culture.DateTimeFormat = newDateTimeFormatInfo;
            Assert.Equal(newDateTimeFormatInfo, culture.DateTimeFormat);
        }

        [Fact]
        public void TestSettingThreadCultures()
        {
            var culture = new CultureInfo("ja-JP");
            using (new ThreadCultureChange(culture))
            {
                var dt = new DateTime(2014, 3, 14, 3, 14, 0);
                Assert.Equal(dt.ToString(), dt.ToString(culture));
                Assert.Equal(dt.ToString(), dt.ToString(culture.DateTimeFormat));
            }
        }

        [Fact]
        public void DateTimeFormatInfo_Set_Properties()
        {
            CultureInfo culture = new CultureInfo("fr");
            culture.DateTimeFormat.AMDesignator = "a.m.";
            Assert.Equal("a.m.", culture.DateTimeFormat.AMDesignator);

            culture.DateTimeFormat.MonthDayPattern = "MMMM-dd";
            Assert.Equal("MMMM-dd", culture.DateTimeFormat.MonthDayPattern);

            culture.DateTimeFormat.ShortTimePattern = "HH|mm";
            Assert.Equal("HH|mm", culture.DateTimeFormat.ShortTimePattern);
        }

        [Fact]
        public void DateTimeFormat_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CultureInfo("en-US").DateTimeFormat = null); // Value is null
            Assert.Throws<InvalidOperationException>(() => CultureInfo.InvariantCulture.DateTimeFormat = new DateTimeFormatInfo()); // DateTimeFormatInfo.InvariantInfo is read only
        }

        public static IEnumerable<object[]> DateTimeFormat_En_Locales_ShortDatePattern_TestData()
        {
            yield return new object[] { "en-AS", "M/d/yyyy" };
            yield return new object[] { "en-BI", "M/d/yyyy" };
            yield return new object[] { "en-GU", "M/d/yyyy" };
            yield return new object[] { "en-HK", "d/M/yyyy" };
            yield return new object[] { "en-MH", "M/d/yyyy" };
            yield return new object[] { "en-MP", "M/d/yyyy" };
            yield return new object[] { "en-NZ", "d/MM/yyyy" };
            yield return new object[] { "en-PR", "M/d/yyyy" };
            yield return new object[] { "en-SG", "d/M/yyyy" };
            yield return new object[] { "en-UM", "M/d/yyyy" };
            yield return new object[] { "en-US", "M/d/yyyy" };
            yield return new object[] { "en-VI", "M/d/yyyy" };
            yield return new object[] { "en-ZA", "yyyy/MM/dd" };
            yield return new object[] { "en-ZW", "d/M/yyyy" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(DateTimeFormat_En_Locales_ShortDatePattern_TestData))]
        public void DateTimeFormat_En_Locales_ShortDatePattern(string locale, string shortDatePattern)
        {
            var cultureInfo = new CultureInfo(locale);
            Assert.Equal(shortDatePattern, cultureInfo.DateTimeFormat.ShortDatePattern);
        }
    }
}
