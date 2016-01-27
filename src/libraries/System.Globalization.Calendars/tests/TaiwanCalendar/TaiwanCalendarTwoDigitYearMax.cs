// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.TwoDigitYearMax
    public class TaiwanCalendarTwoDigitYearMax
    {
        #region Positive Tests
        // PosTest1: Verify the default TwoDigitYearMax is 99
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Assert.Equal(99, tc.TwoDigitYearMax);
        }

        // PosTest2: Verify the TwoDigitYearMax is set random value
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int twoYearMax = rand.Next(99, tc.MaxSupportedDateTime.Year);
            tc.TwoDigitYearMax = twoYearMax;
            Assert.Equal(twoYearMax, tc.TwoDigitYearMax);
        }
        #endregion

        #region Negative Tests
        // NegTest1: Set the TwoDigitYear max as greater than the year of  MaxSuportedDateTime
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 8089;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.TwoDigitYearMax = year;
            });
        }

        // NegTest2: The year less than 99
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 98;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.TwoDigitYearMax = year;
            });
        }
        #endregion
    }
}

