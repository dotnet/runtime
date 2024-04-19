// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class TaiwanCalendarTwoDigitYearMax
    {
        [Fact]
        public void TwoDigitYearMax_Get()
        {
            Assert.Equal(99, new TaiwanCalendar().TwoDigitYearMax);
        }

        [Fact]
        public void TwoDigitYearMax_Set()
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            int newTwoDigitYearMax = new Random(-55).Next(99, calendar.MaxSupportedDateTime.Year);
            calendar.TwoDigitYearMax = newTwoDigitYearMax;
            Assert.Equal(newTwoDigitYearMax, calendar.TwoDigitYearMax);
        }
    }
}
