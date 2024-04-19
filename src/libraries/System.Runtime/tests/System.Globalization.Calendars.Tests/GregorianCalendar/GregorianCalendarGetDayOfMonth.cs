// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDayOfMonth
    {
        public static IEnumerable<object[]> GetDayOfMonth_TestData()
        {
            yield return new object[] { new DateTime(2006, 1, 1) };
            yield return new object[] { new DateTime(2006, 1, 17) };
            yield return new object[] { new DateTime(2006, 1, 31) };
        }

        [Theory]
        [MemberData(nameof(GetDayOfMonth_TestData))]
        public void GetDayOfMonth(DateTime time)
        {
            Assert.Equal(time.Day, new GregorianCalendar().GetDayOfMonth(time));
        }
    }
}
