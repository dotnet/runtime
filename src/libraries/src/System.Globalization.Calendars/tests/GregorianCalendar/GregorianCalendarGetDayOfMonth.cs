// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDayOfMonth
    {
        public static IEnumerable<object[]> GetDayOfMonth_TestData()
        {
            yield return new object[] { new GregorianCalendar().ToDateTime(2006, 1, 1, 0, 0, 0, 0), 1 };
            yield return new object[] { new GregorianCalendar().ToDateTime(2006, 1, 17, 0, 0, 0, 0), 17 };
            yield return new object[] { new GregorianCalendar().ToDateTime(2006, 1, 31, 0, 0, 0, 0), 31 };
        }

        [Theory]
        [MemberData(nameof(GetDayOfMonth_TestData))]
        public void GetDayOfMonth(DateTime time, int expected)
        {
            Assert.Equal(expected, new GregorianCalendar().GetDayOfMonth(time));
        }
    }
}
