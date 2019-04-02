// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Globalization
{
    /// <summary>
    /// Korean calendar is based on the Gregorian calendar.  And the year is an offset to Gregorian calendar.
    /// That is,
    ///      Korean year = Gregorian year + 2333.  So 2000/01/01 A.D. is Korean 4333/01/01
    ///
    /// 0001/1/1 A.D. is Korean year 2334.
    /// </summary>
    /// <remarks>
    /// Calendar support range:
    ///     Calendar    Minimum     Maximum
    ///     ==========  ==========  ==========
    ///     Gregorian   0001/01/01   9999/12/31
    ///     Korean      2334/01/01  12332/12/31
    /// </remarks>
    public class KoreanCalendar : Calendar
    {
        public const int KoreanEra = 1;

        // Since
        //    Gregorian Year = Era Year + yearOffset
        // Gregorian Year 1 is Korean year 2334, so that
        //    1 = 2334 + yearOffset
        //  So yearOffset = -2333
        // Gregorian year 2001 is Korean year 4334.
        private static readonly EraInfo[] s_koreanEraInfo = new EraInfo[]
        {
            new EraInfo( 1, 1, 1, 1, -2333, 2334, GregorianCalendar.MaxYear + 2333)   // era #, start year/month/day, yearOffset, minEraYear
        };

        private readonly GregorianCalendarHelper _helper;

        public override DateTime MinSupportedDateTime => DateTime.MinValue;

        public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        public KoreanCalendar()
        {
            try
            {
                new CultureInfo("ko-KR");
            }
            catch (ArgumentException e)
            {
                throw new TypeInitializationException(GetType().ToString(), e);
            }

            _helper = new GregorianCalendarHelper(this, s_koreanEraInfo);
        }

        internal override CalendarId ID => CalendarId.KOREA;


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

        private const int DefaultTwoDigitYearMax = 4362;


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

        public override int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            return _helper.ToFourDigitYear(year, TwoDigitYearMax);
        }
    }
}
