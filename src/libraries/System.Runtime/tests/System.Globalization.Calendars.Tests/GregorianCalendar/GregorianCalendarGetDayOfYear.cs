// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDayOfYear
    {
        public static IEnumerable<object[]> GetDayOfYear_TestData()
        {
            yield return new object[] { new DateTime(2006, 11, 29) };
            yield return new object[] { new DateTime(2006, 1, 1) };
            yield return new object[] { new DateTime(2007, 12, 31) };
            yield return new object[] { new DateTime(2000, 2, 29) };
            yield return new object[] { new DateTime(2001, 2, 28) };
            yield return new object[] { new DateTime(2000, 1, 1) };
            yield return new object[] { new DateTime(2000, 12, 31) };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { DateTime.MinValue };
        }

        [Theory]
        [MemberData(nameof(GetDayOfYear_TestData))]
        public void GetDayOfYear(DateTime time)
        {
            Assert.Equal(time.DayOfYear, new GregorianCalendar().GetDayOfYear(time));
        }
    }
}
