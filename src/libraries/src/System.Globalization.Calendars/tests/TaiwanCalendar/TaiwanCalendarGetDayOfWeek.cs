// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.GetDayOfWeek(DateTime)
    public class TaiwanCalendarGetDayOfWeek
    {
        private readonly int[] _DAYS_PER_MONTHS_IN_LEAP_YEAR = new int[13]
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private readonly int[] _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR = new int[13]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region PositiveTesting
        // PosTest1: Verify the day is a random Date
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int day;
            if (tc.IsLeapYear(year))
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_LEAP_YEAR[month] + 1);
            }
            else
            {
                day = rand.Next(1, _DAYS_PER_MONTHS_IN_NO_LEAP_YEAR[month] + 1);
            }

            DateTime dt = new DateTime(year, month, day);
            DayOfWeek actualDay = getDayOfWeek(dt);
            Assert.Equal(tc.GetDayOfWeek(dt), actualDay);
        }

        // PosTest2: Verify the DateTime is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            Assert.Equal(tc.GetDayOfWeek(dt), DayOfWeek.Monday);
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            Assert.Equal(tc.GetDayOfWeek(dt), DayOfWeek.Friday);
        }
        #endregion

        #region Helper Methods
        public DayOfWeek getDayOfWeek(DateTime time)
        {
            long TicksPerMillisecond = 10000;
            long TicksPerSecond = TicksPerMillisecond * 1000;
            long TicksPerMinute = TicksPerSecond * 60;
            long TicksPerHour = TicksPerMinute * 60;
            long TicksPerDay = TicksPerHour * 24;
            ;
            return ((DayOfWeek)((time.Ticks / TicksPerDay + 1) % 7));
        }
        #endregion
    }
}