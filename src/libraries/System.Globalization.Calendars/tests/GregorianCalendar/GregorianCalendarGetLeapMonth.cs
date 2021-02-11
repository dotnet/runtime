// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class GregorianCalendarGetLeapMonth
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2016)]
        [InlineData(9999)]
        public void GetLeapMonth(int year)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            Assert.Equal(0, calendar.GetLeapMonth(year, 0));
            Assert.Equal(0, calendar.GetLeapMonth(year, 1));
        }
    }
}
