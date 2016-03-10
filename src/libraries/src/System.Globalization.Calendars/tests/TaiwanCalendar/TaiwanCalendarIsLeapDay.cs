// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}

