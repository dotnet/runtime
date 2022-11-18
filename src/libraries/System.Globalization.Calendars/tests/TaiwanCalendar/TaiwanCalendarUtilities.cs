// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization.Tests
{
    public static class TaiwanCalendarUtilities
    {
        [ThreadStatic]
        private static RandomDataGenerator t_randomDataGenerator;

        private static RandomDataGenerator Generator => t_randomDataGenerator ??= new RandomDataGenerator();

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
            int maxYear = calendar.MaxSupportedDateTime.Year;
            int minYear = calendar.MinSupportedDateTime.Year;
            return minYear + Generator.GetInt32(-55) % (maxYear - 1911 + 1 - minYear);
        }

        public static int RandomMonth() => Generator.GetInt32(-55) % 12 + 1;

        public static int RandomDay(int year, int month)
        {
            if (new TaiwanCalendar().IsLeapYear(year))
            {
                return Generator.GetInt32(-55) % s_daysPerMonthLeapYear[month] + 1;
            }
            else
            {
                return Generator.GetInt32(-55) % s_daysPerMonthCommonYear[month] + 1;
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
