// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class GregorianCalendarGetDayOfMonth
    {
        #region Positive Test Cases
        // PosTest1:Verify method GetDayOfMonth with args 2006-1-1
        [Fact]
        public void PosTest1()
        {
            DateTime myDT = new GregorianCalendar().ToDateTime(2006, 1, 1, 0, 0, 0, 0);
            System.Globalization.Calendar myCal = new GregorianCalendar();
            Assert.Equal(1, myCal.GetDayOfMonth(myDT));
        }

        // PosTest2:Verify method GetDayOfMonth with args 2006-1-17
        [Fact]
        public void PosTest2()
        {
            DateTime myDT = new GregorianCalendar().ToDateTime(2006, 1, 17, 0, 0, 0, 0);
            System.Globalization.Calendar myCal = new GregorianCalendar();
            Assert.Equal(17, myCal.GetDayOfMonth(myDT));
        }

        // PosTest3:Verify method GetDayOfMonth with args 2006-1-31
        [Fact]
        public void PosTest3()
        {
            DateTime myDT = new GregorianCalendar().ToDateTime(2006, 1, 31, 0, 0, 0, 0);
            System.Globalization.Calendar myCal = new GregorianCalendar();
            Assert.Equal(31, myCal.GetDayOfMonth(myDT));
        }
        #endregion
    }
}
