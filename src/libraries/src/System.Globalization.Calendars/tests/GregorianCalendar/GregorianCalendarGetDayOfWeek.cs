// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDayOfWeek
    {
        public static IEnumerable<object[]> GetDayOfWeek_TestData()
        {
            yield return new object[] { new DateTime(2007, 1, 1), DayOfWeek.Monday };
            yield return new object[] { new DateTime(2006, 2, 28), DayOfWeek.Tuesday };
            yield return new object[] { new DateTime(2006, 3, 1), DayOfWeek.Wednesday };
            yield return new object[] { new DateTime(2006, 8, 31), DayOfWeek.Thursday };
            yield return new object[] { new DateTime(2008, 2, 29), DayOfWeek.Friday };
            yield return new object[] { new DateTime(2006, 12, 30), DayOfWeek.Saturday };
            yield return new object[] { new DateTime(2006, 12, 31), DayOfWeek.Sunday };
            yield return new object[] { DateTime.MaxValue, DayOfWeek.Friday };
            yield return new object[] { DateTime.MinValue, DayOfWeek.Monday };
            yield return new object[] { new DateTime(2000, 2, 29), DayOfWeek.Tuesday };
        }

        [Theory]
        [MemberData(nameof(GetDayOfWeek_TestData))]
        public void GetDayOfWeek(DateTime time, DayOfWeek expected)
        {
            Assert.Equal(expected, new GregorianCalendar().GetDayOfWeek(time));
        }
    }
}
