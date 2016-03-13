// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.AddYears(System.DateTime,System.Int32)
    public class KoreanCalendarAddYears
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the method with min datetime
        [Fact]
        public void PosTest1()
        {
            DateTime dateTime = DateTime.MinValue;
            int yearAdd = _generator.GetInt16(-55) % 9999;
            DateTime expectedValue = new GregorianCalendar().ToDateTime(dateTime.Year + yearAdd, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            DateTime actualValue;
            System.Globalization.Calendar kC = new KoreanCalendar();
            actualValue = kC.AddYears(dateTime, yearAdd);
            Assert.Equal(kC.GetYear(expectedValue), kC.GetYear(actualValue));
        }

        // PosTest2:Invoke the method with max datetime
        [Fact]
        public void PosTest2()
        {
            int yearAdd = _generator.GetInt16(-55) % 9999 * (-1);
            DateTime dateTime = DateTime.MaxValue;
            DateTime expectedValue = new GregorianCalendar().ToDateTime(dateTime.Year + yearAdd, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            DateTime actualValue;
            System.Globalization.Calendar kC = new KoreanCalendar();
            actualValue = kC.AddYears(dateTime, yearAdd);
            Assert.Equal(kC.GetYear(expectedValue), kC.GetYear(actualValue));
        }

        // PosTest3:Invoke the method with normal datetime
        [Fact]
        public void PosTest3()
        {
            int yearAdd = _generator.GetInt16(-55) % 7999;
            DateTime dateTime = new DateTime(2000, 1, 1);
            DateTime expectedValue = new GregorianCalendar().ToDateTime(dateTime.Year + yearAdd, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            DateTime actualValue;
            System.Globalization.Calendar kC = new KoreanCalendar();
            actualValue = kC.AddYears(dateTime, yearAdd);
            Assert.Equal(kC.GetYear(expectedValue), kC.GetYear(actualValue));
        }

        // PosTest4:Invoke the method with normal datetime and negative years
        [Fact]
        public void PosTest4()
        {
            int yearAdd = _generator.GetInt16(-55) % 2000 * (-1);
            DateTime dateTime = new DateTime(2000, 1, 1);
            DateTime expectedValue = new GregorianCalendar().ToDateTime(dateTime.Year + yearAdd, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            DateTime actualValue;
            System.Globalization.Calendar kC = new KoreanCalendar();
            actualValue = kC.AddYears(dateTime, yearAdd);
            Assert.Equal(kC.GetYear(expectedValue), kC.GetYear(actualValue));
        }

        // PosTest5:Invoke the method with leap year
        [Fact]
        public void PosTest5()
        {
            int yearAdd = 2;
            DateTime dateTime = new DateTime(2004, 2, 29);
            DateTime expectedValue = new GregorianCalendar().ToDateTime(dateTime.Year + yearAdd, dateTime.Month, dateTime.Day - 1, 0, 0, 0, 0);
            DateTime actualValue;
            System.Globalization.Calendar kC = new KoreanCalendar();
            actualValue = kC.AddYears(dateTime, yearAdd);
            Assert.Equal(kC.GetYear(expectedValue), kC.GetYear(actualValue));
            Assert.Equal(kC.GetDayOfMonth(expectedValue), kC.GetDayOfMonth(actualValue));
        }
        #endregion
    }
}
