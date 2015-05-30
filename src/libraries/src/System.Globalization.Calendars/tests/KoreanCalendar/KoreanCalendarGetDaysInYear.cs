// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetDaysInYear(System.Int32,System.Int32)
    public class KoreanCalendarGetDaysInYear
    {
        #region Positive Test Logic
        // PosTest1:Invoke the mthod with Min DateTime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(1, 1, 1, 0, 0, 0, 0);
            int expectedValue = gC.GetDaysInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the mthod with Max DateTime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(9999, 12, 31, 0, 0, 0, 0);
            int expectedValue = gC.GetDaysInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the mthod with leap year DateTime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(2004, 2, 29, 0, 0, 0, 0);
            int expectedValue = gC.GetDaysInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the mthod with random argument
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            System.Globalization.Calendar gC = new GregorianCalendar();
            DateTime dateTime = new DateTime(TestLibrary.Generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            dateTime = new GregorianCalendar().ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, 0);
            int month = TestLibrary.Generator.GetInt16(-55) % 12 + 1;
            int expectedValue = gC.GetDaysInYear(dateTime.Year, gC.GetEra(dateTime));
            int actualValue;
            actualValue = kC.GetDaysInYear(dateTime.Year + 2333, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negative Test Logic
        // NegTest1:Invoke the method with the year outside the lower supported range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 2334;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(1, era);
            });
        }

        // NegTest2:Invoke the method with negative years outside the lower supported range
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 2334;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(-1, era);
            });
        }

        // NegTest3:Invoke the method with years outside the lower supported range
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 2334;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(2333, era);
            });
        }

        // NegTest4:Invoke the method with random years outside the lower supported range
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 2334;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(2333, era);
            });
        }

        // NegTest5:Invoke the method with years outside the upper supported range
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 10000 + 12333;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(12333, era);
            });
        }

        // NegTest6:Invoke the method with random years outside the upper supported range
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 10000 + 12333;
            int era = kC.GetEra(new DateTime());
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(year, era);
            });
        }

        // NegTest7:Invoke the method with era outside the lower supported range
        [Fact]
        public void NegTest7()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 10000 + 2333;
            int era = -1;
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(year, era);
            });
        }

        // NegTest8:Invoke the method with era outside the upper supported range
        [Fact]
        public void NegTest8()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = TestLibrary.Generator.GetInt16(-55) % 10000 + 2333;
            // In order for an ArgumentOutOfRangeException to get thrown due to an era being outside the supported range, the era must
            // be greater than 1 since 0 and 1 are supported eras
            int era = 2;
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.GetDaysInYear(year, era);
            });
        }
        #endregion
    }
}