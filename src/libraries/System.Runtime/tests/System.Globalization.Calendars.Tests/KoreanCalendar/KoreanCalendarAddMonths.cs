// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarAddMonths
    {
        public static IEnumerable<object[]> AddMonths_TestData()
        {
            yield return new object[] { DateTime.MinValue, 1 };
            yield return new object[] { DateTime.MaxValue, -2 };
            yield return new object[] { new DateTime(2003, 1, 29), 1 };
            yield return new object[] { new DateTime(2006, 7, 31), 2 };
        }

        [Theory]
        [MemberData(nameof(AddMonths_TestData))]
        public void AddMonths(DateTime time, int months)
        {
            Assert.Equal(time.AddMonths(months), new KoreanCalendar().AddMonths(time, months));
        }
    }
}
