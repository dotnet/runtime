// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    /// <summary>
    /// Taiwan calendar is based on the Gregorian calendar.  And the year is an offset to Gregorian calendar.
    /// That is,
    ///      Taiwan year = Gregorian year - 1911.  So 1912/01/01 A.D. is Taiwan 1/01/01
    /// </summary>
    /// <remarks>
    ///  Calendar support range:
    ///      Calendar    Minimum     Maximum
    ///      ==========  ==========  ==========
    ///      Gregorian   1912/01/01  9999/12/31
    ///      Taiwan      01/01/01    8088/12/31
    /// </remarks>
    public class TaiwanCalendar : Calendar
    {
        // Since
        //    Gregorian Year = Era Year + yearOffset
        // When Gregorian Year 1912 is year 1, so that
        //    1912 = 1 + yearOffset
        //  So yearOffset = 1911
        private static EraInfo[] s_taiwanEraInfo = new EraInfo[]
        {
            new EraInfo( 1, 1912, 1, 1, 1911, 1, GregorianCalendar.MaxYear - 1911)    // era #, start year/month/day, yearOffset, minEraYear
        };

        private static volatile Calendar s_defaultInstance;

        private readonly GregorianCalendarHelper _helper;

        internal static Calendar GetDefaultInstance()
        {
            return s_defaultInstance ?? (s_defaultInstance = new TaiwanCalendar());
        }

        private static readonly DateTime s_calendarMinValue = new DateTime(1912, 1, 1);

        public override DateTime MinSupportedDateTime => s_calendarMinValue;

        public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        public TaiwanCalendar()
        {
            try
            {
                new CultureInfo("zh-TW");
            }
            catch (ArgumentException e)
            {
                throw new TypeInitializationException(GetType().ToString(), e);
            }

            _helper = new GregorianCalendarHelper(this, s_taiwanEraInfo);
        }

        internal override CalendarId ID => CalendarId.TAIWAN;

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

        public override int[] Eras => _helper.Eras;

        private const int DefaultTwoDigitYearMax = 99;

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

        /// <summary>
        /// For Taiwan calendar, four digit year is not used.
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
    }
}
