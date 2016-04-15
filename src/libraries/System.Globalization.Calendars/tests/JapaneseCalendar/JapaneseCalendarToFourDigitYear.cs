// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class JapaneseCalendarToFourDigitYear
    {        
        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(2016)]
        public void ToFourDigitYear(int year)
        {
            Calendar calendar = new JapaneseCalendar();
            calendar.TwoDigitYearMax = 99;
            Assert.Equal(year, calendar.ToFourDigitYear(year));
        }
    }
}
