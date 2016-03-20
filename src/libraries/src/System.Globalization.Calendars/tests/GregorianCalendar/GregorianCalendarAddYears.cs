// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarAddYears
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static IEnumerable<object[]> AddYears_TestData()
        {
            yield return new object[] { DateTime.Now, 0 };
            yield return new object[] { s_calendar.MinSupportedDateTime, 100 };
            yield return new object[] { s_calendar.MaxSupportedDateTime, -99 };
            yield return new object[] { new DateTime(s_randomDataGenerator.GetInt64(-55) % s_calendar.MaxSupportedDateTime.Ticks), 1 };

            // February in a leap year
            yield return new object[] { s_calendar.ToDateTime(2000, 2, 29, 10, 30, 24, 0), 13 };
            yield return new object[] { s_calendar.ToDateTime(1996, 2, 29, 10, 30, 24, 0), 4 };

            // Month other than February in a leap year
            yield return new object[] { s_calendar.ToDateTime(1996, 3, 29, 10, 30, 24, 0), 48 };

            // February in a common year
            yield return new object[] { s_calendar.ToDateTime(1999, 2, 28, 10, 30, 24, 0), 48 };
        }

        [Theory]
        [MemberData(nameof(AddYears_TestData))]
        public void AddYears(DateTime time, int years)
        {
            DateTime result = s_calendar.AddYears(time, years);

            int oldYear = s_calendar.GetYear(time);
            int oldMonth = s_calendar.GetMonth(time);
            int oldDay = s_calendar.GetDayOfMonth(time);
            long oldTicksOfDay = time.Ticks % TimeSpan.TicksPerDay;
            int newYear = s_calendar.GetYear(result);
            int newMonth = s_calendar.GetMonth(result);
            int newDay = s_calendar.GetDayOfMonth(result);
            long newTicksOfDay = result.Ticks % TimeSpan.TicksPerDay;
            Assert.Equal(oldTicksOfDay, newTicksOfDay);
            Assert.False(newDay > oldDay);
            Assert.False(newYear != oldYear + years);
        }
    }
}
