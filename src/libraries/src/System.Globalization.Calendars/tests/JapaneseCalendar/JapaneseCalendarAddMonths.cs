// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // AddMonths(System.DateTime,System.Int32)
    public class JapaneseCalendarAddMonths
    {
        #region Positive Test Cases
        // PosTest1: Call AddMonths to add valid value
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar calendar = new JapaneseCalendar();

            VerificationHelper(calendar, new DateTime(2006, 11, 28), 1, new DateTime(2006, 12, 28));
            VerificationHelper(calendar, new DateTime(2006, 11, 28), -1, new DateTime(2006, 10, 28));
            VerificationHelper(calendar, new DateTime(2006, 11, 28), 0, new DateTime(2006, 11, 28));
            VerificationHelper(calendar, new DateTime(2006, 11, 28), 1000, new DateTime(2090, 3, 28));

            VerificationHelper(calendar, new DateTime(2006, 12, 1), 1, new DateTime(2007, 1, 1));
            VerificationHelper(calendar, new DateTime(2007, 1, 1), -1, new DateTime(2006, 12, 1));
        }

        // PosTest2: Call AddMonths to add boundary value
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar calendar = new JapaneseCalendar();

            VerificationHelper(calendar, new DateTime(1868, 9, 8), 1, new DateTime(1868, 10, 8));
            VerificationHelper(calendar, new DateTime(1868, 10, 8), -1, new DateTime(1868, 9, 8));
            VerificationHelper(calendar, new DateTime(9999, 11, 30), 1, new DateTime(9999, 12, 30));
            VerificationHelper(calendar, new DateTime(9999, 12, 30), -1, new DateTime(9999, 11, 30));
            VerificationHelper(calendar, DateTime.MaxValue, 0, DateTime.MaxValue);
            VerificationHelper(calendar, new DateTime(1868, 9, 8), 0, new DateTime(1868, 9, 8));
        }

        // PosTest3: Call AddMonths to add month when the day is not in the month
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar calendar = new JapaneseCalendar();

            VerificationHelper(calendar, new DateTime(2006, 10, 31), 1, new DateTime(2006, 11, 30));
            VerificationHelper(calendar, new DateTime(2006, 12, 31), -1, new DateTime(2006, 11, 30));
        }
        #endregion

        #region Private Methods
        private void VerificationHelper(Calendar calendar, DateTime time, int months, DateTime expected)
        {
            DateTime actual = calendar.AddMonths(time, months);
            Assert.Equal(expected, actual);
        }

        private void VerificationHelper<T>(Calendar calendar, DateTime time, int months) where T : Exception
        {
            Assert.Throws<T>(() =>
            {
                DateTime actual = calendar.AddMonths(time, months);
            });
        }
        #endregion
    }
}
