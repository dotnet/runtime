// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    /// <remarks>
    /// Rules for the Hijri calendar:
    ///   - The Hijri calendar is a strictly Lunar calendar.
    ///   - Days begin at sunset.
    ///   - Islamic Year 1 (Muharram 1, 1 A.H.) is equivalent to absolute date
    ///       227015 (Friday, July 16, 622 C.E. - Julian).
    ///   - Leap Years occur in the 2, 5, 7, 10, 13, 16, 18, 21, 24, 26, &amp; 29th
    ///       years of a 30-year cycle.  Year = leap iff ((11y+14) mod 30 &lt; 11).
    ///   - There are 12 months which contain alternately 30 and 29 days.
    ///   - The 12th month, Dhu al-Hijjah, contains 30 days instead of 29 days
    ///       in a leap year.
    ///   - Common years have 354 days.  Leap years have 355 days.
    ///   - There are 10,631 days in a 30-year cycle.
    ///   - The Islamic months are:
    ///       1.  Muharram   (30 days)     7.  Rajab          (30 days)
    ///       2.  Safar      (29 days)     8.  Sha'ban        (29 days)
    ///       3.  Rabi I     (30 days)     9.  Ramadan        (30 days)
    ///       4.  Rabi II    (29 days)     10. Shawwal        (29 days)
    ///       5.  Jumada I   (30 days)     11. Dhu al-Qada    (30 days)
    ///       6.  Jumada II  (29 days)     12. Dhu al-Hijjah  (29 days) {30}
    ///
    /// NOTENOTE
    ///     The calculation of the HijriCalendar is based on the absolute date.  And the
    ///     absolute date means the number of days from January 1st, 1 A.D.
    ///     Therefore, we do not support the days before the January 1st, 1 A.D.
    ///
    /// Calendar support range:
    ///     Calendar    Minimum     Maximum
    ///     ==========  ==========  ==========
    ///     Gregorian   0622/07/18   9999/12/31
    ///     Hijri       0001/01/01   9666/04/03
    /// </remarks>

    public partial class HijriCalendar : Calendar
    {
        public static readonly int HijriEra = 1;

        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;

        private const int MinAdvancedHijri = -2;
        private const int MaxAdvancedHijri = 2;

        private static ReadOnlySpan<int> HijriMonthDays => [0, 30, 59, 89, 118, 148, 177, 207, 236, 266, 295, 325, 355];

        private int _hijriAdvance = int.MinValue;

        // DateTime.MaxValue = Hijri calendar (year:9666, month: 4, day: 3).
        private const int MaxCalendarYear = 9666;
        private const int MaxCalendarMonth = 4;

        // Hijri calendar (year: 1, month: 1, day:1 ) = Gregorian (year: 622, month: 7, day: 18)
        // This is the minimal Gregorian date that we support in the HijriCalendar.
        private static readonly DateTime s_calendarMinValue = new DateTime(622, 7, 18);
        private static readonly DateTime s_calendarMaxValue = DateTime.MaxValue;

        public override DateTime MinSupportedDateTime => s_calendarMinValue;

        public override DateTime MaxSupportedDateTime => s_calendarMaxValue;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;

        public HijriCalendar()
        {
        }

        internal override CalendarId ID => CalendarId.HIJRI;

        protected override int DaysInYearBeforeMinSupportedYear =>
            // the year before the 1st year of the cycle would have been the 30th year
            // of the previous cycle which is not a leap year. Common years have 354 days.
            354;

        private long GetAbsoluteDateHijri(int y, int m, int d)
        {
            return (long)(DaysUpToHijriYear(y) + HijriMonthDays[m - 1] + d - 1 - HijriAdjustment);
        }

        private long DaysUpToHijriYear(int HijriYear)
        {
            // Compute the number of years up to the current 30 year cycle.
            int numYear30 = ((HijriYear - 1) / 30) * 30;

            // Compute the number of years left.  This is the number of years
            // into the 30 year cycle for the given year.
            int numYearsLeft = HijriYear - numYear30 - 1;

            // Compute the number of absolute days up to the given year.
            long numDays = ((numYear30 * 10631L) / 30L) + 227013L;
            while (numYearsLeft > 0)
            {
                // Common year is 354 days, and leap year is 355 days.
                numDays += 354 + (IsLeapYear(numYearsLeft, CurrentEra) ? 1 : 0);
                numYearsLeft--;
            }

            return numDays;
        }

        public int HijriAdjustment
        {
            get
            {
                if (_hijriAdvance == int.MinValue)
                {
                    // Never been set before.  Use the system value from registry.
                    _hijriAdvance = GetHijriDateAdjustment();
                }

                return _hijriAdvance;
            }

            set
            {
                if (value < MinAdvancedHijri || value > MaxAdvancedHijri)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, MinAdvancedHijri, MaxAdvancedHijri));
                }

                VerifyWritable();
                _hijriAdvance = value;
            }
        }

        internal static void CheckTicksRange(long ticks)
        {
            if (ticks < s_calendarMinValue.Ticks || ticks > s_calendarMaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                    "time",
                    ticks,
                    SR.Format(
                        CultureInfo.InvariantCulture,
                        SR.ArgumentOutOfRange_CalendarRange,
                        s_calendarMinValue,
                        s_calendarMaxValue));
            }
        }

        internal static void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != HijriEra)
            {
                throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
            }
        }

        internal static void CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            if (year < 1 || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(year),
                            SR.Format(SR.ArgumentOutOfRange_Range, 1, MaxCalendarYear));
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
                        month,
                        SR.Format(SR.ArgumentOutOfRange_Range, 1, MaxCalendarMonth));
                }
            }

            if (month < 1 || month > 12)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Month(month);
            }
        }

        /// <summary>
        /// First, we get the absolute date (the number of days from January 1st, 1 A.C) for the given ticks.
        /// Use the formula (((AbsoluteDate - 227013) * 30) / 10631) + 1, we can a rough value for the Hijri year.
        /// In order to get the exact Hijri year, we compare the exact absolute date for HijriYear and (HijriYear + 1).
        /// From here, we can get the correct Hijri year.
        /// </summary>
        internal virtual int GetDatePart(long ticks, int part)
        {
            CheckTicksRange(ticks);

            // Get the absolute date. The absolute date is the number of days from January 1st, 1 A.D.
            // 1/1/0001 is absolute date 1.
            long numDays = ticks / TicksPerDay + 1;

            // See how much we need to backup or advance
            numDays += HijriAdjustment;

            // Calculate the appromixate Hijri Year from this magic formula.
            int hijriYear = (int)(((numDays - 227013) * 30) / 10631) + 1;

            long daysToHijriYear = DaysUpToHijriYear(hijriYear);            // The absolute date for HijriYear
            long daysOfHijriYear = GetDaysInYear(hijriYear, CurrentEra);    // The number of days for (HijriYear+1) year.

            if (numDays < daysToHijriYear)
            {
                daysToHijriYear -= daysOfHijriYear;
                hijriYear--;
            }
            else if (numDays == daysToHijriYear)
            {
                hijriYear--;
                daysToHijriYear -= GetDaysInYear(hijriYear, CurrentEra);
            }
            else
            {
                if (numDays > daysToHijriYear + daysOfHijriYear)
                {
                    daysToHijriYear += daysOfHijriYear;
                    hijriYear++;
                }
            }
            if (part == DatePartYear)
            {
                return hijriYear;
            }

            // Calculate the Hijri Month.
            int hijriMonth = 1;
            numDays -= daysToHijriYear;

            if (part == DatePartDayOfYear)
            {
                return (int)numDays;
            }

            while ((hijriMonth <= 12) && (numDays > HijriMonthDays[hijriMonth - 1]))
            {
                hijriMonth++;
            }
            hijriMonth--;

            if (part == DatePartMonth)
            {
                return hijriMonth;
            }

            // Calculate the Hijri Day.
            int hijriDay = (int)(numDays - HijriMonthDays[hijriMonth - 1]);

            if (part == DatePartDay)
            {
                return hijriDay;
            }

            // Incorrect part value.
            throw new InvalidOperationException(SR.InvalidOperation_DateTimeParsing);
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

            // Get the date in Hijri calendar.
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

            int days = GetDaysInMonth(y, m);
            if (d > days)
            {
                d = days;
            }

            long ticks = GetAbsoluteDateHijri(y, m, d) * TicksPerDay + (time.Ticks % TicksPerDay);
            CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
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
            CheckYearMonthRange(year, month, era);
            if (month == 12)
            {
                // For the 12th month, leap year has 30 days, and common year has 29 days.
                return IsLeapYear(year, CurrentEra) ? 30 : 29;
            }

            // Other months contain 30 and 29 days alternatively.  The 1st month has 30 days.
            return ((month % 2) == 1) ? 30 : 29;
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            // Common years have 354 days. Leap years have 355 days.
            return IsLeapYear(year, CurrentEra) ? 355 : 354;
        }

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return HijriEra;
        }

        public override int[] Eras => new int[] { HijriEra };

        public override int GetMonth(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartMonth);
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
            return 12;
        }

        public override int GetYear(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartYear);
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            // The year/month/era value checking is done in GetDaysInMonth().
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }

            return IsLeapYear(year, era) && month == 12 && day == 30;
        }

        public override int GetLeapMonth(int year, int era)
        {
            CheckYearRange(year, era);
            return 0;
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);
            return false;
        }

        public override bool IsLeapYear(int year, int era)
        {
            CheckYearRange(year, era);
            return (((year * 11) + 14) % 30) < 11;
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            // The year/month/era checking is done in GetDaysInMonth().
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }

            long lDate = GetAbsoluteDateHijri(year, month, day);
            if (lDate < 0)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }

            return new DateTime(lDate * TicksPerDay + TimeToTicks(hour, minute, second, millisecond));
        }

        private const int DefaultTwoDigitYearMax = 1451;

        public override int TwoDigitYearMax
        {
            get
            {
                if (_twoDigitYearMax == -1)
                {
                    _twoDigitYearMax = GetSystemTwoDigitYearSetting(ID, DefaultTwoDigitYearMax);
                }

                return _twoDigitYearMax;
            }
            set
            {
                VerifyWritable();
                if (value < 99 || value > MaxCalendarYear)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Range, 99, MaxCalendarYear));
                }

                _twoDigitYearMax = value;
            }
        }

        public override int ToFourDigitYear(int year)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(year);

            if (year < 100)
            {
                return base.ToFourDigitYear(year);
            }

            if (year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, MaxCalendarYear));
            }
            return year;
        }
    }
}
