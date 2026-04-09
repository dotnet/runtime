// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class ThaiBuddhistCalendarIsLeapDay
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> IsLeapDay_TestData()
        {
            yield return new object[] { 1, 1, 1 };
            yield return new object[] { 9999, 12, 31 };
            yield return new object[] { 2000, 2, 29 };
            yield return new object[] { s_randomDataGenerator.GetInt16(-55) % 9999, s_randomDataGenerator.GetInt16(-55) % 12 + 1, s_randomDataGenerator.GetInt16(-55) % 29 + 1 };
        }

        [Theory]
        [MemberData(nameof(IsLeapDay_TestData))]
        public void IsLeapDay(int year, int month, int day)
        {
            ThaiBuddhistCalendar calendar = new ThaiBuddhistCalendar();
            bool expected = new GregorianCalendar().IsLeapDay(year, month, day);
            Assert.Equal(expected, new ThaiBuddhistCalendar().IsLeapDay(year + 543, month, day));
            Assert.Equal(expected, new ThaiBuddhistCalendar().IsLeapDay(year + 543, month, day, 0));
            Assert.Equal(expected, new ThaiBuddhistCalendar().IsLeapDay(year + 543, month, day, 1));
        }
    }
}
