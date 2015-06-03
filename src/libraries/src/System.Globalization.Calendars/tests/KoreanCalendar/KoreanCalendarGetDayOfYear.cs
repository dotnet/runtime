// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetDayOfYear(System.DateTime)
    public class KoreanCalendarGetDayOfYear
    {
        #region Test Logic
        // PosTest1:Invoke the method with min dateTime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = gC.GetDayOfYear(dateTime);
            int actualValue;
            actualValue = kC.GetDayOfYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with max dateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int expectedValue = gC.GetDayOfYear(dateTime);
            int actualValue;
            actualValue = kC.GetDayOfYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with leap year dateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(2000, 2, 29, 0, 0, 0, 0);
            int expectedValue = gC.GetDayOfYear(dateTime);
            int actualValue;
            actualValue = kC.GetDayOfYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the method with random dateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new DateTime(TestLibrary.Generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            dateTime = gC.ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            int expectedValue = gC.GetDayOfYear(dateTime);
            int actualValue;
            actualValue = kC.GetDayOfYear(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}