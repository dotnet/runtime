// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        #region Negative Tests
        // NegTest1: The year greater than max year
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = tc.MaxSupportedDateTime.Year - 1911 + rand.Next(1, Int32.MaxValue);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapYear(year, era);
            });
        }

        // NegTest2: The year less than min year
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = tc.MinSupportedDateTime.Year - rand.Next(1, Int32.MaxValue);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapYear(year, era);
            });
        }

        // NegTest3: The era greater than 1
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int era = 2;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapYear(year, era);
            });
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

