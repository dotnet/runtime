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
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Tests
{
    public static partial class TimeZoneInfoTests
    {
        private static TimeZoneInfo s_regLocal;
        private static TimeZoneInfo s_GMTLondon;
        private static TimeZoneInfo s_nairobiTz;
        private static TimeZoneInfo s_amsterdamTz;
        private static TimeZoneInfo s_johannesburgTz;
        private static TimeZoneInfo s_casablancaTz;
        private static TimeZoneInfo s_catamarcaTz;
        private static TimeZoneInfo s_LisbonTz;
        private static TimeZoneInfo s_NewfoundlandTz;

        private static bool s_localIsPST;
        private static bool s_regLocalSupportsDST;
        private static bool s_localSupportsDST;

        private static readonly int s_sydneyOffsetLastWeekOfMarch2006;

        static TimeZoneInfoTests()
        {
            s_regLocal = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneInfo.Local.Id); // in case DST is disabled on Local
            s_GMTLondon = TimeZoneInfo.FindSystemTimeZoneById(s_strGMT);
            s_nairobiTz = TimeZoneInfo.FindSystemTimeZoneById(s_strNairobi);
            s_amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById(s_strAmsterdam);
            s_johannesburgTz = TimeZoneInfo.FindSystemTimeZoneById(s_strJohannesburg);
            s_casablancaTz = TimeZoneInfo.FindSystemTimeZoneById(s_strCasablanca);
            s_catamarcaTz = TimeZoneInfo.FindSystemTimeZoneById(s_strCatamarca);
            s_LisbonTz = TimeZoneInfo.FindSystemTimeZoneById(s_strLisbon);
            s_NewfoundlandTz = TimeZoneInfo.FindSystemTimeZoneById(s_strNewfoundland);

            s_localIsPST = TimeZoneInfo.Local.Id == s_strPacific;
            s_regLocalSupportsDST = s_regLocal.SupportsDaylightSavingTime;
            s_localSupportsDST = TimeZoneInfo.Local.SupportsDaylightSavingTime;

            s_sydneyOffsetLastWeekOfMarch2006 = s_isWindows ? 10 : 11;
        }

        //  Due to ICU size limitations, full daylight/standard names are not included for the browser.
        //  Name abbreviations, if available, are used instead
        public static IEnumerable<object[]> Platform_TimeZoneNamesTestData()
        {
            if (PlatformDetection.IsBrowser || (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform  && (PlatformDetection.IsMacCatalyst || PlatformDetection.IsiOS || PlatformDetection.IstvOS)))
                return new TheoryData<TimeZoneInfo, string, string, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) America/Los_Angeles", null, "PST", "PDT", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Australia/Sydney", null, "AEST", "AEDT", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Australia/Perth", null, "AWST", "AWDT", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Asia/Tehran", null, "+0330", "+0430", null },

                    { s_NewfoundlandTz, "(UTC-03:30) America/St_Johns", null, "NST", "NDT", null },
                    { s_catamarcaTz, "(UTC-03:00) America/Argentina/Catamarca", null, "-03", "-02", null }
                };
            else if (PlatformDetection.IsHybridGlobalizationOnApplePlatform && (PlatformDetection.IsMacCatalyst || PlatformDetection.IsiOS || PlatformDetection.IstvOS))
                return new TheoryData<TimeZoneInfo, string, string, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) America/Los_Angeles", null, "Pacific Standard Time", "Pacific Daylight Time", "Pacific Summer Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Australia/Sydney", null, "Australian Eastern Standard Time", "Australian Eastern Daylight Time", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Australia/Perth", null, "Australian Western Standard Time", "Australian Western Daylight Time", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Asia/Tehran", "(UTC+03:30) Iran Standard Time (Tehran)", "Iran Standard Time", "Iran Daylight Time", "Iran Summer Time" },
                    { s_NewfoundlandTz, "(UTC-03:30) America/St_Johns", null, "Newfoundland Standard Time", "Newfoundland Daylight Time", "Newfoundland Summer Time" },
                    { s_catamarcaTz, "(UTC-03:00) America/Argentina/Catamarca", null, "Argentina Standard Time", "Argentina Summer Time", null }
                };
            else if (PlatformDetection.IsWindows)
                return new TheoryData<TimeZoneInfo, string, string, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) Pacific Time (US & Canada)", null, "Pacific Standard Time", "Pacific Daylight Time", "Pacific Summer Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Canberra, Melbourne, Sydney", null, "AUS Eastern Standard Time", "AUS Eastern Daylight Time", "AUS Eastern Summer Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Perth", null, "W. Australia Standard Time", "W. Australia Daylight Time", "W. Australia Summer Time" },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Tehran", null, "Iran Standard Time", "Iran Daylight Time", "Iran Summer Time" },

                    { s_NewfoundlandTz, "(UTC-03:30) Newfoundland", null, "Newfoundland Standard Time", "Newfoundland Daylight Time", "Newfoundland Summer Time" },
                    { s_catamarcaTz, "(UTC-03:00) City of Buenos Aires", null, "Argentina Standard Time", "Argentina Daylight Time", "Argentina Summer Time" }
                };
            else
                return new TheoryData<TimeZoneInfo, string, string, string, string, string>
                {
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPacific), "(UTC-08:00) Pacific Time (Los Angeles)", null, "Pacific Standard Time", "Pacific Daylight Time", "Pacific Summer Time"  },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strSydney), "(UTC+10:00) Eastern Australia Time (Sydney)", null, "Australian Eastern Standard Time", "Australian Eastern Daylight Time", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strPerth), "(UTC+08:00) Australian Western Standard Time (Perth)", null, "Australian Western Standard Time", "Australian Western Daylight Time", null },
                    { TimeZoneInfo.FindSystemTimeZoneById(s_strIran), "(UTC+03:30) Iran Time", "(UTC+03:30) Iran Standard Time (Tehran)", "Iran Standard Time", "Iran Daylight Time", "Iran Summer Time" },
                    { s_NewfoundlandTz, "(UTC-03:30) Newfoundland Time (St. John’s)", null, "Newfoundland Standard Time", "Newfoundland Daylight Time", null },
                    { s_catamarcaTz, "(UTC-03:00) Argentina Standard Time (Catamarca)", null, "Argentina Standard Time", "Argentina Summer Time", null }
                };
        }

        // We test the existence of a specific English time zone name to avoid failures on non-English platforms.
        [ConditionalTheory(nameof(IsEnglishUILanguage))]
        [MemberData(nameof(Platform_TimeZoneNamesTestData))]
        public static void Platform_TimeZoneNames(TimeZoneInfo tzi, string displayName, string alternativeDisplayName, string standardName, string daylightName, string alternativeDaylightName)
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

            Assert.True(displayName == tzi.DisplayName || alternativeDisplayName == tzi.DisplayName,
                         $"Display Name: Neither '{displayName}' nor '{alternativeDisplayName}' equal to '{tzi.DisplayName}'");
            Assert.Equal(standardName, tzi.StandardName);
            Assert.True(daylightName == tzi.DaylightName || alternativeDaylightName == tzi.DaylightName,
                         $"Daylight Name: Neither '{daylightName}' nor '{alternativeDaylightName}' equal to '{tzi.DaylightName}'");
        }

        [Fact]
        public static void LibyaTimeZone()
        {
            TimeZoneInfo tripoli;
            // Make sure first the timezone data is updated in the machine as it should include Libya Timezone
            try
            {
                tripoli = TimeZoneInfo.FindSystemTimeZoneById(s_strLibya);
                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(s_strLibya, out _));
            }
            catch (Exception /* TimeZoneNotFoundException in netstandard1.7 test*/ )
            {
                // Libya time zone not found
                Console.WriteLine("Warning: Libya time zone is not exist in this machine");
                Assert.False(TimeZoneInfo.TryFindSystemTimeZoneById(s_strLibya, out _));
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
                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById("Yukon Standard Time", out _));

                // First, ensure we have the updated data
                TimeZoneInfo.AdjustmentRule[] rules = yukon.GetAdjustmentRules();
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
                Assert.False(TimeZoneInfo.TryFindSystemTimeZoneById("Yukon Standard Time", out _));
                return;
            }
        }

        [Fact]
        public static void RussianTimeZone()
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(s_strRussian);
            Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(s_strRussian, out _));
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
            VerifyConvert(time1utc, s_strPerth, time1.AddHours(9));

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
            if (s_localIsPST)
            {
                int delta = s_localSupportsDST ? 1 : 0;
                var time1 = new DateTime(2006, 4, 2, 1, 30, 0); // no DST (U.S. Pacific)
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
            AssertExtensions.Throws<ArgumentNullException>("sourceTimeZone", () => TimeZoneInfo.ConvertTime(new DateTime(), null, s_casablancaTz));
            AssertExtensions.Throws<ArgumentNullException>("destinationTimeZone", () => TimeZoneInfo.ConvertTime(new DateTime(), s_casablancaTz, null));
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

            Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(s_strSydney, out cst));
            local = TimeZoneInfo.Local;

            TimeZoneInfo.ClearCachedData();
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                TimeZoneInfo.ConvertTime(DateTime.Now, local, cst);
            });
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
            yield return new object[] { "/dev/random" };
            yield return new object[] { "Invalid Id" };
            yield return new object[] { "Invalid/Invalid" };
            yield return new object[] { $"./{s_strPacific}" };
            yield return new object[] { $"{s_strPacific}/../{s_strPacific}" };
        }

        [Theory]
        [MemberData(nameof(ConvertTime_DateTimeOffset_InvalidDestination_TimeZoneNotFoundException_MemberData))]
        public static void ConvertTime_DateTimeOffset_InvalidDestination_TimeZoneNotFoundException(string destinationId)
        {
            DateTimeOffset time1 = new DateTimeOffset(2006, 5, 12, 0, 0, 0, TimeSpan.Zero);
            VerifyConvertException<TimeZoneNotFoundException>(time1, destinationId);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // TimeZone not found on Windows
        public static void HasSameRules_RomeAndVatican()
        {
            TimeZoneInfo rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
            TimeZoneInfo vatican = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vatican");
            Assert.True(rome.HasSameRules(vatican));
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

                if (!PlatformDetection.IsBrowser && !PlatformDetection.IsiOS && !PlatformDetection.IstvOS)
                {
                    foreach (string alias in s_UtcAliases)
                    {
                        yield return new object[] { TimeZoneInfo.FindSystemTimeZoneById(alias) };
                    }
                }
            }
        }

        [GeneratedRegex(@"^(?:[A-Z][A-Za-z]+|[+-]\d{2}|[+-]\d{4})$")]
        private static partial Regex IanaAbbreviationRegex { get; }

        // On Android GMT, GMT+0, and GMT-0 are values
        private static readonly string[] s_GMTAliases = new[] {
            "GMT",
            "GMT0",
            "GMT+0",
            "GMT-0"
        };

        private static bool SupportICUWithUtcAlias => PlatformDetection.IsIcuGlobalization && PlatformDetection.IsNotAppleMobile && PlatformDetection.IsNotBrowser;

        [ConditionalFact(nameof(SupportICUWithUtcAlias))]
        public static void UtcAliases_MapToUtc()
        {
            foreach (string alias in s_UtcAliases)
            {
                TimeZoneInfo actualUtc = TimeZoneInfo.FindSystemTimeZoneById(alias);
                Assert.True(TimeZoneInfo.Utc.HasSameRules(actualUtc));
                Assert.True(actualUtc.HasSameRules(TimeZoneInfo.Utc));
            }
        }

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
                    Assert.True(IanaAbbreviationRegex.IsMatch(timeZone.StandardName),
                        $"Id: \"{timeZone.Id}\", StandardName should have matched the pattern @\"{IanaAbbreviationRegex}\", Actual StandardName: \"{timeZone.StandardName}\"");
                    Assert.True(IanaAbbreviationRegex.IsMatch(timeZone.DaylightName),
                        $"Id: \"{timeZone.Id}\", DaylightName should have matched the pattern @\"{IanaAbbreviationRegex}\", Actual DaylightName: \"{timeZone.DaylightName}\"");
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

                // GMT* on Android does sets daylight savings time to false, so there will be no DaylightName
                if (!PlatformDetection.IsAndroid || (PlatformDetection.IsAndroid && !timeZone.Id.StartsWith("GMT")))
                {
                    Assert.False(string.IsNullOrWhiteSpace(timeZone.DaylightName), $"Id: \"{timeZone.Id}\", DaylightName should not have been empty.");
                }
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

        private static byte[] timeZoneFileContents = new byte[]
        {
            //
            // Start of v1 Header
            //

                        // Magic bytes "TZif"
            /* 0000 */  0x54, 0x5A, 0x69, 0x66,

                        // Version "2".
            /* 0004 */  0x32,

                        // Fifteen bytes containing zeros reserved for future use.
            /* 0005 */  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                        // The number of UT/local indicators stored in the file
            /* 0014 */  0x00, 0x00, 0x00, 0x00,

                        // The number of standard/wall indicators stored in the file
            /* 0018 */  0x00, 0x00, 0x00, 0x00,

                        // The number of leap seconds for which data entries are stored in the file
            /* 001c */  0x00, 0x00, 0x00, 0x00,

                        // The number of transition times for which data entries are stored in the file
            /* 0020 */  0x00, 0x00, 0x00, 0x00,

                        // The number of local time types for which data entries are stored in the file (must not be zero)
            /* 0024 */  0x00, 0x00, 0x00, 0x01,

                        // The number of bytes of time zone abbreviation strings stored in the file
            /* 0028 */  0x00, 0x00, 0x00, 0x00,

            //
            // End of v1 Header
            //

                       // Padding for times count (time type count = 1 * 6 (sizeof(ttinfo)))
                       // struct ttinfo {
                       //     int32_t        tt_utoff;
                       //     unsigned char  tt_isdst;
                       //     unsigned char  tt_desigidx;
                       // };
            /* 002C */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

            //
            // Start of v2 header
            //

                        //  Magic bytes "TZif"
            /* 0032 */  0x54, 0x5A, 0x69, 0x66,

                        // Version "2"
            /* 0036 */  0x32,

                        // Reserved Bytes
            /* 0037 */  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                        // The number of UT/local indicators stored in the file
            /* 0046 */  0x00, 0x00, 0x00, 0x01,

                        // The number of standard/wall indicators stored in the file
            /* 004A */  0x00, 0x00, 0x00, 0x01,

                        // The number of leap seconds for which data entries are stored in the file
            /* 004E */  0x00, 0x00, 0x00, 0x00,

                        // The number of transition times for which data entries are stored in the file
            /* 0052 */  0x00, 0x00, 0x00, 0x01,

                        // The number of local time types for which data entries are stored in the file (must not be zero)
            /* 0056 */  0x00, 0x00, 0x00, 0x01,

                        // The number of bytes of time zone abbreviation strings stored in the file
            /* 005A */  0x00, 0x00, 0x00, 0x0C,

                        //  Transition 0 # seconds
            /* 005E */  0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                        // transition table[0] has the locale time types index
            /* 0065 */  0x00,

                        // ttinfo table[0]: <UtcOffset:-00:30:20, IsDst::00, TZ Abbre Index: 00>
            /* 0066 */  0xFF, 0xFF, 0xF8, 0xE4, 0x00, 0x00,

                        // Zone abbreviation strings: "LMT+01+00"
            /* 0072 */  0x4C, 0x4D, 0x54, 0x00, 0x2B, 0x30, 0x31, 0x00, 0x2B, 0x30, 0x30, 0x00,

                        // standard/wall indicators values [0, 0, 0, 0, 0]
            /* 007E */  0x00,

                        // UT/local indicators [0, 0, 0, 0, 0]
            /* 007F */  0x00,
            // POSIX Rule: <+00>0<+01>,0/0,J365/25
            // 0x0A, 0x3C, 0x2B, 0x30, 0x30, 0x3E, 0x30, 0x3C, 0x2B, 0x30, 0x31,
            // 0x3E, 0x2C, 0x30, 0x2F, 0x30, 0x2C, 0x4A, 0x33, 0x36, 0x35, 0x2F, 0x32, 0x35, 0x0A
        };

        // https://github.com/dotnet/runtime/issues/73031 is the tracking issue to investigate the test failure on Android.
        private static bool CanRunNJulianRuleTest => !PlatformDetection.IsLinuxBionic && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(CanRunNJulianRuleTest))]
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
                    fs.WriteByte((byte)c);
                }
                fs.WriteByte(0x0A);
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo() { UseShellExecute = false };
                psi.Environment.Add("TZ", zoneFilePath);

                RemoteExecutor.Invoke((day, month, succeed) =>
                {
                    bool expectedToSucceed = bool.Parse(succeed);
                    int d = int.Parse(day);
                    int m = int.Parse(month);

                    TimeZoneInfo.AdjustmentRule[] rules = TimeZoneInfo.Local.GetAdjustmentRules();

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
                }, dayNumber.ToString(), monthNumber.ToString(), shouldSucceed.ToString(), new RemoteInvokeOptions { StartInfo = psi }).Dispose();
            }
            finally
            {
                try { File.Delete(zoneFilePath); } catch { } // don't fail the test if we couldn't delete the file.
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void TimeZoneInfo_LocalZoneWithInvariantMode()
        {
            string hostTZId = TimeZoneInfo.Local.Id;

            ProcessStartInfo psi = new ProcessStartInfo() { UseShellExecute = false };
            psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", PlatformDetection.IsInvariantGlobalization ? "0" : "1");

            RemoteExecutor.Invoke((tzId, hostIsRunningInInvariantMode) =>
            {
                bool hostInvariantMode = bool.Parse(hostIsRunningInInvariantMode);

                if (!hostInvariantMode)
                {
                    // If hostInvariantMode is false, means the child process should enable the globalization invariant mode.
                    // We validate here that by trying to create a culture which should throws in such mode.
                    Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo("en-US"));
                }

                Assert.Equal(tzId, TimeZoneInfo.Local.Id);

            }, hostTZId, PlatformDetection.IsInvariantGlobalization.ToString(), new RemoteInvokeOptions { StartInfo = psi }).Dispose();
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
            else if (s_GMTAliases.Contains(tzi.Id, StringComparer.OrdinalIgnoreCase))
            {
                Assert.StartsWith("GMT", tzi.DisplayName);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(tzi.StandardName));
                Match match = Regex.Match(tzi.DisplayName, @"^\(UTC(?<sign>\+|-)(?<amount>[0-9]{2}:[0-9]{2})\) \S.*", RegexOptions.ExplicitCapture);
                Assert.True(match.Success);

                // see https://github.com/dotnet/corefx/pull/33204#issuecomment-438782500
                if (PlatformDetection.IsNotWindowsNanoServer && !PlatformDetection.IsWindows7)
                {
                    string offset = (match.Groups["sign"].Value == "-" ? "-" : "") + match.Groups["amount"].Value;
                    TimeSpan ts = TimeSpan.Parse(offset);
                    if (PlatformDetection.IsWindows &&
                        tzi.BaseUtcOffset != ts &&
                        (tzi.Id.Contains("Morocco") || tzi.Id.Contains("Volgograd")))
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

        public static IEnumerable<object[]> AlternativeName_TestData()
        {
            yield return new object[] { "Pacific Standard Time", "America/Los_Angeles" };
            yield return new object[] { "AUS Eastern Standard Time", "Australia/Sydney" };
            yield return new object[] { "GMT Standard Time", "Europe/London" };
            yield return new object[] { "Tonga Standard Time", "Pacific/Tongatapu" };
            yield return new object[] { "W. Australia Standard Time", "Australia/Perth" };
            yield return new object[] { "E. South America Standard Time", "America/Sao_Paulo" };
            yield return new object[] { "E. Africa Standard Time", "Africa/Nairobi" };
            yield return new object[] { "W. Europe Standard Time", "Europe/Berlin" };
            yield return new object[] { "Russian Standard Time", "Europe/Moscow" };
            yield return new object[] { "Libya Standard Time", "Africa/Tripoli" };
            yield return new object[] { "South Africa Standard Time", "Africa/Johannesburg" };
            yield return new object[] { "Morocco Standard Time", "Africa/Casablanca" };
            yield return new object[] { "Newfoundland Standard Time", "America/St_Johns" };
            yield return new object[] { "Iran Standard Time", "Asia/Tehran" };

            if (SupportLegacyTimeZoneNames)
            {
                yield return new object[] { "Argentina Standard Time", "America/Argentina/Catamarca" };
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(AlternativeName_TestData))]
        public static void UsingAlternativeTimeZoneIdsTest(string windowsId, string ianaId)
        {
            if (PlatformDetection.ICUVersion.Major >= 52 && !PlatformDetection.IsiOS && !PlatformDetection.IstvOS)
            {
                TimeZoneInfo tzi1 = TimeZoneInfo.FindSystemTimeZoneById(ianaId);
                TimeZoneInfo tzi2 = TimeZoneInfo.FindSystemTimeZoneById(windowsId);

                Assert.Equal(tzi1.BaseUtcOffset, tzi2.BaseUtcOffset);
                Assert.NotEqual(tzi1.Id, tzi2.Id);

                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out tzi1));
                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out tzi2));

                Assert.Equal(tzi1.BaseUtcOffset, tzi2.BaseUtcOffset);
                Assert.NotEqual(tzi1.Id, tzi2.Id);
            }
            else
            {
                Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(s_isWindows ? ianaId : windowsId));
                TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(s_isWindows ? windowsId : ianaId);

                Assert.False(TimeZoneInfo.TryFindSystemTimeZoneById(s_isWindows ? ianaId : windowsId, out _));
                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(s_isWindows ? windowsId : ianaId, out _));
            }
        }

        public static bool SupportIanaNamesConversion => PlatformDetection.IsNotMobile && PlatformDetection.ICUVersion.Major >= 52;
        public static bool SupportIanaNamesConversionAndRemoteExecution => SupportIanaNamesConversion && RemoteExecutor.IsSupported;
        public static bool DoesNotSupportIanaNamesConversion => !SupportIanaNamesConversion;

        // This test is executed using the remote execution because it needs to run before creating the time zone cache to ensure testing with that state.
        // There are already other tests that test after creating the cache.
        [ConditionalFact(nameof(SupportIanaNamesConversionAndRemoteExecution))]
        public static void IsIanaIdWithNotCacheTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(!s_isWindows || TimeZoneInfo.Local.Id.Equals("Utc", StringComparison.OrdinalIgnoreCase), TimeZoneInfo.Local.HasIanaId);

                TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                Assert.False(tzi.HasIanaId);

                tzi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                Assert.True(tzi.HasIanaId);
            }).Dispose();
        }

        [ConditionalFact(nameof(SupportIanaNamesConversion))]
        public static void IsIanaIdTest()
        {
            bool expected = !s_isWindows;

            Assert.Equal((expected || TimeZoneInfo.Local.Id.Equals("Utc", StringComparison.OrdinalIgnoreCase)), TimeZoneInfo.Local.HasIanaId);

            foreach (TimeZoneInfo tzi in TimeZoneInfo.GetSystemTimeZones())
            {
                Assert.True((expected || tzi.Id.Equals("Utc", StringComparison.OrdinalIgnoreCase)) == tzi.HasIanaId, $"`{tzi.Id}` has wrong IANA Id indicator");
            }

            Assert.False(TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time").HasIanaId, $" should not be IANA Id.");
            Assert.True(TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles").HasIanaId, $"'America/Los_Angeles' should be IANA Id");
        }

        [ConditionalFact(nameof(DoesNotSupportIanaNamesConversion))]
        [PlatformSpecific(~TestPlatforms.Android)]
        public static void UnsupportedImplicitConversionTest()
        {
            string nonNativeTzName = s_isWindows ? "America/Los_Angeles" : "Pacific Standard Time";

            Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneInfo.FindSystemTimeZoneById(nonNativeTzName));

            Assert.False(TimeZoneInfo.TryFindSystemTimeZoneById(nonNativeTzName, out _));
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))]
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

            try
            {
                TimeZoneInfo.ClearCachedData();
                Environment.SetEnvironmentVariable("TZ", id);

                TimeZoneInfo localtz = TimeZoneInfo.Local;
                Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(id, out TimeZoneInfo tz));

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
        public static void FijiTimeZoneTest()
        {
            TimeZoneInfo fijiTZ = TimeZoneInfo.FindSystemTimeZoneById(s_strFiji); // "Fiji Standard Time" - "Pacific/Fiji"

            DateTime utcDT = new DateTime(2021, 1, 1, 14, 0, 0, DateTimeKind.Utc);
            Assert.Equal(TimeSpan.FromHours(13), fijiTZ.GetUtcOffset(utcDT));
            Assert.True(fijiTZ.IsDaylightSavingTime(utcDT));

            utcDT = new DateTime(2021, 1, 31, 10, 0, 0, DateTimeKind.Utc);
            Assert.Equal(TimeSpan.FromHours(12), fijiTZ.GetUtcOffset(utcDT));
            Assert.False(fijiTZ.IsDaylightSavingTime(utcDT));

            TimeZoneInfo.AdjustmentRule [] rules = fijiTZ.GetAdjustmentRules();

            // Some machines got some weird TZ data which not including all supported years' rules
            // Avoid the test failures in such case.
            if (rules.Length > 0 && rules[rules.Length - 1].DateStart.Year >= 2023)
            {
                utcDT = new DateTime(2022, 10, 1, 10, 0, 0, DateTimeKind.Utc);
                Assert.Equal(TimeSpan.FromHours(12), fijiTZ.GetUtcOffset(utcDT));
                Assert.False(fijiTZ.IsDaylightSavingTime(utcDT));

                utcDT = new DateTime(2022, 12, 31, 11, 0, 0, DateTimeKind.Utc);
                Assert.Equal(TimeSpan.FromHours(13), fijiTZ.GetUtcOffset(utcDT));
                Assert.True(fijiTZ.IsDaylightSavingTime(utcDT));

                utcDT = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
                Assert.Equal(TimeSpan.FromHours(13), fijiTZ.GetUtcOffset(utcDT));
                Assert.True(fijiTZ.IsDaylightSavingTime(utcDT));

                utcDT = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc);
                Assert.Equal(TimeSpan.FromHours(12), fijiTZ.GetUtcOffset(utcDT));
                Assert.False(fijiTZ.IsDaylightSavingTime(utcDT));
            }
        }

        [ConditionalFact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64111", TestPlatforms.Linux)]
        public static void NoBackwardTimeZones()
        {
            if (OperatingSystem.IsAndroid() && !OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                throw new SkipTestException("This test won't work on API level < 26");
            }

            ReadOnlyCollection<TimeZoneInfo> tzCollection = TimeZoneInfo.GetSystemTimeZones();
            HashSet<String> tzDisplayNames = new HashSet<String>();

            foreach (TimeZoneInfo timezone in tzCollection)
            {
                tzDisplayNames.Add(timezone.DisplayName);
            }
            Assert.Equal(tzCollection.Count, tzDisplayNames.Count);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS)]
        [Trait(XunitConstants.Category, "AdditionalTimezoneChecks")]
        public static void LocalTzIsNotUtc()
        {
            Assert.NotEqual(TimeZoneInfo.Utc.StandardName, TimeZoneInfo.Local.StandardName);
        }

        private static bool SupportICUAndRemoteExecution => PlatformDetection.IsIcuGlobalization && RemoteExecutor.IsSupported;

        [InlineData("Pacific Standard Time")]
        [InlineData("America/Los_Angeles")]
        [ConditionalTheory(nameof(SupportICUAndRemoteExecution))]
        public static void TestZoneNamesUsingAlternativeId(string zoneId)
        {
            RemoteExecutor.Invoke(id =>
            {
                TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(id);
                Assert.False(string.IsNullOrEmpty(tzi.StandardName), $"StandardName for '{id}' is null or empty.");
                Assert.False(string.IsNullOrEmpty(tzi.DaylightName), $"DaylightName for '{id}' is null or empty.");
                Assert.False(string.IsNullOrEmpty(tzi.DisplayName), $"DisplayName for '{id}' is null or empty.");
            }, zoneId).Dispose();
        }

        [InlineData("Eastern Standard Time", "America/New_York")]
        [InlineData("Central Standard Time", "America/Chicago")]
        [InlineData("Mountain Standard Time", "America/Denver")]
        [InlineData("Pacific Standard Time", "America/Los_Angeles")]
        [ConditionalTheory(nameof(SupportICUAndRemoteExecution))]
        public static void TestTimeZoneNames(string windowsId, string ianaId)
        {
            RemoteExecutor.Invoke(static (wId, iId) =>
            {
                TimeZoneInfo info1, info2;
                if (PlatformDetection.IsWindows)
                {
                    info1 = TimeZoneInfo.FindSystemTimeZoneById(iId);
                    info2 = TimeZoneInfo.FindSystemTimeZoneById(wId);
                }
                else
                {
                    info1 = TimeZoneInfo.FindSystemTimeZoneById(wId);
                    info2 = TimeZoneInfo.FindSystemTimeZoneById(iId);
                }

                Assert.Equal(info1.StandardName, info2.StandardName);
                Assert.Equal(info1.DaylightName, info2.DaylightName);
                Assert.Equal(info1.DisplayName, info2.DisplayName);
            }, windowsId, ianaId).Dispose();
        }

        private static bool IsEnglishUILanguage => CultureInfo.CurrentUICulture.Name.Length == 0 || CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

        private static bool IsEnglishUILanguageAndRemoteExecutorSupported => IsEnglishUILanguage && RemoteExecutor.IsSupported;
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

#if !TARGET_BROWSER
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
#endif

                return list.ToArray();
            }

#if !TARGET_BROWSER
            [UnmanagedCallersOnly]
            private static unsafe int EnumUiLanguagesCallback(char* lpUiLanguageString, IntPtr lParam)
            {
                // native string is null terminated
                var cultureName = new string(lpUiLanguageString);

                string tzResourceFilePath = Path.Join(Environment.SystemDirectory, cultureName, "tzres.dll.mui");
                if (!File.Exists(tzResourceFilePath))
                {
                    // If Windows installed a UI language but did not include the time zone resources DLL for that language,
                    // then skip this language as .NET will not be able to get the localized resources for that language.
                    return 1;
                }

                try
                {
                    var handle = GCHandle.FromIntPtr(lParam);
                    var list = (List<CultureInfo>)handle.Target;
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
#endif
        }
    }
}
