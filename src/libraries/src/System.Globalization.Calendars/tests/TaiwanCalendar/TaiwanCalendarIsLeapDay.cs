// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.IsLeapDay(Int32,Int32,Int32,Int32)
    public class TaiwanCalendarIsLeapDay
    {
        #region Positive Tests
        // PosTest1: Verify the day  is not leap day
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int day = rand.Next(1, 29);
            int era;

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.False(tc.IsLeapDay(year, month, era));
            }
        }

        // PosTest2: Verify the Date is leap day
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = 2000 - 1911;
            int month = 2;
            int day = 29;
            int era;

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                Assert.True(tc.IsLeapDay(year, month, day, era));
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
            int day = rand.Next(1, 28);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
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
            int day = rand.Next(1, 28);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
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
            int day = rand.Next(1, 28);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
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
            int day = rand.Next(1, 28);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
            });
        }

        // NegTest5: The day greater than 31
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(1, 13);
            int day = rand.Next(31, Int32.MaxValue);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
            });
        }

        // NegTest6: The day less than 1
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(1, 13);
            int day = rand.Next(Int32.MinValue, 1);
            int era = Calendar.CurrentEra;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
            });
        }

        // NegTest7: The era greater than 1
        [Fact]
        public void NegTest7()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year + 1);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 28);
            int era = 2;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.IsLeapDay(year, month, day, era);
            });
        }
        #endregion
    }
}

