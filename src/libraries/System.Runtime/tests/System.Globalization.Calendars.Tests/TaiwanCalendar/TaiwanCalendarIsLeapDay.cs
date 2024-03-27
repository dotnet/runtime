// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarIsLeapDay
    {
        public static IEnumerable<object[]> IsLeapDay_TestData()
        {
            yield return new object[] { 2000 - 1911, 2, 28, false };
            yield return new object[] { 2000 - 1911, 2, 29, true };
            yield return new object[] { 2000 - 1911, 3, 1, false };
            yield return new object[] { 2001 - 1911, 2, 28, false };
        }

        [Theory]
        [MemberData(nameof(IsLeapDay_TestData))]
        public void IsLeapDay(int year, int month, int day, bool expected)
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            Assert.Equal(expected, calendar.IsLeapDay(year, month, day));
            Assert.Equal(expected, calendar.IsLeapDay(year, month, day, 0));
            Assert.Equal(expected, calendar.IsLeapDay(year, month, day, 1));
        }
    }
}
