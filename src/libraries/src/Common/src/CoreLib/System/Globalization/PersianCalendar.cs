// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    // Modern Persian calendar is a solar observation based calendar. Each new year begins on the day when the vernal equinox occurs before noon.
    // The epoch is the date of the vernal equinox prior to the epoch of the Islamic calendar (March 19, 622 Julian or March 22, 622 Gregorian)

    // There is no Persian year 0. Ordinary years have 365 days. Leap years have 366 days with the last month (Esfand) gaining the extra day.
    /*
     **  Calendar support range:
     **      Calendar    Minimum     Maximum
     **      ==========  ==========  ==========
     **      Gregorian   0622/03/22   9999/12/31
     **      Persian     0001/01/01   9378/10/13
     */

    public class PersianCalendar : Calendar
    {
        public static readonly int PersianEra = 1;

        internal static long PersianEpoch = new DateTime(622, 3, 22).Ticks / GregorianCalendar.TicksPerDay;
        private const int ApproximateHalfYear = 180;

        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal const int MonthsPerYear = 12;

        internal static int[] DaysToMonth = { 0, 31, 62, 93, 124, 155, 186, 216, 246, 276, 306, 336, 366 };

        internal const int MaxCalendarYear = 9378;
        internal const int MaxCalendarMonth = 10;
        internal const int MaxCalendarDay = 13;

        // Persian calendar (year: 1, month: 1, day:1 ) = Gregorian (year: 622, month: 3, day: 22)
        // This is the minimal Gregorian date that we support in the PersianCalendar.
        internal static DateTime minDate = new DateTime(622, 3, 22);
        internal static DateTime maxDate = DateTime.MaxValue;

        public override DateTime MinSupportedDateTime
        {
            get
            {
                return (minDate);
            }
        }

        public override DateTime MaxSupportedDateTime
        {
            get
            {
                return (maxDate);
            }
        }

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.SolarCalendar;
            }
        }

        // Construct an instance of Persian calendar.

        public PersianCalendar()
        {
        }


        internal override CalendarId BaseCalendarID
        {
            get
            {
                return CalendarId.GREGORIAN;
            }
        }

        internal override CalendarId ID
        {
            get
            {
                return CalendarId.PERSIAN;
            }
        }


        /*=================================GetAbsoluteDatePersian==========================
        **Action: Gets the Absolute date for the given Persian date.  The absolute date means
        **       the number of days from January 1st, 1 A.D.
        **Returns:
        **Arguments:
        **Exceptions:
        ============================================================================*/

        private long GetAbsoluteDatePersian(int year, int month, int day)
        {
            if (year >= 1 && year <= MaxCalendarYear && month >= 1 && month <= 12)
            {
                int ordinalDay = DaysInPreviousMonths(month) + day - 1; // day is one based, make 0 based since this will be the number of days we add to beginning of year below
                int approximateDaysFromEpochForYearStart = (int)(CalendricalCalculationsHelper.MeanTropicalYearInDays * (year - 1));
                long yearStart = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(PersianEpoch + approximateDaysFromEpochForYearStart + ApproximateHalfYear);
                yearStart += ordinalDay;
                return yearStart;
            }
            throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
        }

        internal static void CheckTicksRange(long ticks)
        {
            if (ticks < minDate.Ticks || ticks > maxDate.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                            "time",
                            String.Format(
                                CultureInfo.InvariantCulture,
                                SR.ArgumentOutOfRange_CalendarRange,
                                minDate,
                                maxDate));
            }
        }

        internal static void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != PersianEra)
            {
                throw new ArgumentOutOfRangeException(nameof(era), SR.ArgumentOutOfRange_InvalidEraValue);
            }
        }

        internal static void CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            if (year < 1 || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(year),
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Range,
                                1,
                                MaxCalendarYear));
            }
        }

        internal static void CheckYearMonthRange(int year, int month, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                if (month > MaxCalendarMonth)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(month),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    1,
                                    MaxCalendarMonth));
                }
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), SR.ArgumentOutOfRange_Month);
            }
        }

        private static int MonthFromOrdinalDay(int ordinalDay)
        {
            Debug.Assert(ordinalDay <= 366);
            int index = 0;
            while (ordinalDay > DaysToMonth[index])
                index++;

            return index;
        }

        private static int DaysInPreviousMonths(int month)
        {
            Debug.Assert(1 <= month && month <= 12);
            --month; // months are one based but for calculations use 0 based
            return DaysToMonth[month];
        }

        /*=================================GetDatePart==========================
        **Action: Returns a given date part of this <i>DateTime</i>. This method is used
        **       to compute the year, day-of-year, month, or day part.
        **Returns:
        **Arguments:
        **Exceptions:  ArgumentException if part is incorrect.
        ============================================================================*/

        internal int GetDatePart(long ticks, int part)
        {
            long NumDays;                 // The calculation buffer in number of days.

            CheckTicksRange(ticks);

            //
            //  Get the absolute date.  The absolute date is the number of days from January 1st, 1 A.D.
            //  1/1/0001 is absolute date 1.
            //
            NumDays = ticks / GregorianCalendar.TicksPerDay + 1;

            //
            //  Calculate the appromixate Persian Year.
            //

            long yearStart = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(NumDays);
            int y = (int)(Math.Floor(((yearStart - PersianEpoch) / CalendricalCalculationsHelper.MeanTropicalYearInDays) + 0.5)) + 1;
            Debug.Assert(y >= 1);

            if (part == DatePartYear)
            {
                return y;
            }

            //
            //  Calculate the Persian Month.
            //

            int ordinalDay = (int)(NumDays - CalendricalCalculationsHelper.GetNumberOfDays(this.ToDateTime(y, 1, 1, 0, 0, 0, 0, 1)));

            if (part == DatePartDayOfYear)
            {
                return ordinalDay;
            }

            int m = MonthFromOrdinalDay(ordinalDay);
            Debug.Assert(ordinalDay >= 1);
            Debug.Assert(m >= 1 && m <= 12);
            if (part == DatePartMonth)
            {
                return m;
            }

            int d = ordinalDay - DaysInPreviousMonths(m);
            Debug.Assert(1 <= d);
            Debug.Assert(d <= 31);

            //
            //  Calculate the Persian Day.
            //

            if (part == DatePartDay)
            {
                return (d);
            }

            // Incorrect part value.
            throw new InvalidOperationException(SR.InvalidOperation_DateTimeParsing);
        }

        // Returns the DateTime resulting from adding the given number of
        // months to the specified DateTime. The result is computed by incrementing
        // (or decrementing) the year and month parts of the specified DateTime by
        // value months, and, if required, adjusting the day part of the
        // resulting date downwards to the last day of the resulting month in the
        // resulting year. The time-of-day part of the result is the same as the
        // time-of-day part of the specified DateTime.
        //
        // In more precise terms, considering the specified DateTime to be of the
        // form y / m / d + t, where y is the
        // year, m is the month, d is the day, and t is the
        // time-of-day, the result is y1 / m1 / d1 + t,
        // where y1 and m1 are computed by adding value months
        // to y and m, and d1 is the largest value less than
        // or equal to d that denotes a valid day in month m1 of year
        // y1.
        //


        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(months),
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Range,
                                -120000,
                                120000));
            }
            // Get the date in Persian calendar.
            int y = GetDatePart(time.Ticks, DatePartYear);
            int m = GetDatePart(time.Ticks, DatePartMonth);
            int d = GetDatePart(time.Ticks, DatePartDay);
            int i = m - 1 + months;
            if (i >= 0)
            {
                m = i % 12 + 1;
                y = y + i / 12;
            }
            else
            {
                m = 12 + (i + 1) % 12;
                y = y + (i - 11) / 12;
            }
            int days = GetDaysInMonth(y, m);
            if (d > days)
            {
                d = days;
            }
            long ticks = GetAbsoluteDatePersian(y, m, d) * TicksPerDay + time.Ticks % TicksPerDay;
            Calendar.CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (new DateTime(ticks));
        }

        // Returns the DateTime resulting from adding the given number of
        // years to the specified DateTime. The result is computed by incrementing
        // (or decrementing) the year part of the specified DateTime by value
        // years. If the month and day of the specified DateTime is 2/29, and if the
        // resulting year is not a leap year, the month and day of the resulting
        // DateTime becomes 2/28. Otherwise, the month, day, and time-of-day
        // parts of the result are the same as those of the specified DateTime.
        //


        public override DateTime AddYears(DateTime time, int years)
        {
            return (AddMonths(time, years * 12));
        }

        // Returns the day-of-month part of the specified DateTime. The returned
        // value is an integer between 1 and 31.
        //


        public override int GetDayOfMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDay));
        }

        // Returns the day-of-week part of the specified DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //


        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return ((DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7));
        }

        // Returns the day-of-year part of the specified DateTime. The returned value
        // is an integer between 1 and 366.
        //


        public override int GetDayOfYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDayOfYear));
        }

        // Returns the number of days in the month given by the year and
        // month arguments.
        //


        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);

            if ((month == MaxCalendarMonth) && (year == MaxCalendarYear))
            {
                return MaxCalendarDay;
            }

            int daysInMonth = DaysToMonth[month] - DaysToMonth[month - 1];
            if ((month == MonthsPerYear) && !IsLeapYear(year))
            {
                Debug.Assert(daysInMonth == 30);
                --daysInMonth;
            }
            return daysInMonth;
        }

        // Returns the number of days in the year given by the year argument for the current era.
        //

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                return DaysToMonth[MaxCalendarMonth - 1] + MaxCalendarDay;
            }
            // Common years have 365 days.  Leap years have 366 days.
            return (IsLeapYear(year, CurrentEra) ? 366 : 365);
        }

        // Returns the era for the specified DateTime value.


        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (PersianEra);
        }



        public override int[] Eras
        {
            get
            {
                return (new int[] { PersianEra });
            }
        }

        // Returns the month part of the specified DateTime. The returned value is an
        // integer between 1 and 12.
        //


        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        // Returns the number of months in the specified year and era.


        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                return MaxCalendarMonth;
            }
            return (12);
        }

        // Returns the year part of the specified DateTime. The returned value is an
        // integer between 1 and MaxCalendarYear.
        //


        public override int GetYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }

        // Checks whether a given day in the specified era is a leap day. This method returns true if
        // the date is a leap day, or false if not.
        //


        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            // The year/month/era value checking is done in GetDaysInMonth().
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(day),
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Day,
                                daysInMonth,
                                month));
            }
            return (IsLeapYear(year, era) && month == 12 && day == 30);
        }

        // Returns  the leap month in a calendar year of the specified era. This method returns 0
        // if this calendar does not have leap month, or this year is not a leap year.
        //


        public override int GetLeapMonth(int year, int era)
        {
            CheckYearRange(year, era);
            return (0);
        }

        // Checks whether a given month in the specified era is a leap month. This method returns true if
        // month is a leap month, or false if not.
        //


        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);
            return (false);
        }

        // Checks whether a given year in the specified era is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //

        public override bool IsLeapYear(int year, int era)
        {
            CheckYearRange(year, era);

            if (year == MaxCalendarYear)
            {
                return false;
            }

            return (GetAbsoluteDatePersian(year + 1, 1, 1) - GetAbsoluteDatePersian(year, 1, 1)) == 366;
        }

        // Returns the date and time converted to a DateTime value.  Throws an exception if the n-tuple is invalid.
        //


        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            // The year/month/era checking is done in GetDaysInMonth().
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(day),
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Day,
                                daysInMonth,
                                month));
            }

            long lDate = GetAbsoluteDatePersian(year, month, day);

            if (lDate >= 0)
            {
                return (new DateTime(lDate * GregorianCalendar.TicksPerDay + TimeToTicks(hour, minute, second, millisecond)));
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1410;

        public override int TwoDigitYearMax
        {
            get
            {
                if (twoDigitYearMax == -1)
                {
                    twoDigitYearMax = GetSystemTwoDigitYearSetting(ID, DEFAULT_TWO_DIGIT_YEAR_MAX);
                }
                return (twoDigitYearMax);
            }

            set
            {
                VerifyWritable();
                if (value < 99 || value > MaxCalendarYear)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(value),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    99,
                                    MaxCalendarYear));
                }
                twoDigitYearMax = value;
            }
        }



        public override int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year),
                    SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (year < 100)
            {
                return (base.ToFourDigitYear(year));
            }

            if (year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(year),
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Range,
                                1,
                                MaxCalendarYear));
            }
            return (year);
        }
    }
}
