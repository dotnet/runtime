// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.IsLeapYear(Int32,Int32)
    public class TaiwanCalendarIsLeapYear
    {
        #region Positive Tests
        // PosTest1: Verify the year  is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int era;

            bool isLeap = isLeapYear(year);
            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.False(tc.IsLeapYear(year, era) ^ isLeap);
            }
        }

        // PosTest2: Verify the Date is leap day
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = 2000 - 1911;
            int era;

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.True(tc.IsLeapYear(year, era));
            }
        }
        #endregion
        
        #region Helper Methods
        private bool isLeapYear(int year)
        {
            year += 1911;
            return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));
        }
        #endregion
    }
}

