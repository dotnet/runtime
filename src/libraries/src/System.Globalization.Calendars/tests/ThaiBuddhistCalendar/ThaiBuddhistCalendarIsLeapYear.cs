// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.IsLeapYear(Int32,Int32)
    public class ThaiBuddhistCalendarIsLeapYear
    {
        #region Positive Tests
        // PosTest1: Verify the year  is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 543);
            int era;
            bool isLeap = isLeapYear(year);
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.False(tbc.IsLeapYear(year, era) ^ isLeap);
            }
        }

        // PosTest2: Verify year  is leap year
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = 2000 + 543;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.True(tbc.IsLeapYear(year, era));
            }
        }

        // PosTest3: Verify the year is ThaiBuddhistCalendar MinSupportedDateTime.Year
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = tbc.MinSupportedDateTime.Year + 543;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.False(tbc.IsLeapYear(year, era));
            }
        }

        // PosTest4: Verify the year is ThaiBuddhistCalendar MaxSupportedDateTime.Year
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = tbc.MaxSupportedDateTime.Year + 543;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.False(tbc.IsLeapYear(year, era));
            }
        }
        #endregion

        #region Helper Methods
        private bool isLeapYear(int year)
        {
            year -= 543;
            return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));
        }
        #endregion
    }
}
