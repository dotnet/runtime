// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetDayOfYear
    {
        public static IEnumerable<object[]> GetDayOfYear_TestData()
        {
            yield return new object[] { new DateTime(2006, 11, 29), 333 };
            yield return new object[] { new DateTime(2006, 1, 1), 1 };
            yield return new object[] { new DateTime(2007, 12, 31), 365 };
            yield return new object[] { new DateTime(2000, 2, 29), 60 };
            yield return new object[] { new DateTime(2001, 2, 28), 59 };
            yield return new object[] { new DateTime(2000, 1, 1), 1 };
            yield return new object[] { new DateTime(2000, 12, 31), 366 };
            yield return new object[] { DateTime.MaxValue, 365 };
            yield return new object[] { DateTime.MinValue, 1 };
        }

        [Theory]
        [MemberData(nameof(GetDayOfYear_TestData))]
        public void GetDayOfYear(DateTime time, int expected)
        {
            Assert.Equal(expected, new GregorianCalendar().GetDayOfYear(time));
        }
    }
}
