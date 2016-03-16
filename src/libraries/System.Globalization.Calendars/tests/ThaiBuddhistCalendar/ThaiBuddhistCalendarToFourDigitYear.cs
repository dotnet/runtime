// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarToFourDigitYear
    {
        public static IEnumerable<object[]> ToFourDigitYear_TestData()
        {
            yield return new object[] { 2029, new Random(-55).Next(1, 99) };
            yield return new object[] { 2029 + 543, new Random(-55).Next(544, 10542) };
            yield return new object[] { 2029 + 543, 10542 };
            yield return new object[] { 2029, 0 };
        }

        [Theory]
        [MemberData(nameof(ToFourDigitYear_TestData))]
        public void ToFourDigitYear(int originalTwoYearMax, int year)
        {
            ThaiBuddhistCalendar calendar = new ThaiBuddhistCalendar();
            calendar.TwoDigitYearMax = 2029;
            if (year > 99)
            {
                Assert.Equal(year, calendar.ToFourDigitYear(year));
            }
            else if (year > 29)
            {
                Assert.Equal(year + 1900, calendar.ToFourDigitYear(year));
            }
            else
            {
                Assert.Equal(year + 2000, calendar.ToFourDigitYear(year));
            }
        }
    }
}
