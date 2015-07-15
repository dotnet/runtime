// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetYear(System.DateTime)
    public class KoreanCalendarGetYear
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Test Logic
        // PosTest1:Invoke the method with min datetime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = gC.GetYear(dateTime) + 2333;
            int actualValue;
            actualValue = kC.GetYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with max datetime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int expectedValue = gC.GetYear(dateTime) + 2333;
            int actualValue;
            actualValue = kC.GetYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with leap year datetime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(2008, 2, 29, 0, 0, 0, 0);
            int expectedValue = gC.GetYear(dateTime) + 2333;
            int actualValue;
            actualValue = kC.GetYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the method with random datetime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            Int64 ticks = _generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1);
            DateTime dateTime = new DateTime(ticks);
            dateTime = gC.ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            int expectedValue = gC.GetYear(dateTime) + 2333;
            int actualValue;
            actualValue = kC.GetYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}