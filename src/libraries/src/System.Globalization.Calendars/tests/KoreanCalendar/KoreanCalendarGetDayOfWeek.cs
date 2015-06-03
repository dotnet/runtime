// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetDayOfWeek(System.DateTime)
    public class KoreanCalendarGetDayOfWeek
    {
        #region Test Logic
        // PosTest1:Test is with min DateTime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(1, 1, 1, 0, 0, 0, 0);
            DayOfWeek expectedValue;
            DayOfWeek actualValue;
            expectedValue = gC.GetDayOfWeek(dateTime);
            actualValue = kC.GetDayOfWeek(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Test it with max DateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            DayOfWeek expectedValue;
            DayOfWeek actualValue;
            expectedValue = gC.GetDayOfWeek(dateTime);
            actualValue = kC.GetDayOfWeek(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Test it with leap year DateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(2000, 2, 29, 0, 0, 0, 0);
            DayOfWeek expectedValue;
            DayOfWeek actualValue;
            expectedValue = gC.GetDayOfWeek(dateTime);
            actualValue = kC.GetDayOfWeek(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Test it with random DateTime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new DateTime(TestLibrary.Generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            dateTime = new GregorianCalendar().ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            DayOfWeek expectedValue;
            DayOfWeek actualValue;
            expectedValue = gC.GetDayOfWeek(dateTime);
            actualValue = kC.GetDayOfWeek(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}