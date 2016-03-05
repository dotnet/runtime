// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.ToFourDigitYear(Int32)
    public class ThaiBuddhistCalendarToFourDigitYear
    {
        #region Positive Tests
        // PosTest1: Verify the day is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            tbc.TwoDigitYearMax = 2029;
            Random rand = new Random(-55);
            int year = rand.Next(1, 99);
            int expertedYear;
            if (year > 29)
            {
                expertedYear = year + 1900;
            }
            else
            {
                expertedYear = year + 2000;
            }
            Assert.Equal(expertedYear, tbc.ToFourDigitYear(year));
        }

        // PosTest2: Verify year is large than 99 and less than 10542
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.GetYear(tbc.MinSupportedDateTime), tbc.GetYear(tbc.MaxSupportedDateTime));
            Assert.Equal(year, tbc.ToFourDigitYear(year));
        }

        // PosTest3: Verify the year is 10542
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            int year = 9999 + 543;
            Assert.Equal(year, tbc.ToFourDigitYear(year));
        }

        // PosTest4: Verify the year is 0
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            tbc.TwoDigitYearMax = 2029;
            int year = 0;
            int expectedYear = 2000;
            Assert.Equal(expectedYear, tbc.ToFourDigitYear(year));
        }
        #endregion

        [Fact]
        public void ToFourDigitYear_Invalid()
        {
            CalendarHelpers.ToFourDigitYear_Invalid(new ThaiBuddhistCalendar());
        }
    }
}
