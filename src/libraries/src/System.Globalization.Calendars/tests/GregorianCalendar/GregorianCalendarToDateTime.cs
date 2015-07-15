// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.ToDateTime(Int32, Int32, Int32, Int32, Int32, Int32, Int32, Int32)
    public class GregorianCalendarToDateTime
    {
        private const int c_DAYS_IN_LEAP_YEAR = 366;
        private const int c_DAYS_IN_COMMON_YEAR = 365;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private static readonly int[] s_daysInMonth365 =
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private static readonly int[] s_daysInMonth366 =
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region Positive tests
        // PosTest1: random valid date time
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = this.GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1; //value of month between 1 - 12
                                                                      //value of day between 1 - last day of month
            int day = (this.IsLeapYear(year)) ? _generator.GetInt32(-55) % s_daysInMonth366[month] + 1 : _generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecutePosTest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // PosTest2: the mininum valid date time value
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = 1;
            int month = 1;
            int day = 1;
            int hour = 0;
            int minute = 0;
            int second = 0;
            int millisecond = 0;
            int era = _generator.GetInt32(-55) & 1;
            ExecutePosTest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // PosTest3: the maximum valid date time value
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = 9999;
            int month = 12;
            int day = 31;
            int hour = 23;
            int minute = 59;
            int second = 59;
            int millisecond = 999;
            int era = _generator.GetInt32(-55) & 1;
            ExecutePosTest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        #endregion
        #region Helper method for positive tests
        private void ExecutePosTest(Calendar myCalendar, int year, int month, int day, int hour, int minute,
            int second, int millisecond, int era)
        {
            DateTime actualTime, expectedTime;
            expectedTime = new DateTime(year, month, day, hour, minute, second, millisecond);
            actualTime = myCalendar.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
            Assert.Equal(expectedTime, actualTime);
        }

        #endregion
        #region Negtive Tests

        // NegTest1: year is greater than maximum supported value
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = myCalendar.MaxSupportedDateTime.Year + 1 + _generator.GetInt32(-55) % (int.MaxValue - myCalendar.MaxSupportedDateTime.Year);
            int month = _generator.GetInt32(-55) % 12 + 1; //value of month between 1 - 12
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest2: year is less than minimum supported value
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = -1 * _generator.GetInt32(-55);
            int month = _generator.GetInt32(-55) % 12 + 1; //value of month between 1 - 12
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest3: month is greater than maximum supported value
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = 13 + _generator.GetInt32(-55) % (int.MaxValue - 12);
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest4: month is less than minimum supported value
        [Fact]
        public void NegTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = -1 * _generator.GetInt32(-55);
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest5: day is greater than maximum supported value
        [Fact]
        public void NegTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = (this.IsLeapYear(year)) ? s_daysInMonth366[month] + 1 + _generator.GetInt32(-55) % (int.MaxValue - s_daysInMonth366[month]) : s_daysInMonth365[month] + 1 + _generator.GetInt32(-55) % (int.MaxValue - s_daysInMonth365[month]);
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest6: day is less than minimum supported value
        [Fact]
        public void NegTest6()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = -1 * _generator.GetInt32(-55);
            int hour = _generator.GetInt32(-55) % 24; //value of hour between 0 - 23
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest7: hour is greater than maximum supported value
        [Fact]
        public void NegTest7()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = 24 + _generator.GetInt32(-55) % (int.MaxValue - 23);
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest8: hour is greater than maximum supported value
        [Fact]
        public void NegTest8()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = -1 * _generator.GetInt32(-55) - 1;
            int minute = _generator.GetInt32(-55) % 60; //value of minute between 0 - 59
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest9: minute is greater than maximum supported value
        [Fact]
        public void NegTest9()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = 60 + _generator.GetInt32(-55) % (int.MaxValue - 59);
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest10: minute is less than minimum supported value
        [Fact]
        public void NegTest10()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = -1 * _generator.GetInt32(-55) - 1;
            int second = _generator.GetInt32(-55) % 60; //value of second between 0 - 59
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest11: second is greater than maximum supported value
        [Fact]
        public void NegTest11()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = _generator.GetInt32(-55) % 60;
            int second = 60 + _generator.GetInt32(-55) % (int.MaxValue - 59);
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest12: second is less than minimum supported value
        [Fact]
        public void NegTest12()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = _generator.GetInt32(-55) % 60;
            int second = -1 * _generator.GetInt32(-55) - 1;
            int millisecond = _generator.GetInt32(-55) % 1000; //value of millisecond between 0 - 999
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest13: millisecond is greater than maximum supported value
        [Fact]
        public void NegTest13()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = _generator.GetInt32(-55) % 60;
            int second = _generator.GetInt32(-55) % 60;
            int millisecond = 1000 + _generator.GetInt32(-55) % (int.MaxValue - 999);
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest14: millisecond is less than minimum supported value
        [Fact]
        public void NegTest14()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = _generator.GetInt32(-55) % 60;
            int second = _generator.GetInt32(-55) % 60;
            int millisecond = -1 * _generator.GetInt32(-55) - 1;
            int era = _generator.GetInt32(-55) & 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest15: era is greater than maximum supported value
        [Fact]
        public void NegTest15()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = _generator.GetInt32(-55) % 60;
            int second = _generator.GetInt32(-55) % 60;
            int millisecond = _generator.GetInt32(-55) % 1000;
            int era = 2 + _generator.GetInt32(-55) % (int.MaxValue - 1);
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        // NegTest16: era is less than minimum supported value
        [Fact]
        public void NegTest16()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year = GetAYear(myCalendar);
            int month = _generator.GetInt32(-55) % 12 + 1;
            int day = 1;
            int hour = _generator.GetInt32(-55) % 24;
            int minute = _generator.GetInt32(-55) % 60;
            int second = _generator.GetInt32(-55) % 60;
            int millisecond = _generator.GetInt32(-55) % 1000;
            int era = -1 * _generator.GetInt32(-55) - 1;
            ExecuteAOORETest(myCalendar, year, month, day, hour, minute, second, millisecond, era);
        }

        #endregion
        #region Helper methods for negtative tests
        private void ExecuteAOORETest(Calendar myCalendar, int year, int month, int day, int hour, int minute,
            int second, int millisecond, int era)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                myCalendar.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
            });
        }

        #endregion
        #region Helper methods for all the tests

        //Indicate whether the specified year is leap year or not
        private bool IsLeapYear(int year)
        {
            if (0 == year % 400 || (0 != year % 100 && 0 == (year & 0x3)))
            {
                return true;
            }

            return false;
        }

        //Get a random year beween minmum supported year and maximum supported year of the specified calendar
        private int GetAYear(Calendar calendar)
        {
            int retVal;
            int maxYear, minYear;
            maxYear = calendar.MaxSupportedDateTime.Year;
            minYear = calendar.MinSupportedDateTime.Year;
            retVal = minYear + _generator.GetInt32(-55) % (maxYear + 1 - minYear);
            return retVal;
        }

        //Get a leap year of the specified calendar
        private int GetALeapYear(Calendar calendar)
        {
            int retVal;
            // A leap year is any year divisible by 4 except for centennial years(those ending in 00)
            // which are only leap years if they are divisible by 400.
            retVal = ~(~GetAYear(calendar) | 0x3); // retVal will be divisible by 4 since the 2 least significant bits will be 0
            retVal = (0 != retVal % 100) ? retVal : (retVal - retVal % 400); // if retVal is divisible by 100 subtract years from it to make it divisible by 400
                                                                             // if retVal was 100, 200, or 300 the above logic will result in 0
            if (0 == retVal)
            {
                retVal = 400;
            }

            return retVal;
        }

        //Get a common year of the specified calendar
        private int GetACommonYear(Calendar calendar)
        {
            int retVal;
            do
            {
                retVal = GetAYear(calendar);
            }
            while ((0 == (retVal & 0x3) && 0 != retVal % 100) || 0 == retVal % 400);
            return retVal;
        }
        #endregion
    }
}