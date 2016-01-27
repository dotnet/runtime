// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    public class GregorianCalendarGetDayOfYear
    {
        private System.Globalization.Calendar _calendar = new GregorianCalendar();
        #region Postive Test Case
        // Call GetDayOfYear to get day of year
        [Fact]
        public void PosTest1()
        {
            VerificationHelper(_calendar, new DateTime(2006, 11, 29), 333, "001.1");
        }

        [Fact]
        public void PosTest2()
        {
            VerificationHelper(_calendar, new DateTime(2006, 1, 1), 1, "001.2");
        }

        [Fact]
        public void PosTest3()
        {
            VerificationHelper(_calendar, new DateTime(2007, 12, 31), 365, "001.3");
        }

        [Fact]
        public void PosTest4()
        {
            VerificationHelper(_calendar, new DateTime(2000, 2, 29), 60, "001.4");
        }

        [Fact]
        public void PosTest5()
        {
            VerificationHelper(_calendar, new DateTime(2001, 2, 28), 59, "001.5");
        }

        [Fact]
        public void PosTest6()
        {
            VerificationHelper(_calendar, new DateTime(2000, 1, 1), 1, "001.6");
        }

        [Fact]
        public void PosTest7()
        {
            VerificationHelper(_calendar, new DateTime(2000, 12, 31), 366, "001.7");
        }

        [Fact]
        public void PosTest8()
        {
            VerificationHelper(_calendar, DateTime.MaxValue, 365, "001.8");
        }

        [Fact]
        public void PosTest9()
        {
            VerificationHelper(_calendar, DateTime.MinValue, 1, "001.9");
        }
        #endregion

        #region Private Methods
        private void VerificationHelper(Calendar calendar, DateTime time, int expected, string errorno)
        {
            int actual = calendar.GetDayOfYear(time);
            Assert.Equal(expected, actual);
        }
        #endregion
    }
}
