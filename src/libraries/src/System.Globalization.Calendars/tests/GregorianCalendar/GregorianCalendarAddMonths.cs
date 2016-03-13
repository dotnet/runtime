// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // GregorianCalendar.AddMonths(DateTime, int)
    public class GregorianCalendarAddMonths
    {
        private const int c_MIN_MONTHS_NUMBER = -120000;
        private const int c_MAX_MONTHS_NUMBER = 120000;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive tests
        // PosTest1: Add zero month to the specified date time
        [Fact]
        public void PosTest1()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = 0;
            initialTime = DateTime.Now;

            resultingTime = myCalendar.AddMonths(initialTime, months);
            Assert.Equal(initialTime, resultingTime);
        }

        // PosTest2: the specified time is MinSupportedDateTime and the number of months added is a random value between 1 and 120000
        [Fact]
        public void PosTest2()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = _generator.GetInt32(-55) % c_MAX_MONTHS_NUMBER + 1;
            initialTime = myCalendar.MinSupportedDateTime;
            resultingTime = myCalendar.AddMonths(initialTime, months);

            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        // PosTest3: the specified time is MaxSupportedDateTime and the number of months added is a random value between -120000 and -1
        [Fact]
        public void PosTest3()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = -1 * _generator.GetInt32(-55) % c_MAX_MONTHS_NUMBER - 1;
            initialTime = myCalendar.MaxSupportedDateTime;
            resultingTime = myCalendar.AddMonths(initialTime, months);
            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        // PosTest4: the specified time is random value between 0 and MaxSupportedDateTime, months added is a normal value
        [Fact]
        public void PosTest4()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = -1;
            long maxTimeInTicks = myCalendar.MaxSupportedDateTime.Ticks;
            long minTimeInTikcs = myCalendar.MinSupportedDateTime.Ticks;
            initialTime = new DateTime(_generator.GetInt64(-55) % (maxTimeInTicks + 1));
            resultingTime = myCalendar.AddMonths(initialTime, months);
            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        // PosTest5: the specified time is February in leap year, months added is a normal value
        [Fact]
        public void PosTest5()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = 13;
            initialTime = myCalendar.ToDateTime(2000, 2, 29, 10, 30, 24, 0);
            resultingTime = myCalendar.AddMonths(initialTime, months);
            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        // PosTest6: the specified time is February in leap year, months added is a normal value
        [Fact]
        public void PosTest6()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = 48;
            initialTime = myCalendar.ToDateTime(1996, 2, 29, 10, 30, 24, 0);
            resultingTime = myCalendar.AddMonths(initialTime, months);
            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        // PosTest7: the specified time is any month other than February in leap year, months added is a normal value
        [Fact]
        public void PosTest7()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = 48;
            initialTime = myCalendar.ToDateTime(1996, 3, 29, 10, 30, 24, 0);
            resultingTime = myCalendar.AddMonths(initialTime, months);
            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        // PosTest8: the specified time is February in common year, months added is a normal value
        [Fact]
        public void PosTest8()
        {
            DateTime initialTime;
            int months;
            DateTime resultingTime;
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            months = 48;
            initialTime = myCalendar.ToDateTime(1999, 2, 28, 10, 30, 24, 0);
            resultingTime = myCalendar.AddMonths(initialTime, months);
            VerifyAddMonthsResult(myCalendar, initialTime, resultingTime, months);
        }

        #endregion
        #region Helper methods for positive tests
        private void VerifyAddMonthsResult(Calendar calendar, DateTime oldTime, DateTime newTime, int months)
        {
            int oldYear = calendar.GetYear(oldTime);
            int oldMonth = calendar.GetMonth(oldTime);
            int oldDay = calendar.GetDayOfMonth(oldTime);
            long oldTicksOfDay = oldTime.Ticks % TimeSpan.TicksPerDay;
            int newYear = calendar.GetYear(newTime);
            int newMonth = calendar.GetMonth(newTime);
            int newDay = calendar.GetDayOfMonth(newTime);
            long newTicksOfDay = newTime.Ticks % TimeSpan.TicksPerDay;
            Assert.Equal(oldTicksOfDay, newTicksOfDay);
            Assert.False(newDay > oldDay);
            Assert.False(newYear * 12 + newMonth != oldYear * 12 + oldMonth + months);
        }

        #endregion
        
        #region Helper method for all the tests
        private string GetParamesInfo(DateTime time, int months)
        {
            string str = string.Empty;
            str += string.Format("\nThe initial time is {0}, number of months added is {1}.", time, months);
            return str;
        }
        #endregion
    }
}
