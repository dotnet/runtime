// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Globalization
{
    public abstract class EastAsianLunisolarCalendar : Calendar
    {
        internal const int LeapMonth = 0;
        internal const int Jan1Month = 1;
        internal const int Jan1Date = 2;
        internal const int nDaysPerMonth = 3;

        // # of days so far in the solar year
        internal static readonly int[] DaysToMonth365 =
        {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334
        };

        internal static readonly int[] DaysToMonth366 =
        {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335
        };

        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.LunisolarCalendar;
            }
        }

        // Return the year number in the 60-year cycle.
        //

        public virtual int GetSexagenaryYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);

            int year = 0, month = 0, day = 0;
            TimeToLunar(time, ref year, ref month, ref day);

            return ((year - 4) % 60) + 1;
        }

        // Return the celestial year from the 60-year cycle.
        // The returned value is from 1 ~ 10.
        //

        public int GetCelestialStem(int sexagenaryYear)
        {
            if ((sexagenaryYear < 1) || (sexagenaryYear > 60))
            {
                throw new ArgumentOutOfRangeException(
                                nameof(sexagenaryYear),
                                SR.Format(SR.ArgumentOutOfRange_Range, 1, 60));
            }

            return ((sexagenaryYear - 1) % 10) + 1;
        }

        // Return the Terrestial Branch from the 60-year cycle.
        // The returned value is from 1 ~ 12.
        //

        public int GetTerrestrialBranch(int sexagenaryYear)
        {
            if ((sexagenaryYear < 1) || (sexagenaryYear > 60))
            {
                throw new ArgumentOutOfRangeException(
                                nameof(sexagenaryYear),
                                SR.Format(SR.ArgumentOutOfRange_Range, 1, 60));
            }

            return ((sexagenaryYear - 1) % 12) + 1;
        }

        internal abstract int GetYearInfo(int LunarYear, int Index);
        internal abstract int GetYear(int year, DateTime time);
        internal abstract int GetGregorianYear(int year, int era);

        internal abstract int MinCalendarYear { get; }
        internal abstract int MaxCalendarYear { get; }
        internal abstract EraInfo[] CalEraInfo { get; }
        internal abstract DateTime MinDate { get; }
        internal abstract DateTime MaxDate { get; }

        internal const int MaxCalendarMonth = 13;
        internal const int MaxCalendarDay = 30;

        internal int MinEraCalendarYear(int era)
        {
            EraInfo[] mEraInfo = CalEraInfo;
            //ChineseLunisolarCalendar does not has m_EraInfo it is going to retuen null
            if (mEraInfo == null)
            {
                return MinCalendarYear;
            }

            if (era == Calendar.CurrentEra)
            {
                era = CurrentEraValue;
            }
            //era has to be in the supported range otherwise we will throw exception in CheckEraRange()
            if (era == GetEra(MinDate))
            {
                return (GetYear(MinCalendarYear, MinDate));
            }

            for (int i = 0; i < mEraInfo.Length; i++)
            {
                if (era == mEraInfo[i].era)
                {
                    return (mEraInfo[i].minEraYear);
                }
            }
            throw new ArgumentOutOfRangeException(nameof(era), SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal int MaxEraCalendarYear(int era)
        {
            EraInfo[] mEraInfo = CalEraInfo;
            //ChineseLunisolarCalendar does not has m_EraInfo it is going to retuen null
            if (mEraInfo == null)
            {
                return MaxCalendarYear;
            }

            if (era == Calendar.CurrentEra)
            {
                era = CurrentEraValue;
            }
            //era has to be in the supported range otherwise we will throw exception in CheckEraRange()
            if (era == GetEra(MaxDate))
            {
                return (GetYear(MaxCalendarYear, MaxDate));
            }

            for (int i = 0; i < mEraInfo.Length; i++)
            {
                if (era == mEraInfo[i].era)
                {
                    return (mEraInfo[i].maxEraYear);
                }
            }
            throw new ArgumentOutOfRangeException(nameof(era), SR.ArgumentOutOfRange_InvalidEraValue);
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
                                String.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRange_CalendarRange,
                                MinSupportedDateTime, MaxSupportedDateTime));
            }
        }

        internal void CheckEraRange(int era)
        {
            if (era == Calendar.CurrentEra)
            {
                era = CurrentEraValue;
            }

            if ((era < GetEra(MinDate)) || (era > GetEra(MaxDate)))
            {
                throw new ArgumentOutOfRangeException(nameof(era), SR.ArgumentOutOfRange_InvalidEraValue);
            }
        }

        internal int CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            year = GetGregorianYear(year, era);

            if ((year < MinCalendarYear) || (year > MaxCalendarYear))
            {
                throw new ArgumentOutOfRangeException(
                                nameof(year),
                                SR.Format(SR.ArgumentOutOfRange_Range, MinEraCalendarYear(era), MaxEraCalendarYear(era)));
            }
            return year;
        }

        internal int CheckYearMonthRange(int year, int month, int era)
        {
            year = CheckYearRange(year, era);

            if (month == 13)
            {
                //Reject if there is no leap month this year
                if (GetYearInfo(year, LeapMonth) == 0)
                    throw new ArgumentOutOfRangeException(nameof(month), SR.ArgumentOutOfRange_Month);
            }

            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException(nameof(month), SR.ArgumentOutOfRange_Month);
            }
            return year;
        }

        internal int InternalGetDaysInMonth(int year, int month)
        {
            int nDays;
            int mask;        // mask for extracting bits

            mask = 0x8000;
            // convert the lunar day into a lunar month/date
            mask >>= (month - 1);
            if ((GetYearInfo(year, nDaysPerMonth) & mask) == 0)
                nDays = 29;
            else
                nDays = 30;
            return nDays;
        }

        // Returns the number of days in the month given by the year and
        // month arguments.
        //

        public override int GetDaysInMonth(int year, int month, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            return InternalGetDaysInMonth(year, month);
        }

        private static int GregorianIsLeapYear(int y)
        {
            return ((((y) % 4) != 0) ? 0 : ((((y) % 100) != 0) ? 1 : ((((y) % 400) != 0) ? 0 : 1)));
        }

        // Returns the date and time converted to a DateTime value.  Throws an exception if the n-tuple is invalid.
        //

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            int daysInMonth = InternalGetDaysInMonth(year, month);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(day),
                            SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }

            int gy = 0; int gm = 0; int gd = 0;

            if (LunarToGregorian(year, month, day, ref gy, ref gm, ref gd))
            {
                return new DateTime(gy, gm, gd, hour, minute, second, millisecond);
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }
        }


        //
        // GregorianToLunar calculates lunar calendar info for the given gregorian year, month, date.
        // The input date should be validated before calling this method.
        //
        internal void GregorianToLunar(int nSYear, int nSMonth, int nSDate, ref int nLYear, ref int nLMonth, ref int nLDate)
        {
            //    unsigned int nLYear, nLMonth, nLDate;    // lunar ymd
            int nSolarDay;        // day # in solar year
            int nLunarDay;        // day # in lunar year
            int fLeap;                    // is it a solar leap year?
            int LDpM;        // lunar days/month bitfield
            int mask;        // mask for extracting bits
            int nDays;        // # days this lunar month
            int nJan1Month, nJan1Date;

            // calc the solar day of year
            fLeap = GregorianIsLeapYear(nSYear);
            nSolarDay = (fLeap == 1) ? DaysToMonth366[nSMonth - 1] : DaysToMonth365[nSMonth - 1];
            nSolarDay += nSDate;

            // init lunar year info
            nLunarDay = nSolarDay;
            nLYear = nSYear;
            if (nLYear == (MaxCalendarYear + 1))
            {
                nLYear--;
                nLunarDay += ((GregorianIsLeapYear(nLYear) == 1) ? 366 : 365);
                nJan1Month = GetYearInfo(nLYear, Jan1Month);
                nJan1Date = GetYearInfo(nLYear, Jan1Date);
            }
            else
            {
                nJan1Month = GetYearInfo(nLYear, Jan1Month);
                nJan1Date = GetYearInfo(nLYear, Jan1Date);

                // check if this solar date is actually part of the previous
                // lunar year
                if ((nSMonth < nJan1Month) ||
                    (nSMonth == nJan1Month && nSDate < nJan1Date))
                {
                    // the corresponding lunar day is actually part of the previous
                    // lunar year
                    nLYear--;

                    // add a solar year to the lunar day #
                    nLunarDay += ((GregorianIsLeapYear(nLYear) == 1) ? 366 : 365);

                    // update the new start of year
                    nJan1Month = GetYearInfo(nLYear, Jan1Month);
                    nJan1Date = GetYearInfo(nLYear, Jan1Date);
                }
            }

            // convert solar day into lunar day.
            // subtract off the beginning part of the solar year which is not
            // part of the lunar year.  since this part is always in Jan or Feb,
            // we don't need to handle Leap Year (LY only affects March
            // and later).
            nLunarDay -= DaysToMonth365[nJan1Month - 1];
            nLunarDay -= (nJan1Date - 1);

            // convert the lunar day into a lunar month/date
            mask = 0x8000;
            LDpM = GetYearInfo(nLYear, nDaysPerMonth);
            nDays = ((LDpM & mask) != 0) ? 30 : 29;
            nLMonth = 1;
            while (nLunarDay > nDays)
            {
                nLunarDay -= nDays;
                nLMonth++;
                mask >>= 1;
                nDays = ((LDpM & mask) != 0) ? 30 : 29;
            }
            nLDate = nLunarDay;
        }

        /*
        //Convert from Lunar to Gregorian
        //Highly inefficient, but it works based on the forward conversion
        */
        internal bool LunarToGregorian(int nLYear, int nLMonth, int nLDate, ref int nSolarYear, ref int nSolarMonth, ref int nSolarDay)
        {
            int numLunarDays;

            if (nLDate < 1 || nLDate > 30)
                return false;

            numLunarDays = nLDate - 1;

            //Add previous months days to form the total num of days from the first of the month.
            for (int i = 1; i < nLMonth; i++)
            {
                numLunarDays += InternalGetDaysInMonth(nLYear, i);
            }

            //Get Gregorian First of year
            int nJan1Month = GetYearInfo(nLYear, Jan1Month);
            int nJan1Date = GetYearInfo(nLYear, Jan1Date);

            // calc the solar day of year of 1 Lunar day
            int fLeap = GregorianIsLeapYear(nLYear);
            int[] days = (fLeap == 1) ? DaysToMonth366 : DaysToMonth365;

            nSolarDay = nJan1Date;

            if (nJan1Month > 1)
                nSolarDay += days[nJan1Month - 1];

            // Add the actual lunar day to get the solar day we want
            nSolarDay = nSolarDay + numLunarDays;// - 1;

            if (nSolarDay > (fLeap + 365))
            {
                nSolarYear = nLYear + 1;
                nSolarDay -= (fLeap + 365);
            }
            else
            {
                nSolarYear = nLYear;
            }

            for (nSolarMonth = 1; nSolarMonth < 12; nSolarMonth++)
            {
                if (days[nSolarMonth] >= nSolarDay)
                    break;
            }

            nSolarDay -= days[nSolarMonth - 1];
            return true;
        }

        internal DateTime LunarToTime(DateTime time, int year, int month, int day)
        {
            int gy = 0; int gm = 0; int gd = 0;
            LunarToGregorian(year, month, day, ref gy, ref gm, ref gd);
            return (GregorianCalendar.GetDefaultInstance().ToDateTime(gy, gm, gd, time.Hour, time.Minute, time.Second, time.Millisecond));
        }

        internal void TimeToLunar(DateTime time, ref int year, ref int month, ref int day)
        {
            int gy = 0; int gm = 0; int gd = 0;

            Calendar Greg = GregorianCalendar.GetDefaultInstance();
            gy = Greg.GetYear(time);
            gm = Greg.GetMonth(time);
            gd = Greg.GetDayOfMonth(time);

            GregorianToLunar(gy, gm, gd, ref year, ref month, ref day);
        }

        // Returns the DateTime resulting from adding the given number of
        // months to the specified DateTime. The result is computed by incrementing
        // (or decrementing) the year and month parts of the specified DateTime by
        // value months, and, if required, adjusting the day part of the
        // resulting date downwards to the last day of the resulting month in the
        // resulting year. The time-of-day part of the result is the same as the
        // time-of-day part of the specified DateTime.
        //

        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(months),
                            SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
            }

            CheckTicksRange(time.Ticks);

            int y = 0; int m = 0; int d = 0;
            TimeToLunar(time, ref y, ref m, ref d);

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

            int y = 0; int m = 0; int d = 0;
            TimeToLunar(time, ref y, ref m, ref d);

            y += years;

            if (m == 13 && !InternalIsLeapYear(y))
            {
                m = 12;
                d = InternalGetDaysInMonth(y, m);
            }
            int DaysInMonths = InternalGetDaysInMonth(y, m);
            if (d > DaysInMonths)
            {
                d = DaysInMonths;
            }

            DateTime dt = LunarToTime(time, y, m, d);
            CheckAddResult(dt.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (dt);
        }

        // Returns the day-of-year part of the specified DateTime. The returned value
        // is an integer between 1 and [354|355 |383|384].
        //

        public override int GetDayOfYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);

            int y = 0; int m = 0; int d = 0;
            TimeToLunar(time, ref y, ref m, ref d);

            for (int i = 1; i < m; i++)
            {
                d = d + InternalGetDaysInMonth(y, i);
            }
            return d;
        }

        // Returns the day-of-month part of the specified DateTime. The returned
        // value is an integer between 1 and 29 or 30.
        //

        public override int GetDayOfMonth(DateTime time)
        {
            CheckTicksRange(time.Ticks);

            int y = 0; int m = 0; int d = 0;
            TimeToLunar(time, ref y, ref m, ref d);

            return d;
        }

        // Returns the number of days in the year given by the year argument for the current era.
        //

        public override int GetDaysInYear(int year, int era)
        {
            year = CheckYearRange(year, era);

            int Days = 0;
            int monthsInYear = InternalIsLeapYear(year) ? 13 : 12;

            while (monthsInYear != 0)
                Days += InternalGetDaysInMonth(year, monthsInYear--);

            return Days;
        }

        // Returns the month part of the specified DateTime. The returned value is an
        // integer between 1 and 13.
        //

        public override int GetMonth(DateTime time)
        {
            CheckTicksRange(time.Ticks);

            int y = 0; int m = 0; int d = 0;
            TimeToLunar(time, ref y, ref m, ref d);

            return m;
        }

        // Returns the year part of the specified DateTime. The returned value is an
        // integer between 1 and MaxCalendarYear.
        //

        public override int GetYear(DateTime time)
        {
            CheckTicksRange(time.Ticks);

            int y = 0; int m = 0; int d = 0;
            TimeToLunar(time, ref y, ref m, ref d);

            return GetYear(y, time);
        }

        // Returns the day-of-week part of the specified DateTime. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return ((DayOfWeek)((int)(time.Ticks / Calendar.TicksPerDay + 1) % 7));
        }

        // Returns the number of months in the specified year and era.

        public override int GetMonthsInYear(int year, int era)
        {
            year = CheckYearRange(year, era);
            return (InternalIsLeapYear(year) ? 13 : 12);
        }

        // Checks whether a given day in the specified era is a leap day. This method returns true if
        // the date is a leap day, or false if not.
        //

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            int daysInMonth = InternalGetDaysInMonth(year, month);

            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(day),
                            SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }
            int m = GetYearInfo(year, LeapMonth);
            return ((m != 0) && (month == (m + 1)));
        }

        // Checks whether a given month in the specified era is a leap month. This method returns true if
        // month is a leap month, or false if not.
        //

        public override bool IsLeapMonth(int year, int month, int era)
        {
            year = CheckYearMonthRange(year, month, era);
            int m = GetYearInfo(year, LeapMonth);
            return ((m != 0) && (month == (m + 1)));
        }

        // Returns  the leap month in a calendar year of the specified era. This method returns 0
        // if this this year is not a leap year.
        //

        public override int GetLeapMonth(int year, int era)
        {
            year = CheckYearRange(year, era);
            int month = GetYearInfo(year, LeapMonth);
            if (month > 0)
            {
                return (month + 1);
            }
            return 0;
        }

        internal bool InternalIsLeapYear(int year)
        {
            return (GetYearInfo(year, LeapMonth) != 0);
        }
        // Checks whether a given year in the specified era is a leap year. This method returns true if
        // year is a leap year, or false if not.
        //

        public override bool IsLeapYear(int year, int era)
        {
            year = CheckYearRange(year, era);
            return InternalIsLeapYear(year);
        }

        private const int DEFAULT_GREGORIAN_TWO_DIGIT_YEAR_MAX = 2029;


        public override int TwoDigitYearMax
        {
            get
            {
                if (twoDigitYearMax == -1)
                {
                    twoDigitYearMax = GetSystemTwoDigitYearSetting(BaseCalendarID, GetYear(new DateTime(DEFAULT_GREGORIAN_TWO_DIGIT_YEAR_MAX, 1, 1)));
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
                                SR.Format(SR.ArgumentOutOfRange_Range, 99, MaxCalendarYear));
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

            year = base.ToFourDigitYear(year);
            CheckYearRange(year, CurrentEra);
            return (year);
        }
    }
}
