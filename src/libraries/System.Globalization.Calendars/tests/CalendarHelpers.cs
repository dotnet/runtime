// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarHelpers
    {
        private static int MaxEra(Calendar calendar)
        {
            return 1;
        }

        private static int YearOffset(Calendar calendar)
        {
            if (calendar is KoreanCalendar)
            {
                return 2333;
            }
            if (calendar is ThaiBuddhistCalendar)
            {
                return 543;
            }
            return 0;
        }

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

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInYear(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInYear(calendar.MinSupportedDateTime.Year + yearOffset - 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInYear(calendar.MaxSupportedDateTime.Year + yearOffset + 1, 1));

            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInYear(yearOffset + 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInYear(yearOffset + 1, MaxEra(calendar) + 1));
        }

        public static void GetDaysInMonth_Invalid(Calendar calendar)
        {
            int yearOffset = YearOffset(calendar);

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(-1, 10, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(0, 10, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(calendar.MinSupportedDateTime.Year + yearOffset - 1, 10, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetDaysInMonth(calendar.MaxSupportedDateTime.Year + yearOffset + 1, 10, 1));

            // Month is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("month", () => calendar.GetDaysInMonth(yearOffset + 1, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("month", () => calendar.GetDaysInMonth(yearOffset + 1, calendar.MinSupportedDateTime.Month - 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("month", () => calendar.GetDaysInMonth(yearOffset + 1, calendar.MaxSupportedDateTime.Month + 1, 1));

            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInMonth(yearOffset + 1, 10, -1));
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetDaysInMonth(yearOffset + 1, 10, MaxEra(calendar) + 1));
        }

        public static void GetMonthsInYear_Invalid(Calendar calendar)
        {
            int yearOffset = YearOffset(calendar);

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(calendar.MinSupportedDateTime.Year + yearOffset - 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => calendar.GetMonthsInYear(calendar.MaxSupportedDateTime.Year + yearOffset + 1, 1));
            
            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetMonthsInYear(yearOffset + 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("era", () => calendar.GetMonthsInYear(yearOffset + 1, MaxEra(calendar) + 1));
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

            DateTime maxDate = new ThaiBuddhistCalendar().MaxSupportedDateTime;
            DateTime minDate = new ThaiBuddhistCalendar().MinSupportedDateTime;

            // Year is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>("year", () => new ThaiBuddhistCalendar().ToDateTime(-1, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => new ThaiBuddhistCalendar().ToDateTime(0, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => new ThaiBuddhistCalendar().ToDateTime(minDate.Year + yearOffset - 1, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>("year", () => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year + yearOffset + 1, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, 1));

            // Month is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month - 1, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month + 1, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, 1));

            // Day is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month, minDate.Day - 1, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day + 1, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, 1));

            // Hour is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour - 1, minDate.Minute, minDate.Second, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour + 1, maxDate.Minute, maxDate.Second, maxDate.Millisecond, 1));

            // Minute is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute - 1, minDate.Second, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute + 1, maxDate.Second, maxDate.Millisecond, 1));

            // Second is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second - 1, minDate.Millisecond, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second + 1, maxDate.Millisecond, 1));

            // Millisecond is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond - 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond + 1, 1));

            // Era is outside supported range
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(maxDate.Year, maxDate.Month, maxDate.Day, maxDate.Hour, maxDate.Minute, maxDate.Second, maxDate.Millisecond, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ThaiBuddhistCalendar().ToDateTime(minDate.Year, minDate.Month, minDate.Day, minDate.Hour, minDate.Minute, minDate.Second, minDate.Millisecond, MaxEra(calendar)));
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
