// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using static System.Globalization.GregorianCalendar;

namespace System.Globalization
{
    public static class ISOWeek
    {
        private const int WeeksInLongYear = 53;
        private const int WeeksInShortYear = 52;

        private const int MinWeek = 1;
        private const int MaxWeek = WeeksInLongYear;

        public static int GetWeekOfYear(DateTime date)
        {
            int week = GetWeekNumber(date);

            if (week < MinWeek)
            {
                // If the week number obtained equals 0, it means that the
                // given date belongs to the preceding (week-based) year.
                return GetWeeksInYear(date.Year - 1);
            }

            if (week > GetWeeksInYear(date.Year))
            {
                // If a week number of 53 is obtained, one must check that
                // the date is not actually in week 1 of the following year.
                return MinWeek;
            }

            return week;
        }

        public static int GetYear(DateTime date)
        {
            int week = GetWeekNumber(date);

            if (week < MinWeek)
            {
                // If the week number obtained equals 0, it means that the
                // given date belongs to the preceding (week-based) year.
                return date.Year - 1;
            }

            if (week > GetWeeksInYear(date.Year))
            {
                // If a week number of 53 is obtained, one must check that
                // the date is not actually in week 1 of the following year.
                return date.Year + 1;
            }

            return date.Year;
        }

        // The year parameter represents an ISO week-numbering year (also called ISO year informally).
        // Each week's year is the Gregorian year in which the Thursday falls.
        // The first week of the year, hence, always contains 4 January.
        // ISO week year numbering therefore slightly deviates from the Gregorian for some days close to 1 January.
        public static DateTime GetYearStart(int year)
        {
            return ToDateTime(year, MinWeek, DayOfWeek.Monday);
        }

        // The year parameter represents an ISO week-numbering year (also called ISO year informally).
        // Each week's year is the Gregorian year in which the Thursday falls.
        // The first week of the year, hence, always contains 4 January.
        // ISO week year numbering therefore slightly deviates from the Gregorian for some days close to 1 January.
        public static DateTime GetYearEnd(int year)
        {
            return ToDateTime(year, GetWeeksInYear(year), DayOfWeek.Sunday);
        }

        // From https://en.wikipedia.org/wiki/ISO_week_date#Weeks_per_year:
        //
        // The long years, with 53 weeks in them, can be described by any of the following equivalent definitions:
        //
        // - Any year starting on Thursday and any leap year starting on Wednesday.
        // - Any year ending on Thursday and any leap year ending on Friday.
        // - Years in which 1 January and 31 December (in common years) or either (in leap years) are Thursdays.
        //
        // All other week-numbering years are short years and have 52 weeks.
        public static int GetWeeksInYear(int year)
        {
            if (year < MinYear || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException(nameof(year), SR.ArgumentOutOfRange_Year);
            }

            int P(int y) => (y + (y / 4) - (y / 100) + (y / 400)) % 7;

            if (P(year) == 4 || P(year - 1) == 3)
            {
                return WeeksInLongYear;
            }

            return WeeksInShortYear;
        }

        // From https://en.wikipedia.org/wiki/ISO_week_date#Calculating_a_date_given_the_year,_week_number_and_weekday:
        //
        // This method requires that one know the weekday of 4 January of the year in question.
        // Add 3 to the number of this weekday, giving a correction to be used for dates within this year.
        //
        // Multiply the week number by 7, then add the weekday. From this sum subtract the correction for the year.
        // The result is the ordinal date, which can be converted into a calendar date.
        //
        // If the ordinal date thus obtained is zero or negative, the date belongs to the previous calendar year.
        // If greater than the number of days in the year, to the following year.
        public static DateTime ToDateTime(int year, int week, DayOfWeek dayOfWeek)
        {
            if (year < MinYear || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException(nameof(year), SR.ArgumentOutOfRange_Year);
            }

            if (week < MinWeek || week > MaxWeek)
            {
                throw new ArgumentOutOfRangeException(nameof(week), SR.ArgumentOutOfRange_Week_ISO);
            }

            // We allow 7 for convenience in cases where a user already has a valid ISO
            // day of week value for Sunday. This means that both 0 and 7 will map to Sunday.
            // The GetWeekday method will normalize this into the 1-7 range required by ISO.
            if ((int)dayOfWeek < 0 || (int)dayOfWeek > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), SR.ArgumentOutOfRange_DayOfWeek);
            }

            var jan4 = new DateTime(year, month: 1, day: 4);

            int correction = GetWeekday(jan4.DayOfWeek) + 3;

            int ordinal = (week * 7) + GetWeekday(dayOfWeek) - correction;

            return new DateTime(year, month: 1, day: 1).AddDays(ordinal - 1);
        }

        // From https://en.wikipedia.org/wiki/ISO_week_date#Calculating_the_week_number_of_a_given_date:
        //
        // Using ISO weekday numbers (running from 1 for Monday to 7 for Sunday),
        // subtract the weekday from the ordinal date, then add 10. Divide the result by 7.
        // Ignore the remainder; the quotient equals the week number.
        //
        // If the week number thus obtained equals 0, it means that the given date belongs to the preceding (week-based) year.
        // If a week number of 53 is obtained, one must check that the date is not actually in week 1 of the following year.
        private static int GetWeekNumber(DateTime date)
        {
            return (date.DayOfYear - GetWeekday(date.DayOfWeek) + 10) / 7;
        }

        // Day of week in ISO is represented by an integer from 1 through 7, beginning with Monday and ending with Sunday.
        // This matches the underlying values of the DayOfWeek enum, except for Sunday, which needs to be converted.
        private static int GetWeekday(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Sunday ? 7 : (int) dayOfWeek;
        }
    }
}
