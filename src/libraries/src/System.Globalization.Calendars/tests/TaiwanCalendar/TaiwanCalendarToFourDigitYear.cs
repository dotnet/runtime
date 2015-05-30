// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        #region Negative Tests
        // NegTest1: The year greater than max year
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 8089;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToFourDigitYear(year);
            });
        }

        // NegTest2: The year less than 1
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 0;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToFourDigitYear(year);
            });
        }
        #endregion
    }
}

