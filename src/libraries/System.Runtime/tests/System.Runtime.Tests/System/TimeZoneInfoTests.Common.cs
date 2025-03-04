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
        private static readonly bool s_isWindows = OperatingSystem.IsWindows();
        private static readonly bool s_isOSX = OperatingSystem.IsMacOS();

        private static string s_strUtc = "UTC";
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
        private static string s_strFiji = s_isWindows ? "Fiji Standard Time" : "Pacific/Fiji";

        private static TimeZoneInfo s_myUtc = TimeZoneInfo.Utc;
        private static TimeZoneInfo s_myLocal = TimeZoneInfo.Local;

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
        public static void CaseInsensitiveLookupUtc()
        {
            Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(s_strUtc), TimeZoneInfo.FindSystemTimeZoneById(s_strUtc.ToLowerInvariant()));

            // Populate internal cache with all timezones. The implementation takes different path for lookup by id
            // when all timezones are populated.
            TimeZoneInfo.GetSystemTimeZones();

            // The timezones used for the tests after GetSystemTimeZones calls have to be different from the ones used before GetSystemTimeZones to
            // exercise the rare path.
            Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(s_strUtc), TimeZoneInfo.FindSystemTimeZoneById(s_strUtc.ToLowerInvariant()));
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
        public static void ConvertTime_DateTime_MiscUtc_Utc()
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
        }

        [Fact]
        public static void ConvertTime_NullTimeZone_ThrowsArgumentNullException_Utc()
        {
            AssertExtensions.Throws<ArgumentNullException>("destinationTimeZone", () => TimeZoneInfo.ConvertTime(new DateTime(), null));
            AssertExtensions.Throws<ArgumentNullException>("destinationTimeZone", () => TimeZoneInfo.ConvertTime(new DateTimeOffset(), null));
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
        public static void IsDaylightSavingTime_Utc()
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
        }

        [Fact]
        public static void IsInvalidTime_Utc()
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
        }


        private static void ValidateTimeZonesSorting(ReadOnlyCollection<TimeZoneInfo> zones)
        {
            // validate sorting: first by base offset, then by display name
            for (int i = 1; i < zones.Count; i++)
            {
                TimeZoneInfo previous = zones[i - 1];
                TimeZoneInfo current = zones[i];

                int baseOffsetsCompared = current.BaseUtcOffset.CompareTo(previous.BaseUtcOffset);
                Assert.True(baseOffsetsCompared >= 0,
                    string.Format($"TimeZoneInfos are out of order. {previous.Id}:{previous.BaseUtcOffset} should be before {current.Id}:{current.BaseUtcOffset}"));

                if (baseOffsetsCompared == 0)
                {
                    Assert.True(string.CompareOrdinal(current.DisplayName, previous.DisplayName) >= 0,
                        string.Format($"TimeZoneInfos are out of order. {previous.DisplayName} should be before {current.DisplayName}"));
                }
            }
        }

        private static void ValidateDifferentTimeZoneLists(ReadOnlyCollection<TimeZoneInfo> defaultList, ReadOnlyCollection<TimeZoneInfo> nonSortedList, ReadOnlyCollection<TimeZoneInfo> sortedList)
        {
            Assert.Equal(defaultList.Count, nonSortedList.Count);
            Assert.Equal(defaultList.Count, sortedList.Count);

            Assert.Equal(defaultList.Count, nonSortedList.Count);
            Assert.True(object.ReferenceEquals(defaultList, sortedList));
            Dictionary<string, TimeZoneInfo> zones1Dict = defaultList.ToDictionary(t => t.Id);
            foreach (TimeZoneInfo zone in nonSortedList)
            {
                Assert.True(zones1Dict.TryGetValue(zone.Id, out TimeZoneInfo zone1));
            }

            ValidateTimeZonesSorting(defaultList);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void TestGetSystemTimeZonesCollectionsCallsOrder()
        {
            RemoteExecutor.Invoke(() =>
            {
                //
                // Get sorted list first and then the unsorted list
                //
                var zones1 = TimeZoneInfo.GetSystemTimeZones();
                var zones2 = TimeZoneInfo.GetSystemTimeZones(skipSorting: true);
                var zones3 = TimeZoneInfo.GetSystemTimeZones(skipSorting: false);

                ValidateDifferentTimeZoneLists(zones1, zones2, zones3);

                //
                // Clear our caches so zone enumeration is forced to re-read the data
                //
                TimeZoneInfo.ClearCachedData();

                //
                // Get unsorted list first and then the sorted list
                //
                zones2 = TimeZoneInfo.GetSystemTimeZones(skipSorting: true);
                zones3 = TimeZoneInfo.GetSystemTimeZones(skipSorting: false);
                zones1 = TimeZoneInfo.GetSystemTimeZones();
                ValidateDifferentTimeZoneLists(zones1, zones2, zones3);

            }).Dispose();
        }

        [Fact]
        public static void TestGetSystemTimeZonesCollections()
        {
            // This test doing similar checks as TestGetSystemTimeZonesCollectionsCallsOrder does except we need to
            // run this test without the RemoteExecutor to ensure testing on platforms like Android.

            ReadOnlyCollection<TimeZoneInfo> unsortedList = TimeZoneInfo.GetSystemTimeZones(skipSorting: true);
            ReadOnlyCollection<TimeZoneInfo> sortedList = TimeZoneInfo.GetSystemTimeZones(skipSorting: false);
            ReadOnlyCollection<TimeZoneInfo> defaultList = TimeZoneInfo.GetSystemTimeZones();
            ValidateDifferentTimeZoneLists(defaultList, unsortedList, sortedList);
        }

        [Fact]
        public static void ConvertTime_DateTimeOffset_NullDestination_ArgumentNullException()
        {
            DateTimeOffset time1 = new DateTimeOffset(2006, 5, 12, 0, 0, 0, TimeSpan.Zero);
            VerifyConvertException<ArgumentNullException>(time1, null);
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
        public static void ConvertTimeFromToUtcUsingCustomZone()
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

            // roundtrip check using ambiguous time.
            DateTime utc = new DateTime(2022, 10, 30, 1, 47, 13, DateTimeKind.Utc);
            DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, london);
            Assert.Equal(DateTimeKind.Unspecified, converted.Kind);
            DateTime back = TimeZoneInfo.ConvertTimeToUtc(converted, london);
            Assert.Equal(DateTimeKind.Utc, back.Kind);
            Assert.True(london.IsAmbiguousTime(converted));
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
            VerifyCustomTimeZoneException<ArgumentException>("mytimezone", -new TimeSpan(14, 1, 0), null, null);   // offset too small
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

        // In recent Linux distros like Ubuntu 24.04, removed the legacy Time Zone names and not mapping it any more. User can still have a way to install it if they need to.
        // UCT is one of the legacy aliases for UTC which we use here to detect if the legacy names is support at the runtime.
        // https://discourse.ubuntu.com/t/ubuntu-24-04-lts-noble-numbat-release-notes/39890#p-99950-tzdata-package-split
        private static bool SupportLegacyTimeZoneNames { get; } = IsSupportedLegacyTimeZones();
        private static bool IsSupportedLegacyTimeZones()
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById("UCT");
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }

            return true;
        }

        // UTC aliases per https://github.com/unicode-org/cldr/blob/master/common/bcp47/timezone.xml
        // (This list is not likely to change.)
        private static readonly string[] s_UtcAliases = SupportLegacyTimeZoneNames ?
        [
            "Etc/UTC",
            "Etc/UCT",
            "Etc/Universal",
            "Etc/Zulu",
            "UCT",
            "UTC",
            "Universal",
            "Zulu"
        ] : [
            "Etc/UTC",
            "Etc/UCT",
            "Etc/Universal",
            "Etc/Zulu",
            "UTC"
        ];

        [Fact]
        public static void TimeZoneInfo_DoesNotCreateAdjustmentRulesWithOffsetOutsideOfRange()
        {
            // On some OSes with some time zones setting
            // time zone may contain old adjustment rule which have offset higher than 14h
            // Assert.DoesNotThrow
            DateTimeOffset.FromFileTime(0);
        }

        [Fact]
        public static void EnsureUtcObjectSingleton()
        {
            TimeZoneInfo utcObject = TimeZoneInfo.GetSystemTimeZones().Single(x => x.Id.Equals("UTC", StringComparison.OrdinalIgnoreCase));
            Assert.True(ReferenceEquals(utcObject, TimeZoneInfo.Utc));
            Assert.True(ReferenceEquals(TimeZoneInfo.FindSystemTimeZoneById("UTC"), TimeZoneInfo.Utc));

            Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById("UTC", out TimeZoneInfo tz));
            Assert.True(ReferenceEquals(tz, TimeZoneInfo.Utc));
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

        [Fact]
        public static void TestCustomTimeZonesWithNullNames()
        {
            TimeZoneInfo custom = TimeZoneInfo.CreateCustomTimeZone("Custom Time Zone With Null Names", TimeSpan.FromHours(-8), null, null);
            Assert.Equal("Custom Time Zone With Null Names", custom.Id);
            Assert.Equal(string.Empty, custom.StandardName);
            Assert.Equal(string.Empty, custom.DaylightName);
            Assert.Equal(string.Empty, custom.DisplayName);
        }

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
    }
}
