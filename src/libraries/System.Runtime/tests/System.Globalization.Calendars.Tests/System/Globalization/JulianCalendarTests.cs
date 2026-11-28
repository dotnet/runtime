// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class JulianCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new JulianCalendar();

        [Theory]
        [InlineData(1700, 1, 31, 1, 1700, 2, 29)]
        [InlineData(1800, 3, 31, -1, 1800, 2, 29)]
        [InlineData(1900, 2, 29, 0, 1900, 2, 29)]
        [InlineData(1900, 1, 28, 1, 1900, 2, 28)]
        [InlineData(1901, 1, 31, 1, 1901, 2, 28)]
        [InlineData(2000, 1, 31, 1, 2000, 2, 29)]
        [InlineData(1900, 12, 31, 2, 1901, 2, 28)]
        [InlineData(1901, 1, 31, -2, 1900, 11, 30)]
        public void AddMonths(int year, int month, int day, int months, int expectedYear, int expectedMonth, int expectedDay)
        {
            var calendar = new JulianCalendar();
            DateTime date = calendar.ToDateTime(year, month, day, 12, 34, 56, 789).AddTicks(1);
            DateTime expected = calendar.ToDateTime(expectedYear, expectedMonth, expectedDay, 12, 34, 56, 789).AddTicks(1);
            Assert.Equal(expected, calendar.AddMonths(date, months));
        }

        [Theory]
        [InlineData(1696, 2, 29, 4, 1700, 2, 29)]
        [InlineData(1804, 2, 29, -4, 1800, 2, 29)]
        [InlineData(1900, 2, 29, 0, 1900, 2, 29)]
        [InlineData(1900, 2, 29, 1, 1901, 2, 28)]
        [InlineData(1904, 2, 29, -1, 1903, 2, 28)]
        [InlineData(1900, 12, 31, 1, 1901, 12, 31)]
        public void AddYears(int year, int month, int day, int years, int expectedYear, int expectedMonth, int expectedDay)
        {
            var calendar = new JulianCalendar();
            DateTime date = calendar.ToDateTime(year, month, day, 12, 34, 56, 789).AddTicks(1);
            DateTime expected = calendar.ToDateTime(expectedYear, expectedMonth, expectedDay, 12, 34, 56, 789).AddTicks(1);
            Assert.Equal(expected, calendar.AddYears(date, years));
        }
    }
}
