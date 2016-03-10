// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarTests
{
    // GregorianCalendar.GetWeekOfYears(DateTime, CalendarWeekRule, DayOfWeek)
    public class GregorianCalendarGetWeekOfYears
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private const int c_DAYS_PER_WEEK = 7;
        private static readonly int[] s_daysInMonth365 =
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };
        private static readonly int[] s_daysInMonth366 =
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        #region Positive tests
        // PosTest1: leap year, any month other than February and any day
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            year = GetALeapYear(myCalendar);
            //Get a random value beween 1 and 12 not including 2.
            do
            {
                month = _generator.GetInt32(-55) % 12 + 1;
            } while (2 == month);
            //Get a day beween 1 and last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth366[month] + 1;
            ExecutePosTest("001", "002", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        // PosTest2: leap year, any day in February
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            year = GetALeapYear(myCalendar);
            month = 2;
            //Get a day beween 1 and the last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth366[month] + 1;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            ExecutePosTest("003", "004", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        // PosTest3: common year, February, any day
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            year = GetACommonYear(myCalendar);
            month = 2;
            //Get a day beween 1 and the last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            ExecutePosTest("005", "006", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        // PosTest4: common year, any day in any month other than February
        [Fact]
        public void PosTest4()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            year = GetACommonYear(myCalendar);
            //Get a random value beween 1 and 12 not including 2.
            do
            {
                month = _generator.GetInt32(-55) % 12 + 1;
            }
            while (2 == month);
            //Get a day beween 1 and the last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            ExecutePosTest("007", "008", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        // PosTest5: Maximum supported year, any month and any day
        [Fact]
        public void PosTest5()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            year = myCalendar.MaxSupportedDateTime.Year;
            //Get a random value beween 1 and 12
            month = _generator.GetInt32(-55) % 12 + 1;
            //Get a day beween 1 and the last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            ExecutePosTest("009", "010", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        // PosTest6: Minimum supported year, any month
        [Fact]
        public void PosTest6()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            year = myCalendar.MinSupportedDateTime.Year;
            //Get a random value beween 1 and 12
            month = _generator.GetInt32(-55) % 12 + 1;
            //Get a day beween 1 and the last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            ExecutePosTest("011", "012", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        // PosTest7: Any year, any month, any day
        [Fact]
        public void PosTest7()
        {
            System.Globalization.Calendar myCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
            int year, month, day;
            CalendarWeekRule rule;
            DayOfWeek firstDayOfWeek;
            year = this.GetAYear(myCalendar);
            //Get a random value beween 1 and 12
            month = _generator.GetInt32(-55) % 12 + 1;
            //Get a day beween 1 and the last day of the month
            day = _generator.GetInt32(-55) % s_daysInMonth365[month] + 1;
            rule = (CalendarWeekRule)(_generator.GetInt32(-55) % 3);
            firstDayOfWeek = (DayOfWeek)(_generator.GetInt32(-55) % 7);
            ExecutePosTest("013", "014", myCalendar, year, month, day, rule, firstDayOfWeek);
        }

        #endregion
        #region Helper methods for postive tests
        private int GetDayOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek, int weekOfYear, Calendar myCalendar)
        {
            int retVal = -367;
            int offset = 0;
            int dayOfWeekForJan1, dayOfYear, dayOfWeek;
            dayOfYear = myCalendar.GetDayOfYear(time); //1-based
            dayOfWeek = (int)myCalendar.GetDayOfWeek(time) - (int)firstDayOfWeek + 1; //1-based
            if (dayOfWeek <= 0)
                dayOfWeek += c_DAYS_PER_WEEK; //Make it a positive value
            dayOfWeekForJan1 = dayOfWeek - (dayOfYear - 1) % c_DAYS_PER_WEEK; //1-based
            if (dayOfWeekForJan1 <= 0)
                dayOfWeekForJan1 += c_DAYS_PER_WEEK; //Make it a positive value
                                                     // When the day of specific time falls on the previous year,
                                                     // return the number of days from January 1 directly.
                                                     // There could be 6 weeks within a month.
            if (time.Month == 1 && weekOfYear > 6)
            {
                return dayOfWeek - dayOfWeekForJan1 + 1;
            }

            switch (rule)
            {
                case CalendarWeekRule.FirstDay:
                    offset = dayOfWeek - dayOfWeekForJan1;
                    break;
                case CalendarWeekRule.FirstFourDayWeek:
                    if (dayOfWeekForJan1 <= 4)
                    {
                        offset = dayOfWeek - dayOfWeekForJan1;
                    }
                    else
                    {
                        offset = dayOfWeek + c_DAYS_PER_WEEK - dayOfWeekForJan1;
                    }

                    break;
                case CalendarWeekRule.FirstFullWeek:
                    if (dayOfWeekForJan1 == 1)
                    {
                        offset = dayOfWeek - dayOfWeekForJan1;
                    }
                    else
                    {
                        offset = dayOfWeek + c_DAYS_PER_WEEK - dayOfWeekForJan1;
                    }

                    break;
            }
            retVal = (weekOfYear - 1) * c_DAYS_PER_WEEK + offset + 1;
            return retVal;
        }

        private void ExecutePosTest(string errorNum1, string errorNum2, Calendar myCalendar, int year,
            int month, int day, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            DateTime time;
            int actualDayOfYear, expectedDayOfYear;
            int weekOfYear;
            time = myCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            expectedDayOfYear = myCalendar.GetDayOfYear(time);
            weekOfYear = myCalendar.GetWeekOfYear(time, rule, firstDayOfWeek);
            actualDayOfYear = this.GetDayOfYear(time, rule, firstDayOfWeek, weekOfYear, myCalendar);
            Assert.Equal(expectedDayOfYear, actualDayOfYear);
        }

        #endregion
        
        #region Helper methods for all the tests
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

        //Get text represntation of the input parmeters
        private string GetParamsInfo(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            string str;
            str = string.Format("\nThe specified time: {0}).", time);
            str += string.Format("\nThe calendar week rule: {0}", rule);
            str += string.Format("\nThe first day of week: {0}", firstDayOfWeek);
            return str;
        }
        #endregion
    }
}
