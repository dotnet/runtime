// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
