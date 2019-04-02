// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Globalization
{
    /// <summary>
    /// JapaneseCalendar is based on Gregorian calendar.  The month and day values are the same as
    /// Gregorian calendar.  However, the year value is an offset to the Gregorian
    /// year based on the era.
    ///
    /// This system is adopted by Emperor Meiji in 1868. The year value is counted based on the reign of an emperor,
    /// and the era begins on the day an emperor ascends the throne and continues until his death.
    /// The era changes at 12:00AM.
    ///
    /// For example, the current era is Reiwa.  It started on 2019/5/1 A.D.  Therefore, Gregorian year 2019 is also Reiwa 1st.
    /// 2019/5/1 A.D. is also Reiwa 1st 5/1.
    ///
    /// Any date in the year during which era is changed can be reckoned in either era.  For example,
    /// 2019/1/1 can be 1/1 Reiwa 1st year or 1/1 Heisei 31st year.
    ///
    /// Note:
    ///  The DateTime can be represented by the JapaneseCalendar are limited to two factors:
    ///      1. The min value and max value of DateTime class.
    ///      2. The available era information.
    /// </summary>
    /// <remarks>
    /// Calendar support range:
    ///     Calendar    Minimum     Maximum
    ///     ==========  ==========  ==========
    ///     Gregorian   1868/09/08  9999/12/31
    ///     Japanese    Meiji 01/01 Reiwa 7981/12/31
    /// </remarks>
    public partial class JapaneseCalendar : Calendar
    {
        private static readonly DateTime s_calendarMinValue = new DateTime(1868, 9, 8);

        public override DateTime MinSupportedDateTime => s_calendarMinValue;

        public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        // Using a field initializer rather than a static constructor so that the whole class can be lazy
        // init.
        private static volatile EraInfo[]? s_japaneseEraInfo;

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
        internal static EraInfo[] GetEraInfo()
        {
            // See if we need to build it
            return s_japaneseEraInfo ??
                (s_japaneseEraInfo = GetJapaneseEras()) ??
                // See if we have to use the built-in eras
                (s_japaneseEraInfo = new EraInfo[]
                {
                    new EraInfo(5, 2019, 5, 1, 2018, 1, GregorianCalendar.MaxYear - 2018, "\x4ee4\x548c", "\x4ee4", "R"),
                    new EraInfo(4, 1989, 1, 8, 1988, 1, 2019 - 1988, "\x5e73\x6210", "\x5e73", "H"),
                    new EraInfo(3, 1926, 12, 25, 1925, 1, 1989 - 1925, "\x662d\x548c", "\x662d", "S"),
                    new EraInfo(2, 1912, 7, 30, 1911, 1, 1926 - 1911, "\x5927\x6b63", "\x5927", "T"),
                    new EraInfo(1, 1868, 1, 1, 1867, 1, 1912 - 1867, "\x660e\x6cbb", "\x660e", "M")
                });
        }

        internal static volatile Calendar s_defaultInstance;
        internal GregorianCalendarHelper _helper;

        internal static Calendar GetDefaultInstance()
        {
            return s_defaultInstance ?? (s_defaultInstance = new JapaneseCalendar());
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

            _helper = new GregorianCalendarHelper(this, GetEraInfo());
        }

        internal override CalendarId ID => CalendarId.JAPAN;

        public override DateTime AddMonths(DateTime time, int months)
        {
            return _helper.AddMonths(time, months);
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            return _helper.AddYears(time, years);
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            return _helper.GetDaysInMonth(year, month, era);
        }

        public override int GetDaysInYear(int year, int era)
        {
            return _helper.GetDaysInYear(year, era);
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return _helper.GetDayOfMonth(time);
        }

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return _helper.GetDayOfWeek(time);
        }

        public override int GetDayOfYear(DateTime time)
        {
            return _helper.GetDayOfYear(time);
        }

        public override int GetMonthsInYear(int year, int era)
        {
            return _helper.GetMonthsInYear(year, era);
        }

        public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            return _helper.GetWeekOfYear(time, rule, firstDayOfWeek);
        }

        public override int GetEra(DateTime time)
        {
            return _helper.GetEra(time);
        }

        public override int GetMonth(DateTime time)
        {
            return _helper.GetMonth(time);
        }

        public override int GetYear(DateTime time)
        {
            return _helper.GetYear(time);
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            return _helper.IsLeapDay(year, month, day, era);
        }

        public override bool IsLeapYear(int year, int era)
        {
            return _helper.IsLeapYear(year, era);
        }

        public override int GetLeapMonth(int year, int era)
        {
            return _helper.GetLeapMonth(year, era);
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            return _helper.IsLeapMonth(year, month, era);
        }


        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            return _helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
        }

        /// <summary>
        /// For Japanese calendar, four digit year is not used. Few emperors will live for more than one hundred years.
        /// Therefore, for any two digit number, we just return the original number.
        /// </summary>
        public override int ToFourDigitYear(int year)
        {
            if (year <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (year > _helper.MaxYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, _helper.MaxYear));
            }

            return year;
        }


        public override int[] Eras => _helper.Eras;

        /// <summary>
        /// Return the various era strings
        /// Note: The arrays are backwards of the eras
        /// </summary>
        internal static string[] EraNames()
        {
            EraInfo[] eras = GetEraInfo();
            string[] eraNames = new string[eras.Length];

            for (int i = 0; i < eras.Length; i++)
            {
                // Strings are in chronological order, eras are backwards order.
                eraNames[i] = eras[eras.Length - i - 1].eraName!;
            }

            return eraNames;
        }

        internal static string[] AbbrevEraNames()
        {
            EraInfo[] eras = GetEraInfo();
            string[] erasAbbrev = new string[eras.Length];

            for (int i = 0; i < eras.Length; i++)
            {
                // Strings are in chronological order, eras are backwards order.
                erasAbbrev[i] = eras[eras.Length - i - 1].abbrevEraName!;
            }

            return erasAbbrev;
        }

        internal static string[] EnglishEraNames()
        {
            EraInfo[] eras = GetEraInfo();
            string[] erasEnglish = new string[eras.Length];

            for (int i = 0; i < eras.Length; i++)
            {
                // Strings are in chronological order, eras are backwards order.
                erasEnglish[i] = eras[eras.Length - i - 1].englishEraName!;
            }

            return erasEnglish;
        }

        private const int DefaultTwoDigitYearMax = 99;

        internal override bool IsValidYear(int year, int era)
        {
            return _helper.IsValidYear(year, era);
        }

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
                if (value < 99 || value > _helper.MaxYear)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Range, 99, _helper.MaxYear));
                }

                _twoDigitYearMax = value;
            }
        }
    }
}
