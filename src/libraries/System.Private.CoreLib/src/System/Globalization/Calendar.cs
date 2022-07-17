// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    // This abstract class represents a calendar. A calendar reckons time in
    // divisions such as weeks, months and years. The number, length and start of
    // the divisions vary in each calendar.
    //
    // Any instant in time can be represented as an n-tuple of numeric values using
    // a particular calendar. For example, the next vernal equinox occurs at (0.0, 0
    // , 46, 8, 20, 3, 1999) in the Gregorian calendar. An  implementation of
    // Calendar can map any DateTime value to such an n-tuple and vice versa. The
    // DateTimeFormat class can map between such n-tuples and a textual
    // representation such as "8:46 AM March 20th 1999 AD".
    //
    // Most calendars identify a year which begins the current era. There may be any
    // number of previous eras. The Calendar class identifies the eras as enumerated
    // integers where the current era (CurrentEra) has the value zero.
    //
    // For consistency, the first unit in each interval, e.g. the first month, is
    // assigned the value one.
    // The calculation of hour/minute/second is moved to Calendar from GregorianCalendar,
    // since most of the calendars (or all?) have the same way of calcuating hour/minute/second.

    public abstract class Calendar : ICloneable
    {
        // Number of 100ns (10E-7 second) ticks per time unit
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

        private int _currentEraValue = -1;

        private bool _isReadOnly;

        public virtual DateTime MinSupportedDateTime => DateTime.MinValue;

        public virtual DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public virtual CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.Unknown;

        protected Calendar()
        {
        }

        internal virtual CalendarId ID => CalendarId.UNINITIALIZED_VALUE;

        // Return the Base calendar ID for calendars that didn't have defined data in calendarData
        internal virtual CalendarId BaseCalendarID => ID;

        public bool IsReadOnly => _isReadOnly;

        public virtual object Clone()
        {
            object o = MemberwiseClone();
            ((Calendar)o).SetReadOnlyState(false);
            return o;
        }

        public static Calendar ReadOnly(Calendar calendar)
        {
            ArgumentNullException.ThrowIfNull(calendar);

            if (calendar.IsReadOnly)
            {
                return calendar;
            }

            Calendar clonedCalendar = (Calendar)(calendar.MemberwiseClone());
            clonedCalendar.SetReadOnlyState(true);
            return clonedCalendar;
        }

        internal void VerifyWritable()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        internal void SetReadOnlyState(bool readOnly)
        {
            _isReadOnly = readOnly;
        }

        /// <summary>
        /// This is used to convert CurrentEra(0) to an appropriate era value.
        /// </summary>
        internal virtual int CurrentEraValue
        {
            get
            {
                // The following code assumes that the current era value can not be -1.
                if (_currentEraValue == -1)
                {
                    Debug.Assert(BaseCalendarID != CalendarId.UNINITIALIZED_VALUE, "[Calendar.CurrentEraValue] Expected a real calendar ID");
                    _currentEraValue = CalendarData.GetCalendarCurrentEra(this);
                }

                return _currentEraValue;
            }
        }

        public const int CurrentEra = 0;

        internal int _twoDigitYearMax = -1;

        internal static void CheckAddResult(long ticks, DateTime minValue, DateTime maxValue)
        {
            if (ticks < minValue.Ticks || ticks > maxValue.Ticks)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ResultCalendarRange, minValue, maxValue));
            }
        }

        internal DateTime Add(DateTime time, double value, int scale)
        {
            // From ECMA CLI spec, Partition III, section 3.27:
            //
            // If overflow occurs converting a floating-point type to an integer, or if the floating-point value
            // being converted to an integer is a NaN, the value returned is unspecified.
            //
            // Based upon this, this method should be performing the comparison against the double
            // before attempting a cast. Otherwise, the result is undefined.
            double tempMillis = (value * scale + (value >= 0 ? 0.5 : -0.5));
            if (!((tempMillis > -(double)MaxMillis) && (tempMillis < (double)MaxMillis)))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_AddValue);
            }

            long millis = (long)tempMillis;
            long ticks = time.Ticks + millis * TicksPerMillisecond;
            CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return new DateTime(ticks);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding the given number of
        /// milliseconds to the specified DateTime. The result is computed by rounding
        /// the number of milliseconds given by value to the nearest integer,
        /// and adding that interval to the specified DateTime. The value
        /// argument is permitted to be negative.
        /// </summary>
        public virtual DateTime AddMilliseconds(DateTime time, double milliseconds)
        {
            return Add(time, milliseconds, 1);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding a fractional number of
        /// days to the specified DateTime. The result is computed by rounding the
        /// fractional number of days given by value to the nearest
        /// millisecond, and adding that interval to the specified DateTime. The
        /// value argument is permitted to be negative.
        /// </summary>
        public virtual DateTime AddDays(DateTime time, int days)
        {
            return Add(time, days, MillisPerDay);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding a fractional number of
        /// hours to the specified DateTime. The result is computed by rounding the
        /// fractional number of hours given by value to the nearest
        /// millisecond, and adding that interval to the specified DateTime. The
        /// value argument is permitted to be negative.
        /// </summary>
        public virtual DateTime AddHours(DateTime time, int hours)
        {
            return Add(time, hours, MillisPerHour);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding a fractional number of
        /// minutes to the specified DateTime. The result is computed by rounding the
        /// fractional number of minutes given by value to the nearest
        /// millisecond, and adding that interval to the specified DateTime. The
        /// value argument is permitted to be negative.
        /// </summary>
        public virtual DateTime AddMinutes(DateTime time, int minutes)
        {
            return Add(time, minutes, MillisPerMinute);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding the given number of
        /// months to the specified DateTime. The result is computed by incrementing
        /// (or decrementing) the year and month parts of the specified DateTime by
        /// value months, and, if required, adjusting the day part of the
        /// resulting date downwards to the last day of the resulting month in the
        /// resulting year. The time-of-day part of the result is the same as the
        /// time-of-day part of the specified DateTime.
        ///
        /// In more precise terms, considering the specified DateTime to be of the
        /// form y / m / d + t, where y is the
        /// year, m is the month, d is the day, and t is the
        /// time-of-day, the result is y1 / m1 / d1 + t,
        /// where y1 and m1 are computed by adding value months
        /// to y and m, and d1 is the largest value less than
        /// or equal to d that denotes a valid day in month m1 of year
        /// y1.
        /// </summary>
        public abstract DateTime AddMonths(DateTime time, int months);

        /// <summary>
        /// Returns the DateTime resulting from adding a number of
        /// seconds to the specified DateTime. The result is computed by rounding the
        /// fractional number of seconds given by value to the nearest
        /// millisecond, and adding that interval to the specified DateTime. The
        /// value argument is permitted to be negative.
        /// </summary>
        public virtual DateTime AddSeconds(DateTime time, int seconds)
        {
            return Add(time, seconds, MillisPerSecond);
        }

        // Returns the DateTime resulting from adding a number of
        // weeks to the specified DateTime. The
        // value argument is permitted to be negative.
        public virtual DateTime AddWeeks(DateTime time, int weeks)
        {
            return AddDays(time, weeks * 7);
        }

        /// <summary>
        /// Returns the DateTime resulting from adding the given number of
        /// years to the specified DateTime. The result is computed by incrementing
        /// (or decrementing) the year part of the specified DateTime by value
        /// years. If the month and day of the specified DateTime is 2/29, and if the
        /// resulting year is not a leap year, the month and day of the resulting
        /// DateTime becomes 2/28. Otherwise, the month, day, and time-of-day
        /// parts of the result are the same as those of the specified DateTime.
        /// </summary>
        public abstract DateTime AddYears(DateTime time, int years);

        /// <summary>
        /// Returns the day-of-month part of the specified DateTime. The returned
        /// value is an integer between 1 and 31.
        /// </summary>
        public abstract int GetDayOfMonth(DateTime time);

        /// <summary>
        /// Returns the day-of-week part of the specified DateTime. The returned value
        /// is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        /// Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        /// Thursday, 5 indicates Friday, and 6 indicates Saturday.
        /// </summary>
        public abstract DayOfWeek GetDayOfWeek(DateTime time);

        /// <summary>
        /// Returns the day-of-year part of the specified DateTime. The returned value
        /// is an integer between 1 and 366.
        /// </summary>
        public abstract int GetDayOfYear(DateTime time);

        /// <summary>
        /// Returns the number of days in the month given by the year and
        /// month arguments.
        /// </summary>
        public virtual int GetDaysInMonth(int year, int month)
        {
            return GetDaysInMonth(year, month, CurrentEra);
        }

        /// <summary>
        /// Returns the number of days in the month given by the year and
        /// month arguments for the specified era.
        /// </summary>
        public abstract int GetDaysInMonth(int year, int month, int era);

        /// <summary>
        /// Returns the number of days in the year given by the year argument
        /// for the current era.
        /// </summary>
        public virtual int GetDaysInYear(int year)
        {
            return GetDaysInYear(year, CurrentEra);
        }

        /// <summary>
        /// Returns the number of days in the year given by the year argument
        /// for the current era.
        /// </summary>
        public abstract int GetDaysInYear(int year, int era);

        /// <summary>
        /// Returns the era for the specified DateTime value.
        /// </summary>
        public abstract int GetEra(DateTime time);

        /// <summary>
        /// Get the list of era values.
        /// </summary>
        /// <returns>The int array of the era names supported in this calendar or null if era is not used.</returns>
        public abstract int[] Eras { get; }

        // Returns the hour part of the specified DateTime. The returned value is an
        // integer between 0 and 23.
        public virtual int GetHour(DateTime time) => time.Hour;

        // Returns the millisecond part of the specified DateTime. The returned value
        // is an integer between 0 and 999.
        public virtual double GetMilliseconds(DateTime time) => time.Millisecond;

        // Returns the minute part of the specified DateTime. The returned value is
        // an integer between 0 and 59.
        public virtual int GetMinute(DateTime time) => time.Minute;

        // Returns the month part of the specified DateTime. The returned value is an
        // integer between 1 and 12.
        public abstract int GetMonth(DateTime time);

        // Returns the number of months in the specified year in the current era.
        public virtual int GetMonthsInYear(int year)
        {
            return GetMonthsInYear(year, CurrentEra);
        }

        // Returns the number of months in the specified year and era.
        public abstract int GetMonthsInYear(int year, int era);

        // Returns the second part of the specified DateTime. The returned value is
        // an integer between 0 and 59.
        public virtual int GetSecond(DateTime time) => time.Second;

        /// <summary>
        /// Get the week of year using the FirstDay rule.
        /// </summary>
        /// <remarks>
        ///  The CalendarWeekRule.FirstDay rule: Week 1 begins on the first day of the year.
        ///  Assume f is the specifed firstDayOfWeek,
        ///  and n is the day of week for January 1 of the specified year.
        ///  Assign offset = n - f;
        ///  Case 1: offset = 0
        ///      E.g.
        ///                     f=1
        ///          weekday 0  1  2  3  4  5  6  0  1
        ///          date       1/1
        ///          week#      1                    2
        ///      then week of year = (GetDayOfYear(time) - 1) / 7 + 1
        ///
        ///  Case 2: offset &lt; 0
        ///      e.g.
        ///                     n=1   f=3
        ///          weekday 0  1  2  3  4  5  6  0
        ///          date       1/1
        ///          week#      1     2
        ///      This means that the first week actually starts 5 days before 1/1.
        ///      So week of year = (GetDayOfYear(time) + (7 + offset) - 1) / 7 + 1
        ///  Case 3: offset > 0
        ///      e.g.
        ///                  f=0   n=2
        ///          weekday 0  1  2  3  4  5  6  0  1  2
        ///          date          1/1
        ///          week#         1                    2
        ///      This means that the first week actually starts 2 days before 1/1.
        ///      So Week of year = (GetDayOfYear(time) + offset - 1) / 7 + 1
        /// </remarks>
        internal int GetFirstDayWeekOfYear(DateTime time, int firstDayOfWeek)
        {
            int dayOfYear = GetDayOfYear(time) - 1;   // Make the day of year to be 0-based, so that 1/1 is day 0.
            // Calculate the day of week for the first day of the year.
            // dayOfWeek - (dayOfYear % 7) is the day of week for the first day of this year.  Note that
            // this value can be less than 0.  It's fine since we are making it positive again in calculating offset.
            int dayForJan1 = (int)GetDayOfWeek(time) - (dayOfYear % 7);
            int offset = (dayForJan1 - firstDayOfWeek + 14) % 7;
            Debug.Assert(offset >= 0, "Calendar.GetFirstDayWeekOfYear(): offset >= 0");
            return (dayOfYear + offset) / 7 + 1;
        }

        private int GetWeekOfYearFullDays(DateTime time, int firstDayOfWeek, int fullDays)
        {
            int dayForJan1;
            int offset;
            int day;

            int dayOfYear = GetDayOfYear(time) - 1; // Make the day of year to be 0-based, so that 1/1 is day 0.

            // Calculate the number of days between the first day of year (1/1) and the first day of the week.
            // This value will be a positive value from 0 ~ 6.  We call this value as "offset".
            //
            // If offset is 0, it means that the 1/1 is the start of the first week.
            //     Assume the first day of the week is Monday, it will look like this:
            //     Sun      Mon     Tue     Wed     Thu     Fri     Sat
            //     12/31    1/1     1/2     1/3     1/4     1/5     1/6
            //              +--> First week starts here.
            //
            // If offset is 1, it means that the first day of the week is 1 day ahead of 1/1.
            //     Assume the first day of the week is Monday, it will look like this:
            //     Sun      Mon     Tue     Wed     Thu     Fri     Sat
            //     1/1      1/2     1/3     1/4     1/5     1/6     1/7
            //              +--> First week starts here.
            //
            // If offset is 2, it means that the first day of the week is 2 days ahead of 1/1.
            //     Assume the first day of the week is Monday, it will look like this:
            //     Sat      Sun     Mon     Tue     Wed     Thu     Fri     Sat
            //     1/1      1/2     1/3     1/4     1/5     1/6     1/7     1/8
            //                      +--> First week starts here.

            // Day of week is 0-based.
            // Get the day of week for 1/1.  This can be derived from the day of week of the target day.
            // Note that we can get a negative value.  It's ok since we are going to make it a positive value when calculating the offset.
            dayForJan1 = (int)GetDayOfWeek(time) - (dayOfYear % 7);

            // Now, calculate the offset.  Subtract the first day of week from the dayForJan1.  And make it a positive value.
            offset = (firstDayOfWeek - dayForJan1 + 14) % 7;
            if (offset != 0 && offset >= fullDays)
            {
                // If the offset is greater than the value of fullDays, it means that
                // the first week of the year starts on the week where Jan/1 falls on.
                offset -= 7;
            }

            // Calculate the day of year for specified time by taking offset into account.
            day = dayOfYear - offset;
            if (day >= 0)
            {
                // If the day of year value is greater than zero, get the week of year.
                return day / 7 + 1;
            }

            // Otherwise, the specified time falls on the week of previous year.
            // Call this method again by passing the last day of previous year.
            // the last day of the previous year may "underflow" to no longer be a valid date time for
            // this calendar if we just subtract so we need the subclass to provide us with
            // that information
            if (time <= MinSupportedDateTime.AddDays(dayOfYear))
            {
                return GetWeekOfYearOfMinSupportedDateTime(firstDayOfWeek, fullDays);
            }

            return GetWeekOfYearFullDays(time.AddDays(-(dayOfYear + 1)), firstDayOfWeek, fullDays);
        }

        private int GetWeekOfYearOfMinSupportedDateTime(int firstDayOfWeek, int minimumDaysInFirstWeek)
        {
            int dayOfYear = GetDayOfYear(MinSupportedDateTime) - 1;  // Make the day of year to be 0-based, so that 1/1 is day 0.
            int dayOfWeekOfFirstOfYear = (int)GetDayOfWeek(MinSupportedDateTime) - dayOfYear % 7;

            // Calculate the offset (how many days from the start of the year to the start of the week)
            int offset = (firstDayOfWeek + 7 - dayOfWeekOfFirstOfYear) % 7;
            if (offset == 0 || offset >= minimumDaysInFirstWeek)
            {
                // First of year falls in the first week of the year
                return 1;
            }

            int daysInYearBeforeMinSupportedYear = DaysInYearBeforeMinSupportedYear - 1; // Make the day of year to be 0-based, so that 1/1 is day 0.
            int dayOfWeekOfFirstOfPreviousYear = dayOfWeekOfFirstOfYear - 1 - (daysInYearBeforeMinSupportedYear % 7);

            // starting from first day of the year, how many days do you have to go forward
            // before getting to the first day of the week?
            int daysInInitialPartialWeek = (firstDayOfWeek - dayOfWeekOfFirstOfPreviousYear + 14) % 7;
            int day = daysInYearBeforeMinSupportedYear - daysInInitialPartialWeek;
            if (daysInInitialPartialWeek >= minimumDaysInFirstWeek)
            {
                // If the offset is greater than the minimum Days in the first week, it means that
                // First of year is part of the first week of the year even though it is only a partial week
                // add another week
                day += 7;
            }

            return day / 7 + 1;
        }

        protected virtual int DaysInYearBeforeMinSupportedYear => 365;

        /// <summary>
        /// Returns the week of year for the specified DateTime. The returned value is an
        /// integer between 1 and 53.
        /// </summary>
        public virtual int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            if (firstDayOfWeek < DayOfWeek.Sunday || firstDayOfWeek > DayOfWeek.Saturday)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstDayOfWeek),
                    firstDayOfWeek,
                    SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

            return rule switch
            {
                CalendarWeekRule.FirstDay => GetFirstDayWeekOfYear(time, (int)firstDayOfWeek),
                CalendarWeekRule.FirstFullWeek => GetWeekOfYearFullDays(time, (int)firstDayOfWeek, 7),
                CalendarWeekRule.FirstFourDayWeek => GetWeekOfYearFullDays(time, (int)firstDayOfWeek, 4),
                _ => throw new ArgumentOutOfRangeException(
                        nameof(rule),
                        rule,
                        SR.Format(SR.ArgumentOutOfRange_Range, CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek)),
            };
        }

        /// <summary>
        /// Returns the year part of the specified DateTime. The returned value is an
        /// integer between 1 and 9999.
        /// </summary>
        public abstract int GetYear(DateTime time);

        /// <summary>
        /// Checks whether a given day in the current era is a leap day.
        /// This method returns true if the date is a leap day, or false if not.
        /// </summary>
        public virtual bool IsLeapDay(int year, int month, int day)
        {
            return IsLeapDay(year, month, day, CurrentEra);
        }

        /// <summary>
        /// Checks whether a given day in the specified era is a leap day.
        /// This method returns true if the date is a leap day, or false if not.
        /// </summary>
        public abstract bool IsLeapDay(int year, int month, int day, int era);

        /// <summary>
        /// Checks whether a given month in the current era is a leap month.
        /// This method returns true if month is a leap month, or false if not.
        /// </summary>
        public virtual bool IsLeapMonth(int year, int month)
        {
            return IsLeapMonth(year, month, CurrentEra);
        }

        /// <summary>
        /// Checks whether a given month in the specified era is a leap month. This method returns true if
        /// month is a leap month, or false if not.
        /// </summary>
        public abstract bool IsLeapMonth(int year, int month, int era);

        /// <summary>
        /// Returns  the leap month in a calendar year of the current era.
        /// This method returns 0 if this calendar does not have leap month,
        /// or this year is not a leap year.
        /// </summary>
        public virtual int GetLeapMonth(int year)
        {
            return GetLeapMonth(year, CurrentEra);
        }

        /// <summary>
        /// Returns  the leap month in a calendar year of the specified era.
        /// This method returns 0 if this calendar does not have leap month,
        /// or this year is not a leap year.
        /// </summary>
        public virtual int GetLeapMonth(int year, int era)
        {
            if (!IsLeapYear(year, era))
            {
                return 0;
            }

            int monthsCount = GetMonthsInYear(year, era);
            for (int month = 1; month <= monthsCount; month++)
            {
                if (IsLeapMonth(year, month, era))
                {
                    return month;
                }
            }

            return 0;
        }

        /// <summary>
        /// Checks whether a given year in the current era is a leap year.
        /// This method returns true if year is a leap year, or false if not.
        /// </summary>
        public virtual bool IsLeapYear(int year)
        {
            return IsLeapYear(year, CurrentEra);
        }

        /// <summary>
        /// Checks whether a given year in the specified era is a leap year.
        /// This method returns true if year is a leap year, or false if not.
        /// </summary>
        public abstract bool IsLeapYear(int year, int era);

        /// <summary>
        /// Returns the date and time converted to a DateTime value.
        /// Throws an exception if the n-tuple is invalid.
        /// </summary>
        public virtual DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            return ToDateTime(year, month, day, hour, minute, second, millisecond, CurrentEra);
        }

        /// <summary>
        /// Returns the date and time converted to a DateTime value.
        /// Throws an exception if the n-tuple is invalid.
        /// </summary>
        public abstract DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);

        internal virtual bool TryToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era, out DateTime result)
        {
            result = DateTime.MinValue;
            try
            {
                result = ToDateTime(year, month, day, hour, minute, second, millisecond, era);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        internal virtual bool IsValidYear(int year, int era)
        {
            return year >= GetYear(MinSupportedDateTime) && year <= GetYear(MaxSupportedDateTime);
        }

        internal virtual bool IsValidMonth(int year, int month, int era)
        {
            return IsValidYear(year, era) && month >= 1 && month <= GetMonthsInYear(year, era);
        }

        internal virtual bool IsValidDay(int year, int month, int day, int era)
        {
            return IsValidMonth(year, month, era) && day >= 1 && day <= GetDaysInMonth(year, month, era);
        }

        /// <summary>
        /// Returns and assigns the maximum value to represent a two digit year.
        /// This value is the upper boundary of a 100 year range that allows a
        /// two digit year to be properly translated to a four digit year.
        /// For example, if 2029 is the upper boundary, then a two digit value of
        /// 30 should be interpreted as 1930 while a two digit value of 29 should
        /// be interpreted as 2029.  In this example, the 100 year range would be
        /// from 1930-2029.  See ToFourDigitYear().
        /// </summary>
        public virtual int TwoDigitYearMax
        {
            get => _twoDigitYearMax;
            set
            {
                VerifyWritable();
                _twoDigitYearMax = value;
            }
        }

        /// <summary>
        /// Converts the year value to the appropriate century by using the
        /// TwoDigitYearMax property.  For example, if the TwoDigitYearMax value is 2029,
        /// then a two digit value of 30 will get converted to 1930 while a two digit
        /// value of 29 will get converted to 2029.
        /// </summary>
        public virtual int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (year < 100)
            {
                return (TwoDigitYearMax / 100 - (year > TwoDigitYearMax % 100 ? 1 : 0)) * 100 + year;
            }

            // If the year value is above 100, just return the year value.  Don't have to do
            // the TwoDigitYearMax comparison.
            return year;
        }

        /// <summary>
        /// Return the tick count corresponding to the given hour, minute, second.
        /// Will check the if the parameters are valid.
        /// </summary>
        internal static long TimeToTicks(int hour, int minute, int second, int millisecond)
        {
            if ((uint)hour >= 24 || (uint)minute >= 60 || (uint)second >= 60)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadHourMinuteSecond);
            }
            if ((uint)millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecond),
                    millisecond,
                    SR.Format(SR.ArgumentOutOfRange_Range, 0, MillisPerSecond - 1));
            }

            int totalSeconds = hour * 3600 + minute * 60 + second;
            return totalSeconds * TicksPerSecond + millisecond * TicksPerMillisecond;
        }

        internal static int GetSystemTwoDigitYearSetting(CalendarId CalID, int defaultYearValue)
        {
            int twoDigitYearMax = GlobalizationMode.UseNls ? CalendarData.NlsGetTwoDigitYearMax(CalID) : CalendarData.IcuGetTwoDigitYearMax();
            return twoDigitYearMax >= 0 ? twoDigitYearMax : defaultYearValue;
        }
    }
}
