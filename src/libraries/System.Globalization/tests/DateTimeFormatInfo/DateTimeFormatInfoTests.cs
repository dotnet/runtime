// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMiscTests
    {
        public static IEnumerable<object[]> DateTimeFormatInfo_TestData()
        {
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("en-US").DateTimeFormat.Clone(), new GregorianCalendar(), "Gregorian Calendar" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("ar-SA").DateTimeFormat.Clone(), new HijriCalendar(), "\u0627\u0644\u062a\u0642\u0648\u064a\u0645\u00a0\u0627\u0644\u0647\u062c\u0631\u064a" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("ar-SA").DateTimeFormat.Clone(), new UmAlQuraCalendar(), "\u062a\u0642\u0648\u064a\u0645\u0020\u0627\u0645\u0020\u0627\u0644\u0642\u0631\u0649" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("ja-JP").DateTimeFormat.Clone(), new JapaneseCalendar(), "\u548c\u66a6" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("th-TH").DateTimeFormat.Clone(), new ThaiBuddhistCalendar(), "\u0e1e\u0e38\u0e17\u0e18\u0e28\u0e31\u0e01\u0e23\u0e32\u0e0a" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("he-IL").DateTimeFormat.Clone(), new HebrewCalendar(), "\u05dc\u05d5\u05d7\u00a0\u05e9\u05e0\u05d4\u00a0\u05e2\u05d1\u05e8\u05d9" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("ko-KR").DateTimeFormat.Clone(), new KoreanCalendar(), "\ub2e8\uae30" };
            yield return new object[] { (DateTimeFormatInfo)new CultureInfo("fa-IR").DateTimeFormat.Clone(), new PersianCalendar(), "\u062a\u0642\u0648\u06cc\u0645\u0020\u0647\u062c\u0631\u06cc\u0020\u0634\u0645\u0633\u06cc" };
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
        public void DateSeparatorTimeSeparator_Get_ReturnsExpected()
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
        public void NativeCalendarName_Get_ReturnsExpected(DateTimeFormatInfo dtfi, Calendar calendar, string nativeCalendarName)
        {
            try
            {
                dtfi.Calendar = calendar;

                if (PlatformDetection.IsNotUsingLimitedCultures)
                {
                    // Mobile / Browser ICU doesn't contain NativeCalendarName,
                    Assert.Equal(nativeCalendarName, dtfi.NativeCalendarName);
                }
            }
            catch
            {
                if (PlatformDetection.IsNlsGlobalization)
                {
                    // Persian calendar is recently supported as one of the optional calendars for fa-IR
                    Assert.True(calendar is PersianCalendar, "Exception can occur only with PersianCalendar");
                }
                else // !PlatformDetection.IsNlsGlobalization
                {
                    Assert.True(calendar is HijriCalendar || calendar is UmAlQuraCalendar || calendar is ThaiBuddhistCalendar ||
                                calendar is HebrewCalendar || calendar is KoreanCalendar, "failed to set the calendar on DTFI");
                }
            }
        }

        [Theory]
        [MemberData(nameof(CultureNames_TestData))]
        public void GetAllDateTimePatterns_Invoke_ReturnsExpected(string cultureName)
        {
            char[] formats = { 'd', 'D', 'f', 'F', 'g', 'G', 'm', 'o', 'r', 's', 't', 'T', 'u', 'U', 'y' };
            DateTimeFormatInfo dtfi = (DateTimeFormatInfo)new CultureInfo(cultureName).DateTimeFormat.Clone();

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
        public void GetShortestDayName_Invoke_ReturnsExpected(string cultureName)
        {
            DateTimeFormatInfo dtfi = new CultureInfo(cultureName).DateTimeFormat;
            string [] shortestDayNames = dtfi.ShortestDayNames;

            for (DayOfWeek day = DayOfWeek.Sunday; day <= DayOfWeek.Saturday; day++)
            {
                Assert.Equal(shortestDayNames[(int) day], dtfi.GetShortestDayName(day));
            }
        }

        [Fact]
        public void Months_GetHebrew_ReturnsExpected()
        {
            CultureInfo ci = new CultureInfo("he-IL");
            ci.DateTimeFormat.Calendar = new HebrewCalendar();

            Assert.Equal(13, ci.DateTimeFormat.MonthNames.Length);
            Assert.Equal(13, ci.DateTimeFormat.MonthGenitiveNames.Length);
            Assert.Equal(13, ci.DateTimeFormat.AbbreviatedMonthNames.Length);
            Assert.Equal(13, ci.DateTimeFormat.AbbreviatedMonthGenitiveNames.Length);

            DateTime dt = ci.DateTimeFormat.Calendar.ToDateTime(5779, 1, 1, 0, 0, 0, 0); // leap year
            for (int i = 0; i < 13; i++)
            {
                string formatted = dt.ToString(ci.DateTimeFormat.LongDatePattern, ci);
                Assert.Equal(dt, DateTime.ParseExact(formatted, ci.DateTimeFormat.LongDatePattern, ci));
                dt = ci.DateTimeFormat.Calendar.AddMonths(dt, 1);
            }

            dt = ci.DateTimeFormat.Calendar.ToDateTime(5778, 1, 1, 0, 0, 0, 0); // non leap year
            for (int i = 0; i < 12; i++)
            {
                string formatted = dt.ToString(ci.DateTimeFormat.LongDatePattern, ci);
                Assert.Equal(dt, DateTime.ParseExact(formatted, ci.DateTimeFormat.LongDatePattern, ci));
                dt = ci.DateTimeFormat.Calendar.AddMonths(dt, 1);
            }
        }

        [Fact]
        public void TestFirstYearOfJapaneseEra()
        {
            DateTimeFormatInfo jpnFormat = new CultureInfo("ja-JP").DateTimeFormat;
            jpnFormat.Calendar = new JapaneseCalendar();

            string pattern = "gg yyyy'\u5E74' MM'\u6708' dd'\u65E5'";
            DateTime dt = new DateTime(1989, 01, 08); // Start of Heisei Era

            string formattedDateWithGannen = "\u5E73\u6210 \u5143\u5E74 01\u6708 08\u65E5";

            string formattedDate = dt.ToString(pattern, jpnFormat);

            Assert.True(DateTime.TryParseExact(formattedDate, pattern, jpnFormat, DateTimeStyles.None, out DateTime parsedDate));
            Assert.Equal(dt, parsedDate);

            // If the formatting with Gan-nen is supported, then parsing should succeed. otherwise parsing should fail.
            Assert.True(formattedDate.IndexOf("\u5143", StringComparison.Ordinal) >= 0 ==
                        DateTime.TryParseExact(formattedDateWithGannen, pattern, jpnFormat, DateTimeStyles.None, out parsedDate),
                        $"Parsing '{formattedDateWithGannen}' result should match if '{formattedDate}' has Gan-nen symbol"
            );
        }

        [Fact]
        public void JapaneseAbbreviatedEnglishEraNamesTest()
        {
            string [] eraNames = { "M", "T", "S", "H", "R" };

            var ci = new CultureInfo("ja-JP") { DateTimeFormat = { Calendar = new JapaneseCalendar() }};

            int eraNumber = ci.DateTimeFormat.GetEra("Q");
            if (eraNumber == 4 || eraNumber == 5)
            {
                // Skip the test on Windows versions which have wrong Japanese Era information.
                // Windows at some point used "Q" as fake era name before getting the official name.
                return;
            }

            int numberOfErasToTest = Math.Min(eraNames.Length, ci.DateTimeFormat.Calendar.Eras.Length);
            for (int i = 0; i < numberOfErasToTest; i++)
            {
                Assert.Equal(i + 1, ci.DateTimeFormat.GetEra(eraNames[i]));
            }
        }

        [Fact]
        public void TestFrenchCanadaTimeFormat()
        {
            CultureInfo ci = CultureInfo.GetCultureInfo("fr-CA");
            Assert.Equal(":", ci.DateTimeFormat.TimeSeparator);

            DateTime time = new DateTime(2021, 10, 1, 5, 36, 50);
            string formattedTime = time.ToString("HH 'h' mm 'min' ss 's'", ci);
            DateTime dt = DateTime.Parse(formattedTime, ci);
            Assert.Equal(time.TimeOfDay, dt.TimeOfDay);
        }
    }
}
