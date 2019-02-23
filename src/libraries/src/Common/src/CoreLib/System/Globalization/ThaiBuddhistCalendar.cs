// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    /// <summary>
    /// ThaiBuddhistCalendar is based on Gregorian calendar.
    /// Its year value has an offset to the Gregorain calendar.
    /// </summary>
    /// <remarks>
    /// Calendar support range:
    ///     Calendar    Minimum     Maximum
    ///     ==========  ==========  ==========
    ///     Gregorian   0001/01/01   9999/12/31
    ///     Thai        0544/01/01  10542/12/31
    /// </remarks>
    public class ThaiBuddhistCalendar : Calendar
    {
        private static readonly EraInfo[] s_thaiBuddhistEraInfo = new EraInfo[]
        {
            new EraInfo( 1, 1, 1, 1, -543, 544, GregorianCalendar.MaxYear + 543)     // era #, start year/month/day, yearOffset, minEraYear
        };

        public const int ThaiBuddhistEra = 1;

        private readonly GregorianCalendarHelper _helper;

        public override DateTime MinSupportedDateTime => DateTime.MinValue;

        public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

        public ThaiBuddhistCalendar()
        {
            _helper = new GregorianCalendarHelper(this, s_thaiBuddhistEraInfo);
        }

        internal override CalendarId ID => CalendarId.THAI;

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

        private const int DefaultTwoDigitYearMax = 2572;


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
