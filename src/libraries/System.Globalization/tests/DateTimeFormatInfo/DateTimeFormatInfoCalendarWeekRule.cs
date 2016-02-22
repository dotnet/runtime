// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendarWeekRule
    {
        public static IEnumerable<object[]> CalendarWeekRule_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, CalendarWeekRule.FirstDay };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, CalendarWeekRule.FirstDay };
            yield return new object[] { new CultureInfo("br-FR").DateTimeFormat, DateTimeFormatInfoData.BrFRCalendarWeekRule() };
        }

        [Theory]
        [MemberData(nameof(CalendarWeekRule_TestData))]
        public void CalendarWeekRuleTest(DateTimeFormatInfo format, CalendarWeekRule expected)
        {
            Assert.Equal(expected, format.CalendarWeekRule);
        }

        [Theory]
        [InlineData(CalendarWeekRule.FirstDay)]
        [InlineData(CalendarWeekRule.FirstFourDayWeek)]
        [InlineData(CalendarWeekRule.FirstFullWeek)]
        public void CalendarWeekRule_Set(CalendarWeekRule newCalendarWeekRule)
        {
            var format = new DateTimeFormatInfo();
            format.CalendarWeekRule = newCalendarWeekRule;
            Assert.Equal(newCalendarWeekRule, format.CalendarWeekRule);
        }

        [Fact]
        public void CalendarWeekRule_Set_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DateTimeFormatInfo().CalendarWeekRule = (CalendarWeekRule)(-1)); // Value is invalid
        }
    }
}
