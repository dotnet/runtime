// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.ToDateTime(Int32,Int32,Int32,Int32,Int32,Int32,Int32,Int32)
    public class TaiwanCalendarToDateTime
    {
        #region Positive Tests
        // PosTest1: Verify the year is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 29);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(0, 60);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(0, 1000);
            int era = 0;

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                DateTime dt = tc.ToDateTime(year, month, day, hour, minute, second, milliSecond);
                DateTime desiredDT = new DateTime(year + 1911, month, day, hour, minute, second, milliSecond);
                Assert.Equal(desiredDT, dt);
            }
        }

        // PosTest2: Verify the DateTime is 8088-12-31 23:59:29:999
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            int year = 8088;
            int month = 12;
            int day = 31;
            int hour = 23;
            int minute = 59;
            int second = 59;
            int milliSecond = 999;
            int era;

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                DateTime dt = tc.ToDateTime(year, month, day, hour, minute, second, milliSecond);
                DateTime desireDT = new DateTime(year + 1911, month, day, hour, minute, second, milliSecond);
                Assert.Equal(desireDT, dt);
            }
        }

        // PosTest3: Verify the DateTime is TaiwanCalendar MinSupportedDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime minDT = tc.MinSupportedDateTime;
            int year = 1;
            int month = 1;
            int day = 1;
            int hour = 0;
            int minute = 0;
            int second = 0;
            int milliSecond = 0;
            int era;

            for (int i = 0; i < tc.Eras.Length; i++)
            {
                era = tc.Eras[i];
                DateTime dt = tc.ToDateTime(year, month, day, hour, minute, second, milliSecond);
                Assert.Equal(minDT, dt);
            }
        }
        #endregion

        #region Negative Tests
        // NegTest1: The year outside the range supported by the TaiwanCalendar
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MaxSupportedDateTime.Year - 1910, Int32.MaxValue);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 29);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(0, 60);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(0, 1000);
            int era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            year = rand.Next(Int32.MinValue, tc.MinSupportedDateTime.Year);

            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }

        // NegTest2: The month  outside the range supported by the TaiwanCalendar
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(Int32.MinValue, 1);
            int day = rand.Next(1, 29);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(0, 60);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(0, 1000);
            int era;
            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            month = rand.Next(13, Int32.MaxValue);
            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }

        // NegTest3: The day outside the range supported by the TaiwanCalendar
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int day = rand.Next(Int32.MinValue, 1);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(0, 60);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(0, 1000);
            int era;
            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            day = rand.Next(tc.GetDaysInMonth(year, month, era) + 1, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }

        // NegTest4: The hour is less than zero or greater than 23
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 29);
            int hour = rand.Next(Int32.MinValue, 0);
            int minute = rand.Next(0, 60);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(0, 1000);
            int era;

            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            hour = rand.Next(24, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }

        // NegTest5: The minute is less than zero or greater than 59
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 29);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(Int32.MinValue, 0);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(0, 1000);
            int era;
            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            //minute greater than 59;
            minute = rand.Next(60, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }

        // NegTest6: The second is less than zero or greater than 59
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 29);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(0, 60);
            int second = rand.Next(Int32.MinValue, 0);
            int milliSecond = rand.Next(0, 1000);
            int era;
            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            //second greater than 59;
            second = rand.Next(60, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }

        // NegTest7: The milliSecond is less than zero or greater than 999
        [Fact]
        public void NegTest7()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, 29);
            int hour = rand.Next(0, 24);
            int minute = rand.Next(0, 60);
            int second = rand.Next(0, 60);
            int milliSecond = rand.Next(Int32.MinValue, 0);
            int era;
            era = tc.Eras[0];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });

            //milliSecond greater than 999;
            second = rand.Next(1000, Int32.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                tc.ToDateTime(year, month, day, hour, minute, second, milliSecond, era);
            });
        }
        #endregion
    }
}

