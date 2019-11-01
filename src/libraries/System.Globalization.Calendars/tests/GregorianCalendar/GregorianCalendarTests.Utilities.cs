// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization.Tests
{
    public class GregorianCalendarTestUtilities
    {
        [ThreadStatic]
        private static RandomDataGenerator t_randomDataGenerator;

        private static RandomDataGenerator Generator => t_randomDataGenerator ?? (t_randomDataGenerator = new RandomDataGenerator());

        private static readonly Calendar s_calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        private static readonly int[] s_daysInMonthInLeapYear = { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        private static readonly int[] s_daysInMonthInCommonYear = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static bool IsLeapYear(int year)
        {
            return (year % 400 == 0) || ((year % 100 != 0) && ((year & 0x3) == 0));
        }

        public static int RandomYear()
        {
            int maxYear = s_calendar.MaxSupportedDateTime.Year;
            int minYear = s_calendar.MinSupportedDateTime.Year;
            return minYear + Generator.GetInt32(-55) % (maxYear + 1 - minYear);
        }

        public static int RandomLeapYear()
        {
            // A leap year is any year divisible by 4 except for centennial years(those ending in 00)
            // which are only leap years if they are divisible by 400.
            int year = ~(~RandomYear() | 0x3); // year will be divisible by 4 since the 2 least significant bits will be 0
            year = (0 != year % 100) ? year : (year - year % 400); // if year is divisible by 100 subtract years from it to make it divisible by 400
            // if year was 100, 200, or 300 the above logic will result in 0
            if (year == 0)
            {
                year = 400;
            }

            return year;
        }

        public static int RandomCommonYear()
        {
            int randomYear;
            do
            {
                randomYear = RandomYear();
            }
            while ((0 == (randomYear & 0x3) && 0 != randomYear % 100) || 0 == randomYear % 400);
            return randomYear;
        }

        public static int RandomMonth() => Generator.GetInt32(-55) % 12 + 1;

        public static int RandomMonthNotFebruary()
        {
            int randomMonthNotFebruary;
            do
            {
                randomMonthNotFebruary = RandomMonth();
            } while (randomMonthNotFebruary == 2);
            return randomMonthNotFebruary;
        }

        public static int RandomLeapYearDay(int month) => Generator.GetInt32(-55) % s_daysInMonthInLeapYear[month] + 1;

        public static int RandomCommonYearDay(int month) => Generator.GetInt32(-55) % s_daysInMonthInCommonYear[month] + 1;

        public static int RandomDay(int year, int month)
        {
            return IsLeapYear(year) ? RandomLeapYearDay(month) : RandomCommonYearDay(month);
        }
    }
}
