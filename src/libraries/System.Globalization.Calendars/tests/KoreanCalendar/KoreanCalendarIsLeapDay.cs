// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarIsLeapDay
    {
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(9999, 12, 31)]
        [InlineData(1900, 2, 28)]
        [InlineData(1200, 2, 29)]
        public void IsLeapDay(int year, int month, int day)
        {
            KoreanCalendar calendar = new KoreanCalendar();
            bool expected = new GregorianCalendar().IsLeapDay(year, month, day);
            Assert.Equal(expected, calendar.IsLeapDay(year + 2333, month, day));
            Assert.Equal(expected, calendar.IsLeapDay(year + 2333, month, day, 0));
            Assert.Equal(expected, calendar.IsLeapDay(year + 2333, month, day, 1));
        }
    }
}
