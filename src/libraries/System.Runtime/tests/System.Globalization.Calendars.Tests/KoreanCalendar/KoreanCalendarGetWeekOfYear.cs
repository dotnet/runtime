// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarGetWeekOfYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetWeekOfYear_TestData()
        {
            CalendarWeekRule rule = new CultureInfo("ko-KR").DateTimeFormat.CalendarWeekRule;
            DayOfWeek firstDayOfWeek = new CultureInfo("ko-KR").DateTimeFormat.FirstDayOfWeek;

            yield return new object[] { DateTime.MinValue, rule, firstDayOfWeek };
            yield return new object[] { DateTime.MaxValue, rule, firstDayOfWeek };
            yield return new object[] { new DateTime(2008, 2, 29), rule, firstDayOfWeek };
            yield return new object[] { s_randomDataGenerator.GetDateTime(-55), rule, firstDayOfWeek };
        }

        [Theory]
        [MemberData(nameof(GetWeekOfYear_TestData))]
        public void GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            int expected = new GregorianCalendar().GetWeekOfYear(time, rule, firstDayOfWeek);
            Assert.Equal(expected, new KoreanCalendar().GetWeekOfYear(time, rule, firstDayOfWeek));
        }
    }
}
