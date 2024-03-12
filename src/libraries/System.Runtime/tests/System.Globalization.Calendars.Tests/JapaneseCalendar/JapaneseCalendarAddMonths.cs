// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseCalendarAddMonths
    {
        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { new DateTime(2006, 11, 28), 1, new DateTime(2006, 12, 28) };
            yield return new object[] { new DateTime(2006, 11, 28), -1, new DateTime(2006, 10, 28) };
            yield return new object[] { new DateTime(2006, 11, 28), 0, new DateTime(2006, 11, 28) };
            yield return new object[] { new DateTime(2006, 11, 28), 1000, new DateTime(2090, 3, 28) };

            yield return new object[] { new DateTime(2006, 12, 1), 1, new DateTime(2007, 1, 1) };
            yield return new object[] { new DateTime(2007, 1, 1), -1, new DateTime(2006, 12, 1) };

            // Boundary values
            yield return new object[] { new DateTime(1868, 9, 8), 1, new DateTime(1868, 10, 8) };
            yield return new object[] { new DateTime(1868, 10, 8), -1, new DateTime(1868, 9, 8) };
            yield return new object[] { new DateTime(9999, 11, 30), 1, new DateTime(9999, 12, 30) };
            yield return new object[] { new DateTime(9999, 12, 30), -1, new DateTime(9999, 11, 30) };
            yield return new object[] { DateTime.MaxValue, 0, DateTime.MaxValue };
            yield return new object[] { new DateTime(1868, 9, 8), 0, new DateTime(1868, 9, 8) };

            // Day is not in the month
            yield return new object[] { new DateTime(2006, 10, 31), 1, new DateTime(2006, 11, 30) };
            yield return new object[] { new DateTime(2006, 12, 31), -1, new DateTime(2006, 11, 30) };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths(DateTime time, int months, DateTime expected)
        {
            Assert.Equal(expected, new JapaneseCalendar().AddMonths(time, months));
        }
    }
}
