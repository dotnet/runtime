// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetDayOfMonth(System.DateTime)
    public class KoreanCalendarGetDayOfMonth
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Test Logic
        // PosTest1:Test the method with min time
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = 1;
            int actualValue;
            actualValue = kC.GetDayOfMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Test the method with max time
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int expectedValue = 31;
            int actualValue;
            actualValue = kC.GetDayOfMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Test the method with leap year time
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(2008, 2, 29, 0, 0, 0, 0);
            int expectedValue = 29;
            int actualValue;
            actualValue = kC.GetDayOfMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Test the method with random time
        [Fact]
        public void PosTest4()
        {
            Int64 ticks = _generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1);
            DateTime dateTime = new DateTime(ticks);
            dateTime = new GregorianCalendar().ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            System.Globalization.Calendar kC = new KoreanCalendar();
            int expectedValue = dateTime.Day;
            int actualValue;
            actualValue = kC.GetDayOfMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}