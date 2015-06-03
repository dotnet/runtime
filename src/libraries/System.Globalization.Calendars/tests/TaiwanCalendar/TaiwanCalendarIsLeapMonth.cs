// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.IsLeapMonth(Int32,Int32,Int32)
    public class TaiwanCalendarIsLeapMonth
    {
        #region Positive Tests
        // PosTest1: Verify IsLeapMonth is false
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int era;
            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.False(tc.IsLeapMonth(year, month, era));
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
            int month = rand.Next(1, 13);
            int era = Calendar.CurrentEra;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapMonth(year, month, era);
            });
        }

        // NegTest2: The year less than min year
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = tc.MinSupportedDateTime.Year - rand.Next(1, Int32.MaxValue);
            int month = rand.Next(1, 13);
            int era = Calendar.CurrentEra;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapMonth(year, month, era);
            });
        }

        // NegTest3: The month greater than 12
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(12, Int32.MaxValue);
            int era = Calendar.CurrentEra;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapMonth(year, month, era);
            });
        }

        // NegTest4: The month less than 1
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(Int32.MinValue, 1);
            int era = Calendar.CurrentEra;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapMonth(year, month, era);
            });
        }

        // NegTest5: The era greater than 1
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 28);
            int era = 2;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapMonth(year, month, era);
            });
        }
        #endregion
    }
}