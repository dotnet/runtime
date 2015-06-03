// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.GetMonthsInYear(Int32,Int32)
    public class TaiwanCalendarGetMonthsInYear
    {
        private readonly int _months_IN_YEAR = 12;

        #region Positive Tests
        // PosTest1: Verify the day is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int era;
            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(_months_IN_YEAR, tc.GetMonthsInYear(year, era));
            }
        }

        // PosTest2: Verify year is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            int year = dt.Year;
            int era;
            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(_months_IN_YEAR, tc.GetMonthsInYear(year, era));
            }
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            int year = dt.Year - 1911;
            int era;
            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.Equal(_months_IN_YEAR, tc.GetMonthsInYear(year, era));
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
                tc.GetMonthsInYear(year, era);
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
                tc.GetMonthsInYear(year, era);
            });
        }
        #endregion
    }
}