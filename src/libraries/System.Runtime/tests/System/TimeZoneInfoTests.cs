// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public static partial class TimeZoneInfoTests
    {
        private static readonly bool s_isWindows = OperatingSystem.IsWindows();
        private static readonly bool s_isOSX = OperatingSystem.IsMacOS();

        private static string s_strPacific = s_isWindows ? "Pacific Standard Time" : "America/Los_Angeles";
        private static string s_strSydney = s_isWindows ? "AUS Eastern Standard Time" : "Australia/Sydney";
        private static string s_strGMT = s_isWindows ? "GMT Standard Time" : "Europe/London";
        private static string s_strTonga = s_isWindows ? "Tonga Standard Time" : "Pacific/Tongatapu";
        private static string s_strBrasil = s_isWindows ? "E. South America Standard Time" : "America/Sao_Paulo";
        private static string s_strPerth = s_isWindows ? "W. Australia Standard Time" : "Australia/Perth";
        private static string s_strBrasilia = s_isWindows ? "E. South America Standard Time" : "America/Sao_Paulo";
        private static string s_strNairobi = s_isWindows ? "E. Africa Standard Time" : "Africa/Nairobi";
        private static string s_strAmsterdam = s_isWindows ? "W. Europe Standard Time" : "Europe/Berlin";
        private static string s_strRussian = s_isWindows ? "Russian Standard Time" : "Europe/Moscow";
        private static string s_strLibya = s_isWindows ? "Libya Standard Time" : "Africa/Tripoli";
        private static string s_strJohannesburg = s_isWindows ? "South Africa Standard Time" : "Africa/Johannesburg";
        private static string s_strCasablanca = s_isWindows ? "Morocco Standard Time" : "Africa/Casablanca";
        private static string s_strCatamarca = s_isWindows ? "Argentina Standard Time" : "America/Argentina/Catamarca";
        private static string s_strLisbon = s_isWindows ? "GMT Standard Time" : "Europe/Lisbon";
        private static string s_strNewfoundland = s_isWindows ? "Newfoundland Standard Time" : "America/St_Johns";
        private static string s_strIran = s_isWindows ? "Iran Standard Time" : "Asia/Tehran";

        private static TimeZoneInfo s_myUtc = TimeZoneInfo.Utc;
        private static TimeZoneInfo s_myLocal = TimeZoneInfo.Local;
        private static TimeZoneInfo s_regLocal = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Local.Id); // in case DST is disabled on Local
        private static TimeZoneInfo s_GMTLondon = TimeZoneInfo.FindSystemTimeZoneById(s_strGMT);
        private static TimeZoneInfo s_nairobiTz = TimeZoneInfo.FindSystemTimeZoneById(s_strNairobi);
        private static TimeZoneInfo s_amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById(s_strAmsterdam);
        private static TimeZoneInfo s_johannesburgTz = TimeZoneInfo.FindSystemTimeZoneById(s_strJohannesburg);
        private static TimeZoneInfo s_casablancaTz = TimeZoneInfo.FindSystemTimeZoneById(s_strCasablanca);
        private static TimeZoneInfo s_catamarcaTz = TimeZoneInfo.FindSystemTimeZoneById(s_strCatamarca);
        private static TimeZoneInfo s_LisbonTz = TimeZoneInfo.FindSystemTimeZoneById(s_strLisbon);
        private static TimeZoneInfo s_NewfoundlandTz = TimeZoneInfo.FindSystemTimeZoneById(s_strNewfoundland);

        private static bool s_localIsPST = TimeZoneInfo.Local.Id == s_strPacific;
        private static bool s_regLocalSupportsDST = s_regLocal.SupportsDaylightSavingTime;
        private static bool s_localSupportsDST = TimeZoneInfo.Local.SupportsDaylightSavingTime;

        // In 2006, Australia delayed ending DST by a week.  However, Windows says it still ended the last week of March.
        private static readonly int s_sydneyOffsetLastWeekOfMarch2006 = s_isWindows ? 10 : 11;

        [Fact]
        public static void Kind()
        {
            TimeZoneInfo tzi = TimeZoneInfo.Local;
            Assert.Equal(tzi, TimeZoneInfo.Local);
            tzi = TimeZoneInfo.Utc;
            Assert.Equal(tzi, TimeZoneInfo.Utc);
        }

        [Fact]
        public static void Names()
        {
            TimeZoneInfo local = TimeZoneInfo.Local;
            TimeZoneInfo utc = TimeZoneInfo.Utc;

            Assert.NotNull(local.DaylightName);
            Assert.NotNull(local.DisplayName);
            Assert.NotNull(local.StandardName);
            Assert.NotNull(local.ToString());

            Assert.NotNull(utc.DaylightName);
            Assert.NotNull(utc.DisplayName);
            Assert.NotNull(utc.StandardName);
            Assert.NotNull(utc.ToString());
        }

        //  Due to ICU size limitations, full daylight/standard names are not included for the browser.
        //  Name abbreviations, if available, are used instead
        public static IEnumerable<object []> Platform_TimeZoneNamesTestData()
        {
            if (PlatformDetection.IsBrowser || PlatformDetection.IsiOS || PlatformDetection.IstvOS)
                return new TheoryData<TimeZoneInfo, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) America/Los_Angeles", "PST", "PDT" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Australia/Sydney", "AEST", "AEDT" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Australia/Perth", "AWST", "AWDT" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Asia/Tehran", "+0330", "+0430" },

                    { s_NewfoundlandTz, "(UTC-03:30) America/St_Johns", "NST", "NDT" },
                    { s_catamarcaTz, "(UTC-03:00) America/Argentina/Catamarca", "-03", "-02" }
                };
            else if (PlatformDetection.IsWindows)
                return new TheoryData<TimeZoneInfo, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) Pacific Time (US & Canada)", "Pacific Standard Time", "Pacific Daylight Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Canberra, Melbourne, Sydney", "AUS Eastern Standard Time", "AUS Eastern Daylight Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Perth", "W. Australia Standard Time", "W. Australia Daylight Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Tehran", "Iran Standard Time", "Iran Daylight Time" },

                    { s_NewfoundlandTz, "(UTC-03:30) Newfoundland", "Newfoundland Standard Time", "Newfoundland Daylight Time" },
                    { s_catamarcaTz, "(UTC-03:00) City of Buenos Aires", "Argentina Standard Time", "Argentina Daylight Time" }
                };
            else
                return new TheoryData<TimeZoneInfo, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) Pacific Time (Los Angeles)", "Pacific Standard Time", "Pacific Daylight Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Eastern Australia Time (Sydney)", "Australian Eastern Standard Time", "Australian Eastern Daylight Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Australian Western Standard Time (Perth)", "Australian Western Standard Time", "Australian Western Daylight Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Iran Time", "Iran Standard Time", "Iran Daylight Time" },

                    { s_NewfoundlandTz, "(UTC-03:30) Newfoundland Time (St. John’s)", "Newfoundland Standard Time", "Newfoundland Daylight Time" },
                    { s_catamarcaTz, "(UTC-03:00) Argentina Standard Time (Catamarca)", "Argentina Standard Time", "Argentina Summer Time" }
                };
        }

        // We test the existence of a specific English time zone name to avoid failures on non-English platforms.
        [ConditionalTheory(nameof(IsEnglishUILanguage))]
        [MemberData(nameof(Platform_TimeZoneNamesTestData))]
        public static void Platform_TimeZoneNames(TimeZoneInfo tzi, string displayName, string standardName, string daylightName)
        {
            // Edge case - Optionally allow some characters to be absent in the display name.
            const string chars = ".’";
            foreach (char c in chars)
            {
                if (displayName.Contains(c, StringComparison.Ordinal) && !tzi.DisplayName.Contains(c, StringComparison.Ordinal))
                {
                    displayName = displayName.Replace(c.ToString(), "", StringComparison.Ordinal);
                }
            }

            Assert.Equal($"DisplayName: \"{displayName}\", StandardName: {standardName}\", DaylightName: {daylightName}\"",
                         $"DisplayName: \"{tzi.DisplayName}\", StandardName: {tzi.StandardName}\", DaylightName: {tzi.DaylightName}\"");
        }

        [Fact]
        public static void ConvertTime()
        {
            TimeZoneInfo local = TimeZoneInfo.Local;
            TimeZoneInfo utc = TimeZoneInfo.Utc;

            DateTime dt = TimeZoneInfo.ConvertTime(DateTime.Today, utc);
            Assert.Equal(DateTime.Today, TimeZoneInfo.ConvertTime(dt, local));

            DateTime today = new DateTime(DateTime.Today.Ticks, DateTimeKind.Utc);
            dt = TimeZoneInfo.ConvertTime(today, local);
            Assert.Equal(today, TimeZoneInfo.ConvertTime(dt, utc));
        }

        [Fact]
        public static void LibyaTimeZone()
        {
            TimeZoneInfo tripoli;
            // Make sure first the timezone data is updated in the machine as it should include Libya Timezone
            try
            {
                tripoli = TimeZoneInfo.FindSystemTimeZoneById(s_strLibya);
            }
            catch (Exception /* TimeZoneNotFoundException in netstandard1.7 test*/ )
            {
                // Libya time zone not found
                Console.WriteLine("Warning: Libya time zone is not exist in this machine");
                return;
            }

            var startOf2012 = new DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOf2011 = startOf2012.AddTicks(-1);

            DateTime libyaLocalTime = TimeZoneInfo.ConvertTime(endOf2011, tripoli);
            DateTime expectResult = new DateTime(2012, 1, 1, 2, 0, 0).AddTicks(-1);
            Assert.True(libyaLocalTime.Equals(expectResult), string.Format("Expected {0} and got {1}", expectResult, libyaLocalTime));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void TestYukonTZ()
        {
            try
            {
                TimeZoneInfo yukon = TimeZoneInfo.FindSystemTimeZoneById("Yukon Standard Time");

                // First, ensure we have the updated data
                TimeZoneInfo.AdjustmentRule [] rules = yukon.GetAdjustmentRules();
                if (rules.Length <= 0 || rules[rules.Length - 1].DateStart.Year != 2021 || rules[rules.Length - 1].DateEnd.Year != 9999)
                {
                    return;
                }

                TimeSpan minus7HoursSpan = new TimeSpan(-7, 0, 0);

                DateTimeOffset midnight = new DateTimeOffset(2021, 1, 1, 0, 0, 0, 0, minus7HoursSpan);
                DateTimeOffset beforeMidnight = new DateTimeOffset(2020, 12, 31, 23, 59, 59, 999, minus7HoursSpan);
                DateTimeOffset before1AM = new DateTimeOffset(2021, 1, 1, 0, 59, 59, 999, minus7HoursSpan);
                DateTimeOffset at1AM = new DateTimeOffset(2021, 1, 1, 1, 0, 0, 0, minus7HoursSpan);
                DateTimeOffset midnight2022 = new DateTimeOffset(2022, 1, 1, 0, 0, 0, 0, minus7HoursSpan);

                Assert.Equal(minus7HoursSpan, yukon.GetUtcOffset(midnight));
                Assert.Equal(minus7HoursSpan, yukon.GetUtcOffset(beforeMidnight));
                Assert.Equal(minus7HoursSpan, yukon.GetUtcOffset(before1AM));
                Assert.Equal(minus7HoursSpan, yukon.GetUtcOffset(at1AM));
                Assert.Equal(minus7HoursSpan, yukon.GetUtcOffset(midnight2022));
            }
            catch (TimeZoneNotFoundException)
            {
                // Some Windows versions don't carry the complete TZ data. Ignore the tests on such versions.
            }
       }

        [Fact]
        public static void RussianTimeZone()
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(s_strRussian);
            var inputUtcDate = new DateTime(2013, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            DateTime russiaTime = TimeZoneInfo.ConvertTime(inputUtcDate, tz);
            DateTime expectResult = new DateTime(2013, 6, 1, 4, 0, 0);
            Assert.True(russiaTime.Equals(expectResult), string.Format("Expected {0} and got {1}", expectResult, russiaTime));

            DateTime dt = new DateTime(2011, 12, 31, 23, 30, 0);
            TimeSpan o = tz.GetUtcOffset(dt);
            Assert.True(o.Equals(TimeSpan.FromHours(4)), string.Format("Expected {0} and got {1}", TimeSpan.FromHours(4), o));
        }

        [Fact]
        public static void CaseInsensitiveLookup()
        {
            Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(s_strBrasilia), TimeZoneInfo.FindSystemTimeZoneById(s_strBrasilia.ToLowerInvariant()));
            Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(s_strJohannesburg), TimeZoneInfo.FindSystemTimeZoneById(s_strJohannesburg.ToUpperInvariant()));

            // Populate internal cache with all timezones. The implementation takes different path for lookup by id
            // when all timezones are populated.
            TimeZoneInfo.GetSystemTimeZones();

            // The timezones used for the tests after GetSystemTimeZones calls have to be different from the ones used before GetSystemTimeZones to
            // exercise the rare path.
            Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), TimeZoneInfo.FindSystemTimeZoneById(s_strSydney.ToLowerInvariant()));
            Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), TimeZoneInfo.FindSystemTimeZoneById(s_strPerth.ToUpperInvariant()));
        }

        [Fact]
        public static void ConvertTime_DateTimeOffset_Invalid()
        {
            DateTimeOffset time1 = new DateTimeOffset(2006, 5, 12, 0, 0, 0, TimeSpan.Zero);

            VerifyConvertException<ArgumentNullException>(time1, null);

            // We catch TimeZoneNotFoundException in then netstandard1.7 tests

            VerifyConvertException<Exception>(time1, string.Empty);
            VerifyConvertException<Exception>(time1, "    ");
            VerifyConvertException<Exception>(time1, "\0");
            VerifyConvertException<Exception>(time1, "Pacific"); // whole string must match
            VerifyConvertException<Exception>(time1, "Pacific Standard Time Zone"); // no extra characters
            VerifyConvertException<Exception>(time1, " Pacific Standard Time"); // no leading space
            VerifyConvertException<Exception>(time1, "Pacific Standard Time "); // no trailing space
            VerifyConvertException<Exception>(time1, "\0Pacific Standard Time"); // no leading null
            VerifyConvertException<Exception>(time1, "Pacific Standard Time\0"); // no trailing null
            VerifyConvertException<Exception>(time1, "Pacific Standard Time\\  "); // no trailing null
            VerifyConvertException<Exception>(time1, "Pacific Standard Time\\Display");
            VerifyConvertException<Exception>(time1, "Pacific Standard Time\n"); // no trailing newline
            VerifyConvertException<Exception>(time1, new string('a', 256)); // long string
        }

        [Fact]
        public static void ConvertTime_DateTimeOffset_NearMinMaxValue()
        {
            VerifyConvert(DateTimeOffset.MaxValue, TimeZoneInfo.Utc.Id, DateTimeOffset.MaxValue);
            VerifyConvert(DateTimeOffset.MaxValue, s_strPacific, new DateTimeOffset(DateTime.MaxValue.AddHours(-8), new TimeSpan(-8, 0, 0)));
            VerifyConvert(DateTimeOffset.MaxValue, s_strSydney, DateTimeOffset.MaxValue);
            VerifyConvert(new DateTimeOffset(DateTime.MaxValue, new TimeSpan(5, 0, 0)), s_strSydney, DateTimeOffset.MaxValue);
            VerifyConvert(new DateTimeOffset(DateTime.MaxValue, new TimeSpan(11, 0, 0)), s_strSydney, new DateTimeOffset(DateTime.MaxValue, new TimeSpan(11, 0, 0)));
            VerifyConvert(DateTimeOffset.MaxValue.AddHours(-11), s_strSydney, new DateTimeOffset(DateTime.MaxValue, new TimeSpan(11, 0, 0)));
            VerifyConvert(DateTimeOffset.MaxValue.AddHours(-11.5), s_strSydney, new DateTimeOffset(DateTime.MaxValue.AddHours(-0.5), new TimeSpan(11, 0, 0)));
            VerifyConvert(new DateTimeOffset(DateTime.MaxValue.AddHours(-5), new TimeSpan(3, 0, 0)), s_strSydney, DateTimeOffset.MaxValue);

            VerifyConvert(DateTimeOffset.MinValue, TimeZoneInfo.Utc.Id, DateTimeOffset.MinValue);
            VerifyConvert(DateTimeOffset.MinValue, s_strSydney, new DateTimeOffset(DateTime.MinValue.AddHours(10), new TimeSpan(10, 0, 0)));
            VerifyConvert(DateTimeOffset.MinValue, s_strPacific, DateTimeOffset.MinValue);
            VerifyConvert(new DateTimeOffset(DateTime.MinValue, new TimeSpan(-3, 0, 0)), s_strPacific, DateTimeOffset.MinValue);
            VerifyConvert(new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)), s_strPacific, new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)));
            VerifyConvert(DateTimeOffset.MinValue.AddHours(8), s_strPacific, new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)));
            VerifyConvert(DateTimeOffset.MinValue.AddHours(8.5), s_strPacific, new DateTimeOffset(DateTime.MinValue.AddHours(0.5), new TimeSpan(-8, 0, 0)));
            VerifyConvert(new DateTimeOffset(DateTime.MinValue.AddHours(5), new TimeSpan(-3, 0, 0)), s_strPacific, new DateTimeOffset(DateTime.MinValue, new TimeSpan(-8, 0, 0)));

            VerifyConvert(DateTime.MaxValue, s_strPacific, s_strSydney, DateTime.MaxValue);
            VerifyConvert(DateTime.MaxValue.AddHours(-19), s_strPacific, s_strSydney, DateTime.MaxValue);
            VerifyConvert(DateTime.MaxValue.AddHours(-19.5), s_strPacific, s_strSydney, DateTime.MaxValue.AddHours(-0.5));

            VerifyConvert(DateTime.MinValue, s_strSydney, s_strPacific, DateTime.MinValue);

            TimeSpan earlyTimesDifference = GetEarlyTimesOffset(s_strSydney) - GetEarlyTimesOffset(s_strPacific);
            VerifyConvert(DateTime.MinValue + earlyTimesDifference, s_strSydney, s_strPacific, DateTime.MinValue);
            VerifyConvert(DateTime.MinValue.AddHours(0.5) + earlyTimesDifference, s_strSydney, s_strPacific, DateTime.MinValue.AddHours(0.5));
        }

        [Fact]
        public static void ConvertTime_DateTimeOffset_VariousSystemTimeZones()
        {
            var time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-7, 0, 0));
            var time2 = new DateTimeOffset(2006, 5, 12, 22, 17, 42, new TimeSpan(10, 0, 0));
            VerifyConvert(time1, s_strSydney, time2);
            VerifyConvert(time2, s_strPacific, time1);

            time1 = new DateTimeOffset(2006, 3, 14, 9, 47, 12, new TimeSpan(-8, 0, 0));
            time2 = new DateTimeOffset(2006, 3, 15, 4, 47, 12, new TimeSpan(11, 0, 0));
            VerifyConvert(time1, s_strSydney, time2);
            VerifyConvert(time2, s_strPacific, time1);

            time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-8, 0, 0));
            time2 = new DateTimeOffset(2006, 11, 5, 20, 3, 0, new TimeSpan(11, 0, 0));
            VerifyConvert(time1, s_strSydney, time2);
            VerifyConvert(time2, s_strPacific, time1);

            time1 = new DateTimeOffset(1987, 1, 1, 2, 3, 0, new TimeSpan(-8, 0, 0));
            time2 = new DateTimeOffset(1987, 1, 1, 21, 3, 0, new TimeSpan(11, 0, 0));
            VerifyConvert(time1, s_strSydney, time2);
            VerifyConvert(time2, s_strPacific, time1);

            time1 = new DateTimeOffset(2001, 5, 12, 5, 17, 42, new TimeSpan(1, 0, 0));
            time2 = new DateTimeOffset(2001, 5, 12, 17, 17, 42, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);

            time1 = new DateTimeOffset(2003, 3, 30, 5, 19, 20, new TimeSpan(1, 0, 0));
            time2 = new DateTimeOffset(2003, 3, 30, 17, 19, 20, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);

            time1 = new DateTimeOffset(2003, 3, 30, 1, 20, 1, new TimeSpan(0, 0, 0));
            var time1a = new DateTimeOffset(2003, 3, 30, 2, 20, 1, new TimeSpan(1, 0, 0));
            time2 = new DateTimeOffset(2003, 3, 30, 14, 20, 1, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time1a, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1a);
            VerifyConvert(time1, s_strGMT, time1a);  // invalid hour
            VerifyConvert(time1a, s_strGMT, time1a);
            VerifyConvert(time2, s_strTonga, time2);

            time1 = new DateTimeOffset(2003, 3, 30, 0, 0, 23, new TimeSpan(0, 0, 0));
            time2 = new DateTimeOffset(2003, 3, 30, 13, 0, 23, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);

            time1 = new DateTimeOffset(2003, 10, 26, 1, 30, 0, new TimeSpan(0)); // ambiguous (STD)
            time1a = new DateTimeOffset(2003, 10, 26, 1, 30, 0, new TimeSpan(1, 0, 0)); // ambiguous (DLT)
            time2 = new DateTimeOffset(2003, 10, 26, 14, 30, 0, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);
            VerifyConvert(time1a, s_strTonga, time2.AddHours(-1));
            VerifyConvert(time1a, s_strGMT, time1a);

            time1 = new DateTimeOffset(2003, 10, 25, 14, 0, 0, new TimeSpan(1, 0, 0));
            time2 = new DateTimeOffset(2003, 10, 26, 2, 0, 0, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);

            time1 = new DateTimeOffset(2003, 10, 26, 2, 20, 0, new TimeSpan(0));
            time2 = new DateTimeOffset(2003, 10, 26, 15, 20, 0, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);

            time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(0));
            time2 = new DateTimeOffset(2003, 10, 26, 16, 0, 1, new TimeSpan(13, 0, 0));
            VerifyConvert(time1, s_strTonga, time2);
            VerifyConvert(time2, s_strGMT, time1);
            VerifyConvert(time1, s_strGMT, time1);
            VerifyConvert(time2, s_strTonga, time2);

            var time3 = new DateTime(2001, 5, 12, 5, 17, 42);
            VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(12));
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
            time3 = new DateTime(2003, 3, 30, 5, 19, 20);
            VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(12));
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-13));
            time3 = new DateTime(2003, 3, 30, 1, 20, 1);
            VerifyConvertException<ArgumentException>(time3, s_strGMT, s_strTonga); // invalid time
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-13));
            time3 = new DateTime(2003, 3, 30, 0, 0, 23);
            VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13));
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-13));
            time3 = new DateTime(2003, 10, 26, 2, 0, 0); // ambiguous
            VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13));
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
            time3 = new DateTime(2003, 10, 26, 2, 20, 0);
            VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13)); // ambiguous
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));
            time3 = new DateTime(2003, 10, 26, 3, 0, 1);
            VerifyConvert(time3, s_strGMT, s_strTonga, time3.AddHours(13));
            VerifyConvert(time3, s_strTonga, s_strGMT, time3.AddHours(-12));

            // Iran has Utc offset 4:30 during the DST and 3:30 during standard time.
            time3 = new DateTime(2018, 4, 20, 7, 0, 0, DateTimeKind.Utc);
            VerifyConvert(time3, s_strIran, time3.AddHours(4.5), DateTimeKind.Unspecified); // DST time

            time3 = new DateTime(2018, 1, 20, 7, 0, 0, DateTimeKind.Utc);
            VerifyConvert(time3, s_strIran, time3.AddHours(3.5), DateTimeKind.Unspecified); // DST time
        }

        [Fact]
        public static void ConvertTime_SameTimeZones()
        {
            var time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-2, 0, 0));
            VerifyConvert(time1, s_strBrasil, time1);
            time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-3, 0, 0));
            VerifyConvert(time1, s_strBrasil, time1);
            time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-3, 0, 0));
            VerifyConvert(time1, s_strBrasil, time1);
            time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-2, 0, 0));
            VerifyConvert(time1, s_strBrasil, time1);
            time1 = new DateTimeOffset(2006, 10, 15, 2, 30, 0, new TimeSpan(-2, 0, 0));  // invalid
            VerifyConvert(time1, s_strBrasil, time1.ToOffset(new TimeSpan(-3, 0, 0)));
            time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-3, 0, 0));  // ambiguous
            VerifyConvert(time1, s_strBrasil, time1);
            time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-2, 0, 0));  // ambiguous
            VerifyConvert(time1, s_strBrasil, time1);

            time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-8, 0, 0));
            VerifyConvert(time1, s_strPacific, time1);
            time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-7, 0, 0));
            VerifyConvert(time1, s_strPacific, time1);
            time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-8, 0, 0));
            VerifyConvert(time1, s_strPacific, time1);
            time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-8, 0, 0));
            VerifyConvert(time1, s_strPacific, time1);

            time1 = new DateTimeOffset(1964, 6, 19, 12, 45, 10, new TimeSpan(0));
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1);
            time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-8, 0, 0));
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.ToUniversalTime());
            time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-3, 0, 0));
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.ToUniversalTime());
        }

        [Fact]
        public static void ConvertTime_DateTime_NearMinAndMaxValue()
        {
            DateTime time1 = new DateTime(2006, 5, 12);

            DateTime utcMaxValue = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            VerifyConvert(utcMaxValue, s_strSydney, DateTime.MaxValue);
            VerifyConvert(utcMaxValue.AddHours(-11), s_strSydney, DateTime.MaxValue);
            VerifyConvert(utcMaxValue.AddHours(-11.5), s_strSydney, DateTime.MaxValue.AddHours(-0.5));
            VerifyConvert(utcMaxValue, s_strPacific, DateTime.MaxValue.AddHours(-8));
            DateTime utcMinValue = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            VerifyConvert(utcMinValue, s_strPacific, DateTime.MinValue);

            TimeSpan earlyTimesOffsetPacific = GetEarlyTimesOffset(s_strPacific);
            earlyTimesOffsetPacific = earlyTimesOffsetPacific.Negate(); // Pacific is behind UTC, so negate for a positive value
            VerifyConvert(utcMinValue + earlyTimesOffsetPacific, s_strPacific, DateTime.MinValue);
            VerifyConvert(utcMinValue.AddHours(0.5) + earlyTimesOffsetPacific, s_strPacific, DateTime.MinValue.AddHours(0.5));

            TimeSpan earlyTimesOffsetSydney = GetEarlyTimesOffset(s_strSydney);
            VerifyConvert(utcMinValue, s_strSydney, DateTime.MinValue + earlyTimesOffsetSydney);
        }

        [Fact]
        public static void ConverTime_DateTime_VariousSystemTimeZonesTest()
        {
            var time1utc = new DateTime(2006, 5, 12, 5, 17, 42, DateTimeKind.Utc);
            var time1 = new DateTime(2006, 5, 12, 5, 17, 42);
            VerifyConvert(time1utc, s_strPacific, time1.AddHours(-7));
            VerifyConvert(time1utc, s_strSydney, time1.AddHours(10));
            VerifyConvert(time1utc, s_strGMT, time1.AddHours(1));
            VerifyConvert(time1utc, s_strTonga, time1.AddHours(13));
            time1utc = new DateTime(2006, 3, 28, 9, 47, 12, DateTimeKind.Utc);
            time1 = new DateTime(2006, 3, 28, 9, 47, 12);
            VerifyConvert(time1utc, s_strPacific, time1.AddHours(-8));
            VerifyConvert(time1utc, s_strSydney, time1.AddHours(s_sydneyOffsetLastWeekOfMarch2006));
            time1utc = new DateTime(2006, 11, 5, 1, 3, 0, DateTimeKind.Utc);
            time1 = new DateTime(2006, 11, 5, 1, 3, 0);
            VerifyConvert(time1utc, s_strPacific, time1.AddHours(-8));
            VerifyConvert(time1utc, s_strSydney, time1.AddHours(11));
            time1utc = new DateTime(1987, 1, 1, 2, 3, 0, DateTimeKind.Utc);
            time1 = new DateTime(1987, 1, 1, 2, 3, 0);
            VerifyConvert(time1utc, s_strPacific, time1.AddHours(-8));
            VerifyConvert(time1utc, s_strSydney, time1.AddHours(11));

            time1utc = new DateTime(2003, 3, 30, 0, 0, 23, DateTimeKind.Utc);
            time1 = new DateTime(2003, 3, 30, 0, 0, 23);
            VerifyConvert(time1utc, s_strGMT, time1);
            time1utc = new DateTime(2003, 3, 30, 2, 0, 24, DateTimeKind.Utc);
            time1 = new DateTime(2003, 3, 30, 2, 0, 24);
            VerifyConvert(time1utc, s_strGMT, time1.AddHours(1));
            time1utc = new DateTime(2003, 3, 30, 5, 19, 20, DateTimeKind.Utc);
            time1 = new DateTime(2003, 3, 30, 5, 19, 20);
            VerifyConvert(time1utc, s_strGMT, time1.AddHours(1));
            time1utc = new DateTime(2003, 10, 26, 2, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2003, 10, 26, 2, 0, 0); // ambiguous
            VerifyConvert(time1utc, s_strGMT, time1);
            time1utc = new DateTime(2003, 10, 26, 2, 20, 0, DateTimeKind.Utc);
            time1 = new DateTime(2003, 10, 26, 2, 20, 0);
            VerifyConvert(time1utc, s_strGMT, time1); // ambiguous
            time1utc = new DateTime(2003, 10, 26, 3, 0, 1, DateTimeKind.Utc);
            time1 = new DateTime(2003, 10, 26, 3, 0, 1);
            VerifyConvert(time1utc, s_strGMT, time1);

            time1utc = new DateTime(2005, 3, 30, 0, 0, 23, DateTimeKind.Utc);
            time1 = new DateTime(2005, 3, 30, 0, 0, 23);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

            time1 = new DateTime(2006, 5, 12, 5, 17, 42);
            VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(17));
            VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-17));
            time1 = new DateTime(2006, 3, 28, 9, 47, 12);
            VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(s_sydneyOffsetLastWeekOfMarch2006 + 8));
            VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-(s_sydneyOffsetLastWeekOfMarch2006 + 8)));
            time1 = new DateTime(2006, 11, 5, 1, 3, 0);
            VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(19));
            VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-19));
            time1 = new DateTime(1987, 1, 1, 2, 3, 0);
            VerifyConvert(time1, s_strPacific, s_strSydney, time1.AddHours(19));
            VerifyConvert(time1, s_strSydney, s_strPacific, time1.AddHours(-19));
        }

        [Fact]
        public static void ConvertTime_DateTime_PerthRules()
        {
            var time1utc = new DateTime(2005, 12, 31, 15, 59, 59, DateTimeKind.Utc);
            var time1 = new DateTime(2005, 12, 31, 15, 59, 59);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2005, 12, 31, 16, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2005, 12, 31, 16, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2005, 12, 31, 16, 30, 0, DateTimeKind.Utc);
            time1 = new DateTime(2005, 12, 31, 16, 30, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2005, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            time1 = new DateTime(2005, 12, 31, 23, 59, 59);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2006, 1, 1, 0, 1, 1, DateTimeKind.Utc);
            time1 = new DateTime(2006, 1, 1, 0, 1, 1);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

            // 2006 rule in effect
            time1utc = new DateTime(2006, 5, 12, 2, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2006, 5, 12, 2, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));

            // begin dst
            time1utc = new DateTime(2006, 11, 30, 17, 59, 59, DateTimeKind.Utc);
            time1 = new DateTime(2006, 11, 30, 17, 59, 59);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2006, 11, 30, 18, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2006, 12, 1, 2, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1);

            time1utc = new DateTime(2006, 12, 31, 15, 59, 59, DateTimeKind.Utc);
            time1 = new DateTime(2006, 12, 31, 15, 59, 59);
            if (s_isWindows)
            {
                // ambiguous time between rules
                // this is not ideal, but the way it works
                VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            }
            else
            {
                // Linux has the correct rules for Perth for days from December 3, 2006 to the end of the year
                VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
            }

            // 2007 rule
            time1utc = new DateTime(2006, 12, 31, 20, 1, 2, DateTimeKind.Utc);
            time1 = new DateTime(2006, 12, 31, 20, 1, 2);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
            // end dst
            time1utc = new DateTime(2007, 3, 24, 16, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2007, 3, 24, 16, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
            time1utc = new DateTime(2007, 3, 24, 17, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2007, 3, 24, 17, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
            time1utc = new DateTime(2007, 3, 24, 18, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2007, 3, 24, 18, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2007, 3, 24, 19, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2007, 3, 24, 19, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            // begin dst
            time1utc = new DateTime(2008, 10, 25, 17, 59, 59, DateTimeKind.Utc);
            time1 = new DateTime(2008, 10, 25, 17, 59, 59);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(8));
            time1utc = new DateTime(2008, 10, 25, 18, 0, 0, DateTimeKind.Utc);
            time1 = new DateTime(2008, 10, 25, 18, 0, 0);
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));
        }

        [Fact]
        public static void ConvertTime_DateTime_UtcToUtc()
        {
            var time1utc = new DateTime(2003, 3, 30, 0, 0, 23, DateTimeKind.Utc);
            VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
            time1utc = new DateTime(2003, 3, 30, 2, 0, 24, DateTimeKind.Utc);
            VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
            time1utc = new DateTime(2003, 3, 30, 5, 19, 20, DateTimeKind.Utc);
            VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
            time1utc = new DateTime(2003, 10, 26, 2, 0, 0, DateTimeKind.Utc);
            VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
            time1utc = new DateTime(2003, 10, 26, 2, 20, 0, DateTimeKind.Utc);
            VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
            time1utc = new DateTime(2003, 10, 26, 3, 0, 1, DateTimeKind.Utc);
            VerifyConvert(time1utc, TimeZoneInfo.Utc.Id, time1utc);
        }

        [Fact]
        public static void ConvertTime_DateTime_UtcToLocal()
        {
            if (s_localIsPST)
            {
                var time1 = new DateTime(2006, 4, 2, 1, 30, 0);
                var time1utc = new DateTime(2006, 4, 2, 1, 30, 0, DateTimeKind.Utc);
                VerifyConvert(time1utc.Subtract(s_regLocal.GetUtcOffset(time1utc)), TimeZoneInfo.Local.Id, time1);

                // Converts to "Pacific Standard Time" not actual Local, so historical rules are always respected
                int delta = s_regLocalSupportsDST ? 1 : 0;
                time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2006, 4, 2, 3, 30, 0); // DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2006, 10, 29, 0, 30, 0);  // DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2006, 10, 29, 1, 30, 0);  // ambiguous hour (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2006, 10, 29, 2, 30, 0);  // no DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);

                // 2007 rule
                time1 = new DateTime(2007, 3, 11, 1, 30, 0); // no DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2007, 3, 11, 3, 30, 0); // DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2007, 11, 4, 0, 30, 0);  // DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2007, 11, 4, 1, 30, 0);  // ambiguous hour (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2007, 11, 4, 2, 30, 0);  // no DST (U.S. Pacific)
                VerifyConvert(DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc), TimeZoneInfo.Local.Id, time1);
            }
        }

        [Fact]
        public static void ConvertTime_DateTime_LocalToSystem()
        {
            var time1 = new DateTime(2006, 5, 12, 5, 17, 42);
            var time1local = new DateTime(2006, 5, 12, 5, 17, 42, DateTimeKind.Local);
            var localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(10));
            VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(10));
            VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-7));
            VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-7));

            time1 = new DateTime(2006, 3, 28, 9, 47, 12);
            time1local = new DateTime(2006, 3, 28, 9, 47, 12, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(s_sydneyOffsetLastWeekOfMarch2006));
            VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(s_sydneyOffsetLastWeekOfMarch2006));

            VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
            VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-8));

            time1 = new DateTime(2006, 11, 5, 1, 3, 0);
            time1local = new DateTime(2006, 11, 5, 1, 3, 0, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(11));
            VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(11));
            VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
            VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
            time1 = new DateTime(1987, 1, 1, 2, 3, 0);
            time1local = new DateTime(1987, 1, 1, 2, 3, 0, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strSydney, time1.Subtract(localOffset).AddHours(11));
            VerifyConvert(time1local, s_strSydney, time1.Subtract(localOffset).AddHours(11));
            VerifyConvert(time1, s_strPacific, time1.Subtract(localOffset).AddHours(-8));
            VerifyConvert(time1local, s_strPacific, time1.Subtract(localOffset).AddHours(-8));

            time1 = new DateTime(2001, 5, 12, 5, 17, 42);
            time1local = new DateTime(2001, 5, 12, 5, 17, 42, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            var gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            time1 = new DateTime(2003, 1, 30, 5, 19, 20);
            time1local = new DateTime(2003, 1, 30, 5, 19, 20, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            time1 = new DateTime(2003, 1, 30, 3, 20, 1);
            time1local = new DateTime(2003, 1, 30, 3, 20, 1, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            time1 = new DateTime(2003, 1, 30, 0, 0, 23);
            time1local = new DateTime(2003, 1, 30, 0, 0, 23, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            time1 = new DateTime(2003, 11, 26, 0, 0, 0);
            time1local = new DateTime(2003, 11, 26, 0, 0, 0, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            time1 = new DateTime(2003, 11, 26, 6, 20, 0);
            time1local = new DateTime(2003, 11, 26, 6, 20, 0, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            time1 = new DateTime(2003, 11, 26, 3, 0, 1);
            time1local = new DateTime(2003, 11, 26, 3, 0, 1, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            gmtOffset = s_GMTLondon.GetUtcOffset(TimeZoneInfo.ConvertTime(time1, s_myUtc));
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
            VerifyConvert(time1local, s_strGMT, time1.Subtract(localOffset).Add(gmtOffset));
        }

        [Fact]
        public static void ConvertTime_DateTime_LocalToLocal()
        {
            if (s_localIsPST)
            {
                var time1 = new DateTime(1964, 6, 19, 12, 45, 10);
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));

                int delta = TimeZoneInfo.Local.Equals(s_regLocal) ? 0 : 1;
                time1 = new DateTime(2007, 3, 11, 1, 0, 0);  // just before DST transition
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2007, 3, 11, 2, 0, 0);  // invalid (U.S. Pacific)
                if (s_localSupportsDST)
                {
                    VerifyConvertException<ArgumentException>(time1, TimeZoneInfo.Local.Id);
                    VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id);
                }
                else
                {
                    VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta));
                    VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1.AddHours(delta));
                }
                time1 = new DateTime(2007, 3, 11, 3, 0, 0);  // just after DST transition (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1.AddHours(delta));
                time1 = new DateTime(2007, 11, 4, 0, 30, 0);  // just before DST transition (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1.AddHours(delta));
                time1 = (new DateTime(2007, 11, 4, 1, 30, 0, DateTimeKind.Local)).ToUniversalTime().AddHours(-1).ToLocalTime();  // DST half of repeated hour (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1.AddHours(delta), DateTimeKind.Unspecified);
                time1 = new DateTime(2007, 11, 4, 1, 30, 0);  // ambiguous (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1);
                time1 = new DateTime(2007, 11, 4, 2, 30, 0);  // just after DST transition (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Local.Id, time1);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, time1);

                time1 = new DateTime(2004, 4, 4, 0, 0, 0);
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                time1 = new DateTime(2004, 4, 4, 4, 0, 0);
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                time1 = new DateTime(2004, 10, 31, 0, 30, 0);
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                time1 = (new DateTime(2004, 10, 31, 1, 30, 0, DateTimeKind.Local)).ToUniversalTime().AddHours(-1).ToLocalTime();
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal), DateTimeKind.Unspecified);
                time1 = new DateTime(2004, 10, 31, 1, 30, 0);
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                time1 = new DateTime(2004, 10, 31, 3, 0, 0);
                VerifyConvert(time1, TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Local.Id, TimeZoneInfo.ConvertTime(time1, s_regLocal));
            }
        }

        [Fact]
        public static void ConvertTime_DateTime_LocalToUtc()
        {
            var time1 = new DateTime(1964, 6, 19, 12, 45, 10);
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);
            // invalid/ambiguous times in Local
            time1 = new DateTime(2006, 5, 12, 7, 34, 59);
            VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc);

            if (s_localIsPST)
            {
                int delta = s_localSupportsDST ? 1 : 0;
                time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                time1 = new DateTime(2006, 4, 2, 2, 30, 0); // invalid hour (U.S. Pacific)
                if (s_localSupportsDST)
                {
                    VerifyConvertException<ArgumentException>(time1, TimeZoneInfo.Utc.Id);
                    VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id);
                }
                else
                {
                    VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                    VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                }
                time1 = new DateTime(2006, 4, 2, 3, 30, 0); // DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                time1 = new DateTime(2006, 10, 29, 0, 30, 0);  // DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - first time, DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - second time, standard (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                time1 = new DateTime(2006, 10, 29, 1, 30, 0);  // ambiguous hour - unspecified, assume standard (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                time1 = new DateTime(2006, 10, 29, 2, 30, 0);  // no DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);

                // 2007 rule
                time1 = new DateTime(2007, 3, 11, 1, 30, 0); // no DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                time1 = new DateTime(2007, 3, 11, 2, 30, 0); // invalid hour (U.S. Pacific)
                if (s_localSupportsDST)
                {
                    VerifyConvertException<ArgumentException>(time1, TimeZoneInfo.Utc.Id);
                    VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id);
                }
                else
                {
                    VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                    VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                }
                time1 = new DateTime(2007, 3, 11, 3, 30, 0); // DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                time1 = new DateTime(2007, 11, 4, 0, 30, 0);  // DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - first time, DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8 - delta), DateTimeKind.Utc);
                time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - second time, standard (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                time1 = new DateTime(2007, 11, 4, 1, 30, 0);  // ambiguous hour - unspecified, assume standard (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                time1 = new DateTime(2007, 11, 4, 2, 30, 0);  // no DST (U.S. Pacific)
                VerifyConvert(time1, TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), TimeZoneInfo.Utc.Id, time1.AddHours(8), DateTimeKind.Utc);
            }
        }

        [Fact]
        public static void ConvertTime_DateTime_VariousDateTimeKinds()
        {
            VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), s_strPacific, s_strSydney);
            VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), s_strSydney, s_strPacific);
            VerifyConvert(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), "UTC", s_strSydney, new DateTime(2006, 2, 13, 16, 37, 48)); // DCR 24267
            VerifyConvert(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Utc), TimeZoneInfo.Utc.Id, s_strSydney, new DateTime(2006, 2, 13, 16, 37, 48)); // DCR 24267
            VerifyConvert(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Unspecified), TimeZoneInfo.Local.Id, s_strSydney, new DateTime(2006, 2, 13, 5, 37, 48).AddHours(11).Subtract(s_regLocal.GetUtcOffset(new DateTime(2006, 2, 13, 5, 37, 48)))); // DCR 24267
            if (TimeZoneInfo.Local.Id != s_strSydney)
            {
                VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Local), s_strSydney, s_strPacific);
            }
            VerifyConvertException<ArgumentException>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Local), "UTC", s_strPacific);
            VerifyConvertException<Exception>(new DateTime(2006, 2, 13, 5, 37, 48, DateTimeKind.Local), "Local", s_strPacific);
        }

        [Fact]
        public static void ConvertTime_DateTime_MiscUtc()
        {
            VerifyConvert(new DateTime(2003, 4, 6, 1, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 4, 6, 1, 30, 0), DateTimeKind.Utc));
            VerifyConvert(new DateTime(2003, 4, 6, 2, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 4, 6, 2, 30, 0), DateTimeKind.Utc));
            VerifyConvert(new DateTime(2003, 10, 26, 1, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 10, 26, 1, 30, 0), DateTimeKind.Utc));
            VerifyConvert(new DateTime(2003, 10, 26, 2, 30, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 10, 26, 2, 30, 0), DateTimeKind.Utc));
            VerifyConvert(new DateTime(2003, 8, 4, 12, 0, 0, DateTimeKind.Utc), "UTC", DateTime.SpecifyKind(new DateTime(2003, 8, 4, 12, 0, 0), DateTimeKind.Utc));

            // Round trip

            VerifyRoundTrip(new DateTime(2003, 8, 4, 12, 0, 0, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);
            VerifyRoundTrip(new DateTime(1929, 3, 9, 23, 59, 59, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);
            VerifyRoundTrip(new DateTime(2000, 2, 28, 23, 59, 59, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);

            // DateTime(2016, 11, 6, 8, 1, 17, DateTimeKind.Utc) is ambiguous time for Pacific Time Zone
            VerifyRoundTrip(new DateTime(2016, 11, 6, 8, 1, 17, DateTimeKind.Utc), "UTC", TimeZoneInfo.Local.Id);

            VerifyRoundTrip(DateTime.UtcNow, "UTC", TimeZoneInfo.Local.Id);

            var time1 = new DateTime(2006, 5, 12, 7, 34, 59);
            VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc));
            VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.Subtract(TimeZoneInfo.Local.GetUtcOffset(time1)), DateTimeKind.Utc));

            if (s_localIsPST)
            {
                int delta = s_localSupportsDST ? 1 : 0;
                time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                time1 = new DateTime(2006, 4, 2, 2, 30, 0); // invalid hour (U.S. Pacific)
                if (s_localSupportsDST)
                {
                    VerifyConvertException<ArgumentException>(time1, "UTC");
                    VerifyConvertException<ArgumentException>(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC");
                }
                else
                {
                    VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                    VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                }
                time1 = new DateTime(2006, 4, 2, 3, 30, 0); // DST (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
                time1 = new DateTime(2006, 10, 29, 0, 30, 0);  // DST (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
                time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - first time, DST (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8 - delta), DateTimeKind.Utc));
                time1 = time1.ToUniversalTime().AddHours(1).ToLocalTime();  // ambiguous hour - second time, standard (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                time1 = new DateTime(2006, 10, 29, 1, 30, 0);  // ambiguous hour - unspecified, assume standard (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                time1 = new DateTime(2006, 10, 29, 2, 30, 0);  // no DST (U.S. Pacific)
                VerifyConvert(time1, "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
                VerifyConvert(DateTime.SpecifyKind(time1, DateTimeKind.Local), "UTC", DateTime.SpecifyKind(time1.AddHours(8), DateTimeKind.Utc));
            }
        }

        [Fact]
        public static void ConvertTime_Brasilia()
        {
            var time1 = new DateTimeOffset(2003, 10, 26, 3, 0, 1, new TimeSpan(-2, 0, 0));
            VerifyConvert(time1, s_strBrasilia, time1);
            time1 = new DateTimeOffset(2006, 5, 12, 5, 17, 42, new TimeSpan(-3, 0, 0));
            VerifyConvert(time1, s_strBrasilia, time1);
            time1 = new DateTimeOffset(2006, 3, 28, 9, 47, 12, new TimeSpan(-3, 0, 0));
            VerifyConvert(time1, s_strBrasilia, time1);
            time1 = new DateTimeOffset(2006, 11, 5, 1, 3, 0, new TimeSpan(-2, 0, 0));
            VerifyConvert(time1, s_strBrasilia, time1);
            time1 = new DateTimeOffset(2006, 10, 15, 2, 30, 0, new TimeSpan(-2, 0, 0));  // invalid
            VerifyConvert(time1, s_strBrasilia, time1.ToOffset(new TimeSpan(-3, 0, 0)));
            time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-3, 0, 0));  // ambiguous
            VerifyConvert(time1, s_strBrasilia, time1);
            time1 = new DateTimeOffset(2006, 2, 12, 1, 30, 0, new TimeSpan(-2, 0, 0));  // ambiguous
            VerifyConvert(time1, s_strBrasilia, time1);
        }

        [Fact]
        public static void ConvertTime_Tonga()
        {
            var time1 = new DateTime(2006, 5, 12, 5, 17, 42, DateTimeKind.Utc);
            VerifyConvert(time1, s_strTonga, DateTime.SpecifyKind(time1.AddHours(13), DateTimeKind.Unspecified));

            time1 = new DateTime(2001, 5, 12, 5, 17, 42);
            var localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));

            var time1local = new DateTime(2001, 5, 12, 5, 17, 42, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1local, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));

            time1 = new DateTime(2003, 1, 30, 5, 19, 20);
            time1local = new DateTime(2003, 1, 30, 5, 19, 20, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));
            VerifyConvert(time1local, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));

            time1 = new DateTime(2003, 1, 30, 1, 20, 1);
            time1local = new DateTime(2003, 1, 30, 1, 20, 1, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));
            VerifyConvert(time1local, s_strTonga, DateTime.SpecifyKind(time1.Subtract(localOffset).AddHours(13), DateTimeKind.Unspecified));

            time1 = new DateTime(2003, 1, 30, 0, 0, 23);
            time1local = new DateTime(2003, 1, 30, 0, 0, 23, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));

            time1 = new DateTime(2003, 11, 26, 2, 0, 0);
            time1local = new DateTime(2003, 11, 26, 2, 0, 0, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));

            time1 = new DateTime(2003, 11, 26, 2, 20, 0);
            time1local = new DateTime(2003, 11, 26, 2, 20, 0, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));

            time1 = new DateTime(2003, 11, 26, 3, 0, 1);
            time1local = new DateTime(2003, 11, 26, 3, 0, 1, DateTimeKind.Local);
            localOffset = TimeZoneInfo.Local.GetUtcOffset(time1);
            VerifyConvert(time1, s_strTonga, time1.Subtract(localOffset).AddHours(13));
            VerifyConvert(time1local, s_strTonga, time1.Subtract(localOffset).AddHours(13));
        }

        [Fact]
        public static void ConvertTime_NullTimeZone_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("destinationTimeZone", () => TimeZoneInfo.ConvertTime(new DateTime(), null));
            AssertExtensions.Throws<ArgumentNullException>("destinationTimeZone", () => TimeZoneInfo.ConvertTime(new DateTimeOffset(), null));

            AssertExtensions.Throws<ArgumentNullException>("sourceTimeZone", () => TimeZoneInfo.ConvertTime(new DateTime(), null, s_casablancaTz));
            AssertExtensions.Throws<ArgumentNullException>("destinationTimeZone", () => TimeZoneInfo.ConvertTime(new DateTime(), s_casablancaTz, null));
        }

        [Fact]
        public static void GetAmbiguousTimeOffsets_Invalid()
        {
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32));

            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32, DateTimeKind.Utc));

            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32, DateTimeKind.Local));
        }

        [Fact]
        public static void GetAmbiguousTimeOffsets_Nairobi_Invalid()
        {
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(2006, 1, 15, 7, 15, 23));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(2050, 2, 15, 8, 30, 24));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1800, 3, 15, 9, 45, 25));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1400, 4, 15, 10, 00, 26));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1234, 5, 15, 11, 15, 27));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(4321, 6, 15, 12, 30, 28));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(1111, 7, 15, 13, 45, 29));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(2222, 8, 15, 14, 00, 30));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(9998, 9, 15, 15, 15, 31));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_nairobiTz, new DateTime(9997, 10, 15, 16, 30, 32));
        }

        [Fact]
        public static void GetAmbiguousTimeOffsets_Amsterdam()
        {
            //
            // * 00:59:59 Sunday March 26, 2006 in Universal converts to
            //   01:59:59 Sunday March 26, 2006 in Europe/Amsterdam (NO DST)
            //
            // * 01:00:00 Sunday March 26, 2006 in Universal converts to
            //   03:00:00 Sunday March 26, 2006 in Europe/Amsterdam (DST)
            //

            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 03, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 04, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 58, 05, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 15, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 30, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 59, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 01, 02, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 03, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 04, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 05, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 06, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 28, 23, 59, 59, DateTimeKind.Utc));

            TimeSpan one = new TimeSpan(+1, 0, 0);
            TimeSpan two = new TimeSpan(+2, 0, 0);

            TimeSpan[] amsterdamAmbiguousOffsets = new TimeSpan[] { one, two };

            //
            // * 00:00:00 Sunday October 29, 2006 in Universal converts to
            //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam  (DST)
            //
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 01, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 02, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 03, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 04, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 05, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 06, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 07, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 08, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 09, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 10, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 11, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 12, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 13, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 14, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 55, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 49, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 50, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 51, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 52, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 53, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 54, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 56, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 57, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 58, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 59, DateTimeKind.Utc), amsterdamAmbiguousOffsets);

            //
            // * 01:00:00 Sunday October 29, 2006 in Universal converts to
            //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam (NO DST)
            //
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 01, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 02, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 03, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 04, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 05, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 06, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 07, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 08, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 09, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 10, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 11, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 12, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 13, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 14, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 00, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 15, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 30, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 45, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 49, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 50, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 51, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 52, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 53, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 55, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 56, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 57, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 58, DateTimeKind.Utc), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 59, DateTimeKind.Utc), amsterdamAmbiguousOffsets);

            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 02, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 03, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 04, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 06, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 07, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 03, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 04, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 05, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 01, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 01, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 01, 00, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 02, 00, 00, DateTimeKind.Utc));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 01, 03, 00, 00, DateTimeKind.Utc));

            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 01, 15, 03, 00, 33));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 02, 15, 04, 00, 34));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 01, 02, 00, 35));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 02, 03, 00, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 15, 03, 00, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 36));

            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 45, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 58, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 50));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 59));

            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 30, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 45, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 50, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 59));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 02));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 01, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 02, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 10, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 11, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 15, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 30, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 45, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 50, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 05, 01, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 06, 02, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 07, 03, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 08, 04, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 09, 05, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 10, 06, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 11, 07, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 12, 08, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 13, 09, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 14, 10, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 15, 01, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 16, 02, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 17, 03, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 18, 04, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 19, 05, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 20, 06, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 21, 07, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 22, 08, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 03, 26, 23, 09, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 20, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 01, 36));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 05, 15, 04, 15, 37));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 06, 15, 02, 15, 38));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 07, 15, 01, 15, 39));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 08, 15, 00, 15, 40));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 09, 15, 10, 15, 41));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 15, 08, 15, 42));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 27, 08, 15, 43));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 28, 08, 15, 44));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 45));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 46));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 47));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 48));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 49));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 50));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 51));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 52));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 58, 53));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54));

            //    March 26, 2006                            October 29, 2006
            // 3AM            4AM                        2AM              3AM
            // |      +1 hr     |                        |       -1 hr      |
            // | <invalid time> |                        | <ambiguous time> |
            //                  *========== DST ========>*
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 09), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 15), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 14), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 12), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 10, 55), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 21, 50), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 32, 40), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 43, 30), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 56, 20), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 57, 10), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 45), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 55), amsterdamAmbiguousOffsets);
            VerifyOffsets(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 59), amsterdamAmbiguousOffsets);

            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 00));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 01, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 02, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 03, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 04, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 05, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 10, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 04, 10, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 05, 10, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 06, 10, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 10, 29, 07, 10, 01));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 11, 15, 10, 15, 43));
            VerifyAmbiguousOffsetsException<ArgumentException>(s_amsterdamTz, new DateTime(2006, 12, 15, 12, 15, 44));
        }

        [Fact]
        public static void GetAmbiguousTimeOffsets_LocalAmbiguousOffsets()
        {
            if (!s_localIsPST)
                return; // Test valid for Pacific TZ only

            TimeSpan[] localOffsets = new TimeSpan[] { new TimeSpan(-7, 0, 0), new TimeSpan(-8, 0, 0) };
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 3, 14, 2, 0, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 3, 14, 2, 0, 0, DateTimeKind.Local)); // use correct rule
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 3, 14, 3, 0, 0, DateTimeKind.Local)); // use correct rule
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 1, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 1, 30, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 0, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 0, 0, DateTimeKind.Local)); // invalid time
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 2, 30, 0, DateTimeKind.Local)); // invalid time
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0, DateTimeKind.Local)); // invalid time
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local)); // invalid time
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 0, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 0, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 3, 30, 0, DateTimeKind.Local));

            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 0, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 0, 30, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0, DateTimeKind.Local));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0));
            VerifyAmbiguousOffsetsException<ArgumentException>(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0, DateTimeKind.Local));
        }

        [Fact]
        public static void IsDaylightSavingTime()
        {
            VerifyDST(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32), false);
            VerifyDST(TimeZoneInfo.Utc, new DateTime(2004, 4, 4, 2, 30, 0, DateTimeKind.Local), false);

            VerifyDST(s_nairobiTz, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local), false);
            VerifyDST(s_nairobiTz, new DateTime(2006, 1, 15, 7, 15, 23), false);
            VerifyDST(s_nairobiTz, new DateTime(2050, 2, 15, 8, 30, 24), false);
            VerifyDST(s_nairobiTz, new DateTime(1800, 3, 15, 9, 45, 25), false);
            VerifyDST(s_nairobiTz, new DateTime(1400, 4, 15, 10, 00, 26), false);
            VerifyDST(s_nairobiTz, new DateTime(1234, 5, 15, 11, 15, 27), false);
            VerifyDST(s_nairobiTz, new DateTime(4321, 6, 15, 12, 30, 28), false);
            VerifyDST(s_nairobiTz, new DateTime(1111, 7, 15, 13, 45, 29), false);
            VerifyDST(s_nairobiTz, new DateTime(2222, 8, 15, 14, 00, 30), false);
            VerifyDST(s_nairobiTz, new DateTime(9998, 9, 15, 15, 15, 31), false);
            VerifyDST(s_nairobiTz, new DateTime(9997, 10, 15, 16, 30, 32), false);

            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 03, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 04, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 58, 05, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 00, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 15, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 30, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 59, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 01, 02, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 03, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 04, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 05, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 06, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 28, 23, 59, 59, DateTimeKind.Utc), true);

            //
            // * 00:00:00 Sunday October 29, 2006 in Universal converts to
            //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam  (DST)
            //
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 15, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 30, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 45, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 55, 55, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 00, DateTimeKind.Utc), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 59, DateTimeKind.Utc), true);

            //
            // * 01:00:00 Sunday October 29, 2006 in Universal converts to
            //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam (NO DST)
            //
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 00, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 59, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 07, DateTimeKind.Utc), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 00, DateTimeKind.Utc), false);

            VerifyDST(s_amsterdamTz, new DateTime(2006, 01, 15, 03, 00, 33), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 02, 15, 04, 00, 34), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 01, 02, 00, 35), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 02, 03, 00, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 15, 03, 00, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 36), false);

            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 00), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 45, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 58, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 36), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 50), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 59), false);

            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 36), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 30, 36), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 45, 36), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 50, 36), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 59), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 01), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 02), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 01, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 02, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 10, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 11, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 15, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 30, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 45, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 50, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 05, 01, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 06, 02, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 07, 03, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 08, 04, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 09, 05, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 10, 06, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 11, 07, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 12, 08, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 13, 09, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 14, 10, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 15, 01, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 16, 02, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 17, 03, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 18, 04, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 19, 05, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 20, 06, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 21, 07, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 22, 08, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 03, 26, 23, 09, 00), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 20, 36), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 01, 36), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 05, 15, 04, 15, 37), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 06, 15, 02, 15, 38), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 07, 15, 01, 15, 39), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 08, 15, 00, 15, 40), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 09, 15, 10, 15, 41), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 15, 08, 15, 42), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 27, 08, 15, 43), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 28, 08, 15, 44), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 45), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 46), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 47), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 48), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 49), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 50), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 51), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 52), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 58, 53), true);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54), true);

            //    March 26, 2006                            October 29, 2006
            // 3AM            4AM                        2AM              3AM
            // |      +1 hr     |                        |       -1 hr      |
            // | <invalid time> |                        | <ambiguous time> |
            //                  *========== DST ========>*
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 09), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 15), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 14), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 12), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 10, 55), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 21, 50), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 32, 40), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 43, 30), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 56, 20), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 57, 10), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 45), false);

            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 00), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 01, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 02, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 03, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 04, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 05, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 10, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 04, 10, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 05, 10, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 06, 10, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 10, 29, 07, 10, 01), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 11, 15, 10, 15, 43), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 12, 15, 12, 15, 44), false);
            VerifyDST(s_amsterdamTz, new DateTime(2006, 4, 2, 2, 30, 0, DateTimeKind.Local), true);

            if (s_localIsPST)
            {
                VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0), true);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 0, 0, DateTimeKind.Local), true);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0), true);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 4, 4, 3, 30, 0, DateTimeKind.Local), true);

                VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 10, 31, 0, 30, 0), true);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2004, 10, 31, 0, 30, 0, DateTimeKind.Local), true);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 1, 30, 0, DateTimeKind.Local), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 0, 0, DateTimeKind.Local), false); // invalid time
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local), false); // invalid time
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 0, 0), false); // ambiguous
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 0, 0, DateTimeKind.Local), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 30, 0), false); // ambiguous
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 1, 30, 0, DateTimeKind.Local), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 0, 0, DateTimeKind.Local), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 11, 4, 2, 30, 0, DateTimeKind.Local), false);
                VerifyDST(TimeZoneInfo.Local, new DateTime(2007, 3, 11, 2, 30, 0, DateTimeKind.Local), false);
            }
        }

        [Fact]
        public static void IsInvalidTime()
        {
            VerifyInv(TimeZoneInfo.Utc, new DateTime(2006, 1, 15, 7, 15, 23), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(2050, 2, 15, 8, 30, 24), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(1800, 3, 15, 9, 45, 25), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(1400, 4, 15, 10, 00, 26), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(1234, 5, 15, 11, 15, 27), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(4321, 6, 15, 12, 30, 28), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(1111, 7, 15, 13, 45, 29), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(2222, 8, 15, 14, 00, 30), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(9998, 9, 15, 15, 15, 31), false);
            VerifyInv(TimeZoneInfo.Utc, new DateTime(9997, 10, 15, 16, 30, 32), false);

            VerifyInv(s_nairobiTz, new DateTime(2006, 1, 15, 7, 15, 23), false);
            VerifyInv(s_nairobiTz, new DateTime(2050, 2, 15, 8, 30, 24), false);
            VerifyInv(s_nairobiTz, new DateTime(1800, 3, 15, 9, 45, 25), false);
            VerifyInv(s_nairobiTz, new DateTime(1400, 4, 15, 10, 00, 26), false);
            VerifyInv(s_nairobiTz, new DateTime(1234, 5, 15, 11, 15, 27), false);
            VerifyInv(s_nairobiTz, new DateTime(4321, 6, 15, 12, 30, 28), false);
            VerifyInv(s_nairobiTz, new DateTime(1111, 7, 15, 13, 45, 29), false);
            VerifyInv(s_nairobiTz, new DateTime(2222, 8, 15, 14, 00, 30), false);
            VerifyInv(s_nairobiTz, new DateTime(9998, 9, 15, 15, 15, 31), false);
            VerifyInv(s_nairobiTz, new DateTime(9997, 10, 15, 16, 30, 32), false);

            //    March 26, 2006                            October 29, 2006
            // 2AM            3AM                        2AM              3AM
            // |      +1 hr     |                        |       -1 hr      |
            // | <invalid time> |                        | <ambiguous time> |
            //                  *========== DST ========>*

            //
            // * 00:59:59 Sunday March 26, 2006 in Universal converts to
            //   01:59:59 Sunday March 26, 2006 in Europe/Amsterdam (NO DST)
            //
            // * 01:00:00 Sunday March 26, 2006 in Universal converts to
            //   03:00:00 Sunday March 26, 2006 in Europe/Amsterdam (DST)
            //

            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 03, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 04, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 58, 05, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 00, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 15, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 30, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 59, 59, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 01, 02, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 03, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 04, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 05, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 06, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 28, 23, 59, 59, DateTimeKind.Utc), false);

            //
            // * 00:00:00 Sunday October 29, 2006 in Universal converts to
            //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam  (DST)
            //
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 00, 00, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 15, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 30, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 45, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 55, 55, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 00, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 59, 59, DateTimeKind.Utc), false);

            //
            // * 01:00:00 Sunday October 29, 2006 in Universal converts to
            //   02:00:00 Sunday October 29, 2006 in Europe/Amsterdam (NO DST)
            //
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 00, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 59, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 07, DateTimeKind.Utc), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 11, 01, 00, 00, 00, DateTimeKind.Utc), false);

            VerifyInv(s_amsterdamTz, new DateTime(2006, 01, 15, 03, 00, 33), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 02, 15, 04, 00, 34), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 01, 02, 00, 35), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 02, 03, 00, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 15, 03, 00, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 25, 03, 00, 59), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 00, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 01, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 00, 30, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 00, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 30, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 50, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 55, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 10), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 20), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 30), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 40), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 50), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 55), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 56), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 57), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 58), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 01, 59, 59), false);

            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 00), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 01), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 10), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 20), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 30), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 36), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 40), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 00, 50), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 01, 00), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 30, 36), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 45, 36), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 55, 36), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 58, 36), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 36), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 50), true);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 02, 59, 59), true);

            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 00, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 30, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 45, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 50, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 03, 59, 59), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 00, 02), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 01, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 02, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 10, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 11, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 15, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 30, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 45, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 04, 50, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 05, 01, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 06, 02, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 07, 03, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 08, 04, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 09, 05, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 10, 06, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 11, 07, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 12, 08, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 13, 09, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 14, 10, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 15, 01, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 16, 02, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 17, 03, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 18, 04, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 19, 05, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 20, 06, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 21, 07, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 22, 08, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 03, 26, 23, 09, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 20, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 04, 15, 03, 01, 36), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 05, 15, 04, 15, 37), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 06, 15, 02, 15, 38), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 07, 15, 01, 15, 39), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 08, 15, 00, 15, 40), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 09, 15, 10, 15, 41), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 15, 08, 15, 42), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 27, 08, 15, 43), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 28, 08, 15, 44), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 15, 45), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 30, 46), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 00, 45, 47), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 00, 48), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 15, 49), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 30, 50), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 45, 51), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 55, 52), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 58, 53), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 01, 59, 54), false);

            //    March 26, 2006                            October 29, 2006
            // 3AM            4AM                        2AM              3AM
            // |      +1 hr     |                        |       -1 hr      |
            // | <invalid time> |                        | <ambiguous time> |
            //                  *========== DST ========>*
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 05), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 09), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 00, 15), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 01, 14), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 02, 12), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 10, 55), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 21, 50), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 32, 40), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 43, 30), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 56, 20), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 57, 10), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 02, 59, 45), false);

            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 00), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 00, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 01, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 02, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 03, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 04, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 05, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 03, 10, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 04, 10, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 05, 10, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 06, 10, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 10, 29, 07, 10, 01), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 11, 15, 10, 15, 43), false);
            VerifyInv(s_amsterdamTz, new DateTime(2006, 12, 15, 12, 15, 44), false);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix and Windows rules differ in this case
        public static void IsDaylightSavingTime_CatamarcaMultiYearDaylightSavings()
        {
            // America/Catamarca had DST from
            //     1946-10-01T04:00:00.0000000Z {-03:00:00 DST=True}
            //     1963-10-01T03:00:00.0000000Z {-04:00:00 DST=False}

            VerifyDST(s_catamarcaTz, new DateTime(1946, 09, 30, 17, 00, 00, DateTimeKind.Utc), false);
            VerifyDST(s_catamarcaTz, new DateTime(1946, 10, 01, 03, 00, 00, DateTimeKind.Utc), false);
            VerifyDST(s_catamarcaTz, new DateTime(1946, 10, 01, 03, 59, 00, DateTimeKind.Utc), false);
            VerifyDST(s_catamarcaTz, new DateTime(1946, 10, 01, 04, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1950, 01, 01, 00, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1953, 03, 01, 15, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1955, 05, 01, 16, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1957, 07, 01, 17, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1959, 09, 01, 00, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1961, 11, 01, 00, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1963, 10, 01, 02, 00, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1963, 10, 01, 02, 59, 00, DateTimeKind.Utc), true);
            VerifyDST(s_catamarcaTz, new DateTime(1963, 10, 01, 03, 00, 00, DateTimeKind.Utc), false);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Linux will use local mean time for DateTimes before standard time came into effect.
        [InlineData("1940-02-24T23:59:59.0000000Z", false, "0:00:00")]
        [InlineData("1940-02-25T00:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1940-11-20T00:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1940-12-31T23:59:59.0000000Z", true, "1:00:00")]
        [InlineData("1941-01-01T00:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1945-02-24T12:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1945-11-17T01:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1945-11-17T22:59:59.0000000Z", true, "1:00:00")]
        [InlineData("1945-11-17T23:00:00.0000000Z", false, "0:00:00")]
        public static void IsDaylightSavingTime_CasablancaMultiYearDaylightSavings(string dateTimeString, bool expectedDST, string expectedOffsetString)
        {
            // Africa/Casablanca had DST from
            //     1940-02-25T00:00:00.0000000Z {+01:00:00 DST=True}
            //     1945-11-17T23:00:00.0000000Z { 00:00:00 DST=False}

            DateTime dt = DateTime.ParseExact(dateTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            VerifyDST(s_casablancaTz, dt, expectedDST);

            TimeSpan offset = TimeSpan.Parse(expectedOffsetString, CultureInfo.InvariantCulture);
            Assert.Equal(offset, s_casablancaTz.GetUtcOffset(dt));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Not supported on Windows.")]
        public static void TestSplittingRulesWhenReported()
        {
            // This test confirm we are splitting the rules which span multiple years on Linux
            // we use "America/Los_Angeles" which has the rule covering 2/9/1942 to 8/14/1945
            // with daylight transition by 01:00:00. This rule should be split into 3 rules:
            //      - rule 1 from 2/9/1942 to 12/31/1942
            //      - rule 2 from 1/1/1943 to 12/31/1944
            //      - rule 3 from 1/1/1945 to 8/14/1945
            TimeZoneInfo.AdjustmentRule[] rules = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific).GetAdjustmentRules();

            bool ruleEncountered = false;
            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].DateStart == new DateTime(1942, 2, 9))
                {
                    Assert.True(i + 2 <= rules.Length - 1);
                    TimeSpan daylightDelta = TimeSpan.FromHours(1);

                    // DateStart                  : 2/9/1942 12:00:00 AM (Unspecified)
                    // DateEnd                    : 12/31/1942 12:00:00 AM (Unspecified)
                    // DaylightDelta              : 01:00:00
                    // DaylightTransitionStart    : ToD:02:00:00 M:2, D:9, W:1, DoW:Sunday, FixedDate:True
                    // DaylightTransitionEnd      : ToD:23:59:59.9990000 M:12, D:31, W:1, DoW:Sunday, FixedDate:True

                    Assert.Equal(new DateTime(1942, 12, 31), rules[i].DateEnd);
                    Assert.Equal(daylightDelta, rules[i].DaylightDelta);
                    Assert.Equal(TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 2, 9), rules[i].DaylightTransitionStart);
                    Assert.Equal(TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 23, 59, 59, 999), 12, 31), rules[i].DaylightTransitionEnd);

                    // DateStart                  : 1/1/1943 12:00:00 AM (Unspecified)
                    // DateEnd                    : 12/31/1944 12:00:00 AM (Unspecified)
                    // DaylightDelta              : 01:00:00
                    // DaylightTransitionStart    : ToD:00:00:00 M:1, D:1, W:1, DoW:Sunday, FixedDate:True
                    // DaylightTransitionEnd      : ToD:23:59:59.9990000 M:12, D:31, W:1, DoW:Sunday, FixedDate:True

                    Assert.Equal(new DateTime(1943, 1, 1), rules[i + 1].DateStart);
                    Assert.Equal(new DateTime(1944, 12, 31), rules[i + 1].DateEnd);
                    Assert.Equal(daylightDelta, rules[i + 1].DaylightDelta);
                    Assert.Equal(TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 1, 1), rules[i + 1].DaylightTransitionStart);
                    Assert.Equal(TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 23, 59, 59, 999), 12, 31), rules[i + 1].DaylightTransitionEnd);

                    // DateStart                  : 1/1/1945 12:00:00 AM (Unspecified)
                    // DateEnd                    : 8/14/1945 12:00:00 AM (Unspecified)
                    // DaylightDelta              : 01:00:00
                    // DaylightTransitionStart    : ToD:00:00:00 M:1, D:1, W:1, DoW:Sunday, FixedDate:True
                    // DaylightTransitionEnd      : ToD:15:59:59.9990000 M:8, D:14, W:1, DoW:Sunday, FixedDate:True

                    Assert.Equal(new DateTime(1945, 1, 1), rules[i + 2].DateStart);
                    Assert.Equal(new DateTime(1945, 8, 14), rules[i + 2].DateEnd);
                    Assert.Equal(daylightDelta, rules[i + 2].DaylightDelta);
                    Assert.Equal(TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 0, 0, 0), 1, 1), rules[i + 2].DaylightTransitionStart);
                    Assert.Equal(TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 15, 59, 59, 999), 8, 14), rules[i + 2].DaylightTransitionEnd);

                    ruleEncountered = true;
                    break;
                }
            }

            Assert.True(ruleEncountered, "The 1942 rule of America/Los_Angeles not found.");
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Linux will use local mean time for DateTimes before standard time came into effect.
        // in 1996 Europe/Lisbon changed from standard time to DST without changing the UTC offset
        [InlineData("1995-09-30T17:00:00.0000000Z", false, "1:00:00")]
        [InlineData("1996-03-31T00:59:59.0000000Z", false, "1:00:00")]
        [InlineData("1996-03-31T01:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1996-03-31T01:00:01.0000000Z", true, "1:00:00")]
        [InlineData("1996-03-31T11:00:01.0000000Z", true, "1:00:00")]
        [InlineData("1996-08-31T11:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1996-10-27T00:00:00.0000000Z", true, "1:00:00")]
        [InlineData("1996-10-27T00:59:59.0000000Z", true, "1:00:00")]
        [InlineData("1996-10-27T01:00:00.0000000Z", false, "0:00:00")]
        [InlineData("1996-10-28T01:00:00.0000000Z", false, "0:00:00")]
        [InlineData("1997-03-30T00:59:59.0000000Z", false, "0:00:00")]
        [InlineData("1997-03-30T01:00:00.0000000Z", true, "1:00:00")]
        public static void IsDaylightSavingTime_LisbonDaylightSavingsWithNoOffsetChange(string dateTimeString, bool expectedDST, string expectedOffsetString)
        {
            DateTime dt = DateTime.ParseExact(dateTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            VerifyDST(s_LisbonTz, dt, expectedDST);

            TimeSpan offset = TimeSpan.Parse(expectedOffsetString, CultureInfo.InvariantCulture);
            Assert.Equal(offset, s_LisbonTz.GetUtcOffset(dt));
        }

        [Theory]
        // Newfoundland is UTC-3:30 standard and UTC-2:30 dst
        // using non-UTC date times in this test to get some coverage for non-UTC date times
        [InlineData("2015-03-08T01:59:59", false, false, false, "-3:30:00", "-8:00:00")]
        // since DST kicks in a 2AM, from 2AM - 3AM is Invalid
        [InlineData("2015-03-08T02:00:00", false, true, false, "-3:30:00", "-8:00:00")]
        [InlineData("2015-03-08T02:59:59", false, true, false, "-3:30:00", "-8:00:00")]
        [InlineData("2015-03-08T03:00:00", true, false, false, "-2:30:00", "-8:00:00")]
        [InlineData("2015-03-08T07:29:59", true, false, false, "-2:30:00", "-8:00:00")]
        [InlineData("2015-03-08T07:30:00", true, false, false, "-2:30:00", "-7:00:00")]
        [InlineData("2015-11-01T00:59:59", true, false, false, "-2:30:00", "-7:00:00")]
        [InlineData("2015-11-01T01:00:00", false, false, true, "-3:30:00", "-7:00:00")]
        [InlineData("2015-11-01T01:59:59", false, false, true, "-3:30:00", "-7:00:00")]
        [InlineData("2015-11-01T02:00:00", false, false, false, "-3:30:00", "-7:00:00")]
        [InlineData("2015-11-01T05:29:59", false, false, false, "-3:30:00", "-7:00:00")]
        [InlineData("2015-11-01T05:30:00", false, false, false, "-3:30:00", "-8:00:00")]
        public static void NewfoundlandTimeZone(string dateTimeString, bool expectedDST, bool isInvalidTime, bool isAmbiguousTime,
            string expectedOffsetString, string pacificOffsetString)
        {
            DateTime dt = DateTime.ParseExact(dateTimeString, "s", CultureInfo.InvariantCulture);
            VerifyInv(s_NewfoundlandTz, dt, isInvalidTime);

            if (!isInvalidTime)
            {
                VerifyDST(s_NewfoundlandTz, dt, expectedDST);
                VerifyAmbiguous(s_NewfoundlandTz, dt, isAmbiguousTime);

                TimeSpan offset = TimeSpan.Parse(expectedOffsetString, CultureInfo.InvariantCulture);
                Assert.Equal(offset, s_NewfoundlandTz.GetUtcOffset(dt));

                TimeSpan pacificOffset = TimeSpan.Parse(pacificOffsetString, CultureInfo.InvariantCulture);
                VerifyConvert(dt, s_strNewfoundland, s_strPacific, dt - (offset - pacificOffset));
            }
        }

        [Fact]
        public static void GetSystemTimeZones()
        {
            ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();
            Assert.NotEmpty(timeZones);
            Assert.Contains(timeZones, t => t.Id == s_strPacific);
            Assert.Contains(timeZones, t => t.Id == s_strSydney);
            Assert.Contains(timeZones, t => t.Id == s_strGMT);
            Assert.Contains(timeZones, t => t.Id == s_strTonga);
            Assert.Contains(timeZones, t => t.Id == s_strBrasil);
            Assert.Contains(timeZones, t => t.Id == s_strPerth);
            Assert.Contains(timeZones, t => t.Id == s_strBrasilia);
            Assert.Contains(timeZones, t => t.Id == s_strNairobi);
            Assert.Contains(timeZones, t => t.Id == s_strAmsterdam);
            Assert.Contains(timeZones, t => t.Id == s_strRussian);
            Assert.Contains(timeZones, t => t.Id == s_strLibya);
            Assert.Contains(timeZones, t => t.Id == s_strCatamarca);
            Assert.Contains(timeZones, t => t.Id == s_strLisbon);
            Assert.Contains(timeZones, t => t.Id == s_strNewfoundland);

            // ensure the TimeZoneInfos are sorted by BaseUtcOffset and then DisplayName.
            TimeZoneInfo previous = timeZones[0];
            for (int i = 1; i < timeZones.Count; i++)
            {
                TimeZoneInfo current = timeZones[i];
                int baseOffsetsCompared = current.BaseUtcOffset.CompareTo(previous.BaseUtcOffset);
                Assert.True(baseOffsetsCompared >= 0,
                    string.Format("TimeZoneInfos are out of order. {0}:{1} should be before {2}:{3}",
                        previous.Id, previous.BaseUtcOffset, current.Id, current.BaseUtcOffset));

                if (baseOffsetsCompared == 0)
                {
                    Assert.True(current.DisplayName.CompareTo(previous.DisplayName) >= 0,
                        string.Format("TimeZoneInfos are out of order. {0} should be before {1}",
                            previous.DisplayName, current.DisplayName));
                }
            }
        }

        [Fact]
        public static void DaylightTransitionsExactTime()
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific);

            DateTime after = new DateTime(2011, 11, 6, 9, 0, 0, 0, DateTimeKind.Utc);
            DateTime mid = after.AddTicks(-1);
            DateTime before = after.AddTicks(-2);

            Assert.Equal(TimeSpan.FromHours(-7), zone.GetUtcOffset(before));
            Assert.Equal(TimeSpan.FromHours(-7), zone.GetUtcOffset(mid));
            Assert.Equal(TimeSpan.FromHours(-8), zone.GetUtcOffset(after));
        }

        /// <summary>
        /// Ensure Africa/Johannesburg transitions from +3 to +2 at
        /// 1943-02-20T23:00:00Z, and not a tick before that.
        /// See https://github.com/dotnet/runtime/issues/4728
        /// </summary>
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Linux and Windows rules differ in this case
        public static void DaylightTransitionsExactTime_Johannesburg()
        {
            DateTimeOffset transition = new DateTimeOffset(1943, 3, 20, 23, 0, 0, TimeSpan.Zero);

            Assert.Equal(TimeSpan.FromHours(3), s_johannesburgTz.GetUtcOffset(transition.AddTicks(-2)));
            Assert.Equal(TimeSpan.FromHours(3), s_johannesburgTz.GetUtcOffset(transition.AddTicks(-1)));
            Assert.Equal(TimeSpan.FromHours(2), s_johannesburgTz.GetUtcOffset(transition));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { s_casablancaTz, s_casablancaTz, true };
            yield return new object[] { s_casablancaTz, s_LisbonTz, false };

            yield return new object[] { TimeZoneInfo.Utc, TimeZoneInfo.Utc, true };
            yield return new object[] { TimeZoneInfo.Utc, s_casablancaTz, false };

            yield return new object[] { TimeZoneInfo.Local, TimeZoneInfo.Local, true };

            yield return new object[] { TimeZoneInfo.Local, new object(), false };
            yield return new object[] { TimeZoneInfo.Local, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(TimeZoneInfo timeZoneInfo, object obj, bool expected)
        {
            Assert.Equal(expected, timeZoneInfo.Equals(obj));
            if (obj is TimeZoneInfo)
            {
                Assert.Equal(expected, timeZoneInfo.Equals((TimeZoneInfo)obj));
                Assert.Equal(expected, timeZoneInfo.GetHashCode().Equals(obj.GetHashCode()));
            }
        }

        [Fact]
        public static void ClearCachedData()
        {
            TimeZoneInfo cst = TimeZoneInfo.FindSystemTimeZoneById(s_strSydney);
            TimeZoneInfo local = TimeZoneInfo.Local;

            TimeZoneInfo.ClearCachedData();
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                TimeZoneInfo.ConvertTime(DateTime.Now, local, cst);
            });
        }

        [Fact]
        public static void ConvertTime_DateTimeOffset_NullDestination_ArgumentNullException()
        {
            DateTimeOffset time1 = new DateTimeOffset(2006, 5, 12, 0, 0, 0, TimeSpan.Zero);
            VerifyConvertException<ArgumentNullException>(time1, null);
        }

        public static IEnumerable<object[]> ConvertTime_DateTimeOffset_InvalidDestination_TimeZoneNotFoundException_MemberData()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { "    " };
            yield return new object[] { "\0" };
            yield return new object[] { s_strPacific.Substring(0, s_strPacific.Length / 2) }; // whole string must match
            yield return new object[] { s_strPacific + " Zone" }; // no extra characters
            yield return new object[] { " " + s_strPacific }; // no leading space
            yield return new object[] { s_strPacific + " " }; // no trailing space
            yield return new object[] { "\0" + s_strPacific }; // no leading null
            yield return new object[] { s_strPacific + "\0" }; // no trailing null
            yield return new object[] { s_strPacific + "\\  " }; // no trailing null
            yield return new object[] { s_strPacific + "\\Display" };
            yield return new object[] { s_strPacific + "\n" }; // no trailing newline
            yield return new object[] { new string('a', 100) }; // long string
        }

        [Theory]
        [MemberData(nameof(ConvertTime_DateTimeOffset_InvalidDestination_TimeZoneNotFoundException_MemberData))]
        public static void ConvertTime_DateTimeOffset_InvalidDestination_TimeZoneNotFoundException(string destinationId)
        {
            DateTimeOffset time1 = new DateTimeOffset(2006, 5, 12, 0, 0, 0, TimeSpan.Zero);
            VerifyConvertException<TimeZoneNotFoundException>(time1, destinationId);
        }

        [Fact]
        public static void ConvertTimeFromUtc()
        {
            // destination timezone is null
            Assert.Throws<ArgumentNullException>(() =>
            {
                DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2007, 5, 3, 11, 8, 0), null);
            });

            // destination timezone is UTC
            DateTime now = DateTime.UtcNow;
            DateTime convertedNow = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Utc);
            Assert.Equal(now, convertedNow);
        }

        [Fact]
        public static void ConvertTimeToUtc()
        {
            // null source
            VerifyConvertToUtcException<ArgumentNullException>(new DateTime(2007, 5, 3, 12, 16, 0), null);

            TimeZoneInfo london = CreateCustomLondonTimeZone();

            // invalid DateTime
            DateTime invalidDate = new DateTime(2007, 3, 25, 1, 30, 0);
            VerifyConvertToUtcException<ArgumentException>(invalidDate, london);

            // DateTimeKind and source types don't match
            VerifyConvertToUtcException<ArgumentException>(new DateTime(2007, 5, 3, 12, 8, 0, DateTimeKind.Utc), london);

            // correct UTC conversion
            DateTime date = new DateTime(2007, 01, 01, 0, 0, 0);
            Assert.Equal(date.ToUniversalTime(), TimeZoneInfo.ConvertTimeToUtc(date));
        }

        [Fact]
        public static void ConvertTimeFromToUtc()
        {
            TimeZoneInfo london = CreateCustomLondonTimeZone();

            DateTime utc = DateTime.UtcNow;
            Assert.Equal(DateTimeKind.Utc, utc.Kind);

            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Utc);
            Assert.Equal(DateTimeKind.Utc, converted.Kind);
            DateTime back = TimeZoneInfo.ConvertTimeToUtc(converted, TimeZoneInfo.Utc);
            Assert.Equal(DateTimeKind.Utc, back.Kind);
            Assert.Equal(utc, back);

            converted = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
            DateTimeKind expectedKind = (TimeZoneInfo.Local == TimeZoneInfo.Utc) ? DateTimeKind.Utc : DateTimeKind.Local;
            Assert.Equal(expectedKind, converted.Kind);
            back = TimeZoneInfo.ConvertTimeToUtc(converted, TimeZoneInfo.Local);
            Assert.Equal(DateTimeKind.Utc, back.Kind);
            Assert.Equal(utc, back);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior specific to Unix
        public static void ConvertTimeFromToUtc_UnixOnly()
        {
            // DateTime Kind is Local
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2007, 5, 3, 11, 8, 0, DateTimeKind.Local), TimeZoneInfo.Local);
            });

            TimeZoneInfo london = CreateCustomLondonTimeZone();

            // winter (no DST)
            DateTime winter = new DateTime(2007, 12, 25, 12, 0, 0);
            DateTime convertedWinter = TimeZoneInfo.ConvertTimeFromUtc(winter, london);
            Assert.Equal(winter, convertedWinter);

            // summer (DST)
            DateTime summer = new DateTime(2007, 06, 01, 12, 0, 0);
            DateTime convertedSummer = TimeZoneInfo.ConvertTimeFromUtc(summer, london);
            Assert.Equal(summer + new TimeSpan(1, 0, 0), convertedSummer);

            // Kind and source types don't match
            VerifyConvertToUtcException<ArgumentException>(new DateTime(2007, 5, 3, 12, 8, 0, DateTimeKind.Local), london);

            // Test the ambiguous date
            DateTime utcAmbiguous = new DateTime(2016, 10, 30, 0, 14, 49, DateTimeKind.Utc);
            DateTime convertedAmbiguous = TimeZoneInfo.ConvertTimeFromUtc(utcAmbiguous, london);
            Assert.Equal(DateTimeKind.Unspecified, convertedAmbiguous.Kind);
            Assert.True(london.IsAmbiguousTime(convertedAmbiguous), $"Expected to have {convertedAmbiguous} is ambiguous");

            // convert to London time and back
            DateTime utc = DateTime.UtcNow;
            Assert.Equal(DateTimeKind.Utc, utc.Kind);
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, london);
            Assert.Equal(DateTimeKind.Unspecified, converted.Kind);
            DateTime back = TimeZoneInfo.ConvertTimeToUtc(converted, london);
            Assert.Equal(DateTimeKind.Utc, back.Kind);

            if (london.IsAmbiguousTime(converted))
            {
                // if the time is ambiguous this will not round trip the original value because this ambiguous time can be mapped into
                // 2 UTC times. usually we return the value with the DST delta added to it.
                back = back.AddTicks(- london.GetAdjustmentRules()[0].DaylightDelta.Ticks);
            }

            Assert.Equal(utc, back);
        }

        [Fact]
        public static void CreateCustomTimeZone()
        {
            TimeZoneInfo.TransitionTime s1 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 3, 2, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime e1 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 2, DayOfWeek.Sunday);
            TimeZoneInfo.AdjustmentRule r1 = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2000, 1, 1), new DateTime(2005, 1, 1), new TimeSpan(1, 0, 0), s1, e1);

            // supports DST
            TimeZoneInfo tz1 = TimeZoneInfo.CreateCustomTimeZone("mytimezone", new TimeSpan(6, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { r1 });
            Assert.True(tz1.SupportsDaylightSavingTime);

            // doesn't support DST
            TimeZoneInfo tz2 = TimeZoneInfo.CreateCustomTimeZone("mytimezone", new TimeSpan(4, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { r1 }, true);
            Assert.False(tz2.SupportsDaylightSavingTime);

            TimeZoneInfo tz3 = TimeZoneInfo.CreateCustomTimeZone("mytimezone", new TimeSpan(6, 0, 0), null, null, null, null);
            Assert.False(tz3.SupportsDaylightSavingTime);
        }

        [Fact]
        public static void CreateCustomTimeZone_Invalid()
        {
            VerifyCustomTimeZoneException<ArgumentNullException>(null, new TimeSpan(0), null, null);                // null Id
            VerifyCustomTimeZoneException<ArgumentException>("", new TimeSpan(0), null, null);                      // empty string Id
            VerifyCustomTimeZoneException<ArgumentException>("mytimezone", new TimeSpan(0, 0, 55), null, null);     // offset not minutes
            VerifyCustomTimeZoneException<ArgumentException>("mytimezone", new TimeSpan(14, 1, 0), null, null);     // offset too big
            VerifyCustomTimeZoneException<ArgumentException>("mytimezone", - new TimeSpan(14, 1, 0), null, null);   // offset too small
        }

        [Fact]
        public static void CreateCustomTimeZone_InvalidTimeZone()
        {
            TimeZoneInfo.TransitionTime s1 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 3, 2, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime e1 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 2, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime s2 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 2, 2, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime e2 = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 11, 2, DayOfWeek.Sunday);

            TimeZoneInfo.AdjustmentRule r1 = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2000, 1, 1), new DateTime(2005, 1, 1), new TimeSpan(1, 0, 0), s1, e1);

            // AdjustmentRules overlap
            TimeZoneInfo.AdjustmentRule r2 = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2004, 1, 1), new DateTime(2007, 1, 1), new TimeSpan(1, 0, 0), s2, e2);
            VerifyCustomTimeZoneException<InvalidTimeZoneException>("mytimezone", new TimeSpan(6, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { r1, r2 });

            // AdjustmentRules not ordered
            TimeZoneInfo.AdjustmentRule r3 = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2006, 1, 1), new DateTime(2007, 1, 1), new TimeSpan(1, 0, 0), s2, e2);
            VerifyCustomTimeZoneException<InvalidTimeZoneException>("mytimezone", new TimeSpan(6, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { r3, r1 });

            // Offset out of range
            TimeZoneInfo.AdjustmentRule r4 = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2000, 1, 1), new DateTime(2005, 1, 1), new TimeSpan(3, 0, 0), s1, e1);
            VerifyCustomTimeZoneException<InvalidTimeZoneException>("mytimezone", new TimeSpan(12, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { r4 });

            // overlapping AdjustmentRules for a date
            TimeZoneInfo.AdjustmentRule r5 = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2005, 1, 1), new DateTime(2007, 1, 1), new TimeSpan(1, 0, 0), s2, e2);
            VerifyCustomTimeZoneException<InvalidTimeZoneException>("mytimezone", new TimeSpan(6, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { r1, r5 });

            // null AdjustmentRule
            VerifyCustomTimeZoneException<InvalidTimeZoneException>("mytimezone", new TimeSpan(12, 0, 0), null, null, null, new TimeZoneInfo.AdjustmentRule[] { null });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // TimeZone not found on Windows
        public static void HasSameRules_RomeAndVatican()
        {
            TimeZoneInfo rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
            TimeZoneInfo vatican = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vatican");
            Assert.True(rome.HasSameRules(vatican));
        }

        [Fact]
        public static void HasSameRules_NullAdjustmentRules()
        {
            TimeZoneInfo utc = TimeZoneInfo.Utc;
            TimeZoneInfo custom = TimeZoneInfo.CreateCustomTimeZone("Custom", new TimeSpan(0), "Custom", "Custom");
            Assert.True(utc.HasSameRules(custom));
        }

        [Fact]
        public static void ConvertTimeBySystemTimeZoneIdTests()
        {
            DateTime now = DateTime.Now;
            DateTime utcNow = TimeZoneInfo.ConvertTimeToUtc(now);

            Assert.Equal(now, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcNow, TimeZoneInfo.Local.Id));
            Assert.Equal(utcNow, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, TimeZoneInfo.Utc.Id));

            Assert.Equal(now, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcNow, TimeZoneInfo.Utc.Id, TimeZoneInfo.Local.Id));
            Assert.Equal(utcNow, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, TimeZoneInfo.Local.Id, TimeZoneInfo.Utc.Id));

            DateTimeOffset offsetNow = new DateTimeOffset(now);
            DateTimeOffset utcOffsetNow = new DateTimeOffset(utcNow);

            Assert.Equal(offsetNow, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcOffsetNow, TimeZoneInfo.Local.Id));
            Assert.Equal(utcOffsetNow, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(offsetNow, TimeZoneInfo.Utc.Id));
        }

        public static IEnumerable<object[]> SystemTimeZonesTestData()
        {
            foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
            {
                yield return new object[] { tz };
            }

            // Include fixed offset IANA zones in the test data when they are available.
            if (!PlatformDetection.IsWindows)
            {
                for (int i = -14; i <= 12; i++)
                {
                    TimeZoneInfo tz = null;

                    try
                    {
                        string id = $"Etc/GMT{i:+0;-0}";
                        tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                    }

                    if (tz != null)
                    {
                        yield return new object[] { tz };
                    }
                }
            }
        }

        private const string IanaAbbreviationPattern = @"^(?:[A-Z][A-Za-z]+|[+-]\d{2}|[+-]\d{4})$";
        private static readonly Regex s_IanaAbbreviationRegex = new Regex(IanaAbbreviationPattern);

        // UTC aliases per https://github.com/unicode-org/cldr/blob/master/common/bcp47/timezone.xml
        // (This list is not likely to change.)
        private static readonly string[] s_UtcAliases = new[] {
            "Etc/UTC",
            "Etc/UCT",
            "Etc/Universal",
            "Etc/Zulu",
            "UCT",
            "UTC",
            "Universal",
            "Zulu"
        };

        [Theory]
        [MemberData(nameof(SystemTimeZonesTestData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void TimeZoneDisplayNames_Unix(TimeZoneInfo timeZone)
        {
            bool isUtc = s_UtcAliases.Contains(timeZone.Id, StringComparer.OrdinalIgnoreCase);

            if (PlatformDetection.IsBrowser)
            {
                // Browser platform doesn't have full ICU names, but uses the IANA IDs and abbreviations instead.

                // The display name will be the offset plus the ID.
                // The offset is checked separately in TimeZoneInfo_DisplayNameStartsWithOffset
                Assert.True(timeZone.DisplayName.EndsWith(" " + timeZone.Id),
                    $"Id: \"{timeZone.Id}\", DisplayName should have ended with the ID, Actual DisplayName: \"{timeZone.DisplayName}\"");

                if (isUtc)
                {
                    // Make sure UTC and its aliases have exactly "UTC" for the standard and daylight names
                    Assert.True(timeZone.StandardName == "UTC",
                        $"Id: \"{timeZone.Id}\", Expected StandardName: \"UTC\", Actual StandardName: \"{timeZone.StandardName}\"");
                    Assert.True(timeZone.DaylightName == "UTC",
                        $"Id: \"{timeZone.Id}\", Expected DaylightName: \"UTC\", Actual DaylightName: \"{timeZone.DaylightName}\"");
                }
                else
                {
                    // For other time zones, match any valid IANA time zone abbreviation, including numeric forms
                    Assert.True(s_IanaAbbreviationRegex.IsMatch(timeZone.StandardName),
                        $"Id: \"{timeZone.Id}\", StandardName should have matched the pattern @\"{IanaAbbreviationPattern}\", Actual StandardName: \"{timeZone.StandardName}\"");
                    Assert.True(s_IanaAbbreviationRegex.IsMatch(timeZone.DaylightName),
                        $"Id: \"{timeZone.Id}\", DaylightName should have matched the pattern @\"{IanaAbbreviationPattern}\", Actual DaylightName: \"{timeZone.DaylightName}\"");
                }
            }
            else if (isUtc)
            {
                // UTC's display name is the string "(UTC) " and the same text as the standard name.
                Assert.True(timeZone.DisplayName == $"(UTC) {timeZone.StandardName}",
                    $"Id: \"{timeZone.Id}\", Expected DisplayName: \"(UTC) {timeZone.StandardName}\", Actual DisplayName: \"{timeZone.DisplayName}\"");

                // All aliases of UTC should have the same names as UTC itself
                Assert.True(timeZone.DisplayName == TimeZoneInfo.Utc.DisplayName,
                    $"Id: \"{timeZone.Id}\", Expected DisplayName: \"{TimeZoneInfo.Utc.DisplayName}\", Actual DisplayName: \"{timeZone.DisplayName}\"");
                Assert.True(timeZone.StandardName == TimeZoneInfo.Utc.StandardName,
                    $"Id: \"{timeZone.Id}\", Expected StandardName: \"{TimeZoneInfo.Utc.StandardName}\", Actual StandardName: \"{timeZone.StandardName}\"");
                Assert.True(timeZone.DaylightName == TimeZoneInfo.Utc.DaylightName,
                    $"Id: \"{timeZone.Id}\", Expected DaylightName: \"{TimeZoneInfo.Utc.DaylightName}\", Actual DaylightName: \"{timeZone.DaylightName}\"");
            }
            else
            {
                // All we can really say generically here is that they aren't empty.
                Assert.False(string.IsNullOrWhiteSpace(timeZone.DisplayName), $"Id: \"{timeZone.Id}\", DisplayName should not have been empty.");
                Assert.False(string.IsNullOrWhiteSpace(timeZone.StandardName), $"Id: \"{timeZone.Id}\", StandardName should not have been empty.");
                Assert.False(string.IsNullOrWhiteSpace(timeZone.DaylightName), $"Id: \"{timeZone.Id}\", DaylightName should not have been empty.");
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/19794", TestPlatforms.AnyUnix)]
        [Theory]
        [MemberData(nameof(SystemTimeZonesTestData))]
        public static void ToSerializedString_FromSerializedString_RoundTrips(TimeZoneInfo timeZone)
        {
            string serialized = timeZone.ToSerializedString();
            TimeZoneInfo deserializedTimeZone = TimeZoneInfo.FromSerializedString(serialized);
            Assert.Equal(timeZone, deserializedTimeZone);
            Assert.Equal(serialized, deserializedTimeZone.ToSerializedString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [MemberData(nameof(SystemTimeZonesTestData))]
        public static void BinaryFormatter_RoundTrips(TimeZoneInfo timeZone)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, timeZone);
                stream.Position = 0;

                TimeZoneInfo deserializedTimeZone = (TimeZoneInfo)formatter.Deserialize(stream);
                Assert.Equal(timeZone, deserializedTimeZone);
            }
        }

        [Fact]
        public static void TimeZoneInfo_DoesNotCreateAdjustmentRulesWithOffsetOutsideOfRange()
        {
            // On some OSes with some time zones setting
            // time zone may contain old adjustment rule which have offset higher than 14h
            // Assert.DoesNotThrow
            DateTimeOffset.FromFileTime(0);
        }

        [Fact]
        public static void TimeZoneInfo_DoesConvertTimeForOldDatesOfTimeZonesWithExceedingMaxRange()
        {
            // On some OSes this time zone contains old adjustment rules which have offset higher than 14h
            TimeZoneInfo tzi = TryGetSystemTimeZone("Asia/Manila");
            if (tzi == null)
            {
                // Time zone could not be found
                return;
            }

            // Assert.DoesNotThrow
            TimeZoneInfo.ConvertTime(new DateTimeOffset(1800, 4, 4, 10, 10, 4, 2, TimeSpan.Zero), tzi);
        }

        [Fact]
        public static void GetSystemTimeZones_AllTimeZonesHaveOffsetInValidRange()
        {
            foreach (TimeZoneInfo tzi in TimeZoneInfo.GetSystemTimeZones())
            {
                foreach (TimeZoneInfo.AdjustmentRule ar in tzi.GetAdjustmentRules())
                {
                    Assert.True(Math.Abs((tzi.GetUtcOffset(ar.DateStart)).TotalHours) <= 14.0);
                }
            }
        }

        private static byte [] timeZoneFileContents = new byte[]
        {
            0x54, 0x5A, 0x69, 0x66, 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x54, 0x5A, 0x69, 0x66,
            0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x0C, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xFF, 0xFF, 0xF8, 0xE4, 0x00, 0x00, 0x00, 0x00, 0x0E, 0x10, 0x01, 0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x08, 0x00, 0x00, 0x0E, 0x10, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x4C,
            0x4D, 0x54, 0x00, 0x2B, 0x30, 0x31, 0x00, 0x2B, 0x30, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            // POSIX Rule
            // 0x0A, 0x3C, 0x2B, 0x30, 0x30, 0x3E, 0x30, 0x3C, 0x2B, 0x30, 0x31,
            // 0x3E, 0x2C, 0x30, 0x2F, 0x30, 0x2C, 0x4A, 0x33, 0x36, 0x35, 0x2F, 0x32, 0x35, 0x0A
        };

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("<+00>0<+01>,0/0,J365/25", 1, 1, true)]
        [InlineData("<+00>0<+01>,30/0,J365/25", 31, 1, true)]
        [InlineData("<+00>0<+01>,31/0,J365/25", 1, 2, true)]
        [InlineData("<+00>0<+01>,58/0,J365/25", 28, 2, true)]
        [InlineData("<+00>0<+01>,59/0,J365/25", 0, 0, false)]
        [InlineData("<+00>0<+01>,9999999/0,J365/25", 0, 0, false)]
        [InlineData("<+00>0<+01>,A/0,J365/25", 0, 0, false)]
        public static void NJulianRuleTest(string posixRule, int dayNumber, int monthNumber, bool shouldSucceed)
        {
            string zoneFilePath = Path.GetTempPath() + Path.GetRandomFileName();
            using (FileStream fs = new FileStream(zoneFilePath, FileMode.Create))
            {
                fs.Write(timeZoneFileContents.AsSpan());

                // Append the POSIX rule
                fs.WriteByte(0x0A);
                foreach (char c in posixRule)
                {
                    fs.WriteByte((byte) c);
                }
                fs.WriteByte(0x0A);
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo() {  UseShellExecute = false };
                psi.Environment.Add("TZ", zoneFilePath);

                RemoteExecutor.Invoke((day, month, succeed) =>
                {
                    bool expectedToSucceed = bool.Parse(succeed);
                    int d = int.Parse(day);
                    int m = int.Parse(month);

                    TimeZoneInfo.AdjustmentRule [] rules = TimeZoneInfo.Local.GetAdjustmentRules();

                    if (expectedToSucceed)
                    {
                        Assert.Equal(1, rules.Length);
                        Assert.Equal(d, rules[0].DaylightTransitionStart.Day);
                        Assert.Equal(m, rules[0].DaylightTransitionStart.Month);
                    }
                    else
                    {
                        Assert.Equal(0, rules.Length);
                    }
                }, dayNumber.ToString(), monthNumber.ToString(), shouldSucceed.ToString(), new RemoteInvokeOptions { StartInfo =  psi}).Dispose();
            }
            finally
            {
                try { File.Delete(zoneFilePath); } catch { } // don't fail the test if we couldn't delete the file.
            }
        }

        [Fact]
        public static void TimeZoneInfo_DaylightDeltaIsNoMoreThan12Hours()
        {
            foreach (TimeZoneInfo tzi in TimeZoneInfo.GetSystemTimeZones())
            {
                foreach (TimeZoneInfo.AdjustmentRule ar in tzi.GetAdjustmentRules())
                {
                    Assert.True(Math.Abs(ar.DaylightDelta.TotalHours) <= 12.0);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SystemTimeZonesTestData))]
        public static void TimeZoneInfo_DisplayNameStartsWithOffset(TimeZoneInfo tzi)
        {
            if (s_UtcAliases.Contains(tzi.Id, StringComparer.OrdinalIgnoreCase))
            {
                // UTC and all of its aliases (Etc/UTC, and others) start with just "(UTC) "
                Assert.StartsWith("(UTC) ", tzi.DisplayName);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(tzi.StandardName));
                Assert.Matches(@"^\(UTC(\+|-)[0-9]{2}:[0-9]{2}\) \S.*", tzi.DisplayName);

                // see https://github.com/dotnet/corefx/pull/33204#issuecomment-438782500
                if (PlatformDetection.IsNotWindowsNanoServer && !PlatformDetection.IsWindows7)
                {
                    string offset = Regex.Match(tzi.DisplayName, @"(-|)[0-9]{2}:[0-9]{2}").Value;
                    TimeSpan ts = TimeSpan.Parse(offset);
                    if (PlatformDetection.IsWindows &&
                        tzi.BaseUtcOffset != ts &&
                        (tzi.Id.Contains("Morocco")  || tzi.Id.Contains("Volgograd")))
                    {
                        // Windows data can report display name with UTC+01:00 offset which is not matching the actual BaseUtcOffset.
                        // We special case this in the test to avoid the test failures like:
                        //      01:00 != 00:00:00, dn:(UTC+01:00) Casablanca, sn:Morocco Standard Time
                        //      04:00 != 03:00:00, dn:(UTC+04:00) Volgograd, sn:Volgograd Standard Time
                        if (tzi.Id.Contains("Morocco"))
                        {
                            Assert.True(tzi.BaseUtcOffset == new TimeSpan(0, 0, 0), $"{offset} != {tzi.BaseUtcOffset}, dn:{tzi.DisplayName}, sn:{tzi.StandardName}");
                        }
                        else
                        {
                            // Volgograd, Russia
                            Assert.True(tzi.BaseUtcOffset == new TimeSpan(3, 0, 0), $"{offset} != {tzi.BaseUtcOffset}, dn:{tzi.DisplayName}, sn:{tzi.StandardName}");
                        }
                    }
                    else
                    {
                        Assert.True(tzi.BaseUtcOffset == ts || tzi.GetUtcOffset(DateTime.Now) == ts, $"{offset} != {tzi.BaseUtcOffset}, dn:{tzi.DisplayName}, sn:{tzi.StandardName}");
                    }
                }
            }
        }

        [Fact]
        public static void EnsureUtcObjectSingleton()
        {
            TimeZoneInfo utcObject = TimeZoneInfo.GetSystemTimeZones().Single(x => x.Id.Equals("UTC", StringComparison.OrdinalIgnoreCase));
            Assert.True(ReferenceEquals(utcObject, TimeZoneInfo.Utc));
            Assert.True(ReferenceEquals(TimeZoneInfo.FindSystemTimeZoneById("UTC"), TimeZoneInfo.Utc));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [InlineData("Pacific Standard Time", "America/Los_Angeles")]
        [InlineData("AUS Eastern Standard Time", "Australia/Sydney")]
        [InlineData("GMT Standard Time", "Europe/London")]
        [InlineData("Tonga Standard Time", "Pacific/Tongatapu")]
        [InlineData("W. Australia Standard Time", "Australia/Perth")]
        [InlineData("E. South America Standard Time", "America/Sao_Paulo")]
        [InlineData("E. Africa Standard Time", "Africa/Nairobi")]
        [InlineData("W. Europe Standard Time", "Europe/Berlin")]
        [InlineData("Russian Standard Time", "Europe/Moscow")]
        [InlineData("Libya Standard Time", "Africa/Tripoli")]
        [InlineData("South Africa Standard Time", "Africa/Johannesburg")]
        [InlineData("Morocco Standard Time", "Africa/Casablanca")]
        [InlineData("Argentina Standard Time", "America/Argentina/Catamarca")]
        [InlineData("Newfoundland Standard Time", "America/St_Johns")]
        [InlineData("Iran Standard Time", "Asia/Tehran")]
        public static void UsingAlternativeTimeZoneIdsTest(string windowsId, string ianaId)
        {
            if (PlatformDetection.ICUVersion.Major >= 52 && !PlatformDetection.IsiOS && !PlatformDetection.IstvOS)
            {
                TimeZoneInfo tzi1 = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
                TimeZoneInfo tzi2 = TimeZoneInfo.FindSystemTimeZoneById(windowsId);

                Assert.Equal(tzi1.BaseUtcOffset,  tzi2.BaseUtcOffset);
                Assert.NotEqual(tzi1.Id,  tzi2.Id);
            }
            else
            {
                Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_isWindows ? ianaId : windowsId));
                TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(s_isWindows ? windowsId : ianaId);
            }
        }

        public static bool SupportIanaNamesConversion => PlatformDetection.IsNotBrowser && PlatformDetection.ICUVersion.Major >= 52;

        [ConditionalFact(nameof(SupportIanaNamesConversion))]
        public static void IsIanaIdTest()
        {
            bool expected = !s_isWindows;

            foreach (TimeZoneInfo tzi in TimeZoneInfo.GetSystemTimeZones())
            {
                Assert.True((expected || tzi.Id.Equals("Utc", StringComparison.OrdinalIgnoreCase)) == tzi.HasIanaId, $"`{tzi.Id}` has wrong IANA Id indicator");
            }

            string timeZoneIdNotIANAId = PlatformDetection.IsiOS || PlatformDetection.IstvOS ? "America/Buenos_Aires" : "Pacific Standard Time";
            Assert.False(TimeZoneInfo.FindSystemTimeZoneById(timeZoneIdNotIANAId).HasIanaId, $" should not be IANA Id.");
            Assert.True(TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles").HasIanaId, $"'America/Los_Angeles' should be IANA Id");
        }

        [ConditionalTheory(nameof(SupportIanaNamesConversion))]
        [InlineData("Pacific Standard Time", "America/Los_Angeles")]
        [InlineData("AUS Eastern Standard Time", "Australia/Sydney")]
        [InlineData("GMT Standard Time", "Europe/London")]
        [InlineData("Tonga Standard Time", "Pacific/Tongatapu")]
        [InlineData("W. Australia Standard Time", "Australia/Perth")]
        [InlineData("E. South America Standard Time", "America/Sao_Paulo")]
        [InlineData("E. Africa Standard Time", "Africa/Nairobi")]
        [InlineData("W. Europe Standard Time", "Europe/Berlin")]
        [InlineData("Russian Standard Time", "Europe/Moscow")]
        [InlineData("Libya Standard Time", "Africa/Tripoli")]
        [InlineData("South Africa Standard Time", "Africa/Johannesburg")]
        [InlineData("Morocco Standard Time", "Africa/Casablanca")]
        [InlineData("Argentina Standard Time", "America/Buenos_Aires")]
        [InlineData("Newfoundland Standard Time", "America/St_Johns")]
        [InlineData("Iran Standard Time", "Asia/Tehran")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on iOS/MacCatalyst/tvOS.")]
        public static void IdsConversionsTest(string windowsId, string ianaId)
        {
            Assert.True(TimeZoneInfo.TryConvertIanaIdToWindowsId(ianaId, out string winId));
            Assert.Equal(windowsId, winId);

            Assert.True(TimeZoneInfo.TryConvertWindowsIdToIanaId(winId, out string ianaConvertedId));
            Assert.Equal(ianaId, ianaConvertedId);
        }

        [ConditionalTheory(nameof(SupportIanaNamesConversion))]
        [InlineData("Pacific Standard Time", "America/Vancouver", "CA")]
        [InlineData("Pacific Standard Time", "America/Los_Angeles", "US")]
        [InlineData("Pacific Standard Time", "America/Los_Angeles", "\u0600NotValidRegion")]
        [InlineData("Central Europe Standard Time", "Europe/Budapest", "DJ")]
        [InlineData("Central Europe Standard Time", "Europe/Budapest", "\uFFFFNotValidRegion")]
        [InlineData("Central Europe Standard Time", "Europe/Prague", "CZ")]
        [InlineData("Central Europe Standard Time", "Europe/Ljubljana", "SI")]
        [InlineData("Central Europe Standard Time", "Europe/Bratislava", "SK")]
        [InlineData("Central Europe Standard Time", "Europe/Tirane", "AL")]
        [InlineData("Central Europe Standard Time", "Europe/Podgorica", "ME")]
        [InlineData("Central Europe Standard Time", "Europe/Belgrade", "RS")]
        // lowercased region name cases:
        [InlineData("Cen. Australia Standard Time", "Australia/Adelaide", "au")]
        [InlineData("AUS Central Standard Time", "Australia/Darwin", "au")]
        [InlineData("E. Australia Standard Time", "Australia/Brisbane", "au")]
        [InlineData("AUS Eastern Standard Time", "Australia/Sydney", "au")]
        [InlineData("Tasmania Standard Time", "Australia/Hobart", "au")]
        [InlineData("Romance Standard Time", "Europe/Madrid", "es")]
        [InlineData("Romance Standard Time", "Europe/Madrid", "Es")]
        [InlineData("Romance Standard Time", "Europe/Madrid", "eS")]
        [InlineData("GMT Standard Time", "Europe/London", "gb")]
        [InlineData("GMT Standard Time", "Europe/Dublin", "ie")]
        [InlineData("W. Europe Standard Time", "Europe/Rome", "it")]
        [InlineData("New Zealand Standard Time", "Pacific/Auckland", "nz")]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on iOS/MacCatalyst/tvOS.")]
        public static void IdsConversionsWithRegionTest(string windowsId, string ianaId, string region)
        {
            Assert.True(TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId, region, out string ianaConvertedId));
            Assert.Equal(ianaId, ianaConvertedId);
        }

        // We test the existence of a specific English time zone name to avoid failures on non-English platforms.
        [ConditionalFact(nameof(IsEnglishUILanguageAndRemoteExecutorSupported))]
        public static void TestNameWithInvariantCulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                // We call ICU to get the names. When passing invariant culture name to ICU, it fail and we'll use the abbreviated names at that time.
                // We fixed this issue by avoid sending the invariant culture name to ICU and this test is confirming we work fine at that time.
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
                TimeZoneInfo.ClearCachedData();

                TimeZoneInfo pacific = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific);

                Assert.True(pacific.StandardName.IndexOf("Pacific", StringComparison.OrdinalIgnoreCase) >= 0, $"'{pacific.StandardName}' is not the expected standard name for Pacific time zone");
                Assert.True(pacific.DaylightName.IndexOf("Pacific", StringComparison.OrdinalIgnoreCase) >= 0, $"'{pacific.DaylightName}' is not the expected daylight name for Pacific time zone");
                Assert.True(pacific.DisplayName.IndexOf("Pacific", StringComparison.OrdinalIgnoreCase) >= 0, $"'{pacific.DisplayName}' is not the expected display name for Pacific time zone");

            }).Dispose();
        }

        private static readonly CultureInfo[] s_CulturesForWindowsNlsDisplayNamesTest = WindowsUILanguageHelper.GetInstalledWin32CulturesWithUniqueLanguages();
        private static bool CanTestWindowsNlsDisplayNames => RemoteExecutor.IsSupported && s_CulturesForWindowsNlsDisplayNamesTest.Length > 1;

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(nameof(CanTestWindowsNlsDisplayNames))]
        public static void TestWindowsNlsDisplayNames()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo[] cultures = s_CulturesForWindowsNlsDisplayNamesTest;

                CultureInfo.CurrentUICulture = cultures[0];
                TimeZoneInfo.ClearCachedData();
                TimeZoneInfo tz1 = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific);

                CultureInfo.CurrentUICulture = cultures[1];
                TimeZoneInfo.ClearCachedData();
                TimeZoneInfo tz2 = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific);

                Assert.True(tz1.DisplayName != tz2.DisplayName, $"The display name '{tz1.DisplayName}' should be different between {cultures[0].Name} and {cultures[1].Name}.");
                Assert.True(tz1.StandardName != tz2.StandardName, $"The standard name '{tz1.StandardName}' should be different between {cultures[0].Name} and {cultures[1].Name}.");
                Assert.True(tz1.DaylightName != tz2.DaylightName, $"The daylight name '{tz1.DaylightName}' should be different between {cultures[0].Name} and {cultures[1].Name}.");
            }).Dispose();
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Browser)]
        [InlineData("America/Buenos_Aires", "America/Argentina/Buenos_Aires")]
        [InlineData("America/Catamarca", "America/Argentina/Catamarca")]
        [InlineData("America/Cordoba", "America/Argentina/Cordoba")]
        [InlineData("America/Jujuy", "America/Argentina/Jujuy")]
        [InlineData("America/Mendoza", "America/Argentina/Mendoza")]
        [InlineData("America/Indianapolis", "America/Indiana/Indianapolis")]
        public static void TestTimeZoneIdBackwardCompatibility(string oldId, string currentId)
        {
            TimeZoneInfo oldtz = TimeZoneInfo.FindSystemTimeZoneById(oldId);
            TimeZoneInfo currenttz = TimeZoneInfo.FindSystemTimeZoneById(currentId);

            Assert.Equal(oldtz.StandardName, currenttz.StandardName);
            Assert.Equal(oldtz.DaylightName, currenttz.DaylightName);
            // Note we cannot test the DisplayName, as it will contain the ID.
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Browser)]
        [InlineData("America/Buenos_Aires")]
        [InlineData("America/Catamarca")]
        [InlineData("America/Cordoba")]
        [InlineData("America/Jujuy")]
        [InlineData("America/Mendoza")]
        [InlineData("America/Indianapolis")]
        public static void ChangeLocalTimeZone(string id)
        {
            string originalTZ = Environment.GetEnvironmentVariable("TZ");
            try
            {
                TimeZoneInfo.ClearCachedData();
                Environment.SetEnvironmentVariable("TZ", id);

                TimeZoneInfo localtz = TimeZoneInfo.Local;
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(id);

                Assert.Equal(tz.StandardName, localtz.StandardName);
                Assert.Equal(tz.DisplayName, localtz.DisplayName);
            }
            finally
            {
                TimeZoneInfo.ClearCachedData();
                Environment.SetEnvironmentVariable("TZ", originalTZ);
            }
        }

        [Fact]
        public static void AdjustmentRuleBaseUtcOffsetDeltaTest()
        {
            TimeZoneInfo.TransitionTime start = TimeZoneInfo.TransitionTime.CreateFixedDateRule(timeOfDay: new DateTime(1, 1, 1, 2, 0, 0), month: 3, day: 7);
            TimeZoneInfo.TransitionTime end = TimeZoneInfo.TransitionTime.CreateFixedDateRule(timeOfDay: new DateTime(1, 1, 1, 1, 0, 0), month: 11, day: 7);
            TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(DateTime.MinValue.Date, DateTime.MaxValue.Date, new TimeSpan(1, 0, 0), start, end, baseUtcOffsetDelta: new TimeSpan(1, 0, 0));
            TimeZoneInfo customTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                                                            id: "Fake Time Zone",
                                                            baseUtcOffset: new TimeSpan(0),
                                                            displayName: "Fake Time Zone",
                                                            standardDisplayName: "Standard Fake Time Zone",
                                                            daylightDisplayName: "British Summer Time",
                                                            new TimeZoneInfo.AdjustmentRule[] { rule });

            TimeZoneInfo.AdjustmentRule[] rules = customTimeZone.GetAdjustmentRules();

            Assert.Equal(1, rules.Length);
            Assert.Equal(new TimeSpan(1, 0, 0), rules[0].BaseUtcOffsetDelta);

            // BaseUtcOffsetDelta should be counted to the returned offset during the standard time.
            Assert.Equal(new TimeSpan(1, 0, 0), customTimeZone.GetUtcOffset(new DateTime(2021, 1, 1, 2, 0, 0)));

            // BaseUtcOffsetDelta should be counted to the returned offset during the daylight time.
            Assert.Equal(new TimeSpan(2, 0, 0), customTimeZone.GetUtcOffset(new DateTime(2021, 3, 10, 2, 0, 0)));
        }

        private static bool IsEnglishUILanguage => CultureInfo.CurrentUICulture.Name.Length == 0 || CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

        private static bool IsEnglishUILanguageAndRemoteExecutorSupported => IsEnglishUILanguage && RemoteExecutor.IsSupported;

        private static void VerifyConvertException<TException>(DateTimeOffset inputTime, string destinationTimeZoneId) where TException : Exception
        {
            Assert.ThrowsAny<TException>(() => TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId)));
        }

        private static void VerifyConvertException<TException>(DateTime inputTime, string destinationTimeZoneId) where TException : Exception
        {
            Assert.ThrowsAny<TException>(() => TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId)));
        }

        private static void VerifyConvertException<TException>(DateTime inputTime, string sourceTimeZoneId, string destinationTimeZoneId) where TException : Exception
        {
            Assert.ThrowsAny<TException>(() => TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId), TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId)));
        }

        private static void VerifyConvert(DateTimeOffset inputTime, string destinationTimeZoneId, DateTimeOffset expectedTime)
        {
            DateTimeOffset returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
            Assert.True(returnedTime.Equals(expectedTime), string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime, returnedTime, inputTime, destinationTimeZoneId));
        }

        private static void VerifyConvert(DateTime inputTime, string destinationTimeZoneId, DateTime expectedTime)
        {
            DateTime returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
            Assert.True(returnedTime.Equals(expectedTime), string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime, returnedTime, inputTime, destinationTimeZoneId));
            Assert.True(expectedTime.Kind == returnedTime.Kind, string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime.Kind, returnedTime.Kind, inputTime, destinationTimeZoneId));
        }

        private static void VerifyConvert(DateTime inputTime, string destinationTimeZoneId, DateTime expectedTime, DateTimeKind expectedKind)
        {
            DateTime returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
            Assert.True(returnedTime.Equals(expectedTime), string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime, returnedTime, inputTime, destinationTimeZoneId));
            Assert.True(expectedKind == returnedTime.Kind, string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', TimeZone: {3}", expectedTime.Kind, returnedTime.Kind, inputTime, destinationTimeZoneId));
        }

        private static void VerifyConvert(DateTime inputTime, string sourceTimeZoneId, string destinationTimeZoneId, DateTime expectedTime)
        {
            DateTime returnedTime = TimeZoneInfo.ConvertTime(inputTime, TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId), TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId));
            Assert.True(returnedTime.Equals(expectedTime), string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', Source TimeZone: {3}, Dest. Time Zone: {4}", expectedTime, returnedTime, inputTime, sourceTimeZoneId, destinationTimeZoneId));
            Assert.True(expectedTime.Kind == returnedTime.Kind, string.Format("Error: Expected value '{0}' but got '{1}', input value is '{2}', Source TimeZone: {3}, Dest. Time Zone: {4}", expectedTime.Kind, returnedTime.Kind, inputTime, sourceTimeZoneId, destinationTimeZoneId));
        }

        private static void VerifyRoundTrip(DateTime dt1, string sourceTimeZoneId, string destinationTimeZoneId)
        {
            TimeZoneInfo sourceTzi = TimeZoneInfo.FindSystemTimeZoneById(sourceTimeZoneId);
            TimeZoneInfo destTzi = TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId);

            DateTime dt2 = TimeZoneInfo.ConvertTime(dt1, sourceTzi, destTzi);
            DateTime dt3 = TimeZoneInfo.ConvertTime(dt2, destTzi, sourceTzi);

            if (!destTzi.IsAmbiguousTime(dt2))
            {
                // the ambiguous time can be mapped to 2 UTC times so it is not guaranteed to round trip
                Assert.True(dt1.Equals(dt3), string.Format("{0} failed to round trip using source '{1}' and '{2}' zones. wrong result {3}", dt1, sourceTimeZoneId, destinationTimeZoneId, dt3));
            }

            if (sourceTimeZoneId == TimeZoneInfo.Utc.Id)
            {
                Assert.True(dt3.Kind == DateTimeKind.Utc, string.Format("failed to get the right DT Kind after round trip {0} using source TZ {1} and dest TZi {2}", dt1, sourceTimeZoneId, destinationTimeZoneId));
            }
        }

        private static void VerifyAmbiguousOffsetsException<TException>(TimeZoneInfo tz, DateTime dt) where TException : Exception
        {
            Assert.Throws<TException>(() => tz.GetAmbiguousTimeOffsets(dt));
        }

        private static void VerifyOffsets(TimeZoneInfo tz, DateTime dt, TimeSpan[] expectedOffsets)
        {
            TimeSpan[] ret = tz.GetAmbiguousTimeOffsets(dt);
            VerifyTimeSpanArray(ret, expectedOffsets, string.Format("Wrong offsets when used {0} with the zone {1}", dt, tz.Id));
        }

        private static void VerifyTimeSpanArray(TimeSpan[] actual, TimeSpan[] expected, string errorMsg)
        {
            Assert.True(actual != null);
            Assert.True(expected != null);
            Assert.True(actual.Length == expected.Length);

            Array.Sort(expected); // TimeZoneInfo is expected to always return sorted TimeSpan arrays

            for (int i = 0; i < actual.Length; i++)
            {
                Assert.True(actual[i].Equals(expected[i]), errorMsg);
            }
        }

        private static void VerifyDST(TimeZoneInfo tz, DateTime dt, bool expectedDST)
        {
            bool ret = tz.IsDaylightSavingTime(dt);
            Assert.True(ret == expectedDST, string.Format("Test with the zone {0} and date {1} failed", tz.Id, dt));
        }

        private static void VerifyInv(TimeZoneInfo tz, DateTime dt, bool expectedInvalid)
        {
            bool ret = tz.IsInvalidTime(dt);
            Assert.True(expectedInvalid == ret, string.Format("Test with the zone {0} and date {1} failed", tz.Id, dt));
        }

        private static void VerifyAmbiguous(TimeZoneInfo tz, DateTime dt, bool expectedAmbiguous)
        {
            bool ret = tz.IsAmbiguousTime(dt);
            Assert.True(expectedAmbiguous == ret, string.Format("Test with the zone {0} and date {1} failed", tz.Id, dt));
        }

        /// <summary>
        /// Gets the offset for the time zone for early times (close to DateTime.MinValue).
        /// </summary>
        /// <remarks>
        /// Windows uses the current daylight savings rules for early times.
        ///
        /// Other Unix distros use V2 tzfiles, which use local mean time (LMT), which is based on the solar time.
        /// The Pacific Standard Time LMT is UTC-07:53.  For Sydney, LMT is UTC+10:04.
        /// </remarks>
        private static TimeSpan GetEarlyTimesOffset(string timeZoneId)
        {
            if (timeZoneId == s_strPacific)
            {
                if (s_isWindows)
                {
                    return TimeSpan.FromHours(-8);
                }
                else
                {
                    return new TimeSpan(7, 53, 0).Negate();
                }
            }
            else if (timeZoneId == s_strSydney)
            {
                if (s_isWindows)
                {
                    return TimeSpan.FromHours(11);
                }
                else
                {
                    return new TimeSpan(10, 4, 0);
                }
            }
            else
            {
                throw new NotSupportedException(string.Format("The timeZoneId '{0}' is not supported by GetEarlyTimesOffset.", timeZoneId));
            }
        }

        private static TimeZoneInfo TryGetSystemTimeZone(string id)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
        }

        private static TimeZoneInfo CreateCustomLondonTimeZone()
        {
            TimeZoneInfo.TransitionTime start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(DateTime.MinValue.Date, DateTime.MaxValue.Date, new TimeSpan(1, 0, 0), start, end);
            return TimeZoneInfo.CreateCustomTimeZone("Europe/London", new TimeSpan(0), "Europe/London", "British Standard Time", "British Summer Time", new TimeZoneInfo.AdjustmentRule[] { rule });
        }

        private static void VerifyConvertToUtcException<TException>(DateTime dateTime, TimeZoneInfo sourceTimeZone) where TException : Exception
        {
            Assert.ThrowsAny<TException>(() => TimeZoneInfo.ConvertTimeToUtc(dateTime, sourceTimeZone));
        }

        private static void VerifyCustomTimeZoneException<TException>(string id, TimeSpan baseUtcOffset, string displayName, string standardDisplayName, string daylightDisplayName = null, TimeZoneInfo.AdjustmentRule[] adjustmentRules = null) where TException : Exception
        {
            Assert.ThrowsAny<TException>(() =>
            {
                if (daylightDisplayName == null && adjustmentRules == null)
                {
                    TimeZoneInfo.CreateCustomTimeZone(id, baseUtcOffset, displayName, standardDisplayName);
                }
                else
                {
                    TimeZoneInfo.CreateCustomTimeZone(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules);
                }
            });
        }

        //  This helper class is used to retrieve information about installed OS languages from Windows.
        //  Its methods returns empty when run on non-Windows platforms.
        private static class WindowsUILanguageHelper
        {
            public static CultureInfo[] GetInstalledWin32CulturesWithUniqueLanguages() =>
                GetInstalledWin32Cultures()
                    .GroupBy(c => c.TwoLetterISOLanguageName)
                    .Select(g => g.First())
                    .ToArray();

            public static unsafe CultureInfo[] GetInstalledWin32Cultures()
            {
                if (!OperatingSystem.IsWindows())
                {
                    return new CultureInfo[0];
                }

                var list = new List<CultureInfo>();
                GCHandle handle = GCHandle.Alloc(list);
                try
                {
                    EnumUILanguages(
                        &EnumUiLanguagesCallback,
                        MUI_ALL_INSTALLED_LANGUAGES | MUI_LANGUAGE_NAME,
                        GCHandle.ToIntPtr(handle));
                }
                finally
                {
                    handle.Free();
                }

                return list.ToArray();
            }

            [UnmanagedCallersOnly]
            private static unsafe int EnumUiLanguagesCallback(char* lpUiLanguageString, IntPtr lParam)
            {
                // native string is null terminated
                var cultureName = new string(lpUiLanguageString);

                try
                {
                    var handle = GCHandle.FromIntPtr(lParam);
                    var list = (List<CultureInfo>) handle.Target;
                    list!.Add(CultureInfo.GetCultureInfo(cultureName));
                    return 1;
                }
                catch
                {
                    return 0;
                }
            }

            private const uint MUI_LANGUAGE_NAME = 0x8;
            private const uint MUI_ALL_INSTALLED_LANGUAGES = 0x20;

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
            private static extern unsafe bool EnumUILanguages(delegate* unmanaged<char*, IntPtr, int> lpUILanguageEnumProc, uint dwFlags, IntPtr lParam);
        }
    }
}
