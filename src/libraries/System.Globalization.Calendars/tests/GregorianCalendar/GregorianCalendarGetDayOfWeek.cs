// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class GregorianCalendarGetDayOfWeek
    {
        private System.Globalization.Calendar _calendar = new GregorianCalendar();

        #region Positive Test Cases
        // PosTest: Get day of week
        [Fact]
        public void PosTest1()
        {
            Verification(_calendar, new DateTime(2007, 1, 1), DayOfWeek.Monday, "001.1");
        }

        [Fact]
        public void PosTest2()
        {
            Verification(_calendar, new DateTime(2006, 2, 28), DayOfWeek.Tuesday, "001.2");
        }
        [Fact]
        public void PosTest3()
        {
            Verification(_calendar, new DateTime(2006, 3, 1), DayOfWeek.Wednesday, "001.3");
        }
        [Fact]
        public void PosTest4()
        {
            Verification(_calendar, new DateTime(2006, 8, 31), DayOfWeek.Thursday, "001.4");
        }
        [Fact]
        public void PosTest5()
        {
            Verification(_calendar, new DateTime(2008, 2, 29), DayOfWeek.Friday, "001.5");
        }

        [Fact]
        public void PosTest6()
        {
            Verification(_calendar, new DateTime(2006, 12, 30), DayOfWeek.Saturday, "001.6");
        }

        [Fact]
        public void PosTest7()
        {
            Verification(_calendar, new DateTime(2006, 12, 31), DayOfWeek.Sunday, "001.7");
        }

        [Fact]
        public void PosTest8()
        {
            Verification(_calendar, DateTime.MaxValue, DayOfWeek.Friday, "001.8");
        }

        [Fact]
        public void PosTest9()
        {
            Verification(_calendar, DateTime.MinValue, DayOfWeek.Monday, "001.9");
        }

        [Fact]
        public void PosTest10()
        {
            Verification(_calendar, new DateTime(2000, 2, 29), DayOfWeek.Tuesday, "001.10");
        }

        #endregion
        #region Private Methods
        private void Verification(Calendar calendar, DateTime time, DayOfWeek expected, string errorno)
        {
            DayOfWeek RealVal = calendar.GetDayOfWeek(time);
            Assert.Equal(expected, RealVal);
        }
        #endregion
    }
}
