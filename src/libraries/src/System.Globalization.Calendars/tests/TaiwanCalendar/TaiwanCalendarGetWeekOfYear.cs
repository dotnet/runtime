// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetWeekOfYear
    {
        public static IEnumerable<object[]> GetWeekOfYear_TestData()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    yield return new object[] { new TaiwanCalendar().MinSupportedDateTime, (CalendarWeekRule)i, (DayOfWeek)j };
                    yield return new object[] { new TaiwanCalendar().MaxSupportedDateTime, (CalendarWeekRule)i, (DayOfWeek)j };
                    yield return new object[] { TaiwanCalendarUtilities.RandomDateTime(), (CalendarWeekRule)i, (DayOfWeek)j };
                }
            }
        }
        
        [Theory]
        [MemberData(nameof(GetWeekOfYear_TestData))]
        public void GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            Assert.InRange(rule, (CalendarWeekRule)0, (CalendarWeekRule)3);
            int expected = 0;
            if (rule == CalendarWeekRule.FirstDay)
            {
                expected = GetWeekOfYearFirstDay(time, (int)firstDayOfWeek);
            }
            else if (rule == CalendarWeekRule.FirstFullWeek)
            {
                expected = GetWeekOfYearFullDays(time, rule, (int)firstDayOfWeek, 7);
            }
            else
            {
                expected = GetWeekOfYearFullDays(time, rule, (int)firstDayOfWeek, 4);
            }
            Assert.Equal(expected, new TaiwanCalendar().GetWeekOfYear(time, rule, firstDayOfWeek));
        }
        
        [Fact]
        public void GetWeekOfYear_LastDayOfYear()
        {
            DateTime time = new DateTime(TaiwanCalendarUtilities.RandomYear(), 12, 31);

            int expected = 53;
            if (new DateTime(time.Year, 1, 1).DayOfWeek == DayOfWeek.Saturday && DateTime.IsLeapYear(time.Year))
            {
                expected = 54;
            }
            Assert.Equal(expected, new TaiwanCalendar().GetWeekOfYear(time, CalendarWeekRule.FirstDay, DayOfWeek.Sunday));
        }
        
        internal int GetWeekOfYearFirstDay(DateTime time, int firstDayOfWeek)
        {
            Calendar gc = new GregorianCalendar();
            // Make the day of year to be 0-based, so that 1/1 is day 0.
            int dayOfYear = gc.GetDayOfYear(time) - 1;
            // Calculate the day of week for the first day of the year.
            // dayOfWeek - (dayOfYear % 7) is the day of week for the first day of this year.  Note that
            // this value can be less than 0.  It's fine since we are making it positive again in calculating offset.
            int dayForJan1 = (int)gc.GetDayOfWeek(time) - (dayOfYear % 7);
            int offset = (dayForJan1 - firstDayOfWeek + 14) % 7;
            return (dayOfYear + offset) / 7 + 1;
        }

        private int GetWeekOfYearFullDays(DateTime time, CalendarWeekRule rule, int firstDayOfWeek, int fullDays)
        {
            GregorianCalendar gregorianCalendar = new GregorianCalendar();
            // Make the day of year to be 0-based, so that 1/1 is day 0.
            int dayOfYear = gregorianCalendar.GetDayOfYear(time) - 1; 
            //
            // Calculate the number of days between the first day of year (1/1) and the first day of the week.
            // This value will be a positive value from 0 ~ 6.  We call this value as "offset".
            //
            // If offset is 0, it means that the 1/1 is the start of the first week.
            //     Assume the first day of the week is Monday, it will look like this:
            //     Sun      Mon     Tue     Wed     Thu     Fri     Sat
            //     12/31    1/1     1/2     1/3     1/4     1/5     1/6
            //              +--> First week starts here.
            //
            // If offset is 1, it means that the first day of the week is 1 day ahead of 1/1.
            //     Assume the first day of the week is Monday, it will look like this:
            //     Sun      Mon     Tue     Wed     Thu     Fri     Sat
            //     1/1      1/2     1/3     1/4     1/5     1/6     1/7
            //              +--> First week starts here.
            //
            // If offset is 2, it means that the first day of the week is 2 days ahead of 1/1.
            //     Assume the first day of the week is Monday, it will look like this:
            //     Sat      Sun     Mon     Tue     Wed     Thu     Fri     Sat
            //     1/1      1/2     1/3     1/4     1/5     1/6     1/7     1/8
            //                      +--> First week starts here.

            // Day of week is 0-based.
            // Get the day of week for 1/1.  This can be derived from the day of week of the target day.
            // Note that we can get a negative value.  It's ok since we are going to make it a positive value when calculating the offset.
            int dayForJan1 = (int)gregorianCalendar.GetDayOfWeek(time) - (dayOfYear % 7);

            // Now, calculate the offset.  Subtract the first day of week from the dayForJan1.  And make it a positive value.
            int offset = (firstDayOfWeek - dayForJan1 + 14) % 7;
            if (offset != 0 && offset >= fullDays)
            {
                // If the offset is greater than the value of fullDays, it means that
                // the first week of the year starts on the week where Jan/1 falls on.
                offset -= 7;
            }
            // Calculate the day of year for specified time by taking offset into account.
            int day = dayOfYear - offset;
            if (day >= 0)
            {
                // If the day of year value is greater than zero, get the week of year.
                return (day / 7 + 1);
            }

            // Otherwise, the specified time falls on the week of previous year.
            // Call this method again by passing the last day of previous year.
            return GetWeekOfYearFullDays(time.AddDays(-(dayOfYear + 1)), rule, firstDayOfWeek, fullDays);
        }
    }
}
