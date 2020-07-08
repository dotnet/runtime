// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarGetDaysInYear
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetDaysInYear_TestData()
        {
            yield return new object[] { 1 };
            yield return new object[] { 9999 };
            yield return new object[] { 2000 };
            yield return new object[] { s_randomDataGenerator.GetInt16(-55) % 9999 };
        }

        [Theory]
        [MemberData(nameof(GetDaysInYear_TestData))]
        public void GetDaysInYear(int year)
        {
            ThaiBuddhistCalendar calendar = new ThaiBuddhistCalendar();
            int expected = new GregorianCalendar().GetDaysInYear(year);
            Assert.Equal(expected, calendar.GetDaysInYear(year + 543));
            Assert.Equal(expected, calendar.GetDaysInYear(year + 543, 0));
            Assert.Equal(expected, calendar.GetDaysInYear(year + 543, 1));
        }
    }
}
