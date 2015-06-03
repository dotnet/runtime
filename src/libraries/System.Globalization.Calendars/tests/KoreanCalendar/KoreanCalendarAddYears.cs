// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.AddYears(System.DateTime,System.Int32)
    public class KoreanCalendarAddYears
    {
        #region Positive Test Logic
        // PosTest1:Invoke the method with min datetime
        [Fact]
        public void PosTest1()
        {
            DateTime dateTime = DateTime.MinValue;
            int yearAdd = TestLibrary.Generator.GetInt16(-55) % 9999;
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
            int yearAdd = TestLibrary.Generator.GetInt16(-55) % 9999 * (-1);
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
            int yearAdd = TestLibrary.Generator.GetInt16(-55) % 7999;
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
            int yearAdd = TestLibrary.Generator.GetInt16(-55) % 2000 * (-1);
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

        #region Negative Test Logic
        // NegTest1:Invoke the method with the result outside the upper supported range
        [Fact]
        public void NegTest1()
        {
            int year = TestLibrary.Generator.GetInt16(-55) % 9999 + 1;
            int month = TestLibrary.Generator.GetInt16(-55) % 12 + 1;
            int day = TestLibrary.Generator.GetInt16(-55) % 28 + 1;
            DateTime dateTime = new GregorianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0);
            int yearAdd = 9999 - year + 2;
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.AddYears(new GregorianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0), yearAdd);
            });
        }

        // NegTest2:Invoke the method with the result outside the lower supported range
        [Fact]
        public void NegTest2()
        {
            int year = TestLibrary.Generator.GetInt16(-55) % 9999 + 1;
            int month = TestLibrary.Generator.GetInt16(-55) % 12 + 1;
            int day = TestLibrary.Generator.GetInt16(-55) % 28 + 1;
            DateTime dateTime = new GregorianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0);
            int yearAdd = year * (-1);
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.AddYears(new GregorianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0), yearAdd);
            });
        }
        #endregion
    }
}