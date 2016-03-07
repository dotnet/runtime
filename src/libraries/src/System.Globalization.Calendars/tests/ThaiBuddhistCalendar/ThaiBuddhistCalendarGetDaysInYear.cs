// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.GetDaysInYear(Int32,Int32)
    public class ThaiBuddhistCalendarGetDaysInYear
    {
        #region Positive Tests
        // PosTest1: Verify the year is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 544);
            int era;
            int actualDays;
            if (IsLeapYear(year))
            {
                actualDays = 366;
            }
            else
            {
                actualDays = 365;
            }

            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInYear(year, era));
            }
        }

        // PosTest2: Verify year is ThaiBuddhistCalendar MinSupportedDateTime.Year 
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int year = dt.Year + 543;
            int era;
            int actualDays = 365;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInYear(year, era));
            }
        }

        // PosTest3: Verify the year is ThaiBuddhistCalendar MaxSupportDateTime.Year
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int year = dt.Year + 543;
            int era;
            int actualDays = 365;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInYear(year, era));
            }
        }

        // PosTest4: Verify the DateTime is leap year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = new DateTime(2000, 12, 31);
            int year = 2000 + 543;
            int era;
            int actualDays = 366;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(actualDays, tbc.GetDaysInYear(year, era));
            }
        }
        #endregion

        #region Helper Methods
        private bool IsLeapYear(int i)
        {
            int year = i - 543;
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }
        #endregion
    }
}
