// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarTwoDigitYearMax
    {
        [Fact]
        public void TwoDigitYearMax_Get()
        {
            Assert.Equal(4362, new KoreanCalendar().TwoDigitYearMax);
        }

        [Theory]
        [InlineData(99)]
        [InlineData(2016)]
        public void TwoDigitYearMax_Set(int newTwoDigitYearMax)
        {
            Calendar calendar = new KoreanCalendar();
            calendar.TwoDigitYearMax = newTwoDigitYearMax;
            Assert.Equal(newTwoDigitYearMax, calendar.TwoDigitYearMax);
        }
    }
}
