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
        // 1 tick = 100ns = 10E-7 second
        // Number of ticks per time unit
        internal const long TicksPerMillisecond = 10000;
        internal const long TicksPerSecond = TicksPerMillisecond * 1000;
        internal const long TicksPerMinute = TicksPerSecond * 60;
        internal const long TicksPerHour = TicksPerMinute * 60;
        internal const long TicksPerDay = TicksPerHour * 24;

        // Number of milliseconds per time unit
        internal const int MillisPerSecond = 1000;
        internal const int MillisPerMinute = MillisPerSecond * 60;
        internal const int MillisPerHour = MillisPerMinute * 60;
        internal const int MillisPerDay = MillisPerHour * 24;

        // Number of days in a non-leap year
        internal const int DaysPerYear = 365;
        // Number of days in 4 years
        internal const int DaysPer4Years = DaysPerYear * 4 + 1;
        // Number of days in 100 years
        internal const int DaysPer100Years = DaysPer4Years * 25 - 1;
        // Number of days in 400 years
        internal const int DaysPer400Years = DaysPer100Years * 4 + 1;

        // Number of days from 1/1/0001 to 1/1/10000
        internal const int DaysTo10000 = DaysPer400Years * 25 - 366;

        internal const long MaxMillis = (long)DaysTo10000 * MillisPerDay;

        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;

        //
        // This is the max Gregorian year can be represented by DateTime class.  The limitation
        // is derived from DateTime class.
        //
        internal int MaxYear => m_maxYear;

        internal static readonly int[] DaysToMonth365 =
        {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
        };

        internal static readonly int[] DaysToMonth366 =
        {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
        };

        internal int m_maxYear;
        internal int m_minYear;
        internal Calendar m_Cal;

        internal EraInfo[] m_EraInfo;
        internal int[]? m_eras;

        // Construct an instance of gregorian calendar.
        internal GregorianCalendarHelper(Calendar cal, EraInfo[] eraInfo)
        {
            m_Cal = cal;
            m_EraInfo = eraInfo;
            m_maxYear = m_EraInfo[0].maxEraYear;
            m_minYear = m_EraInfo[0].minEraYear;
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

            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (era == m_EraInfo[i].era)
                {
                    if (year >= m_EraInfo[i].minEraYear)
                    {
                        if (year <= m_EraInfo[i].maxEraYear)
                        {
                            return m_EraInfo[i].yearOffset;
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

                            int remainingYears = year - m_EraInfo[i].maxEraYear;

                            for (int j = i - 1; j >= 0; j--)
                            {
                                if (remainingYears <= m_EraInfo[j].maxEraYear)
                                {
                                    return m_EraInfo[i].yearOffset;
                                }
                                remainingYears -= m_EraInfo[j].maxEraYear;
                            }
                        }
                    }

                    if (throwOnError)
                    {
                        throw new ArgumentOutOfRangeException(
                                    nameof(year),
                                    SR.Format(
                                        SR.ArgumentOutOfRange_Range,
                                        m_EraInfo[i].minEraYear,
                                        m_EraInfo[i].maxEraYear));
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

        // Returns a given date part of this DateTime. This method is used
        // to compute the year, day-of-year, month, or day part.
        internal int GetDatePart(long ticks, int part)
        {
            CheckTicksRange(ticks);
            // n = number of days since 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = number of whole 400-year periods since 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = day number within 400-year period
            n -= y400 * DaysPer400Years;
            // y100 = number of whole 100-year periods within 400-year period
            int y100 = n / DaysPer100Years;
            // Last 100-year period has an extra day, so decrement result if 4
            if (y100 == 4) y100 = 3;
            // n = day number within 100-year period
            n -= y100 * DaysPer100Years;
            // y4 = number of whole 4-year periods within 100-year period
            int y4 = n / DaysPer4Years;
            // n = day number within 4-year period
            n -= y4 * DaysPer4Years;
            // y1 = number of whole years within 4-year period
            int y1 = n / DaysPerYear;
            // Last year has an extra day, so decrement result if 4
            if (y1 == 4) y1 = 3;
            // If year was requested, compute and return it
            if (part == DatePartYear)
            {
                return y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            }
            // n = day number within year
            n -= y1 * DaysPerYear;
            // If day-of-year was requested, return it
            if (part == DatePartDayOfYear)
            {
                return n + 1;
            }
            // Leap year calculation looks different from IsLeapYear since y1, y4,
            // and y100 are relative to year 1, not year 0
            bool leapYear = (y1 == 3 && (y4 != 24 || y100 == 3));
            int[] days = leapYear ? DaysToMonth366 : DaysToMonth365;
            // All months have less than 32 days, so n >> 5 is a good conservative
            // estimate for the month
            int m = (n >> 5) + 1;
            // m = 1-based month number
            while (n >= days[m]) m++;
            // If month was requested, return it
            if (part == DatePartMonth) return m;
            // Return 1-based day-of-month
            return n - days[m - 1] + 1;
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
                int[] days = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365;
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
            return GetAbsoluteDate(year, month, day) * TicksPerDay;
        }

        // Return the tick count corresponding to the given hour, minute, second.
        // Will check the if the parameters are valid.
        internal static long TimeToTicks(int hour, int minute, int second, int millisecond)
        {
            // TimeSpan.TimeToTicks is a family access function which does no error checking, so
            // we need to put some error checking out here.
            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60)
            {
                if (millisecond < 0 || millisecond >= MillisPerSecond)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(millisecond),
                                SR.Format(
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    MillisPerSecond - 1));
                }
                return InternalGlobalizationHelper.TimeToTicks(hour, minute, second) + millisecond * TicksPerMillisecond;
            }
            throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
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
            int[] daysArray = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) ? DaysToMonth366 : DaysToMonth365;
            int days = (daysArray[m] - daysArray[m - 1]);

            if (d > days)
            {
                d = days;
            }
            long ticks = DateToTicks(y, m, d) + (time.Ticks % TicksPerDay);
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
            return GetDatePart(time.Ticks, DatePartDay);
        }

        // Returns the day-of-week part of the specified DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        public DayOfWeek GetDayOfWeek(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (DayOfWeek)((time.Ticks / TicksPerDay + 1) % 7);
        }

        // Returns the day-of-year part of the specified DateTime. The returned value
        // is an integer between 1 and 366.
        //
        public int GetDayOfYear(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartDayOfYear);
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
            int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
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
            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (ticks >= m_EraInfo[i].ticks)
                {
                    return m_EraInfo[i].era;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(time), SR.ArgumentOutOfRange_Era);
        }

        public int[] Eras
        {
            get
            {
                if (m_eras == null)
                {
                    m_eras = new int[m_EraInfo.Length];
                    for (int i = 0; i < m_EraInfo.Length; i++)
                    {
                        m_eras[i] = m_EraInfo[i].era;
                    }
                }
                return (int[])m_eras.Clone();
            }
        }

        // Returns the month part of the specified DateTime. The returned value is an
        // integer between 1 and 12.
        //
        public int GetMonth(DateTime time)
        {
            return GetDatePart(time.Ticks, DatePartMonth);
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
            int year = GetDatePart(ticks, DatePartYear);
            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (ticks >= m_EraInfo[i].ticks)
                {
                    return year - m_EraInfo[i].yearOffset;
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
            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                // while calculating dates with JapaneseLuniSolarCalendar, we can run into cases right after the start of the era
                // and still belong to the month which is started in previous era. Calculating equivalent calendar date will cause
                // using the new era info which will have the year offset equal to the year we are calculating year = m_EraInfo[i].yearOffset
                // which will end up with zero as calendar year.
                // We should use the previous era info instead to get the right year number. Example of such date is Feb 2nd 1989
                if (ticks >= m_EraInfo[i].ticks && year > m_EraInfo[i].yearOffset)
                {
                    return year - m_EraInfo[i].yearOffset;
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
            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second, millisecond);
            CheckTicksRange(ticks);
            return new DateTime(ticks);
        }

        public int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            CheckTicksRange(time.Ticks);
            // Use GregorianCalendar to get around the problem that the implmentation in Calendar.GetWeekOfYear()
            // can call GetYear() that exceeds the supported range of the Gregorian-based calendars.
            return GregorianCalendar.GetDefaultInstance().GetWeekOfYear(time, rule, firstDayOfWeek);
        }

        public int ToFourDigitYear(int year, int twoDigitYearMax)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year),
                    SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (year < 100)
            {
                int y = year % 100;
                return (twoDigitYearMax / 100 - (y > twoDigitYearMax % 100 ? 1 : 0)) * 100 + y;
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
