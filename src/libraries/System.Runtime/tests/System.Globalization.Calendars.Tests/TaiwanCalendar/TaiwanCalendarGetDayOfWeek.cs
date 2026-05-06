// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetDayOfWeek
    {
        public static IEnumerable<object[]> GetDayOfWeek_TestData()
        {
            yield return new object[] { new TaiwanCalendar().MinSupportedDateTime };
            yield return new object[] { new TaiwanCalendar().MaxSupportedDateTime };
            yield return new object[] { TaiwanCalendarUtilities.RandomDateTime() };
        }

        [Theory]
        [MemberData(nameof(GetDayOfWeek_TestData))]
        public void GetDayOfWeek(DateTime time)
        {
            long TicksPerMillisecond = 10000;
            long TicksPerSecond = TicksPerMillisecond * 1000;
            long TicksPerMinute = TicksPerSecond * 60;
            long TicksPerHour = TicksPerMinute * 60;
            long TicksPerDay = TicksPerHour * 24;

            DayOfWeek expected = ((DayOfWeek)((time.Ticks / TicksPerDay + 1) % 7));
            Assert.Equal(expected, new TaiwanCalendar().GetDayOfWeek(time));
        }
    }
}
