// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarIsLeapDay
    {
        public static IEnumerable<object[]> IsLeapDay_TestData()
        {
            int randomYear = TaiwanCalendarUtilities.RandomYear();
            int radomMonth = TaiwanCalendarUtilities.RandomMonth();
            int randomDay = TaiwanCalendarUtilities.RandomDay(randomYear, radomMonth);
            yield return new object[] { randomYear, radomMonth, randomDay, false };

            yield return new object[] { 2000 - 1911, 2, 29, true };
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
