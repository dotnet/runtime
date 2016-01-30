// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class JapaneseCalendarTwoDigitYearMax
    {
        #region Positive Test Cases
        // PosTest1: Call TwoDigitYearMax to get max supported date time
        [Fact]
        public void PosTest1()
        {
            int actual = new JapaneseCalendar().TwoDigitYearMax;
            int expected = 99;
            Assert.Equal(expected, actual);
        }

        // PosTest2: Call TwoDigitYearMax to get max supported date time
        [Fact]
        public void PosTest2()
        {
            int expected = 200;
            System.Globalization.Calendar calendar = new JapaneseCalendar();
            calendar.TwoDigitYearMax = expected;
            int actual = calendar.TwoDigitYearMax;
            Assert.Equal(expected, actual);
        }
        #endregion
    }
}
