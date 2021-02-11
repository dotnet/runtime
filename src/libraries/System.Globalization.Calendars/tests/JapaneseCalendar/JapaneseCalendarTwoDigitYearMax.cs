// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseCalendarTwoDigitYearMax
    {
        [Fact]
        public void TwoDigitYearMax_Get()
        {
            Assert.Equal(99, new JapaneseCalendar().TwoDigitYearMax);
        }

        [Theory]
        [InlineData(200)]
        [InlineData(99)]
        public void TwoDigitYearMax_Set(int newTwoDigitYearMax)
        {
            Calendar calendar = new JapaneseCalendar();
            calendar.TwoDigitYearMax = newTwoDigitYearMax;
            Assert.Equal(newTwoDigitYearMax, calendar.TwoDigitYearMax);
        }
    }
}
