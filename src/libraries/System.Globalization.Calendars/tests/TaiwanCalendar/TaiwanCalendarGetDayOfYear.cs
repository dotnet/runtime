// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetDayOfYear
    {
        public static IEnumerable<object[]> GetDayOfYear_TestData()
        {
            yield return new object[] { new TaiwanCalendar().MinSupportedDateTime };
            yield return new object[] { new TaiwanCalendar().MaxSupportedDateTime };
            yield return new object[] { TaiwanCalendarUtilities.RandomDateTime() };
            yield return new object[] { new DateTime(2000, 12, 31) };
        }

        [Theory]
        [MemberData(nameof(GetDayOfYear_TestData))]
        public void GetDayOfYear(DateTime time)
        {
            long TicksPerMillisecond = 10000;
            long TicksPerSecond = TicksPerMillisecond * 1000;
            long TicksPerMinute = TicksPerSecond * 60;
            long TicksPerHour = TicksPerMinute * 60;
            long TicksPerDay = TicksPerHour * 24;
            int DaysPerYear = 365;
            int DaysPer4Years = DaysPerYear * 4 + 1;
            int DaysPer100Years = DaysPer4Years * 25 - 1;
            int DaysPer400Years = DaysPer100Years * 4 + 1;
            // n = number of days since 1/1/0001
            int n = (int)(time.Ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4)
                y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4)
                y1 = 3;
            // n = day number within year
            n -= y1 * DaysPerYear;

            int expected = n + 1;
            Assert.Equal(expected, new TaiwanCalendar().GetDayOfYear(time));
        }
    }
}
