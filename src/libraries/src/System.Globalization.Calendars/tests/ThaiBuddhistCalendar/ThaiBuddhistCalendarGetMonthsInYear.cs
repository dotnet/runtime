// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Globalization.Tests;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.GetMonthsInYear(Int32,Int32)
    public class ThaiBuddhistCalendarGetMonthsInYear
    {
        private readonly int _months_IN_YEAR = 12;

        #region Positive Tests
        // PosTest1: Verify the day is a random year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 544);
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(_months_IN_YEAR, tbc.GetMonthsInYear(year, era));
            }
        }

        // PosTest2: Verify year is ThaiBuddhistCalendar MinSupportedDateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int year = dt.Year + 543;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(_months_IN_YEAR, tbc.GetMonthsInYear(year, era));
            }
        }

        // PosTest3: Verify the DateTime is ThaiBuddhistCalendar MaxSupportDateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int year = dt.Year + 543;
            int era;
            for (int i = 0; i < tbc.Eras.Length; i++)
            {
                era = tbc.Eras[i];
                Assert.Equal(_months_IN_YEAR, tbc.GetMonthsInYear(year, era));
            }
        }
        #endregion
        
        [Fact]
        public void GetMonthsInYear_Invalid()
        {
            CalendarHelpers.GetMonthsInYear_Invalid(new ThaiBuddhistCalendar());
        }
    }
}
