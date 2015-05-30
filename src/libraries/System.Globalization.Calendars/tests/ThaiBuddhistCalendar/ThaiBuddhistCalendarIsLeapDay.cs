// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.IsLeapDay(Int32,Int32,Int32,Int32)
    public class ThaiBuddhistCalendarIsLeapDay
    {
        #region Positive Tests
        // PosTest1: Verify the day  is not leap day
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 544);
            int month = rand.Next(1, 12);
            int day = rand.Next(1, 29);
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.False(tbc.IsLeapDay(year, month, day, era));
            }
        }

        // PosTest2: Verify the Date is leap day
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = 2000 + 543;
            int month = 2;
            int day = 29;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.True(tbc.IsLeapDay(year, month, day, era));
            }
        }

        // PosTest3: Verify the DateTime is ThaiBuddhistCalendar MinSupportedDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = tbc.MinSupportedDateTime.Year + 543;
            int month = tbc.MinSupportedDateTime.Month;
            int day = tbc.MinSupportedDateTime.Day;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.False(tbc.IsLeapDay(year, month, day, era));
            }
        }

        // PosTest4: Verify the DateTime is ThaiBuddhistCalendar MaxSupportedDateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = tbc.MaxSupportedDateTime.Year + 543;
            int month = tbc.MaxSupportedDateTime.Month;
            int day = tbc.MaxSupportedDateTime.Day;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.False(tbc.IsLeapDay(year, month, day, era));
            }
        }
        #endregion
    }
}