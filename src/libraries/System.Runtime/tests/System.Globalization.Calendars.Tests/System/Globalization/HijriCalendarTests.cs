// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class HijriCalendarTests : CalendarTestBase
    {
        public override Calendar Calendar => new HijriCalendar();

        public override DateTime MinSupportedDateTime => new DateTime(0622, 07, 20);

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;

        [Theory]
        [InlineData(-2)]
        [InlineData(0)]
        [InlineData(2)]
        public void HijriAdjustment_SupportedRange(int adjustment)
        {
            var calendar = new HijriCalendar { HijriAdjustment = adjustment };

            Assert.Equal(1, calendar.GetYear(calendar.MinSupportedDateTime));
            Assert.Equal(1, calendar.GetMonth(calendar.MinSupportedDateTime));
            Assert.Equal(3 + adjustment, calendar.GetDayOfMonth(calendar.MinSupportedDateTime));
            Assert.Equal(3 + adjustment, calendar.GetDayOfYear(calendar.MinSupportedDateTime));

            Assert.Equal(9666, calendar.GetYear(calendar.MaxSupportedDateTime));
            Assert.Equal(4, calendar.GetMonth(calendar.MaxSupportedDateTime));
            Assert.Equal(3 + adjustment, calendar.GetDayOfMonth(calendar.MaxSupportedDateTime));
            Assert.Equal(92 + adjustment, calendar.GetDayOfYear(calendar.MaxSupportedDateTime));
        }
    }
}
