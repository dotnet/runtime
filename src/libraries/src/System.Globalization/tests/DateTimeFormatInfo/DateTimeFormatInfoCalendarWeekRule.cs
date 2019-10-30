// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendarWeekRule
    {
        public static IEnumerable<object[]> CalendarWeekRule_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, CalendarWeekRule.FirstDay };
            yield return new object[] { new CultureInfo("en-US").DateTimeFormat, CalendarWeekRule.FirstDay };
            yield return new object[] { new CultureInfo("br-FR").DateTimeFormat, DateTimeFormatInfoData.BrFRCalendarWeekRule() };
        }

        [Theory]
        [MemberData(nameof(CalendarWeekRule_Get_TestData))]
        public void CalendarWeekRuleTest(DateTimeFormatInfo format, CalendarWeekRule expected)
        {
            Assert.Equal(expected, format.CalendarWeekRule);
        }

        [Theory]
        [InlineData(CalendarWeekRule.FirstDay)]
        [InlineData(CalendarWeekRule.FirstFourDayWeek)]
        [InlineData(CalendarWeekRule.FirstFullWeek)]
        public void CalendarWeekRule_Set_GetReturnsExpected(CalendarWeekRule value)
        {
            var format = new DateTimeFormatInfo();
            format.CalendarWeekRule = value;
            Assert.Equal(value, format.CalendarWeekRule);
        }

        [Theory]
        [InlineData(CalendarWeekRule.FirstDay - 1)]
        [InlineData(CalendarWeekRule.FirstFourDayWeek + 1)]
        public void CalendarWeekRule_SetInvalidValue_ThrowsArgumentOutOfRangeException(CalendarWeekRule value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => format.CalendarWeekRule = value);
        }

        [Fact]
        public void CalendarWeekRule_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.CalendarWeekRule = CalendarWeekRule.FirstDay);
        }
    }
}
