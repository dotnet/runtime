// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}

