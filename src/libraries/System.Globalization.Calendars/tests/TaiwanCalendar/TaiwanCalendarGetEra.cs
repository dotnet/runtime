// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.GetEra(DateTime)
    public class TaiwanCalendarGetEar
    {
        #region Positive Tests
        // PosTest1: Verify the return is current Era when DateTime random time
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Random rand = new Random(-55);
            int year = rand.Next(tc.MinSupportedDateTime.Year, tc.MaxSupportedDateTime.Year - 1911);
            int month = rand.Next(1, 12);
            int day = rand.Next(1, tc.GetDaysInMonth(year, month) + 1);
            DateTime dt = new DateTime(year, month, day);
            int era = tc.GetEra(dt);
            Assert.Equal(1, era);
        }

        // PosTest2: Verify DateTime is MaxSuppeortedDateTime of TaiwanCalendar
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MaxSupportedDateTime;
            int era = tc.GetEra(dt);
            Assert.Equal(1, era);
        }

        // PosTest3: Verify DateTime is MinSupportedDateTime of TaiwanCalendar
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            DateTime dt = tc.MinSupportedDateTime;
            int era = tc.GetEra(dt);
            Assert.Equal(1, era);
        }
        #endregion
    }
}