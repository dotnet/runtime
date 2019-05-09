// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    public abstract class EastAsianLunisolarCalendar : Calendar
    {
        private const int LeapMonth = 0;
        private const int Jan1Month = 1;
        private const int Jan1Date = 2;
        private const int nDaysPerMonth = 3;

        // # of days so far in the solar year
        private static readonly int[] s_daysToMonth365 = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

        private static readonly int[] s_daysToMonth366 = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335 };

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunisolarCalendar;

        /// <summary>
        /// Return the year number in the 60-year cycle.
        /// </summary>
        public virtual int GetSexagenaryYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);

            TimeToLunar(time, out int year, out int month, out int day);
            return ((year - 4) % 60) + 1;
        }

        /// <summary>
        /// Return the celestial year from the 60-year cycle.
        /// The returned value is from 1 ~ 10.
        /// </summary>
        public int GetCelestialStem(int sexagenaryYear)
        {
            if (sexagenaryYear < 1 || sexagenaryYear > 60)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sexagenaryYear),
                    sexagenaryYear,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, 60));
            }

            return ((sexagenaryYear - 1) % 10) + 1;
        }

        /// <summary>
        /// Return the Terrestial Branch from the 60-year cycle.
        /// The returned value is from 1 ~ 12.
        /// </summary>
        public int GetTerrestrialBranch(int sexagenaryYear)
        {
            if (sexagenaryYear < 1 || sexagenaryYear > 60)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sexagenaryYear),
                    sexagenaryYear,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, 60));
            }

            return ((sexagenaryYear - 1) % 12) + 1;
        }

        internal abstract int GetYearInfo(int LunarYear, int Index);
        internal abstract int GetYear(int year, DateTime time);
        internal abstract int GetGregorianYear(int year, int era);

        internal abstract int MinCalendarYear { get; }
        internal abstract int MaxCalendarYear { get; }
        internal abstract EraInfo[]? CalEraInfo { get; }
        internal abstract DateTime MinDate { get; }
        internal abstract DateTime MaxDate { get; }

        internal const int MaxCalendarMonth = 13;
        internal const int MaxCalendarDay = 30;

        internal int MinEraCalendarYear(int era)
        {
            EraInfo[]? eraInfo = CalEraInfo;
            if (eraInfo == null)
            {
                return MinCalendarYear;
            }

            if (era == Calendar.CurrentEra)
            {
                era = CurrentEraValue;
            }

            // Era has to be in the supported range otherwise we will throw exception in CheckEraRange()
            if (era == GetEra(MinDate))
            {
                return GetYear(MinCalendarYear, MinDate);
            }

            for (int i = 0; i < eraInfo.Length; i++)
            {
                if (era == eraInfo[i].era)
                {
                    return eraInfo[i].minEraYear;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal int MaxEraCalendarYear(int era)
        {
            EraInfo[]? eraInfo = CalEraInfo;
            if (eraInfo == null)
            {
                return MaxCalendarYear;
            }

            if (era == Calendar.CurrentEra)
            {
                era = CurrentEraValue;
            }

            // Era has to be in the supported range otherwise we will throw exception in CheckEraRange()
            if (era == GetEra(MaxDate))
            {
                return GetYear(MaxCalendarYear, MaxDate);
            }

            for (int i = 0; i < eraInfo.Length; i++)
            {
                if (era == eraInfo[i].era)
                {
                    return eraInfo[i].maxEraYear;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal EastAsianLunisolarCalendar()
        {
        }

        internal void CheckTicksRange(long ticks)
        {
            if (ticks < MinSupportedDateTime.Ticks || ticks > MaxSupportedDateTime.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                                "time",
                                ticks,
                                SR.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRange_CalendarRange,
                                MinSupportedDateTime, MaxSupportedDateTime));
            }
        }

        internal void CheckEraRange(int era)
        {
            if (era == Calendar.CurrentEra)
            {
                era = CurrentEraValue;
            }

            if (era < GetEra(MinDate) || era > GetEra(MaxDate))
            {
                throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
            }
        }

        internal int CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            year = GetGregorianYear(year, era);

            if (year < MinCalendarYear || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Range, MinEraCalendarYear(era), MaxEraCalendarYear(era)));
            }
            return year;
        }

        internal int CheckYearMonthRange(int year, int month, int era)
        {
            year = CheckYearRange(year, era);

            if (month == 13)
            {
                // Reject if there is no leap month this year
                if (GetYearInfo(year, LeapMonth) == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(month), month, SR.ArgumentOutOfRange_Month);
                }
            }

            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, SR.ArgumentOutOfRange_Month);
            }

            return year;
        }

        internal int InternalGetDaysInMonth(int year, int month)
        {
            int mask = 0x8000;

            // convert the lunar day into a lunar month/date
            mask >>= (month - 1);
            if ((GetYearInfo(year, nDaysPerMonth) & mask) == 0)
            {
                return 29;
            }

            return 30;
        }

        /// <summary>
        /// Returns the number of days in the month given by the year and
        /// month arguments.
        /// </summary>
        public override int GetDaysInMonth(int year, int month, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            return InternalGetDaysInMonth(year, month);
        }

        private static bool GregorianIsLeapYear(int y)
        {
            if ((y % 4) != 0)
            {
                return false;
            }
            if ((y % 100) != 0)
            {
                return true;
            }

            return (y % 400) == 0;
        }

        /// <summary>
        /// Returns the date and time converted to a DateTime value.
        /// Throws an exception if the n-tuple is invalid.
        /// </summary>
        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            int daysInMonth = InternalGetDaysInMonth(year, month);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }

            if (!LunarToGregorian(year, month, day, out int gy, out int gm, out int gd))
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }

            return new DateTime(gy, gm, gd, hour, minute, second, millisecond);
        }

        /// <summary>
        /// Calculates lunar calendar info for the given gregorian year, month, date.
        /// The input date should be validated before calling this method.
        /// </summary>
        private void GregorianToLunar(int solarYear, int solarMonth, int solarDate, out int lunarYear, out int lunarMonth, out int lunarDate)
        {
            bool isLeapYear = GregorianIsLeapYear(solarYear);
            int jan1Month;
            int jan1Date;

            // Calculate the day number in the solar year.
            int solarDay = isLeapYear ? s_daysToMonth366[solarMonth - 1] : s_daysToMonth365[solarMonth - 1];
            solarDay += solarDate;

            // Calculate the day number in the lunar year.
            int lunarDay = solarDay;
            lunarYear = solarYear;
            if (lunarYear == (MaxCalendarYear + 1))
            {
                lunarYear--;
                lunarDay += (GregorianIsLeapYear(lunarYear) ? 366 : 365);
                jan1Month = GetYearInfo(lunarYear, Jan1Month);
                jan1Date = GetYearInfo(lunarYear, Jan1Date);
            }
            else
            {
                jan1Month = GetYearInfo(lunarYear, Jan1Month);
                jan1Date = GetYearInfo(lunarYear, Jan1Date);

                // check if this solar date is actually part of the previous
                // lunar year
                if ((solarMonth < jan1Month) ||
                    (solarMonth == jan1Month && solarDate < jan1Date))
                {
                    // the corresponding lunar day is actually part of the previous
                    // lunar year
                    lunarYear--;

                    // add a solar year to the lunar day #
                    lunarDay += (GregorianIsLeapYear(lunarYear) ? 366 : 365);

                    // update the new start of year
                    jan1Month = GetYearInfo(lunarYear, Jan1Month);
                    jan1Date = GetYearInfo(lunarYear, Jan1Date);
                }
            }

            // convert solar day into lunar day.
            // subtract off the beginning part of the solar year which is not
            // part of the lunar year.  since this part is always in Jan or Feb,
            // we don't need to handle Leap Year (LY only affects March
            // and later).
            lunarDay -= s_daysToMonth365[jan1Month - 1];
            lunarDay -= (jan1Date - 1);

            // convert the lunar day into a lunar month/date
            int mask = 0x8000;
            int yearInfo = GetYearInfo(lunarYear, nDaysPerMonth);
            int days = ((yearInfo & mask) != 0) ? 30 : 29;
            lunarMonth = 1;
            while (lunarDay > days)
            {
                lunarDay -= days;
                lunarMonth++;
                mask >>= 1;
                days = ((yearInfo & mask) != 0) ? 30 : 29;
            }
            lunarDate = lunarDay;
        }

        /// <summary>
        /// Convert from Lunar to Gregorian
        /// </summary>
        /// <remarks>
        /// Highly inefficient, but it works based on the forward conversion
        /// </remarks>
        private bool LunarToGregorian(int lunarYear, int lunarMonth, int lunarDate, out int solarYear, out int solarMonth, out int solarDay)
        {
            if (lunarDate < 1 || lunarDate > 30)
            {
                solarYear = 0;
                solarMonth = 0;
                solarDay = 0;
                return false;
            }

            int numLunarDays = lunarDate - 1;

            // Add previous months days to form the total num of days from the first of the month.
            for (int i = 1; i < lunarMonth; i++)
            {
                numLunarDays += InternalGetDaysInMonth(lunarYear, i);
            }

            // Get Gregorian First of year
            int jan1Month = GetYearInfo(lunarYear, Jan1Month);
            int jan1Date = GetYearInfo(lunarYear, Jan1Date);

            // calc the solar day of year of 1 Lunar day
            bool isLeapYear = GregorianIsLeapYear(lunarYear);
            int[] days = isLeapYear ? s_daysToMonth366 : s_daysToMonth365;

            solarDay = jan1Date;

            if (jan1Month > 1)
            {
                solarDay += days[jan1Month - 1];
            }

            // Add the actual lunar day to get the solar day we want
            solarDay = solarDay + numLunarDays;

            if (solarDay > (365 + (isLeapYear ? 1 : 0)))
            {
                solarYear = lunarYear + 1;
                solarDay -= (365 + (isLeapYear ? 1 : 0));
            }
            else
            {
                solarYear = lunarYear;
            }

            for (solarMonth = 1; solarMonth < 12; solarMonth++)
            {
                if (days[solarMonth] >= solarDay)
                {
                    break;
                }
            }

            solarDay -= days[solarMonth - 1];
            return true;
        }

        private DateTime LunarToTime(DateTime time, int year, int month, int day)
        {
            LunarToGregorian(year, month, day, out int gy, out int gm, out int gd);
            return GregorianCalendar.GetDefaultInstance().ToDateTime(gy, gm, gd, time.Hour, time.Minute, time.Second, time.Millisecond);
        }

        private void TimeToLunar(DateTime time, out int year, out int month, out int day)
        {
            Calendar gregorianCalendar = GregorianCalendar.GetDefaultInstance();
            int gy = gregorianCalendar.GetYear(time);
            int gm = gregorianCalendar.GetMonth(time);
            int gd = gregorianCalendar.GetDayOfMonth(time);

            GregorianToLunar(gy, gm, gd, out year, out month, out day);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding the given number of
        /// months to the specified DateTime. The result is computed by incrementing
        /// (or decrementing) the year and month parts of the specified DateTime by
        /// value months, and, if required, adjusting the day part of the
        /// resulting date downwards to the last day of the resulting month in the
        /// resulting year. The time-of-day part of the result is the same as the
        /// time-of-day part of the specified DateTime.
        /// </summary>
        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(months),
                    months,
                    SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
            }

            CheckTicksRange(time.Ticks);
            TimeToLunar(time, out int y, out int m, out int d);

            int i = m + months;
            if (i > 0)
            {
                int monthsInYear = InternalIsLeapYear(y) ? 13 : 12;

                while (i - monthsInYear > 0)
                {
                    i -= monthsInYear;
                    y++;
                    monthsInYear = InternalIsLeapYear(y) ? 13 : 12;
                }
                m = i;
            }
            else
            {
                int monthsInYear;
                while (i <= 0)
                {
                    monthsInYear = InternalIsLeapYear(y - 1) ? 13 : 12;
                    i += monthsInYear;
                    y--;
                }
                m = i;
            }

            int days = InternalGetDaysInMonth(y, m);
            if (d > days)
            {
                d = days;
            }
            DateTime dt = LunarToTime(time, y, m, d);

            CheckAddResult(dt.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (dt);
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            CheckTicksRange(time.Ticks);
            TimeToLunar(time, out int y, out int m, out int d);

            y += years;

            if (m == 13 && !InternalIsLeapYear(y))
            {
                m = 12;
                d = InternalGetDaysInMonth(y, m);
            }
            int daysInMonths = InternalGetDaysInMonth(y, m);
            if (d > daysInMonths)
            {
                d = daysInMonths;
            }

            DateTime dt = LunarToTime(time, y, m, d);
            CheckAddResult(dt.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return dt;
        }

        /// <summary>
        /// Returns the day-of-year part of the specified DateTime. The returned value
        /// is an integer between 1 and [354|355 |383|384].
        /// </summary>
        public override int GetDayOfYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            TimeToLunar(time, out int y, out int m, out int d);

            for (int i = 1; i < m; i++)
            {
                d = d + InternalGetDaysInMonth(y, i);
            }
            return d;
        }

        /// <summary>
        /// Returns the day-of-month part of the specified DateTime. The returned
        /// value is an integer between 1 and 29 or 30.
        /// </summary>
        public override int GetDayOfMonth(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            TimeToLunar(time, out int y, out int m, out int d);

            return d;
        }

        /// <summary>
        /// Returns the number of days in the year given by the year argument for the current era.
        /// </summary>
        public override int GetDaysInYear(int year, int era)
        {
            year = CheckYearRange(year, era);

            int days = 0;
            int monthsInYear = InternalIsLeapYear(year) ? 13 : 12;

            while (monthsInYear != 0)
            {
                days += InternalGetDaysInMonth(year, monthsInYear--);
            }

            return days;
        }

        /// <summary>
        /// Returns the month part of the specified DateTime.
        /// The returned value is an integer between 1 and 13.
        /// </summary>
        public override int GetMonth(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            TimeToLunar(time, out int y, out int m, out int d);

            return m;
        }

        /// <summary>
        /// Returns the year part of the specified DateTime.
        /// The returned value is an integer between 1 and MaxCalendarYear.
        /// </summary>
        public override int GetYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            TimeToLunar(time, out int y, out int m, out int d);

            return GetYear(y, time);
        }

        /// <summary>
        /// Returns the day-of-week part of the specified DateTime. The returned value
        /// is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        /// Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        /// Thursday, 5 indicates Friday, and 6 indicates Saturday.
        /// </summary>
        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (DayOfWeek)((int)(time.Ticks / Calendar.TicksPerDay + 1) % 7);
        }

        /// <summary>
        /// Returns the number of months in the specified year and era.
        /// </summary>
        public override int GetMonthsInYear(int year, int era)
        {
            year = CheckYearRange(year, era);
            return InternalIsLeapYear(year) ? 13 : 12;
        }

        /// <summary>
        /// Checks whether a given day in the specified era is a leap day.
        /// This method returns true if the date is a leap day, or false if not.
        /// </summary>
        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            int daysInMonth = InternalGetDaysInMonth(year, month);

            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }

            int m = GetYearInfo(year, LeapMonth);
            return (m != 0) && (month == (m + 1));
        }

        /// <summary>
        /// Checks whether a given month in the specified era is a leap month.
        /// This method returns true if month is a leap month, or false if not.
        /// </summary>
        public override bool IsLeapMonth(int year, int month, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            int m = GetYearInfo(year, LeapMonth);
            return (m != 0) && (month == (m + 1));
        }

        /// <summary>
        /// Returns  the leap month in a calendar year of the specified era. This method returns 0
        /// if this year is not a leap year.
        /// </summary>
        public override int GetLeapMonth(int year, int era)
        {
            year = CheckYearRange(year, era);
            int month = GetYearInfo(year, LeapMonth);
            return month > 0 ? month + 1 : 0;
        }

        internal bool InternalIsLeapYear(int year)
        {
            return GetYearInfo(year, LeapMonth) != 0;
        }

        /// <summary>
        /// Checks whether a given year in the specified era is a leap year.
        /// This method returns true if year is a leap year, or false if not.
        /// </summary>
        public override bool IsLeapYear(int year, int era)
        {
            year = CheckYearRange(year, era);
            return InternalIsLeapYear(year);
        }

        private const int DefaultGregorianTwoDigitYearMax = 2029;

        public override int TwoDigitYearMax
        {
            get
            {
                if (_twoDigitYearMax == -1)
                {
                    _twoDigitYearMax = GetSystemTwoDigitYearSetting(BaseCalendarID, GetYear(new DateTime(DefaultGregorianTwoDigitYearMax, 1, 1)));
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
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            year = base.ToFourDigitYear(year);
            CheckYearRange(year, CurrentEra);
            return year;
        }
    }
}
