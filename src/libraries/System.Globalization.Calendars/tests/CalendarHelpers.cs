// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarHelpers
    {
        public static Calendar[] s_calendars = new Calendar[]
        {
            new ThaiBuddhistCalendar(),
            new JapaneseCalendar()
        };

        private static int MinEra(Calendar calendar) => calendar.GetEra(calendar.MinSupportedDateTime);
        private static int MaxEra(Calendar calendar) => calendar.GetEra(calendar.MaxSupportedDateTime);

        private static int YearOffset(Calendar calendar)
        {
            if (calendar is ThaiBuddhistCalendar)
            {
                return 543;
            }
            return 0;
        }

        private static int MaxRelativeYearInEra(Calendar calendar, int era)
        {
            return MaxYearInEra(calendar, era) - MaxYearInEra(calendar, era - 1) + 1;
        }

        private static int MaxYearInEra(Calendar calendar, int era)
        {
            if (era == 0) { return MinYearInEra(calendar, era) - YearOffset(calendar); }

            int oldEra = era;
            int currentEra = era;
            for (int year = calendar.MinSupportedDateTime.Year; year < calendar.MaxSupportedDateTime.Year; year++)
            {
                currentEra = calendar.GetEra(new DateTime(year, 1, 1));
                if (currentEra > oldEra)
                {
                    return year - 1;
                }
            }
            return calendar.MaxSupportedDateTime.Year + YearOffset(calendar);
        }

        private static int MinYearInEra(Calendar calendar, int era)
        {
            int oldEra = era;
            int currentEra = era;
            for (int year = calendar.MaxSupportedDateTime.Year; year > calendar.MinSupportedDateTime.Year; year--)
            {
                currentEra = calendar.GetEra(new DateTime(year, 1, 1));
                if (currentEra < oldEra)
                {
                    return year + 1;
                }
            }
            return calendar.MinSupportedDateTime.Year + YearOffset(calendar);
        }

        public static IEnumerable<object[]> Calendars_TestData()
        {
            foreach (Calendar calendar in s_calendars)
            {
                yield return new object[] { calendar };
            }
        }

        public static IEnumerable<object[]> Year_Era_TestData()
        {
            foreach (Calendar calendar in s_calendars)
            {
                foreach (int era in calendar.Eras)
                {
                    // Year is invalid
                    yield return new object[] { calendar, -1, era, "year" };
                    yield return new object[] { calendar, 0, era, "year" };
                    yield return new object[] { calendar, MaxRelativeYearInEra(calendar, era) + 1, era, "year" };
                }
                // Era is invalid
                yield return new object[] { calendar, YearOffset(calendar) + 1, MinEra(calendar) - 2, "era" };
                yield return new object[] { calendar, YearOffset(calendar) + 1, MaxEra(calendar) + 1, "era" };
            }
        }

        public static IEnumerable<object[]> Year_Month_Era_TestData()
        {
            int month = 1;
            foreach (Calendar calendar in s_calendars)
            {
                foreach (int era in calendar.Eras)
                {
                    int year = MaxRelativeYearInEra(calendar, era);

                    // Year is invalid
                    yield return new object[] { calendar, -1, month, era, "year" };
                    yield return new object[] { calendar, 0, month, era, "year" };
                    yield return new object[] { calendar, MaxRelativeYearInEra(calendar, era) + 1, month, era, "year" };

                    // Month is invalid
                    yield return new object[] { calendar, year, -1, era, "month" };
                    yield return new object[] { calendar, year, 0, era, "month" };
                    yield return new object[] { calendar, year, calendar.GetMonthsInYear(year, era) + 1, era, "month" };
                }
                // Era is invalid
                yield return new object[] { calendar, YearOffset(calendar) + 1, month, MinEra(calendar) - 2, "era" };
                yield return new object[] { calendar, YearOffset(calendar) + 1, month, MaxEra(calendar) + 1, "era" };
            }
        }

        public static IEnumerable<object[]> Year_Month_Day_Era_TestData()
        {
            int month = 1;
            int day = 1;
            foreach (Calendar calendar in s_calendars)
            {
                foreach (int era in calendar.Eras)
                {
                    int year = MaxRelativeYearInEra(calendar, era);

                    // Year is invalid
                    yield return new object[] { calendar, -1, month, day, era, "year" };
                    yield return new object[] { calendar, 0, month, day, era, "year" };
                    yield return new object[] { calendar, MaxRelativeYearInEra(calendar, era) + 1, month, day, era, "year" };

                    // Month is invalid
                    yield return new object[] { calendar, year, -1, day, era, "month" };
                    yield return new object[] { calendar, year, 0, day, era, "month" };
                    //yield return new object[] { calendar, year, calendar.GetMonthsInYear(year, era) + 1, day, era, "month" };

                    // Day is invalid
                    yield return new object[] { calendar, year, month, -1, era, "day" };
                    yield return new object[] { calendar, year, month, 0, era, "day" };
                    yield return new object[] { calendar, year, month, calendar.GetDaysInMonth(year, month, era) + 1, era, "day" };
                }
                // Era is invalid
                yield return new object[] { calendar, YearOffset(calendar) + 1, month, day, MinEra(calendar) - 2, "era" };
                yield return new object[] { calendar, YearOffset(calendar) + 1, month, day, MaxEra(calendar) + 1, "era" };
            }
        }

        [Theory]
        [MemberData(nameof(Year_Era_TestData))]
        public static void GetDaysInYear_Invalid(Calendar calendar, int year, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.GetDaysInYear(year, era));
        }

        [Theory]
        [MemberData(nameof(Year_Era_TestData))]
        public static void GetMonthsInYear_Invalid(Calendar calendar, int year, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.GetMonthsInYear(year, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Era_TestData))]
        public static void GetDaysInMonth_Invalid(Calendar calendar, int year, int month, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.GetDaysInMonth(year, month, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Day_Era_TestData))]
        public static void IsLeapDay_Invalid(Calendar calendar, int year, int month, int day, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.IsLeapDay(year, month, day, era));
        }

        [Theory]
        [MemberData(nameof(Year_Month_Era_TestData))]
        public static void IsLeapMonth_Invalid(Calendar calendar, int year, int month, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.IsLeapMonth(year, month, era));
        }

        [Theory]
        [MemberData(nameof(Year_Era_TestData))]
        public static void IsLeapYear_Invalid(Calendar calendar, int year, int era, string exceptionParamName)
        {
            Assert.Throws<ArgumentOutOfRangeException>(exceptionParamName, () => calendar.IsLeapYear(year, era));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddYears_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddYears(calendar.MaxSupportedDateTime, 1));
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddYears(calendar.MinSupportedDateTime, -1)); // JapaneseCalendar throws ArgumentException
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void AddMonths_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddMonths(calendar.MaxSupportedDateTime, 1));
            Assert.ThrowsAny<ArgumentException>(() => calendar.AddMonths(calendar.MinSupportedDateTime, -1)); // JapaneseCalendar throws ArgumentException

            Assert.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, -120001));
            Assert.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, 120001));
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
                int year = MaxRelativeYearInEra(calendar, era) - 2;

                // Year is invalid
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(-1, month, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(0, month, day, hour, minute, second, millisecond, era));
                Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(MaxRelativeYearInEra(calendar, era) + 1, month, day, hour, minute, second, millisecond, era));

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
            // Era is invalid
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(YearOffset(calendar) + 1, month, day, hour, minute, second, millisecond, MinEra(calendar) - 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(YearOffset(calendar) + 1, month, day, hour, minute, second, millisecond, MaxEra(calendar) + 1));

            // New date is out of range
            DateTime minDateTime = calendar.MinSupportedDateTime;
            int minEra = calendar.GetEra(minDateTime);
            int minYear = MinYearInEra(calendar, minEra);

            DateTime maxDateTime = calendar.MaxSupportedDateTime;
            int maxEra = calendar.GetEra(maxDateTime);
            int maxYear = MaxRelativeYearInEra(calendar, maxEra);

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear - 1, minDateTime.Month, minDateTime.Day, minDateTime.Hour, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear + 1, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear, minDateTime.Month - 1, minDateTime.Day, minDateTime.Hour, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear, maxDateTime.Month + 1, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear, minDateTime.Month, minDateTime.Day - 1, minDateTime.Hour, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear, maxDateTime.Month, maxDateTime.Day + 1, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear, minDateTime.Month, minDateTime.Day, minDateTime.Hour - 1, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour + 1, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear, minDateTime.Month, minDateTime.Day, minDateTime.Hour, minDateTime.Minute - 1, minDateTime.Second, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute + 1, maxDateTime.Second, maxDateTime.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear, minDateTime.Month, minDateTime.Day, minDateTime.Hour, minDateTime.Minute, minDateTime.Second - 1, minDateTime.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second + 1, maxDateTime.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minYear, minDateTime.Month, minDateTime.Day, minDateTime.Hour, minDateTime.Minute, minDateTime.Second, minDateTime.Millisecond - 1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxYear, maxDateTime.Month, maxDateTime.Day, maxDateTime.Hour, maxDateTime.Minute, maxDateTime.Second, maxDateTime.Millisecond + 1, maxEra));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void ToFourDigitYear_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(-1));
            // JapaneseCalandar allows any inputs below the max year
            if (!(calendar is JapaneseCalendar))
            {
                Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(MinYearInEra(calendar, MinEra(calendar)) - 1));
            }
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(MaxRelativeYearInEra(calendar, MaxEra(calendar)) + 1));
        }

        [Theory]
        [MemberData(nameof(Calendars_TestData))]
        public static void TwoDigitYearMax_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = 98);
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = calendar.MaxSupportedDateTime.Year + YearOffset(calendar) + 1);
        }
    }
}
