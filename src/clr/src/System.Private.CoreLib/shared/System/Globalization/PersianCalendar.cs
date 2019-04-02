// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Globalization
{
    /// <summary>
    /// Modern Persian calendar is a solar observation based calendar. Each new year begins on the day when the vernal equinox occurs before noon.
    /// The epoch is the date of the vernal equinox prior to the epoch of the Islamic calendar (March 19, 622 Julian or March 22, 622 Gregorian)
    /// There is no Persian year 0. Ordinary years have 365 days. Leap years have 366 days with the last month (Esfand) gaining the extra day.
    /// </summary>
    /// <remarks>
    ///  Calendar support range:
    ///      Calendar    Minimum     Maximum
    ///      ==========  ==========  ==========
    ///      Gregorian   0622/03/22   9999/12/31
    ///      Persian     0001/01/01   9378/10/13
    /// </remarks>
    public class PersianCalendar : Calendar
    {
        public static readonly int PersianEra = 1;

        private static readonly long s_persianEpoch = new DateTime(622, 3, 22).Ticks / GregorianCalendar.TicksPerDay;
        private const int ApproximateHalfYear = 180;

        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;
        private const int MonthsPerYear = 12;

        private static readonly int[] s_daysToMonth = { 0, 31, 62, 93, 124, 155, 186, 216, 246, 276, 306, 336, 366 };

        private const int MaxCalendarYear = 9378;
        private const int MaxCalendarMonth = 10;
        private const int MaxCalendarDay = 13;

        // Persian calendar (year: 1, month: 1, day:1 ) = Gregorian (year: 622, month: 3, day: 22)
        // This is the minimal Gregorian date that we support in the PersianCalendar.
        private static readonly DateTime s_minDate = new DateTime(622, 3, 22);
        private static readonly DateTime s_maxDate = DateTime.MaxValue;

        public override DateTime MinSupportedDateTime => s_minDate;

        public override DateTime MaxSupportedDateTime => s_maxDate;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        public PersianCalendar()
        {
        }

        internal override CalendarId BaseCalendarID => CalendarId.GREGORIAN;

        internal override CalendarId ID => CalendarId.PERSIAN;

        private long GetAbsoluteDatePersian(int year, int month, int day)
        {
            if (year < 1 || year > MaxCalendarYear || month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }

            // day is one based, make 0 based since this will be the number of days we add to beginning of year below
            int ordinalDay = DaysInPreviousMonths(month) + day - 1;
            int approximateDaysFromEpochForYearStart = (int)(CalendricalCalculationsHelper.MeanTropicalYearInDays * (year - 1));
            long yearStart = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(s_persianEpoch + approximateDaysFromEpochForYearStart + ApproximateHalfYear);
            yearStart += ordinalDay;
            return yearStart;
        }

        internal static void CheckTicksRange(long ticks)
        {
            if (ticks < s_minDate.Ticks || ticks > s_maxDate.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                    "time",
                    ticks,
                    SR.Format(SR.ArgumentOutOfRange_CalendarRange, s_minDate, s_maxDate));
            }
        }

        internal static void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != PersianEra)
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
                    year,
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
                throw new ArgumentOutOfRangeException(nameof(month), month, SR.ArgumentOutOfRange_Month);
            }
        }

        private static int MonthFromOrdinalDay(int ordinalDay)
        {
            Debug.Assert(ordinalDay <= 366);
            int index = 0;
            while (ordinalDay > s_daysToMonth[index])
            {
                index++;
            }

            return index;
        }

        private static int DaysInPreviousMonths(int month)
        {
            Debug.Assert(1 <= month && month <= 12);
            // months are one based but for calculations use 0 based
            --month;
            return s_daysToMonth[month];
        }

        internal int GetDatePart(long ticks, int part)
        {
            CheckTicksRange(ticks);

            // Get the absolute date. The absolute date is the number of days from January 1st, 1 A.D.
            // 1/1/0001 is absolute date 1.
            long numDays = ticks / GregorianCalendar.TicksPerDay + 1;

            //  Calculate the appromixate Persian Year.
            long yearStart = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(numDays);
            int y = (int)(Math.Floor(((yearStart - s_persianEpoch) / CalendricalCalculationsHelper.MeanTropicalYearInDays) + 0.5)) + 1;
            Debug.Assert(y >= 1);

            if (part == DatePartYear)
            {
                return y;
            }

            //  Calculate the Persian Month.
            int ordinalDay = (int)(numDays - CalendricalCalculationsHelper.GetNumberOfDays(this.ToDateTime(y, 1, 1, 0, 0, 0, 0, 1)));
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

            //  Calculate the Persian Day.
            if (part == DatePartDay)
            {
                return d;
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

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return (DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7);
        }

        public override int GetDayOfYear(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartDayOfYear);
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);

            if ((month == MaxCalendarMonth) && (year == MaxCalendarYear))
            {
                return MaxCalendarDay;
            }

            int daysInMonth = s_daysToMonth[month] - s_daysToMonth[month - 1];
            if ((month == MonthsPerYear) && !IsLeapYear(year))
            {
                Debug.Assert(daysInMonth == 30);
                --daysInMonth;
            }

            return daysInMonth;
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                return s_daysToMonth[MaxCalendarMonth - 1] + MaxCalendarDay;
            }

            return IsLeapYear(year, CurrentEra) ? 366 : 365;
        }

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return PersianEra;
        }

        public override int[] Eras => new int[] { PersianEra };

        public override int GetMonth(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartMonth);
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                return MaxCalendarMonth;
            }

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

            if (year == MaxCalendarYear)
            {
                return false;
            }

            return (GetAbsoluteDatePersian(year + 1, 1, 1) - GetAbsoluteDatePersian(year, 1, 1)) == 366;
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

            long lDate = GetAbsoluteDatePersian(year, month, day);

            if (lDate < 0)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }

            return new DateTime(lDate * GregorianCalendar.TicksPerDay + TimeToTicks(hour, minute, second, millisecond));
        }

        private const int DefaultTwoDigitYearMax = 1410;

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
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
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
