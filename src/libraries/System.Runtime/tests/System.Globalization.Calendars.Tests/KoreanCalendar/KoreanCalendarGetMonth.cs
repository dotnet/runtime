// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarGetMonth
    {
        public static IEnumerable<object[]> GetMonth_TestData()
        {
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.MaxValue };
            yield return new object[] { new DateTime(2004, 2, 29) };
        }

        [Theory]
        [MemberData(nameof(GetMonth_TestData))]
        public void GetMonth(DateTime time)
        {
            Assert.Equal(time.Month, new KoreanCalendar().GetMonth(time));
        }
    }
}
