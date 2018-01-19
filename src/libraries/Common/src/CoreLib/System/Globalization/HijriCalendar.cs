// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Globalization
{
    ////////////////////////////////////////////////////////////////////////////
    //
    //  Rules for the Hijri calendar:
    //    - The Hijri calendar is a strictly Lunar calendar.
    //    - Days begin at sunset.
    //    - Islamic Year 1 (Muharram 1, 1 A.H.) is equivalent to absolute date
    //        227015 (Friday, July 16, 622 C.E. - Julian).
    //    - Leap Years occur in the 2, 5, 7, 10, 13, 16, 18, 21, 24, 26, & 29th
    //        years of a 30-year cycle.  Year = leap iff ((11y+14) mod 30 < 11).
    //    - There are 12 months which contain alternately 30 and 29 days.
    //    - The 12th month, Dhu al-Hijjah, contains 30 days instead of 29 days
    //        in a leap year.
    //    - Common years have 354 days.  Leap years have 355 days.
    //    - There are 10,631 days in a 30-year cycle.
    //    - The Islamic months are:
    //        1.  Muharram   (30 days)     7.  Rajab          (30 days)
    //        2.  Safar      (29 days)     8.  Sha'ban        (29 days)
    //        3.  Rabi I     (30 days)     9.  Ramadan        (30 days)
    //        4.  Rabi II    (29 days)     10. Shawwal        (29 days)
    //        5.  Jumada I   (30 days)     11. Dhu al-Qada    (30 days)
    //        6.  Jumada II  (29 days)     12. Dhu al-Hijjah  (29 days) {30}
    //
    //  NOTENOTE
    //      The calculation of the HijriCalendar is based on the absolute date.  And the
    //      absolute date means the number of days from January 1st, 1 A.D.
    //      Therefore, we do not support the days before the January 1st, 1 A.D.
    //
    ////////////////////////////////////////////////////////////////////////////
    /*
    **  Calendar support range:
    **      Calendar    Minimum     Maximum
    **      ==========  ==========  ==========
    **      Gregorian   0622/07/18   9999/12/31
    **      Hijri       0001/01/01   9666/04/03
    */

    public partial class HijriCalendar : Calendar
    {
        public static readonly int HijriEra = 1;

        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;

        internal const int MinAdvancedHijri = -2;
        internal const int MaxAdvancedHijri = 2;

        internal static readonly int[] HijriMonthDays = { 0, 30, 59, 89, 118, 148, 177, 207, 236, 266, 295, 325, 355 };

        private int _hijriAdvance = Int32.MinValue;

        // DateTime.MaxValue = Hijri calendar (year:9666, month: 4, day: 3).
        internal const int MaxCalendarYear = 9666;
        internal const int MaxCalendarMonth = 4;
        internal const int MaxCalendarDay = 3;
        // Hijri calendar (year: 1, month: 1, day:1 ) = Gregorian (year: 622, month: 7, day: 18)
        // This is the minimal Gregorian date that we support in the HijriCalendar.
        internal static readonly DateTime calendarMinValue = new DateTime(622, 7, 18);
        internal static readonly DateTime calendarMaxValue = DateTime.MaxValue;


        public override DateTime MinSupportedDateTime
        {
            get
            {
                return (calendarMinValue);
            }
        }


        public override DateTime MaxSupportedDateTime
        {
            get
            {
                return (calendarMaxValue);
            }
        }

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.LunarCalendar;
            }
        }

        public HijriCalendar()
        {
        }

        internal override CalendarId ID
        {
            get
            {
                return CalendarId.HIJRI;
            }
        }

        protected override int DaysInYearBeforeMinSupportedYear
        {
            get
            {
                // the year before the 1st year of the cycle would have been the 30th year
                // of the previous cycle which is not a leap year. Common years have 354 days.
                return 354;
            }
        }



        /*=================================GetAbsoluteDateHijri==========================
        **Action: Gets the Absolute date for the given Hijri date.  The absolute date means
        **       the number of days from January 1st, 1 A.D.
        **Returns:
        **Arguments:
        **Exceptions:
        ============================================================================*/

        private long GetAbsoluteDateHijri(int y, int m, int d)
        {
            return (long)(DaysUpToHijriYear(y) + HijriMonthDays[m - 1] + d - 1 - HijriAdjustment);
        }

        /*=================================DaysUpToHijriYear==========================
        **Action: Gets the total number of days (absolute date) up to the given Hijri Year.
        **       The absolute date means the number of days from January 1st, 1 A.D.
        **Returns: Gets the total number of days (absolute date) up to the given Hijri Year.
        **Arguments: HijriYear year value in Hijri calendar.
        **Exceptions: None
        **Notes:
        ============================================================================*/

        private long DaysUpToHijriYear(int HijriYear)
        {
            long NumDays;           // number of absolute days
            int NumYear30;         // number of years up to current 30 year cycle
            int NumYearsLeft;      // number of years into 30 year cycle

            //
            //  Compute the number of years up to the current 30 year cycle.
            //
            NumYear30 = ((HijriYear - 1) / 30) * 30;

            //
            //  Compute the number of years left.  This is the number of years
            //  into the 30 year cycle for the given year.
            //
            NumYearsLeft = HijriYear - NumYear30 - 1;

            //
            //  Compute the number of absolute days up to the given year.
            //
            NumDays = ((NumYear30 * 10631L) / 30L) + 227013L;
            while (NumYearsLeft > 0)
            {
                // Common year is 354 days, and leap year is 355 days.
                NumDays += 354 + (IsLeapYear(NumYearsLeft, CurrentEra) ? 1 : 0);
                NumYearsLeft--;
            }

            //
            //  Return the number of absolute days.
            //
            return (NumDays);
        }

        public int HijriAdjustment
        {
            get
            {
                if (_hijriAdvance == Int32.MinValue)
                {
                    // Never been set before.  Use the system value from registry.
                    _hijriAdvance = GetHijriDateAdjustment();
                }
                return (_hijriAdvance);
            }

            set
            {
                // NOTE: Check the value of Min/MaxAdvancedHijri with Arabic speakers to see if the assumption is good.
                if (value < MinAdvancedHijri || value > MaxAdvancedHijri)
                {
                    throw new ArgumentOutOfRangeException(
                                "HijriAdjustment",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Bounds_Lower_Upper,
                                    MinAdvancedHijri,
                                    MaxAdvancedHijri));
                }
                VerifyWritable();

                _hijriAdvance = value;
            }
        }

        internal static void CheckTicksRange(long ticks)
        {
            if (ticks < calendarMinValue.Ticks || ticks > calendarMaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                            "time",
                            String.Format(
                                CultureInfo.InvariantCulture,
                                SR.ArgumentOutOfRange_CalendarRange,
                                calendarMinValue,
                                calendarMaxValue));
            }
        }

        internal static void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != HijriEra)
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

        /*=================================GetDatePart==========================
        **Action: Returns a given date part of this <i>DateTime</i>. This method is used
        **       to compute the year, day-of-year, month, or day part.
        **Returns:
        **Arguments:
        **Exceptions:  ArgumentException if part is incorrect.
        **Notes:
        **      First, we get the absolute date (the number of days from January 1st, 1 A.C) for the given ticks.
        **      Use the formula (((AbsoluteDate - 227013) * 30) / 10631) + 1, we can a rough value for the Hijri year.
        **      In order to get the exact Hijri year, we compare the exact absolute date for HijriYear and (HijriYear + 1).
        **      From here, we can get the correct Hijri year.
        ============================================================================*/

        internal virtual int GetDatePart(long ticks, int part)
        {
            int HijriYear;                   // Hijri year
            int HijriMonth;                  // Hijri month
            int HijriDay;                    // Hijri day
            long NumDays;                 // The calculation buffer in number of days.

            CheckTicksRange(ticks);

            //
            //  Get the absolute date.  The absolute date is the number of days from January 1st, 1 A.D.
            //  1/1/0001 is absolute date 1.
            //
            NumDays = ticks / GregorianCalendar.TicksPerDay + 1;

            //
            //  See how much we need to backup or advance
            //
            NumDays += HijriAdjustment;

            //
            //  Calculate the appromixate Hijri Year from this magic formula.
            //
            HijriYear = (int)(((NumDays - 227013) * 30) / 10631) + 1;

            long daysToHijriYear = DaysUpToHijriYear(HijriYear);            // The absolute date for HijriYear
            long daysOfHijriYear = GetDaysInYear(HijriYear, CurrentEra);    // The number of days for (HijriYear+1) year.

            if (NumDays < daysToHijriYear)
            {
                daysToHijriYear -= daysOfHijriYear;
                HijriYear--;
            }
            else if (NumDays == daysToHijriYear)
            {
                HijriYear--;
                daysToHijriYear -= GetDaysInYear(HijriYear, CurrentEra);
            }
            else
            {
                if (NumDays > daysToHijriYear + daysOfHijriYear)
                {
                    daysToHijriYear += daysOfHijriYear;
                    HijriYear++;
                }
            }
            if (part == DatePartYear)
            {
                return (HijriYear);
            }

            //
            //  Calculate the Hijri Month.
            //

            HijriMonth = 1;
            NumDays -= daysToHijriYear;

            if (part == DatePartDayOfYear)
            {
                return ((int)NumDays);
            }

            while ((HijriMonth <= 12) && (NumDays > HijriMonthDays[HijriMonth - 1]))
            {
                HijriMonth++;
            }
            HijriMonth--;

            if (part == DatePartMonth)
            {
                return (HijriMonth);
            }

            //
            //  Calculate the Hijri Day.
            //
            HijriDay = (int)(NumDays - HijriMonthDays[HijriMonth - 1]);

            if (part == DatePartDay)
            {
                return (HijriDay);
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
            // Get the date in Hijri calendar.
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
            long ticks = GetAbsoluteDateHijri(y, m, d) * TicksPerDay + (time.Ticks % TicksPerDay);
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
            if (month == 12)
            {
                // For the 12th month, leap year has 30 days, and common year has 29 days.
                return (IsLeapYear(year, CurrentEra) ? 30 : 29);
            }
            // Other months contain 30 and 29 days alternatively.  The 1st month has 30 days.
            return (((month % 2) == 1) ? 30 : 29);
        }

        // Returns the number of days in the year given by the year argument for the current era.
        //

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            // Common years have 354 days.  Leap years have 355 days.
            return (IsLeapYear(year, CurrentEra) ? 355 : 354);
        }

        // Returns the era for the specified DateTime value.

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (HijriEra);
        }


        public override int[] Eras
        {
            get
            {
                return (new int[] { HijriEra });
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
            return ((((year * 11) + 14) % 30) < 11);
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

            long lDate = GetAbsoluteDateHijri(year, month, day);

            if (lDate >= 0)
            {
                return (new DateTime(lDate * GregorianCalendar.TicksPerDay + TimeToTicks(hour, minute, second, millisecond)));
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1451;


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
