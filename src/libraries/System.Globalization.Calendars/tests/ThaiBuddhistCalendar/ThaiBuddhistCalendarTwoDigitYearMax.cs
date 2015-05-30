// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.TwoDigitYearMax(Int32)
    public class ThaiBuddhistCalendarTwoDigitYearMax
    {
        #region Positive Tests
        // PosTest1:Verify the default TwoDigitYearMax is 2029
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int expectedMax = 2029 + 543;
            Assert.Equal(tbc.TwoDigitYearMax, expectedMax);
        }

        // PosTest2: Verify the TwoDigitYearMax is set random value
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int twoYearMax = rand.Next(99, tbc.MaxSupportedDateTime.Year + 543);
            tbc.TwoDigitYearMax = twoYearMax;
            Assert.Equal(tbc.TwoDigitYearMax, twoYearMax);
        }

        // PosTest3: Verify the TwoDigitYearMax is set 10542
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int twoYearMax = 10542;
            tbc.TwoDigitYearMax = twoYearMax;
            Assert.Equal(tbc.TwoDigitYearMax, twoYearMax);
        }

        // PosTest4: Verify the TwoDigitYearMax is set 99
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int twoYearMax = 99;
            tbc.TwoDigitYearMax = twoYearMax;
            Assert.Equal(tbc.TwoDigitYearMax, twoYearMax);
        }
        #endregion

        #region Negative Tests
        // NegTest1: Set the TwoDigitYear max as greater than the year of  MaxSuportedDateTime
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int year = 10000 + 543;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.TwoDigitYearMax = year;
            });
        }

        // NegTest2: The year less than 99
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int year = 98;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tbc.TwoDigitYearMax = year;
            });
        }
        #endregion
    }
}