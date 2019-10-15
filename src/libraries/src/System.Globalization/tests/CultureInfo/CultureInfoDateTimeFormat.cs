// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
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
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo culture = new CultureInfo("ja-JP");
                CultureInfo.CurrentCulture = culture;
                DateTime dt = new DateTime(2014, 3, 14, 3, 14, 0);
                Assert.Equal(dt.ToString(), dt.ToString(culture));
                Assert.Equal(dt.ToString(), dt.ToString(culture.DateTimeFormat));
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
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
    }
}
