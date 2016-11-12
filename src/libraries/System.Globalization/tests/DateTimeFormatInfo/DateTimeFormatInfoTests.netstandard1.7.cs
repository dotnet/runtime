// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMiscTests
    {
        public static IEnumerable<object[]> DateTimeFormatInfo_TestData()
        {
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("en-US").DateTimeFormat.Clone(), new GregorianCalendar(), "Gregorian Calendar" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("ar-SA").DateTimeFormat.Clone(), new HijriCalendar(), "\u0627\u0644\u062a\u0642\u0648\u064a\u0645\u00a0\u0627\u0644\u0647\u062c\u0631\u064a" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("ar-SA").DateTimeFormat.Clone(), new UmAlQuraCalendar(), "\u062a\u0642\u0648\u064a\u0645\u0020\u0627\u0645\u0020\u0627\u0644\u0642\u0631\u0649" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("ja-JP").DateTimeFormat.Clone(), new JapaneseCalendar(), "\u548c\u66a6" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("th-TH").DateTimeFormat.Clone(), new ThaiBuddhistCalendar(), "\u0e1e\u0e38\u0e17\u0e18\u0e28\u0e31\u0e01\u0e23\u0e32\u0e0a" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("he-IL").DateTimeFormat.Clone(), new HebrewCalendar(), "\u05dc\u05d5\u05d7\u00a0\u05e9\u05e0\u05d4\u00a0\u05e2\u05d1\u05e8\u05d9" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("ko-KR").DateTimeFormat.Clone(), new KoreanCalendar(), "\ub2e8\uae30" };
            yield return new object[] { (DateTimeFormatInfo) new CultureInfo("fa-IR").DateTimeFormat.Clone(), new PersianCalendar(), "\u062a\u0642\u0648\u06cc\u0645\u0020\u0647\u062c\u0631\u06cc\u0020\u0634\u0645\u0633\u06cc" };
        }

        public static IEnumerable<object[]> CultureNames_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { "en-US" };
            yield return new object[] { "ja-JP" };
            yield return new object[] { "zh-CN" };
            yield return new object[] { "ar-SA" };
            yield return new object[] { "ko-KR" };
            yield return new object[] { "he-IL" };
        }

        [Fact]
        public void SeparatorsTest()
        {
            DateTimeFormatInfo dtfi = (DateTimeFormatInfo) CultureInfo.InvariantCulture.DateTimeFormat.Clone();
            Assert.False(dtfi.IsReadOnly, "IsReadOnly expected to be false");

            string dateSep = dtfi.DateSeparator;
            string timeSep = dtfi.TimeSeparator;

            Assert.True(dtfi.ShortDatePattern.IndexOf(dateSep) > 0, "dtfi.ShortDatePattern should be greater than 0");
            Assert.True(dtfi.ShortTimePattern.IndexOf(timeSep) > 0, "dtfi.ShortTimePattern should be greater than 0");

            DateTime d = DateTime.Now;

            string formattedDate = d.ToString("MM/dd/yyyy", dtfi);
            Assert.True(formattedDate.IndexOf(dateSep) > 0, "Expected to find date separator in the formatted string");
            dtfi.DateSeparator = "-";
            string expectedFormattedString = formattedDate.Replace(dateSep, dtfi.DateSeparator);
            Assert.Equal(expectedFormattedString, d.ToString("MM/dd/yyyy", dtfi));

            string formattedTime = d.ToString("HH:mm:ss", dtfi);
            Assert.True(formattedTime.IndexOf(timeSep) > 0, "Expected to find time separator in the formatted string");
            dtfi.TimeSeparator = ".";
            expectedFormattedString = formattedTime.Replace(timeSep, dtfi.TimeSeparator);
            Assert.Equal(expectedFormattedString, d.ToString("HH:mm:ss", dtfi));
        }
        
        [Theory]
        [MemberData(nameof(DateTimeFormatInfo_TestData))]
        public void NativeCalendarNameTest(DateTimeFormatInfo dtfi, Calendar calendar, string nativeCalendarName)
        {
            try 
            {
                dtfi.Calendar = calendar;
                Assert.Equal(nativeCalendarName, dtfi.NativeCalendarName);
            }
            catch
            {
                if (PlatformDetection.IsWindows)
                {
                    // Persian calendar is recently supported as one of the optional calendars for fa-IR
                    Assert.True(calendar is PersianCalendar, "Exception can occur only with PersianCalendar");
                }
                else // !PlatformDetection.IsWindows
                {                 
                    Assert.True(calendar is HijriCalendar || calendar is UmAlQuraCalendar || calendar is ThaiBuddhistCalendar || 
                                calendar is HebrewCalendar || calendar is KoreanCalendar, "failed to set the calendar on DTFI");
                }
            }
        }

        [Theory]
        [MemberData(nameof(CultureNames_TestData))]
        public void AllDateTimePatternsTest(string cultureName)
        {
            char[] formats = { 'd', 'D', 'f', 'F', 'g', 'G', 'm', 'o', 'r', 's', 't', 'T', 'u', 'U', 'y' };
            DateTimeFormatInfo dtfi = (DateTimeFormatInfo) new CultureInfo(cultureName).DateTimeFormat.Clone();

            var allPatterns = dtfi.GetAllDateTimePatterns();
            Dictionary<string, string> dic = new Dictionary<string, string>();

            string value = "";
            foreach (char format in formats)
            {
                foreach (string pattern in dtfi.GetAllDateTimePatterns(format))
                {
                    if (!dic.TryGetValue(pattern, out value))
                    {
                        dic.Add(pattern, "");
                    }
                }
            }

            foreach (string pattern in allPatterns)
            {
                Assert.True(dic.TryGetValue(pattern, out value), "Couldn't find the pattern in the patterns list");
            }

            char[] setterFormats = { 'd', 'D', 't', 'T', 'y', 'Y' };
            foreach (char format in setterFormats)
            {
                var formatPatterns = dtfi.GetAllDateTimePatterns(format);
                string [] newPatterns = new string[1] { formatPatterns[formatPatterns.Length - 1] };
                dtfi.SetAllDateTimePatterns(newPatterns, format);
                Assert.Equal(newPatterns, dtfi.GetAllDateTimePatterns(format));
            }
        }

        [Theory]
        [MemberData(nameof(CultureNames_TestData))]
        public void ShortestDayNamesTest(string cultureName)
        {
            DateTimeFormatInfo dtfi = new CultureInfo(cultureName).DateTimeFormat;
            string [] shortestDayNames = dtfi.ShortestDayNames;

            for (DayOfWeek day=DayOfWeek.Sunday; day <= DayOfWeek.Saturday; day++)
            {
                Assert.Equal(shortestDayNames[(int) day], dtfi.GetShortestDayName(day));
            } 
        }
    }
}
