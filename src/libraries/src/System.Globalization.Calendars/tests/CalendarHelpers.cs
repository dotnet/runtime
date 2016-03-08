// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarHelpers
    {
        public enum DataType
        {
            Year = 1,
            Month = 2,
            Day = 8
        }
        
        public static Calendar[] s_calendars = new Calendar[]
        {
            new ThaiBuddhistCalendar(),
            new JapaneseCalendar(),
            new KoreanCalendar(),
            new TaiwanCalendar(),
            new GregorianCalendar(GregorianCalendarTypes.Arabic),
            new GregorianCalendar(GregorianCalendarTypes.Localized),
            new GregorianCalendar(GregorianCalendarTypes.MiddleEastFrench),
            new GregorianCalendar(GregorianCalendarTypes.TransliteratedEnglish),
            new GregorianCalendar(GregorianCalendarTypes.TransliteratedFrench),
            new GregorianCalendar(GregorianCalendarTypes.USEnglish),
            new HijriCalendar(),
            new HebrewCalendar(),
            new JulianCalendar(),
            new TaiwanLunisolarCalendar(),
            new ChineseLunisolarCalendar(),
            new KoreanLunisolarCalendar(),
            new PersianCalendar()
        };

        private static int MinEra(Calendar calendar) => calendar.GetEra(calendar.MinSupportedDateTime);
        private static int MaxEra(Calendar calendar) => calendar.GetEra(calendar.MaxSupportedDateTime);

        private static int MaxCalendarYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[0] || era == 0)
            {
                return calendar.GetYear(calendar.MaxSupportedDateTime);
            }
            return MinGregorianYearInEra(calendar, era + 1) - MinGregorianYearInEra(calendar, era) + 1;
        }

        private static int MaxGregorianYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[0] || era == 0)
            {
                return calendar.MaxSupportedDateTime.Year;
            }
            return MinGregorianYearInEra(calendar, era + 1);
        }

        private static int MinGregorianYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[eras.Length - 1] || era == 0)
            {
                return calendar.MinSupportedDateTime.Year;
            }

            return calendar.ToDateTime(1, 1, 1, 0, 0, 0, 0, era).Year;
        }

        private static int MinCalendarYearInEra(Calendar calendar, int era)
        {
            int[] eras = calendar.Eras;
            Assert.InRange(era, 0, eras[0]);
            if (eras.Length == 1 || era == eras[eras.Length - 1] || era == 0)
            {
                return calendar.GetYear(calendar.MinSupportedDateTime);
            }

            return 1;
        }

        public static IEnumerable<object[]> Calendars_TestData()
        {
            foreach (Calendar calendar in s_calendars)
            {
                yield return new object[] { calendar };
            }
        }

        public static IEnumerable<object[]> Year_Month_Day_Era_TestData(DataType type)
        {
            int month = 1;
            int day = 1;
            foreach (Calendar calendar in s_calendars)
            {
                foreach (int era in calendar.Eras)
                {
                    int year = MaxCalendarYearInEra(calendar, era) - 2;

                    // Year is invalid
                    yield return new object[] { calendar, -1, month, day, era, "year" };
                    yield return new object[] { calendar, 0, month, day, era, "year" };
                    yield return new object[] { calendar, MaxCalendarYearInEra(calendar, era) + 1, month, day, era, "year" };

                    if ((type & DataType.Month) != 0)
                    {
                        // Month is invalid
                        yield return new object[] { calendar, year, -1, day, era, "month" };
                        yield return new object[] { calendar, year, 0, day, era, "month" };
                        yield return new object[] { calendar, year, calendar.GetMonthsInYear(year, era) + 1, day, era, "month" };
                    }

                    if ((type & DataType.Day) != 0)
                    {
                        // Day is invalid
                        yield return new object[] { calendar, year, month, -1, era, "day" };
                        yield return new object[] { calendar, year, month, 0, era, "day" };
                        yield return new object[] { calendar, year, month, calendar.GetDaysInMonth(year, month, era) + 1, era, "day" };
                    }
                }
                // Year is invalid
                yield return new object[] { calendar, MinCalendarYearInEra(calendar, MinEra(calendar)) - 1,month, day, MinEra(calendar), "year" };

                // Era is invalid
                yield return new object[] { calendar, calendar.GetYear(calendar.MaxSupportedDateTime), month, day, MinEra(calendar) - 2, "era" };
                yield return new object[] { calendar, calendar.GetYear(calendar.MaxSupportedDateTime), month, day, MaxEra(calendar) + 1, "era" };
            }
        }

        public static IEnumerable<object[]> DateTime_TestData()
        {
            foreach (Calendar calendar in s_calendars)
            {
                DateTime minDate = calendar.MinSupportedDateTime;
                if (minDate != DateTime.MinValue)
                {
                    yield return new object[] { calendar, minDate.AddYears(-1) };
                }
                DateTime maxDate = calendar.MaxSupportedDateTime;
                if (maxDate != DateTime.MaxValue)
                {
                    yield return new object[] { calendar, maxDate.AddYears(1) }; 
                }
            }
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year)]
        public static void GetDaysInYear_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.GetDaysInYear(year, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year)]
        public static void GetMonthsInYear_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.GetMonthsInYear(year, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year | DataType.Month)]
        public static void GetDaysInMonth_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.GetDaysInMonth(year, month, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year | DataType.Month | DataType.Day)]
        public static void IsLeapDay_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.IsLeapDay(year, month, day, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year | DataType.Month)]
        public static void IsLeapMonth_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.IsLeapMonth(year, month, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year)]
        public static void IsLeapYear_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.IsLeapYear(year, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData), DataType.Year)]
        public static void GetLeapMonth_Invalid(Calendar calendar, int year, int month, int day, int era, string expectedParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(expectedParamName, () => calendar.GetLeapMonth(year, era));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddYears_Invalid(Calendar calendar)
        {
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddYears(calendar.MaxSupportedDateTime, 1));
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddYears(calendar.MinSupportedDateTime, -1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddMonths_Invalid(Calendar calendar)
        {
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddMonths(calendar.MaxSupportedDateTime, 1));
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddMonths(calendar.MinSupportedDateTime, -1)); // JapaneseCalendar throws ArgumentException

            Assert.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, -120001));
            Assert.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, 120001));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddDays_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentException>(() => calendar.AddDays(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentException>(() => calendar.AddDays(calendar.MinSupportedDateTime, -1));

            Assert.Throws<ArgumentException>(() => calendar.AddDays(DateTime.Now, -120001 * 30));
            Assert.Throws<ArgumentException>(() => calendar.AddDays(DateTime.Now, 120001 * 30));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddHours_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentException>(() => calendar.AddHours(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentException>(() => calendar.AddHours(calendar.MinSupportedDateTime, -1));

            Assert.Throws<ArgumentException>(() => calendar.AddHours(DateTime.Now, -120001 * 30 * 24));
            Assert.Throws<ArgumentException>(() => calendar.AddHours(DateTime.Now, 120001 * 30 * 24));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddMinutes_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentException>(() => calendar.AddMinutes(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentException>(() => calendar.AddMinutes(calendar.MinSupportedDateTime, -1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddSeconds_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentException>(() => calendar.AddSeconds(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentException>(() => calendar.AddSeconds(calendar.MinSupportedDateTime, -1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddMilliseconds_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentException>(() => calendar.AddMilliseconds(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentException>(() => calendar.AddMilliseconds(calendar.MinSupportedDateTime, -1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void GetWeekOfYear_Invalid(Calendar calendar)
        {
            // Rule is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("rule", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay - 1, DayOfWeek.Saturday));
            Assert.Throws<ArgumentOutOfRangeException>("rule", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstFourDayWeek + 1, DayOfWeek.Saturday));

            // FirstDayOfWeek is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("firstDayOfWeek", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday - 1));
            Assert.Throws<ArgumentOutOfRangeException>("firstDayOfWeek", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Saturday + 1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void ToDateTime_Invalid(Calendar calendar)
        {
            int month = 1;
            int day = 1;
            int hour = 1;
            int minute = 1;
            int second = 1;
            int millisecond = 1;
            foreach (int era in calendar.Eras)
            {
                int year = MaxCalendarYearInEra(calendar, era) - 2;

                // Year is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(-1, month, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(0, month, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(MaxCalendarYearInEra(calendar, era) + 1, month, day, hour, minute, second, millisecond, era));

                // Month is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, -1, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, 0, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, calendar.GetMonthsInYear(year, era) + 1, day, hour, minute, second, millisecond, era));

                // Day is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, -1, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, 0, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, calendar.GetDaysInMonth(year, month, era) + 1, minute, second, millisecond, era));

                // Hour is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, -1, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, 60, minute, second, millisecond, era));

                // Minute is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, -1, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, 60, second, millisecond, era));

                // Second is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, -1, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, 60, millisecond, era));

                // Millisecond is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, second, -1, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(year, month, day, hour, minute, second, 1000, era));
            }
            // Year is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(MinCalendarYearInEra(calendar, MinEra(calendar)) - 1, month, day, hour, minute, second, millisecond, MinEra(calendar)));

            // Era is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(calendar.GetYear(calendar.MaxSupportedDateTime), month, day, hour, minute, second, millisecond, MinEra(calendar) - 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(calendar.GetYear(calendar.MaxSupportedDateTime), month, day, hour, minute, second, millisecond, MaxEra(calendar) + 1));

            // New date is out of range
            DateTime minDateTime = calendar.MinSupportedDateTime;
            int minEra = calendar.GetEra(minDateTime);
            int minYear = calendar.GetYear(minDateTime);

            DateTime maxDateTime = calendar.MaxSupportedDateTime;
            int maxEra = calendar.GetEra(maxDateTime);
            int maxYear = calendar.GetYear(maxDateTime);

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear - 1, minDateTime.Month, minDateTime.Day, minDateTime.Hour, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear + 1, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond, maxEra));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void ToFourDigitYear_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(-1));
            // JapaneseCalandar allows any inputs below the max year
            if (!(calendar is JapaneseCalendar))
            {
                //Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(MinDateTimeInEra(calendar, MinEra(calendar)) - 2));
            }
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(MaxCalendarYearInEra(calendar, MaxEra(calendar)) + 1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void TwoDigitYearMax_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = 98);
            int max = Math.Max(MaxGregorianYearInEra(calendar, MaxEra(calendar)), MaxCalendarYearInEra(calendar, MaxEra(calendar)));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = max + 1);
        }

        [Theory]
        [MemberData(nameof(DateTime_TestData))]
        public static void GetEra_Invalid(Calendar calendar, DateTime dt)
        {
            // TODO: This fails for HebrewCalendar, TaiwanLunisolarCalendar and JapaneseLunisolarCalendar
            // Assert.Throws<ArgumentOutOfRangeException>(() => calendar.GetEra(dt));
        }

        [Theory]
        [MemberData(nameof(DateTime_TestData))]
        public static void GetYear_Invalid(Calendar calendar, DateTime dt)
        {
            Assert.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetYear(dt));
        }

        [Theory]
        [MemberData(nameof(DateTime_TestData))]
        public static void GetMonth_Invalid(Calendar calendar, DateTime dt)
        {
            Assert.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetMonth(dt));
        }

        [Theory]
        [MemberData(nameof(DateTime_TestData))]
        public static void GetDayOfYear_Invalid(Calendar calendar, DateTime dt)
        {
            Assert.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetDayOfYear(dt));
        }

        [Theory]
        [MemberData(nameof(DateTime_TestData))]
        public static void GetDayOfMonth_Invalid(Calendar calendar, DateTime dt)
        {
            Assert.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetDayOfMonth(dt));
        }

        [Theory]
        [MemberData(nameof(DateTime_TestData))]
        public static void GetDayOfWeek_Invalid(Calendar calendar, DateTime dt)
        {
            // TODO: this fails (expected) for lunar and lunarsolar calendars.
            // Calendar.AlgorithmType is not exposed in corefx so how do we test for this?
            // HijiriCalendar, UmAlQuraCalendar, PersianCalendar, HebrewCalendar
            // Assert.Throws<ArgumentOutOfRangeException>("time", () => calendar.GetDayOfWeek(dt));
        }
    }
}
