// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization.Tests
{
    public static class TaiwanCalendarUtilities
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        private static readonly int[] s_daysPerMonthLeapYear = new int[]
        {
            0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        private static readonly int[] s_daysPerMonthCommonYear = new int[]
        {
            0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
        };

        public static int RandomYear()
        {
            TaiwanCalendar calendar = new TaiwanCalendar();
            return new Random(-55).Next(calendar.MinSupportedDateTime.Year, calendar.MaxSupportedDateTime.Year - 1911);
        }

        public static int RandomMonth() => new Random(-55).Next(1, 12);

        public static int RandomDay(int year, int month)
        {
            if (new TaiwanCalendar().IsLeapYear(year))
            {
                return new Random(-55).Next(1, s_daysPerMonthLeapYear[month] + 1);
            }
            else
            {
                return new Random(-55).Next(1, s_daysPerMonthCommonYear[month] + 1);
            }
        }

        public static DateTime RandomDateTime()
        {
            int randomYear = RandomYear();
            int randomMonth = RandomMonth();
            int randomDay = RandomDay(randomYear, randomMonth);
            return new DateTime(randomYear, randomMonth, randomDay);
        }
    }
}
