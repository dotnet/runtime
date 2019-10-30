// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarToDateTime
    {
        public static IEnumerable<object[]> ToDateTime_TestData()
        {
            yield return new object[] { 1, 1, 1, 0, 0, 0, 0 };
            yield return new object[] { 8088, 12, 31, 23, 59, 59, 999 };

            Random random = new Random(-55);
            yield return new object[] { TaiwanCalendarUtilities.RandomYear(), random.Next(1, 13), random.Next(1, 29), random.Next(0, 24), random.Next(0, 60), random.Next(0, 60), random.Next(0, 1000) };
        }

        [Theory]
        [MemberData(nameof(ToDateTime_TestData))]
        public void ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            DateTime expected = new DateTime(year + 1911, month, day, hour, minute, second, millisecond);
            Assert.Equal(expected, calendar.ToDateTime(year, month, day, hour, minute, second, millisecond));
            Assert.Equal(expected, calendar.ToDateTime(year, month, day, hour, minute, second, millisecond, 0));
            Assert.Equal(expected, calendar.ToDateTime(year, month, day, hour, minute, second, millisecond, 1));
        }
    }
}
