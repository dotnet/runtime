// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarGetMonth
    {
        public static IEnumerable<object[]> GetMonth_TestData()
        {
            yield return new object[] { new TaiwanCalendar().MinSupportedDateTime };
            yield return new object[] { new TaiwanCalendar().MaxSupportedDateTime };
            yield return new object[] { TaiwanCalendarUtilities.RandomDateTime() };
        }

        [Theory]
        [MemberData(nameof(GetMonth_TestData))]
        public void GetMonth(DateTime time)
        {
            Assert.Equal(time.Month, new TaiwanCalendar().GetMonth(time));
        }
    }
}
