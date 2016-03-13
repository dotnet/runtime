// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetMonthsInYear(System.Int32,System.Int32)
    public class KoreanCalendarGetMonthsInYear
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the method with min datetime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = gC.GetMonthsInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetMonthsInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with max datetime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc);
            int expectedValue = gC.GetMonthsInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetMonthsInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with max datetime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(2004, 2, 29, 0, 0, 0, 0);
            int expectedValue = gC.GetMonthsInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetMonthsInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}
