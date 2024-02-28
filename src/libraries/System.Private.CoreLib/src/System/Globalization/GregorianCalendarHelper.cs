// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    // Gregorian Calendars use Era Info
    internal sealed class EraInfo
    {
        internal int era;          // The value of the era.
        internal long ticks;    // The time in ticks when the era starts
        internal int yearOffset;   // The offset to Gregorian year when the era starts.
                                   // Gregorian Year = Era Year + yearOffset
                                   // Era Year = Gregorian Year - yearOffset
        internal int minEraYear;   // Min year value in this era. Generally, this value is 1, but this may
                                   // be affected by the DateTime.MinValue;
        internal int maxEraYear;   // Max year value in this era. (== the year length of the era + 1)

        internal string? eraName;    // The era name
        internal string? abbrevEraName;  // Abbreviated Era Name
        internal string? englishEraName; // English era name

        internal EraInfo(int era, int startYear, int startMonth, int startDay, int yearOffset, int minEraYear, int maxEraYear)
        {
            this.era = era;
            this.yearOffset = yearOffset;
            this.minEraYear = minEraYear;
            this.maxEraYear = maxEraYear;
            this.ticks = new DateTime(startYear, startMonth, startDay).Ticks;
        }

        internal EraInfo(int era, int startYear, int startMonth, int startDay, int yearOffset, int minEraYear, int maxEraYear,
                          string eraName, string abbrevEraName, string englishEraName)
        {
            this.era = era;
            this.yearOffset = yearOffset;
            this.minEraYear = minEraYear;
            this.maxEraYear = maxEraYear;
            // codeql[cs/leap-year/unsafe-date-construction-from-two-elements] - DateTime is constructed using the user specifed values, not a combination of different sources.
            this.ticks = new DateTime(startYear, startMonth, startDay).Ticks;
            this.eraName = eraName;
            this.abbrevEraName = abbrevEraName;
            this.englishEraName = englishEraName;
        }
    }

    // This calendar recognizes two era values:
    // 0 CurrentEra (AD)
    // 1 BeforeCurrentEra (BC)
    internal sealed class GregorianCalendarHelper
    {
        //
        // This is the max Gregorian year can be represented by DateTime class.  The limitation
        // is derived from DateTime class.
        //
        internal int MaxYear => m_maxYear;

        private readonly int m_maxYear;
        private readonly int m_minYear;
        private readonly Calendar m_Cal;
        private readonly EraInfo[] m_EraInfo;

        // Construct an instance of gregorian calendar.
        internal GregorianCalendarHelper(Calendar cal, EraInfo[] eraInfo)
        {
            m_Cal = cal;
            m_EraInfo = eraInfo;
            m_maxYear = eraInfo[0].maxEraYear;
            m_minYear = eraInfo[0].minEraYear;
        }

        // EraInfo.yearOffset:  The offset to Gregorian year when the era starts. Gregorian Year = Era Year + yearOffset
        //                      Era Year = Gregorian Year - yearOffset
        // EraInfo.minEraYear:  Min year value in this era. Generally, this value is 1, but this may be affected by the DateTime.MinValue;
        // EraInfo.maxEraYear:  Max year value in this era. (== the year length of the era + 1)
        private int GetYearOffset(int year, int era, bool throwOnError)
        {
            if (year < 0)
            {
                if (throwOnError)
                {
                    throw new ArgumentOutOfRangeException(nameof(year), SR.ArgumentOutOfRange_NeedNonNegNum);
                }
                return -1;
            }

            if (era == Calendar.CurrentEra)
            {
                era = m_Cal.CurrentEraValue;
            }

            var eras = m_EraInfo;
            for (int i = 0; i < eras.Length; i++)
            {
                EraInfo eraInfo = eras[i];
                if (era == eraInfo.era)
                {
                    if (year >= eraInfo.minEraYear)
                    {
                        if (year <= eraInfo.maxEraYear)
                        {
                            return eraInfo.yearOffset;
                        }
                        else if (!LocalAppContextSwitches.EnforceJapaneseEraYearRanges)
                        {
                            // If we got the year number exceeding the era max year number, this still possible be valid as the date can be created before
                            // introducing new eras after the era we are checking. we'll loop on the eras after the era we have and ensure the year
                            // can exist in one of these eras. otherwise, we'll throw.
                            // Note, we always return the offset associated with the requested era.
                            //
                            // Here is some example:
                            // if we are getting the era number 4 (Heisei) and getting the year number 32. if the era 4 has year range from 1 to 31
                            // then year 32 exceeded the range of era 4 and we'll try to find out if the years difference (32 - 31 = 1) would lay in
                            // the subsequent eras (e.g era 5 and up)

                            int remainingYears = year - eraInfo.maxEraYear;

                            for (int j = i - 1; j >= 0; j--)
                            {
                                if (remainingYears <= eras[j].maxEraYear)
                                {
                                    return eraInfo.yearOffset;
                                }
                                remainingYears -= eras[j].maxEraYear;
                            }
                        }
                    }

                    if (throwOnError)
                    {
                        throw new ArgumentOutOfRangeException(
                                    nameof(year),
                                    SR.Format(
                                        SR.ArgumentOutOfRange_Range,
                                        eraInfo.minEraYear,
                                        eraInfo.maxEraYear));
                    }

                    break; // no need to iterate more on eras.
                }
            }

            if (throwOnError)
            {
                throw new ArgumentOutOfRangeException(nameof(era), SR.ArgumentOutOfRange_InvalidEraValue);
            }
            return -1;
        }

        /*=================================GetGregorianYear==========================
        **Action: Get the Gregorian year value for the specified year in an era.
        **Returns: The Gregorian year value.
        **Arguments:
        **      year    the year value in Japanese calendar
        **      era     the Japanese emperor era value.
        **Exceptions:
        **      ArgumentOutOfRangeException if year value is invalid or era value is invalid.
        ============================================================================*/

        internal int GetGregorianYear(int year, int era)
        {
            return GetYearOffset(year, era, throwOnError: true) + year;
        }

        internal bool IsValidYear(int year, int era)
        {
            return GetYearOffset(year, era, throwOnError: false) >= 0;
        }

        /*=================================GetAbsoluteDate==========================
        **Action: Gets the absolute date for the given Gregorian date.  The absolute date means
        **       the number of days from January 1st, 1 A.D.
        **Returns:  the absolute date
        **Arguments:
        **      year    the Gregorian year
        **      month   the Gregorian month
        **      day     the day
        **Exceptions:
        **      ArgumentOutOfRangException  if year, month, day value is valid.
        **Note:
        **      This is an internal method used by DateToTicks() and the calculations of Hijri and Hebrew calendars.
        **      Number of Days in Prior Years (both common and leap years) +
        **      Number of Days in Prior Months of Current Year +
        **      Number of Days in Current Month
        **
        ============================================================================*/

        internal static long GetAbsoluteDate(int year, int month, int day)
        {
            if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
            {
                ReadOnlySpan<int> days = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365;
                if (day >= 1 && (day <= days[month] - days[month - 1]))
                {
                    int y = year - 1;
                    int absoluteDate = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
                    return absoluteDate;
                }
            }
            throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
        }

        // Returns the tick count corresponding to the given year, month, and day.
        // Will check the if the parameters are valid.
        internal static long DateToTicks(int year, int month, int day)
        {
            return GetAbsoluteDate(year, month, day) * TimeSpan.TicksPerDay;
        }

        internal void CheckTicksRange(long ticks)
        {
            if (ticks < m_Cal.MinSupportedDateTime.Ticks || ticks > m_Cal.MaxSupportedDateTime.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                            "time",
                            SR.Format(
                                CultureInfo.InvariantCulture,
                                SR.ArgumentOutOfRange_CalendarRange,
                                m_Cal.MinSupportedDateTime,
                                m_Cal.MaxSupportedDateTime));
            }
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
        public DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(months),
                            SR.Format(
                                SR.ArgumentOutOfRange_Range,
                                -120000,
                                120000));
            }
            CheckTicksRange(time.Ticks);

            time.GetDate(out int y, out int m, out int d);
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
            ReadOnlySpan<int> daysArray = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365;
            int days = (daysArray[m] - daysArray[m - 1]);

            if (d > days)
            {
                d = days;
            }
            long ticks = DateToTicks(y, m, d) + time.TimeOfDay.Ticks;
            Calendar.CheckAddResult(ticks, m_Cal.MinSupportedDateTime, m_Cal.MaxSupportedDateTime);
            return new DateTime(ticks);
        }

        // Returns the DateTime resulting from adding the given number of
        // years to the specified DateTime. The result is computed by incrementing
        // (or decrementing) the year part of the specified DateTime by value
        // years. If the month and day of the specified DateTime is 2/29, and if the
        // resulting year is not a leap year, the month and day of the resulting
        // DateTime becomes 2/28. Otherwise, the month, day, and time-of-day
        // parts of the result are the same as those of the specified DateTime.
        //
        public DateTime AddYears(DateTime time, int years)
        {
            return AddMonths(time, years * 12);
        }

        // Returns the day-of-month part of the specified DateTime. The returned
        // value is an integer between 1 and 31.
        //
        public int GetDayOfMonth(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return time.Day;
        }

        // Returns the day-of-week part of the specified DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        public DayOfWeek GetDayOfWeek(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return time.DayOfWeek;
        }

        // Returns the day-of-year part of the specified DateTime. The returned value
        // is an integer between 1 and 366.
        //
        public int GetDayOfYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return time.DayOfYear;
        }

        // Returns the number of days in the month given by the year and
        // month arguments.
        //
        public int GetDaysInMonth(int year, int month, int era)
        {
            //
            // Convert year/era value to Gregorain year value.
            //
            year = GetGregorianYear(year, era);
            if (month < 1 || month > 12)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Month(month);
            }
            ReadOnlySpan<int> days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
            return days[month] - days[month - 1];
        }

        // Returns the number of days in the year given by the year argument for the current era.
        //

        public int GetDaysInYear(int year, int era)
        {
            //
            // Convert year/era value to Gregorain year value.
            //
            year = GetGregorianYear(year, era);
            return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? 366 : 365;
        }

        // Returns the era for the specified DateTime value.
        public int GetEra(DateTime time)
        {
            long ticks = time.Ticks;
            // The assumption here is that m_EraInfo is listed in reverse order.
            foreach (EraInfo eraInfo in m_EraInfo)
            {
                if (ticks >= eraInfo.ticks)
                {
                    return eraInfo.era;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(time), SR.ArgumentOutOfRange_Era);
        }

        public int[] Eras
        {
            get
            {
                EraInfo[] eraInfo = m_EraInfo;
                var eras = new int[eraInfo.Length];
                for (int i = 0; i < eraInfo.Length; i++)
                {
                    eras[i] = eraInfo[i].era;
                }
                return eras;
            }
        }

        // Returns the month part of the specified DateTime. The returned value is an
        // integer between 1 and 12.
        //
        public int GetMonth(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return time.Month;
        }

        // Returns the number of months in the specified year and era.
        // Always return 12.
        public int GetMonthsInYear(int year, int era)
        {
            ValidateYearInEra(year, era);
            return 12;
        }

        // Returns the year part of the specified DateTime. The returned value is an
        // integer between 1 and 9999.
        //
        public int GetYear(DateTime time)
        {
            long ticks = time.Ticks;
            CheckTicksRange(ticks);
            foreach (EraInfo eraInfo in m_EraInfo)
            {
                if (ticks >= eraInfo.ticks)
                {
                    return time.Year - eraInfo.yearOffset;
                }
            }
            throw new ArgumentException(SR.Argument_NoEra);
        }

        // Returns the year that match the specified Gregorian year. The returned value is an
        // integer between 1 and 9999.
        //
        public int GetYear(int year, DateTime time)
        {
            long ticks = time.Ticks;
            foreach (EraInfo eraInfo in m_EraInfo)
            {
                // while calculating dates with JapaneseLuniSolarCalendar, we can run into cases right after the start of the era
                // and still belong to the month which is started in previous era. Calculating equivalent calendar date will cause
                // using the new era info which will have the year offset equal to the year we are calculating year = m_EraInfo[i].yearOffset
                // which will end up with zero as calendar year.
                // We should use the previous era info instead to get the right year number. Example of such date is Feb 2nd 1989
                if (ticks >= eraInfo.ticks && year > eraInfo.yearOffset)
                {
                    return year - eraInfo.yearOffset;
                }
            }
            throw new ArgumentException(SR.Argument_NoEra);
        }

        // Checks whether a given day in the specified era is a leap day. This method returns true if
        // the date is a leap day, or false if not.
        //
        public bool IsLeapDay(int year, int month, int day, int era)
        {
            // year/month/era checking is done in GetDaysInMonth()
            if (day < 1 || day > GetDaysInMonth(year, month, era))
            {
                throw new ArgumentOutOfRangeException(
                            nameof(day),
                            SR.Format(
                                SR.ArgumentOutOfRange_Range,
                                1,
                                GetDaysInMonth(year, month, era)));
            }

            if (!IsLeapYear(year, era))
            {
                return false;
            }

            if (month == 2 && day == 29)
            {
                return true;
            }

            return false;
        }

        // Giving the calendar year and era, ValidateYearInEra will validate the existence of the input year in the input era.
        // This method will throw if the year or the era is invalid.
        public void ValidateYearInEra(int year, int era) => GetYearOffset(year, era, throwOnError: true);

        // Returns the leap month in a calendar year of the specified era.
        // This method always returns 0 as all calendars using this method don't have leap months.
        public int GetLeapMonth(int year, int era)
        {
            ValidateYearInEra(year, era);
            return 0;
        }

        // Checks whether a given month in the specified era is a leap month.
        // This method always returns false as all calendars using this method don't have leap months.
        public bool IsLeapMonth(int year, int month, int era)
        {
            ValidateYearInEra(year, era);
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(month),
                            SR.Format(
                                SR.ArgumentOutOfRange_Range,
                                1,
                                12));
            }
            return false;
        }

        // Checks whether a given year in the specified era is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //
        public bool IsLeapYear(int year, int era)
        {
            year = GetGregorianYear(year, era);
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        // Returns the date and time converted to a DateTime value.  Throws an exception if the n-tuple is invalid.
        //
        public DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            year = GetGregorianYear(year, era);
            long ticks = DateToTicks(year, month, day) + Calendar.TimeToTicks(hour, minute, second, millisecond);
            CheckTicksRange(ticks);
            return new DateTime(ticks);
        }

        public int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            CheckTicksRange(time.Ticks);
            // Use GregorianCalendar to get around the problem that the implementation in Calendar.GetWeekOfYear()
            // can call GetYear() that exceeds the supported range of the Gregorian-based calendars.
            return GregorianCalendar.GetDefaultInstance().GetWeekOfYear(time, rule, firstDayOfWeek);
        }

        public int ToFourDigitYear(int year, int twoDigitYearMax)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(year);

            if (year < 100)
            {
                return (twoDigitYearMax / 100 - (year > twoDigitYearMax % 100 ? 1 : 0)) * 100 + year;
            }

            if (year < m_minYear || year > m_maxYear)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(year),
                            SR.Format(SR.ArgumentOutOfRange_Range, m_minYear, m_maxYear));
            }

            // If the year value is above 100, just return the year value.  Don't have to do
            // the TwoDigitYearMax comparison.
            return year;
        }
    }
}
