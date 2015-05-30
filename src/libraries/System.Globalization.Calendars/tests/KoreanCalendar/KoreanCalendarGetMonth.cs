// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.GetMonth(System.DateTime)
    public class KoreanCalendarGetMonth
    {
        #region Test Logic
        // PosTest1:Invoke the method with min datetime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc);
            int expectedValue = dateTime.Month;
            int actualValue;
            actualValue = kC.GetMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with max datetime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(DateTime.MaxValue.Ticks, DateTimeKind.Utc);
            int expectedValue = dateTime.Month;
            int actualValue;
            actualValue = kC.GetMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with leap year datetime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(2004, 2, 29, 0, 0, 0, 0);
            int expectedValue = dateTime.Month;
            int actualValue;
            actualValue = kC.GetMonth(dateTime);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}