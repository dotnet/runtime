// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Globalization.Tests;
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

        [Fact]
        public void TwoDigitYearMax_Invalid()
        {
            CalendarHelpers.TwoDigitYearMax_Invalid(new ThaiBuddhistCalendar());
        }
    }
}
