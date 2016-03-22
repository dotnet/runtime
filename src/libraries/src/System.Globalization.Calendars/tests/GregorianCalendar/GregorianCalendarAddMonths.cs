// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarAddMonths
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        private const int MinMonths = -120000;
        private const int MaxMonths = 120000;

        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { DateTime.Now, 0 };
            yield return new object[] { s_calendar.MinSupportedDateTime, s_randomDataGenerator.GetInt32(-55) % MaxMonths + 1 };
            yield return new object[] { s_calendar.MaxSupportedDateTime, -1 * s_randomDataGenerator.GetInt32(-55) % MaxMonths - 1 };
            yield return new object[] { new DateTime(s_randomDataGenerator.GetInt64(-55) % (s_calendar.MaxSupportedDateTime.Ticks + 1)), -1 };
            yield return new object[] { s_calendar.ToDateTime(2000, 2, 29, 10, 30, 24, 0), 13 }; // February in a leap year
            yield return new object[] { s_calendar.ToDateTime(1996, 2, 29, 10, 30, 24, 0), 48 }; // February in a leap year
            yield return new object[] { s_calendar.ToDateTime(1996, 3, 29, 10, 30, 24, 0), 48 }; // Other month in a leap year
            yield return new object[] { s_calendar.ToDateTime(1999, 2, 28, 10, 30, 24, 0), 48 }; // February in a common year
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths(DateTime time, int months)
        {
            DateTime result = s_calendar.AddMonths(time, months);

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
            Assert.False(newYear * 12 + newMonth != oldYear * 12 + oldMonth + months);
        }
    }
}
