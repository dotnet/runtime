// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarTwoDigitYearMax
    {
        [Fact]
        public void TwoDigitYearMax_Get()
        {
            var calendar = new ThaiBuddhistCalendar();
            Assert.True(calendar.TwoDigitYearMax == 2029 + 543 || calendar.TwoDigitYearMax == 2049 + 543, $"Unexpected calendar.TwoDigitYearMax {calendar.TwoDigitYearMax}");
        }

        public static IEnumerable<object[]> TwoDigitYearMax_Set_TestData()
        {
            yield return new object[] { new Random(-55).Next(99, 10542) };
            yield return new object[] { 10542 };
            yield return new object[] { 99 };
        }

        [Theory]
        [MemberData(nameof(TwoDigitYearMax_Set_TestData))]
        public void TwoDigitYearMax_Set(int newTwoDigitYearMax)
        {
            ThaiBuddhistCalendar calendar = new ThaiBuddhistCalendar();
            calendar.TwoDigitYearMax = newTwoDigitYearMax;
            Assert.Equal(newTwoDigitYearMax, calendar.TwoDigitYearMax);
        }
    }
}
