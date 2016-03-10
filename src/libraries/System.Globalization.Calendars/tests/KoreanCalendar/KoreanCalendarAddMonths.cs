// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
