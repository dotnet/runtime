// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDayOfWeek
    {
        public static IEnumerable<object[]> GetDayOfWeek_TestData()
        {
            yield return new object[] { new DateTime(2007, 1, 1) };
            yield return new object[] { new DateTime(2006, 2, 28) };
            yield return new object[] { new DateTime(2006, 3, 1) };
            yield return new object[] { new DateTime(2006, 8, 31) };
            yield return new object[] { new DateTime(2008, 2, 29) };
            yield return new object[] { new DateTime(2006, 12, 30) };
            yield return new object[] { new DateTime(2006, 12, 31) };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { new DateTime(2000, 2, 29) };
        }

        [Theory]
        [MemberData(nameof(GetDayOfWeek_TestData))]
        public void GetDayOfWeek(DateTime time)
        {
            Assert.Equal(time.DayOfWeek, new GregorianCalendar().GetDayOfWeek(time));
        }
    }
}
