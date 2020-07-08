// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarGetWeekOfYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetWeekOfYear_TestData()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    yield return new object[] { DateTime.MinValue, (CalendarWeekRule)i, (DayOfWeek)j };
                    yield return new object[] { DateTime.MaxValue, (CalendarWeekRule)i, (DayOfWeek)j };
                    yield return new object[] { s_randomDataGenerator.GetDateTime(-55), (CalendarWeekRule)i, (DayOfWeek)j };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetWeekOfYear_TestData))]
        public void GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            int expected = new GregorianCalendar().GetWeekOfYear(time, rule, firstDayOfWeek);
            Assert.Equal(expected, new ThaiBuddhistCalendar().GetWeekOfYear(time, rule, firstDayOfWeek));
        }

        [Fact]
        public void GetWeekOfYear_LastDayOfYear()
        {
            DateTimeFormatInfo thaiDateFormat = new CultureInfo("th-TH").DateTimeFormat;
            DateTime time = new DateTime(new Random(-55).Next(1, 9999), 12, 31);
            Assert.Equal(53, new ThaiBuddhistCalendar().GetWeekOfYear(time, thaiDateFormat.CalendarWeekRule, thaiDateFormat.FirstDayOfWeek));
        }
    }
}
