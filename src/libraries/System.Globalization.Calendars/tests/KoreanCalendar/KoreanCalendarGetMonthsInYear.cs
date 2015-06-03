// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetMonthsInYear(System.Int32,System.Int32)
    public class KoreanCalendarGetMonthsInYear
    {
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

        #region Negative Test Logic
        // NegTest1:Invoke the method with the year outside the lower supported range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(0, era);
            });
        }

        // NegTest2:Invoke the method with the year outside the lower supported range
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(2333, era);
            });
        }

        // NegTest3:Invoke the method with random years outside the lower supported range
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 2334;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(year, era);
            });
        }

        // NegTest4:Invoke the method with the year outside the upper supported range
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(12333, era);
            });
        }

        // NegTest5:Invoke the method with the year outside the upper supported range
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(-1, era);
            });
        }

        // NegTest6:Invoke the method with random years outside the upper supported range
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % int.MaxValue + 12333;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(year, era);
            });
        }

        // NegTest7:Invoke the method with era outside the lower supported range
        [Fact]
        public void NegTest7()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 9999 + 2334;
            int era = -1;
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInMonth(year, 1, era);
            });
        }

        // NegTest8:Invoke the method with era outside the upper supported range
        [Fact]
        public void NegTest8()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 9999 + 2334;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                // The in order for this test to throw ArgumentOutOfRangeException, the Era must be greater than 1 or less than 0.
                // Era is the 3rd argument in GetDaysInMonth(year, month, era)
                actualValue = kC.GetDaysInMonth(year, 12, 2);
            });
        }
        #endregion
    }
}