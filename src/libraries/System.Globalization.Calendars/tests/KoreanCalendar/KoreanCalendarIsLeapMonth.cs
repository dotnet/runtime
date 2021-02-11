// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarIsLeapMonth
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(9999, 12)]
        [InlineData(1900, 2)]
        [InlineData(1200, 2)]
        public void IsLeapMonth(int year, int month)
        {
            KoreanCalendar calendar = new KoreanCalendar();
            bool expected = new GregorianCalendar().IsLeapMonth(year, month);
            Assert.Equal(expected, calendar.IsLeapMonth(year + 2333, month));
            Assert.Equal(expected, calendar.IsLeapMonth(year + 2333, month, 0));
            Assert.Equal(expected, calendar.IsLeapMonth(year + 2333, month, 1));
        }
    }
}
