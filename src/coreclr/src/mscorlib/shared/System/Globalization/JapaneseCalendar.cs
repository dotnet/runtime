// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    /*=================================JapaneseCalendar==========================
    **
    ** JapaneseCalendar is based on Gregorian calendar.  The month and day values are the same as
    ** Gregorian calendar.  However, the year value is an offset to the Gregorian
    ** year based on the era.
    **
    ** This system is adopted by Emperor Meiji in 1868. The year value is counted based on the reign of an emperor,
    ** and the era begins on the day an emperor ascends the throne and continues until his death.
    ** The era changes at 12:00AM.
    **
    ** For example, the current era is Heisei.  It started on 1989/1/8 A.D.  Therefore, Gregorian year 1989 is also Heisei 1st.
    ** 1989/1/8 A.D. is also Heisei 1st 1/8.
    **
    ** Any date in the year during which era is changed can be reckoned in either era.  For example,
    ** 1989/1/1 can be 1/1 Heisei 1st year or 1/1 Showa 64th year.
    **
    ** Note:
    **  The DateTime can be represented by the JapaneseCalendar are limited to two factors:
    **      1. The min value and max value of DateTime class.
    **      2. The available era information.
    **
    **  Calendar support range:
    **      Calendar    Minimum     Maximum
    **      ==========  ==========  ==========
    **      Gregorian   1868/09/08  9999/12/31
    **      Japanese    Meiji 01/01 Heisei 8011/12/31
    ============================================================================*/


    public partial class JapaneseCalendar : Calendar
    {
        internal static readonly DateTime calendarMinValue = new DateTime(1868, 9, 8);


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
                return (DateTime.MaxValue);
            }
        }

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.SolarCalendar;
            }
        }

        //
        // Using a field initializer rather than a static constructor so that the whole class can be lazy
        // init.
        internal static volatile EraInfo[] japaneseEraInfo;

        //
        // Read our era info
        //
        // m_EraInfo must be listed in reverse chronological order.  The most recent era
        // should be the first element.
        // That is, m_EraInfo[0] contains the most recent era.
        //
        // We know about 4 built-in eras, however users may add additional era(s) from the
        // registry, by adding values to HKLM\SYSTEM\CurrentControlSet\Control\Nls\Calendars\Japanese\Eras
        // we don't read the registry and instead we call WinRT to get the needed informatio
        //
        // Registry values look like:
        //      yyyy.mm.dd=era_abbrev_english_englishabbrev
        //
        // Where yyyy.mm.dd is the registry value name, and also the date of the era start.
        // yyyy, mm, and dd are the year, month & day the era begins (4, 2 & 2 digits long)
        // era is the Japanese Era name
        // abbrev is the Abbreviated Japanese Era Name
        // english is the English name for the Era (unused)
        // englishabbrev is the Abbreviated English name for the era.
        // . is a delimiter, but the value of . doesn't matter.
        // '_' marks the space between the japanese era name, japanese abbreviated era name
        //     english name, and abbreviated english names.
        //
        internal static EraInfo[] GetEraInfo()
        {
            // See if we need to build it
            if (japaneseEraInfo == null)
            {
                japaneseEraInfo = GetJapaneseEras();
                // See if we have to use the built-in eras
                if (japaneseEraInfo == null)
                {
                    // We know about some built-in ranges
                    EraInfo[] defaultEraRanges = new EraInfo[4];
                    defaultEraRanges[0] = new EraInfo(4, 1989, 1, 8, 1988, 1, GregorianCalendar.MaxYear - 1988,
                                                       "\x5e73\x6210", "\x5e73", "H");    // era #4 start year/month/day, yearOffset, minEraYear
                    defaultEraRanges[1] = new EraInfo(3, 1926, 12, 25, 1925, 1, 1989 - 1925,
                                                       "\x662d\x548c", "\x662d", "S");    // era #3,start year/month/day, yearOffset, minEraYear
                    defaultEraRanges[2] = new EraInfo(2, 1912, 7, 30, 1911, 1, 1926 - 1911,
                                                       "\x5927\x6b63", "\x5927", "T");    // era #2,start year/month/day, yearOffset, minEraYear
                    defaultEraRanges[3] = new EraInfo(1, 1868, 1, 1, 1867, 1, 1912 - 1867,
                                                       "\x660e\x6cbb", "\x660e", "M");    // era #1,start year/month/day, yearOffset, minEraYear

                    // Remember the ranges we built
                    japaneseEraInfo = defaultEraRanges;
                }
            }

            // return the era we found/made
            return japaneseEraInfo;
        }

        internal static volatile Calendar s_defaultInstance;
        internal GregorianCalendarHelper helper;

        /*=================================GetDefaultInstance==========================
        **Action: Internal method to provide a default intance of JapaneseCalendar.  Used by NLS+ implementation
        **       and other calendars.
        **Returns:
        **Arguments:
        **Exceptions:
        ============================================================================*/

        internal static Calendar GetDefaultInstance()
        {
            if (s_defaultInstance == null)
            {
                s_defaultInstance = new JapaneseCalendar();
            }
            return (s_defaultInstance);
        }


        public JapaneseCalendar()
        {
            try
            {
                new CultureInfo("ja-JP");
            }
            catch (ArgumentException e)
            {
                throw new TypeInitializationException(this.GetType().ToString(), e);
            }
            helper = new GregorianCalendarHelper(this, GetEraInfo());
        }

        internal override CalendarId ID
        {
            get
            {
                return CalendarId.JAPAN;
            }
        }


        public override DateTime AddMonths(DateTime time, int months)
        {
            return (helper.AddMonths(time, months));
        }


        public override DateTime AddYears(DateTime time, int years)
        {
            return (helper.AddYears(time, years));
        }

        /*=================================GetDaysInMonth==========================
        **Action: Returns the number of days in the month given by the year and month arguments.
        **Returns: The number of days in the given month.
        **Arguments:
        **      year The year in Japanese calendar.
        **      month The month
        **      era     The Japanese era value.
        **Exceptions
        **  ArgumentException  If month is less than 1 or greater * than 12.
        ============================================================================*/


        public override int GetDaysInMonth(int year, int month, int era)
        {
            return (helper.GetDaysInMonth(year, month, era));
        }


        public override int GetDaysInYear(int year, int era)
        {
            return (helper.GetDaysInYear(year, era));
        }


        public override int GetDayOfMonth(DateTime time)
        {
            return (helper.GetDayOfMonth(time));
        }


        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return (helper.GetDayOfWeek(time));
        }


        public override int GetDayOfYear(DateTime time)
        {
            return (helper.GetDayOfYear(time));
        }


        public override int GetMonthsInYear(int year, int era)
        {
            return (helper.GetMonthsInYear(year, era));
        }


        public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            return (helper.GetWeekOfYear(time, rule, firstDayOfWeek));
        }

        /*=================================GetEra==========================
        **Action: Get the era value of the specified time.
        **Returns: The era value for the specified time.
        **Arguments:
        **      time the specified date time.
        **Exceptions: ArgumentOutOfRangeException if time is out of the valid era ranges.
        ============================================================================*/


        public override int GetEra(DateTime time)
        {
            return (helper.GetEra(time));
        }


        public override int GetMonth(DateTime time)
        {
            return (helper.GetMonth(time));
        }


        public override int GetYear(DateTime time)
        {
            return (helper.GetYear(time));
        }


        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            return (helper.IsLeapDay(year, month, day, era));
        }


        public override bool IsLeapYear(int year, int era)
        {
            return (helper.IsLeapYear(year, era));
        }

        // Returns  the leap month in a calendar year of the specified era. This method returns 0
        // if this calendar does not have leap month, or this year is not a leap year.
        //

        public override int GetLeapMonth(int year, int era)
        {
            return (helper.GetLeapMonth(year, era));
        }


        public override bool IsLeapMonth(int year, int month, int era)
        {
            return (helper.IsLeapMonth(year, month, era));
        }


        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            return (helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era));
        }

        // For Japanese calendar, four digit year is not used.  Few emperors will live for more than one hundred years.
        // Therefore, for any two digit number, we just return the original number.

        public override int ToFourDigitYear(int year)
        {
            if (year <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year),
                    SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (year > helper.MaxYear)
            {
                throw new ArgumentOutOfRangeException(
                            nameof(year),
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Range,
                                1,
                                helper.MaxYear));
            }
            return (year);
        }


        public override int[] Eras
        {
            get
            {
                return (helper.Eras);
            }
        }

        //
        // Return the various era strings
        // Note: The arrays are backwards of the eras
        //
        internal static String[] EraNames()
        {
            EraInfo[] eras = GetEraInfo();
            String[] eraNames = new String[eras.Length];

            for (int i = 0; i < eras.Length; i++)
            {
                // Strings are in chronological order, eras are backwards order.
                eraNames[i] = eras[eras.Length - i - 1].eraName;
            }

            return eraNames;
        }

        internal static String[] AbbrevEraNames()
        {
            EraInfo[] eras = GetEraInfo();
            String[] erasAbbrev = new String[eras.Length];

            for (int i = 0; i < eras.Length; i++)
            {
                // Strings are in chronological order, eras are backwards order.
                erasAbbrev[i] = eras[eras.Length - i - 1].abbrevEraName;
            }

            return erasAbbrev;
        }

        internal static String[] EnglishEraNames()
        {
            EraInfo[] eras = GetEraInfo();
            String[] erasEnglish = new String[eras.Length];

            for (int i = 0; i < eras.Length; i++)
            {
                // Strings are in chronological order, eras are backwards order.
                erasEnglish[i] = eras[eras.Length - i - 1].englishEraName;
            }

            return erasEnglish;
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 99;

        internal override bool IsValidYear(int year, int era)
        {
            return helper.IsValidYear(year, era);
        }

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
                if (value < 99 || value > helper.MaxYear)
                {
                    throw new ArgumentOutOfRangeException(
                                "year",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    99,
                                    helper.MaxYear));
                }
                twoDigitYearMax = value;
            }
        }
    }
}
