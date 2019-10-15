// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarIsLeapYear
    {
        [Theory]
        [InlineData(1)]
        [InlineData(9999)]
        [InlineData(1900)]
        [InlineData(1200)]
        public void IsLeapYear(int year)
        {
            KoreanCalendar calendar = new KoreanCalendar();
            bool expected = new GregorianCalendar().IsLeapYear(year);
            Assert.Equal(expected, calendar.IsLeapYear(year + 2333));
            Assert.Equal(expected, calendar.IsLeapYear(year + 2333, 0));
            Assert.Equal(expected, calendar.IsLeapYear(year + 2333, 1));
        }
    }
}
