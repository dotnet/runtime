// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.ToFourDigitYear(System.Int32)
    public class TaiwanCalendarToFourDigitYear
    {
        #region Positive Tests
        // PosTest1: Verify the day is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(1, 8088);
            Assert.Equal(year, tc.ToFourDigitYear(year));
        }

        // PosTest2: Verify year is 1
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 1;
            Assert.Equal(year, tc.ToFourDigitYear(year));
        }

        // PosTest3: Verify the year is 8088
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 8088;
            Assert.Equal(year, tc.ToFourDigitYear(year));
        }
        #endregion
    }
}

