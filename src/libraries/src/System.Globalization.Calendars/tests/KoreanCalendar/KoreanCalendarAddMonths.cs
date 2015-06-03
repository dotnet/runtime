// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.AddMonths(System.DateTime,System.Int32)
    public class KoreanCalendarAddMonths
    {
        #region Positive Test Logic
        // PosTest1:Call the method with min time
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime minDT = DateTime.MinValue;
            DateTime expectedValue = new GregorianCalendar().ToDateTime(minDT.Year, minDT.Month, minDT.Day, 0, 0, 0, 0);
            DateTime actualValue;
            actualValue = kC.AddMonths(expectedValue, 1);
            Assert.Equal(kC.GetMonth(expectedValue) + 1, kC.GetMonth(actualValue));
        }

        // PosTest2:Call the method with max time
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime maxDT = DateTime.MaxValue;
            DateTime expectedValue = new GregorianCalendar().ToDateTime(maxDT.Year, maxDT.Month, maxDT.Day, 0, 0, 0, 0);
            DateTime actualValue;
            actualValue = kC.AddMonths(expectedValue, -2);
            Assert.Equal(kC.GetMonth(expectedValue) - 2, kC.GetMonth(actualValue));
        }

        // PosTest3:Call the method with leap time
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime leapDT = new GregorianCalendar().ToDateTime(2003, 1, 29, 0, 0, 0, 0);
            DateTime expectedValue = leapDT;
            DateTime actualValue;
            actualValue = kC.AddMonths(expectedValue, 1);
            Assert.Equal(kC.GetMonth(expectedValue) + 1, kC.GetMonth(actualValue));
            Assert.Equal(kC.GetDayOfMonth(expectedValue) - 1, kC.GetDayOfMonth(actualValue));
        }

        // PosTest4:Call the method with normal time
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime tempDT = new GregorianCalendar().ToDateTime(2006, 7, 31, 0, 0, 0, 0);
            DateTime expectedValue = tempDT;
            DateTime actualValue;
            actualValue = kC.AddMonths(expectedValue, 2);

            Assert.Equal(kC.GetMonth(expectedValue) + 2, kC.GetMonth(actualValue));
            Assert.Equal(kC.GetDayOfMonth(actualValue) + 1, kC.GetDayOfMonth(expectedValue));
        }
        #endregion

        #region Negative Test Logic
        // NegTest1:Invoke the method with min datetime
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.AddMonths(new GregorianCalendar().ToDateTime(1, 1, 1, 0, 0, 0, 0), -1);
            });
        }

        // NegTest2:Invoke the method with max datetime
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.AddMonths(new GregorianCalendar().ToDateTime(9999, 12, 31, 0, 0, 0, 0), 5);
            });
        }

        // NegTest3:Invoke the method with Large months
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.AddMonths(new GregorianCalendar().ToDateTime(9999, 12, 31, 0, 0, 0, 0), -120000);
            });
        }

        // NegTest4:Invoke the method with Large months
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.AddMonths(new GregorianCalendar().ToDateTime(1, 1, 1, 0, 0, 0, 0), 120000);
            });
        }
        #endregion
    }
}