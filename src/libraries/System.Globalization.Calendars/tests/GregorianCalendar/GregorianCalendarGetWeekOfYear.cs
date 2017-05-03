// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

using static System.Globalization.Tests.GregorianCalendarTestUtilities;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetWeekOfYears
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        private static readonly int[] s_daysInMonthInCommonYear = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static readonly int[] s_daysInMonthInLeapYear = { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        private const int DaysPerWeek = 7;

        public static CalendarWeekRule RandomCalendarWeekRule() => (CalendarWeekRule)(s_randomDataGenerator.GetInt32(-55) % 3);
        public static DayOfWeek RandomDayOfWeek() => (DayOfWeek)(s_randomDataGenerator.GetInt32(-55) % 7);

        public static IEnumerable<object[]> GetWeekOfYear_TestData()
        {
            int randomMonth = RandomMonth();
            int randomMonthNotFebruary = RandomMonthNotFebruary();

            // Any day in any month other than February in a leap year
            yield return new object[] { new DateTime(RandomLeapYear(), randomMonthNotFebruary, RandomLeapYearDay(randomMonthNotFebruary)), RandomCalendarWeekRule(), RandomDayOfWeek() };

            // Any day in February in a leap year
            yield return new object[] { new DateTime(RandomLeapYear(), 2, RandomLeapYearDay(2)), RandomCalendarWeekRule(), RandomDayOfWeek() };

            // Any day in February in a common year
            yield return new object[] { new DateTime(RandomCommonYear(), 2, RandomCommonYearDay(2)), RandomCalendarWeekRule(), RandomDayOfWeek() };

            // Any day in any month other than February in a common year
            yield return new object[] { new DateTime(RandomCommonYear(), randomMonthNotFebruary, RandomCommonYearDay(randomMonthNotFebruary)), RandomCalendarWeekRule(), RandomDayOfWeek() };

            // Any day in any month in the maximum supported year
            yield return new object[] { new DateTime(9999, randomMonth, RandomCommonYearDay(randomMonth)), RandomCalendarWeekRule(), RandomDayOfWeek() };

            // Any day in any month in the minimum supported year
            yield return new object[] { new DateTime(1, randomMonth, RandomCommonYearDay(randomMonth)), RandomCalendarWeekRule(), RandomDayOfWeek() };

            // Any day in any month in any year
            int randomYear = RandomYear();
            yield return new object[] { new DateTime(randomYear, randomMonth, RandomDay(randomYear, randomMonth)), RandomCalendarWeekRule(), RandomDayOfWeek() };
        }

        [Theory]
        [MemberData(nameof(GetWeekOfYear_TestData))]
        public void GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            Calendar calendar = new GregorianCalendar();
            Assert.Equal(GetDayOfYearHelper(calendar, time, rule, firstDayOfWeek), calendar.GetDayOfYear(time));
        }

        private static int GetDayOfYearHelper(Calendar calendar, DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            int weekOfYear = calendar.GetWeekOfYear(time, rule, firstDayOfWeek);
            int dayOfYear = calendar.GetDayOfYear(time); // 1-based
            int dayOfWeek = calendar.GetDayOfWeek(time) - firstDayOfWeek + 1; // 1-based
            if (dayOfWeek <= 0)
                dayOfWeek += DaysPerWeek; // Make it a positive value
            int dayOfWeekForJan1 = dayOfWeek - (dayOfYear - 1) % DaysPerWeek; // 1-based
            if (dayOfWeekForJan1 <= 0)
                dayOfWeekForJan1 += DaysPerWeek; // Make it a positive value
            
            // When the day of specific time falls on the previous year,
            // return the number of days from January 1 directly.
            // There could be 6 weeks within a month.
            if (time.Month == 1 && weekOfYear > 6)
            {
                return dayOfWeek - dayOfWeekForJan1 + 1;
            }

            int offset = 0;
            switch (rule)
            {
                case CalendarWeekRule.FirstDay:
                    offset = dayOfWeek - dayOfWeekForJan1;
                    break;
                case CalendarWeekRule.FirstFourDayWeek:
                    if (dayOfWeekForJan1 <= 4)
                    {
                        offset = dayOfWeek - dayOfWeekForJan1;
                    }
                    else
                    {
                        offset = dayOfWeek + DaysPerWeek - dayOfWeekForJan1;
                    }

                    break;
                case CalendarWeekRule.FirstFullWeek:
                    if (dayOfWeekForJan1 == 1)
                    {
                        offset = dayOfWeek - dayOfWeekForJan1;
                    }
                    else
                    {
                        offset = dayOfWeek + DaysPerWeek - dayOfWeekForJan1;
                    }

                    break;
            }
            return (weekOfYear - 1) * DaysPerWeek + offset + 1;;
        }
    }
}
