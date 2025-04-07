// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendarWeekRule
    {
        public static IEnumerable<object[]> CalendarWeekRule_Get_TestData()
        {
            yield return new object[] { "invariant", CalendarWeekRule.FirstDay };
            yield return new object[] { "en-US", CalendarWeekRule.FirstDay };

            if (PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { "br-FR", DateTimeFormatInfoData.BrFRCalendarWeekRule() };
            }
            else
            {
                // "br-FR" is not presented in Browser's ICU. Let's test ru-RU instead.
                yield return new object[] { "ru-RU", CalendarWeekRule.FirstFourDayWeek };
            }
        }

        [Theory]
        [MemberData(nameof(CalendarWeekRule_Get_TestData))]
        public void CalendarWeekRuleTest(string cultureName, CalendarWeekRule expected)
        {
            var format = cultureName == "invariant" ? DateTimeFormatInfo.InvariantInfo : new CultureInfo(cultureName).DateTimeFormat;
            Assert.True(expected == format.CalendarWeekRule, $"Failed for culture: {cultureName}. Expected: {expected}, Actual: {format.CalendarWeekRule}");
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
