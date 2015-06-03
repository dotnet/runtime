// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.GetEra(DateTime)
    public class ThaiBuddhistCalendarGetEar
    {
        #region Positive Tests
        // PosTest1: Verify the return is current Era when DateTime random time
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tbc.MinSupportedDateTime.Year + 543, tbc.MaxSupportedDateTime.Year + 544);
            int month = rand.Next(1, 13);
            int day = rand.Next(1, tbc.GetDaysInMonth(year, month) + 1);
            DateTime dt = tbc.ToDateTime(year, month, day, 0, 0, 0, 0);
            int era = tbc.GetEra(dt);
            Assert.Equal(1, era);
        }

        // PosTest2: Verify DateTime is MaxSuppeortedDateTime of ThaiBuddhistCalendar
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MaxSupportedDateTime;
            int era = tbc.GetEra(dt);
            Assert.Equal(1, era);
        }

        // PosTest3: Verify DateTime is MinSupportedDateTime of ThaiBuddhistCalendar
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = tbc.MinSupportedDateTime;
            int era = tbc.GetEra(dt);
            Assert.Equal(1, era);
        }

        // PosTest4: Verify DateTime is leap day
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            DateTime dt = new DateTime(2000, 2, 29);
            int era = tbc.GetEra(dt);
            Assert.Equal(1, era);
        }
        #endregion
    }
}