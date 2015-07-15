// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class KoreanCalendarToDateTime
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the mthod with min datetime
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = DateTime.MinValue;
            DateTime expectedValue = dateTime;
            DateTime actualValue;
            actualValue = kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day,
                        dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the mthod with max datetime
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = DateTime.MaxValue;
            DateTime expectedValue = new GregorianCalendar().ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
            DateTime actualValue;
            actualValue = kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the mthod with leap year datetime
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new GregorianCalendar().ToDateTime(2004, 2, 29, 1, 1, 1, 0);
            DateTime expectedValue = dateTime;
            DateTime actualValue;
            actualValue = kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4:Invoke the mthod with random datetime
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            dateTime = new GregorianCalendar().ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, 0);
            DateTime expectedValue = dateTime;
            DateTime actualValue;
            actualValue = kC.ToDateTime(dateTime.Year + 2333, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond, kC.GetEra(dateTime));
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion

        #region Negative Test Logic
        // NegTest1:Invoke the method with year out of range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = 2333;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest2:Invoke the method with year out of range
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = 0;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest3:Invoke the method with year out of range
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = -1;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest4:Invoke the method with year out of range
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = 12333;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest5:Invoke the method with month out of range
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = 0;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest6:Invoke the method with month out of range
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = -1;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest7:Invoke the method with month out of range
        [Fact]
        public void NegTest7()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = 13;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest8:Invoke the method with wrong leap day
        [Fact]
        public void NegTest8()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int year = 2006;
            int month = 2;
            int day = 29;
            int hour = 1;
            int minute = 1;
            int second = 1;
            int msecond = 1;
            int era = 1;
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest9:Invoke the method with day out of range
        [Fact]
        public void NegTest9()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = 0;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest10:Invoke the method with day out of range
        [Fact]
        public void NegTest10()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = -1;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest11:Invoke the method with day out of range
        [Fact]
        public void NegTest11()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = -1;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest12:Invoke the method with day out of range
        [Fact]
        public void NegTest12()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = 32;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest13:Invoke the method with hour out of range
        [Fact]
        public void NegTest13()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = -1;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest14:Invoke the method with hour out of range
        [Fact]
        public void NegTest14()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = 25;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest15:Invoke the method with minute out of range
        [Fact]
        public void NegTest15()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = -1;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest16:Invoke the method with minute out of range
        [Fact]
        public void NegTest16()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = 60;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest17:Invoke the method with second out of range
        [Fact]
        public void NegTest17()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = -1;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest18:Invoke the method with second out of range
        [Fact]
        public void NegTest18()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = 60;
            int msecond = dateTime.Millisecond;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest19:Invoke the method with millisecond out of range
        [Fact]
        public void NegTest19()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = -1;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest20:Invoke the method with millisecond out of range
        [Fact]
        public void NegTest20()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = 1000;
            int era = kC.GetEra(dateTime);
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest21:Invoke the method with era out of range
        [Fact]
        public void NegTest21()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = -1;
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }

        // NegTest22:Invoke the method with era out of range
        [Fact]
        public void NegTest22()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime dateTime = new DateTime(_generator.GetInt64(-55) % (DateTime.MaxValue.Ticks + 1));
            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;
            int msecond = dateTime.Millisecond;
            int era = 2;
            DateTime actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToDateTime(year, month, day, hour, minute, second, msecond, era);
            });
        }
        #endregion
    }
}
