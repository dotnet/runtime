// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.IsLeapYear(System.Int32,System.Int32)
    public class KoreanCalendarIsLeapYear
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the method with min date time
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int year = dateTime.Year;
            int era = gC.GetEra(dateTime);
            bool expectedValue = gC.IsLeapYear(year, era);
            bool actualValue;
            actualValue = kC.IsLeapYear(year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with max date time
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int year = dateTime.Year;
            int era = gC.GetEra(dateTime);
            bool expectedValue = gC.IsLeapYear(year, era);
            bool actualValue;
            actualValue = kC.IsLeapYear(year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with normal date time
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1900, 2, 28, 0, 0, 0, 0);
            int year = dateTime.Year;
            int era = gC.GetEra(dateTime);
            bool expectedValue = gC.IsLeapYear(year, era);
            bool actualValue;
            actualValue = kC.IsLeapYear(year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the method with leap day date time
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = gC.ToDateTime(1200, 2, 29, 0, 0, 0, 0);
            int year = dateTime.Year;
            int era = gC.GetEra(dateTime);
            bool expectedValue = gC.IsLeapYear(year, era);
            bool actualValue;
            actualValue = kC.IsLeapYear(year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negative Test Logic
        // NegTest1:Invoke the method with the year outside the lower supported range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = 2333;
            int era = 1;
            bool actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.IsLeapYear(year, era);
            });
        }

        // NegTest2:Invoke the method with the year outside the lower supported range
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = 0;
            int era = 1;
            bool actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.IsLeapYear(year, era);
            });
        }

        // NegTest3:Invoke the method with the year outside the upper supported range
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = 123333;
            int era = 1;
            bool actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.IsLeapYear(year, era);
            });
        }

        // NegTest4:Invoke the method with the era outside the lower supported range
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = _generator.GetInt16(-55) % 9999 + 2334;
            int month = _generator.GetInt16(-55) % 12 + 1;
            // The KoreanEra is 1, however using an Era value of 0 defaults to "current era" for the calendar being used. In order to force
            // the ArgumentOutOfRangeException the era must not be 0 or 1
            int era = -1;
            bool actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.IsLeapYear(year, era);
            });
        }

        // NegTest5:Invoke the method with the era outside the upper supported range
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = _generator.GetInt16(-55) % 9999 + 2334;
            int month = _generator.GetInt16(-55) % 12 + 1;
            int era = 2;
            bool actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.IsLeapYear(year, era);
            });
        }
        #endregion
    }
}