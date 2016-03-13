// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetDaysInMonth(System.Int32,System.Int32,System.Int32)
    public class KoreanCalendarGetDaysInMonth
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the mthod with Min DateTime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = gC.GetDaysInMonth(dateTime.Year, dateTime.Month, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInMonth(dateTime.Year + 2333, dateTime.Month, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the mthod with Max DateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int expectedValue = gC.GetDaysInMonth(dateTime.Year, dateTime.Month, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInMonth(dateTime.Year + 2333, dateTime.Month, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the mthod with leap year DateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(2004, 2, 29, 0, 0, 0, 0);
            int expectedValue = gC.GetDaysInMonth(dateTime.Year, dateTime.Month, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInMonth(dateTime.Year + 2333, dateTime.Month, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the mthod with random argument
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            dateTime = new GregorianCalendar().ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            int month = _generator.GetInt16(-55) % 12 + 1;
            int expectedValue = gC.GetDaysInMonth(dateTime.Year, month, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInMonth(dateTime.Year + 2333, month, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}
