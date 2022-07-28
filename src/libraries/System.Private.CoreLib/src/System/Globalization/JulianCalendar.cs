// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    /// <summary>
    /// This class implements the Julian calendar. In 48 B.C. Julius Caesar
    /// ordered a calendar reform, and this calendar is called Julian calendar.
    /// It consisted of a solar year of twelve months and of 365 days with an
    /// extra day every fourth year.
    /// </summary>
    /// <remarks>
    /// Calendar support range:
    ///     Calendar    Minimum     Maximum
    ///     ==========  ==========  ==========
    ///     Gregorian   0001/01/01   9999/12/31
    ///     Julia       0001/01/03   9999/10/19
    /// </remarks>
    public class JulianCalendar : Calendar
    {
        public static readonly int JulianEra = 1;

        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;

        // Number of days in a non-leap year
        private const int JulianDaysPerYear = 365;

        // Number of days in 4 years
        private const int JulianDaysPer4Years = JulianDaysPerYear * 4 + 1;

        private static readonly int[] s_daysToMonth365 =
        {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
        };

        private static readonly int[] s_daysToMonth366 =
        {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
        };

        // Gregorian Calendar 9999/12/31 = Julian Calendar 9999/10/19
        // keep it as variable field for serialization compat.
        internal int MaxYear = 9999;

        public override DateTime MinSupportedDateTime => DateTime.MinValue;

        public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        public JulianCalendar()
        {
            // There is no system setting of TwoDigitYear max, so set the value here.
            _twoDigitYearMax = 2029;
        }

        internal override CalendarId ID => CalendarId.JULIAN;

        internal static void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != JulianEra)
            {
                throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
            }
        }

        internal void CheckYearEraRange(int year, int era)
        {
            CheckEraRange(era);
            if (year <= 0 || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, MaxYear));
            }
        }

        internal static void CheckMonthRange(int month)
        {
            if (month < 1 || month > 12)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Month(month);
            }
        }

        /// <summary>
        /// Check for if the day value is valid.
        /// </summary>
        /// <remarks>
        /// Before calling this method, call CheckYearEraRange()/CheckMonthRange() to make
        /// sure year/month values are correct.
        /// </remarks>
        internal static void CheckDayRange(int year, int month, int day)
        {
            if (year == 1 && month == 1)
            {
                // The minimum supported Julia date is Julian 0001/01/03.
                if (day < 3)
                {
                    throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
                }
            }

            bool isLeapYear = (year % 4) == 0;
            int[] days = isLeapYear ? s_daysToMonth366 : s_daysToMonth365;
            int monthDays = days[month] - days[month - 1];
            if (day < 1 || day > monthDays)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, monthDays));
            }
        }

        /// <summary>
        /// Returns a given date part of this DateTime. This method is used
        /// to compute the year, day-of-year, month, or day part.
        /// </summary>
        internal static int GetDatePart(long ticks, int part)
        {
            // Gregorian 1/1/0001 is Julian 1/3/0001. Remember DateTime(0) is referred to Gregorian 1/1/0001.
            // The following line convert Gregorian ticks to Julian ticks.
            long julianTicks = ticks + TicksPerDay * 2;
            // n = number of days since 1/1/0001
            int n = (int)(julianTicks / TicksPerDay);
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / JulianDaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * JulianDaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / JulianDaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // If year was requested, compute and return it
            if (part == DatePartYear)
            {
                return y4 * 4 + y1 + 1;
            }

            // n = day number within year
            n -= y1 * JulianDaysPerYear;
            // If day-of-year was requested, return it
            if (part == DatePartDayOfYear)
            {
                return n + 1;
            }

            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = (y1 == 3);
            int[] days = leapYear ? s_daysToMonth366 : s_daysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            int m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m])
            {
                m++;
            }

            // If month was requested, return it
            if (part == DatePartMonth)
            {
                return m;
            }

            // Return 1-based day-of-month
            return n - days[m - 1] + 1;
        }

        /// <summary>
        /// Returns the tick count corresponding to the given year, month, and day.
        /// </summary>
        internal static long DateToTicks(int year, int month, int day)
        {
            int[] days = (year % 4 == 0) ? s_daysToMonth366 : s_daysToMonth365;
            int y = year - 1;
            int n = y * 365 + y / 4 + days[month - 1] + day - 1;
            // Gregorian 1/1/0001 is Julian 1/3/0001. n * TicksPerDay is the ticks in JulianCalendar.
            // Therefore, we subtract two days in the following to convert the ticks in JulianCalendar
            // to ticks in Gregorian calendar.
            return (n - 2) * TicksPerDay;
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(months),
                    months,
                    SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
            }

            int y = GetDatePart(time.Ticks, DatePartYear);
            int m = GetDatePart(time.Ticks, DatePartMonth);
            int d = GetDatePart(time.Ticks, DatePartDay);
            int i = m - 1 + months;
            if (i >= 0)
            {
                m = i % 12 + 1;
                y += i / 12;
            }
            else
            {
                m = 12 + (i + 1) % 12;
                y += (i - 11) / 12;
            }

            int[] daysArray = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) ? s_daysToMonth366 : s_daysToMonth365;
            int days = daysArray[m] - daysArray[m - 1];
            if (d > days)
            {
                d = days;
            }

            long ticks = DateToTicks(y, m, d) + time.Ticks % TicksPerDay;
            Calendar.CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return new DateTime(ticks);
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            return AddMonths(time, years * 12);
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartDay);
        }

        public override DayOfWeek GetDayOfWeek(DateTime time) => time.DayOfWeek;

        public override int GetDayOfYear(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartDayOfYear);
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            int[] days = (year % 4 == 0) ? s_daysToMonth366 : s_daysToMonth365;
            return days[month] - days[month - 1];
        }

        public override int GetDaysInYear(int year, int era)
        {
            // Year/Era range is done in IsLeapYear().
            return IsLeapYear(year, era) ? 366 : 365;
        }

        public override int GetEra(DateTime time) => JulianEra;

        public override int GetMonth(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartMonth);
        }

        public override int[] Eras => new int[] { JulianEra };

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearEraRange(year, era);
            return 12;
        }

        public override int GetYear(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartYear);
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            CheckMonthRange(month);
            // Year/Era range check is done in IsLeapYear().
            if (IsLeapYear(year, era))
            {
                CheckDayRange(year, month, day);
                return month == 2 && day == 29;
            }

            CheckDayRange(year, month, day);
            return false;
        }

        public override int GetLeapMonth(int year, int era)
        {
            CheckYearEraRange(year, era);
            return 0;
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            return false;
        }

        public override bool IsLeapYear(int year, int era)
        {
            CheckYearEraRange(year, era);
            return year % 4 == 0;
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            CheckDayRange(year, month, day);
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecond),
                    millisecond,
                    SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
            }

            if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second >= 60)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
            }

            return new DateTime(DateToTicks(year, month, day) + (new TimeSpan(0, hour, minute, second, millisecond)).Ticks);
        }

        public override int TwoDigitYearMax
        {
            get => _twoDigitYearMax;
            set
            {
                VerifyWritable();
                if (value < 99 || value > MaxYear)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Range, 99, MaxYear));
                }

                _twoDigitYearMax = value;
            }
        }

        public override int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (year > MaxYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, MaxYear));
            }

            return base.ToFourDigitYear(year);
        }
    }
}
