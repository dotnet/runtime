// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarHelpers
    {
        private static int MaxEra(Calendar calendar) => calendar.GetEra(calendar.MaxSupportedDateTime);
        private static int MinEra(Calendar calendar) => calendar.GetEra(calendar.MinSupportedDateTime);

        private static int YearOffset(Calendar calendar) => calendar.GetYear(calendar.MinSupportedDateTime) - 1;

        public static void AddYears_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddYears(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddYears(calendar.MinSupportedDateTime, -1));
        }

        public static void AddMonths_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddMonths(calendar.MaxSupportedDateTime, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.AddMonths(calendar.MinSupportedDateTime, -1));

            Assert.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, -120001));
            Assert.Throws<ArgumentOutOfRangeException>("months", () => calendar.AddMonths(DateTime.Now, 120001));
        }

        public static void GetDaysInYear_Invalid(Calendar calendar)
        {
            int yearOffset = YearOffset(calendar);
            int minEra = MinEra(calendar);
            int maxEra = MaxEra(calendar);

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInYear(-1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInYear(calendar.MinSupportedDateTime.Year + yearOffset - 1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInYear(calendar.MaxSupportedDateTime.Year + yearOffset + 1, maxEra));

            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInYear(yearOffset + 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInYear(yearOffset + 1, maxEra + 1));
        }

        public static void GetDaysInMonth_Invalid(Calendar calendar)
        {
            int yearOffset = YearOffset(calendar);
            int minEra = MinEra(calendar);
            int maxEra = MaxEra(calendar);
            int currentEra = calendar.GetEra(calendar.ToDateTime(yearOffset + 1, 1, 1, 1, 1, 1, 1));

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(-1, 10, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(0, 10, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(calendar.MinSupportedDateTime.Year + yearOffset - 1, 10, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(calendar.MaxSupportedDateTime.Year + yearOffset + 1, 10, maxEra));

            // Month is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("month", () => calendar.GetDaysInMonth(yearOffset + 1, -1, currentEra));
            Assert.Throws<ArgumentOutOfRangeException>("month", () => calendar.GetDaysInMonth(yearOffset + 1, calendar.MinSupportedDateTime.Month - 1, currentEra));
            Assert.Throws<ArgumentOutOfRangeException>("month", () => calendar.GetDaysInMonth(yearOffset + 1, calendar.MaxSupportedDateTime.Month + 1, currentEra));

            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInMonth(yearOffset + 1, 10, -1));
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInMonth(yearOffset + 1, 10, maxEra + 1));
        }

        public static void GetMonthsInYear_Invalid(Calendar calendar)
        {
            int yearOffset = YearOffset(calendar);
            int minEra = MinEra(calendar);
            int maxEra = MaxEra(calendar);

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(-1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(0, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(calendar.MinSupportedDateTime.Year + yearOffset - 1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(calendar.MaxSupportedDateTime.Year + yearOffset + 1, maxEra));
            
            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetMonthsInYear(yearOffset + 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetMonthsInYear(yearOffset + 1, maxEra + 1));
        }

        public static void GetWeekOfYear_Invalid(Calendar calendar)
        {
            // Rule is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("rule", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay - 1, DayOfWeek.Saturday));
            Assert.Throws<ArgumentOutOfRangeException>("rule", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstFourDayWeek + 1, DayOfWeek.Saturday));

            // FirstDayOfWeek is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("firstDayOfWeek", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday - 1));
            Assert.Throws<ArgumentOutOfRangeException>("firstDayOfWeek", () => calendar.GetWeekOfYear(calendar.MaxSupportedDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Saturday + 1));
        }

        public static void ToDateTime_Invalid(Calendar calendar)
        {
            int yearOffset = YearOffset(calendar);

            DateTime maxDate = calendar.MaxSupportedDateTime;
            DateTime minDate = calendar.MinSupportedDateTime;

            int maxEra = MinEra(calendar);
            int minEra = MaxEra(calendar);

            DateTime currentDate = DateTime.Now;

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToDateTime(-1, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToDateTime(0, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToDateTime(minDate.Year + yearOffset - 1, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToDateTime(maxDate.Year + yearOffset + 1, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, maxEra));

            // Month is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month - 1, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month + 1, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, -1, currentDate.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, 0, currentDate.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, 13, currentDate.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond, maxEra));

            // Day is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month, minDate.Day - 1, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day + 1, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, -1, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond, maxEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, 0, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond, maxEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, 32, currentDate.Hour, currentDate.Minute, currentDate.Second, currentDate.Millisecond, maxEra));

            // Hour is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour - 1, minDate.Minute, minDate.Second, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour + 1, maxDate.Minute, maxDate.Second, maxDate.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, -1, currentDate.Minute, currentDate.Second, currentDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, 60, currentDate.Minute, currentDate.Second, currentDate.Millisecond, maxEra));

            // Minute is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute - 1, minDate.Second, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute + 1, maxDate.Second, maxDate.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, -1, currentDate.Second, currentDate.Millisecond, maxEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, 60, currentDate.Second, currentDate.Millisecond, maxEra));

            // Second is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second - 1, minDate.Millisecond, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second + 1, maxDate.Millisecond, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, -1, currentDate.Millisecond, maxEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, 60, currentDate.Millisecond, maxEra));

            // Millisecond is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond - 1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond + 1, maxEra));

            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, -1, minEra));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, currentDate.Second, 1000, maxEra));

            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, MaxEra(calendar) + 1));
        }

        public static void ToFourDigitYear_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(-1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(calendar.MinSupportedDateTime.Year + YearOffset(calendar) - 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.ToFourDigitYear(calendar.MaxSupportedDateTime.Year + YearOffset(calendar) + 1));
        }

        public static void TwoDigitYearMax_Invalid(Calendar calendar)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = 98);
            Assert.Throws<ArgumentOutOfRangeException>(() => calendar.TwoDigitYearMax = calendar.MaxSupportedDateTime.Year + YearOffset(calendar) + 1);
        }
    }
}
