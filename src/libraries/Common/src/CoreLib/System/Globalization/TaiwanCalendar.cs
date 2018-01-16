// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    /*=================================TaiwanCalendar==========================
    **
    ** Taiwan calendar is based on the Gregorian calendar.  And the year is an offset to Gregorian calendar.
    ** That is,
    **      Taiwan year = Gregorian year - 1911.  So 1912/01/01 A.D. is Taiwan 1/01/01
    **
    **  Calendar support range:
    **      Calendar    Minimum     Maximum
    **      ==========  ==========  ==========
    **      Gregorian   1912/01/01  9999/12/31
    **      Taiwan      01/01/01    8088/12/31
    ============================================================================*/

    public class TaiwanCalendar : Calendar
    {
        //
        // The era value for the current era.
        //

        // Since
        //    Gregorian Year = Era Year + yearOffset
        // When Gregorian Year 1912 is year 1, so that
        //    1912 = 1 + yearOffset
        //  So yearOffset = 1911
        //m_EraInfo[0] = new EraInfo(1, new DateTime(1912, 1, 1).Ticks, 1911, 1, GregorianCalendar.MaxYear - 1911);

        // Initialize our era info.
        internal static EraInfo[] taiwanEraInfo = new EraInfo[] {
            new EraInfo( 1, 1912, 1, 1, 1911, 1, GregorianCalendar.MaxYear - 1911)    // era #, start year/month/day, yearOffset, minEraYear 
        };

        internal static volatile Calendar s_defaultInstance;

        internal GregorianCalendarHelper helper;

        /*=================================GetDefaultInstance==========================
        **Action: Internal method to provide a default intance of TaiwanCalendar.  Used by NLS+ implementation
        **       and other calendars.
        **Returns:
        **Arguments:
        **Exceptions:
        ============================================================================*/

        internal static Calendar GetDefaultInstance()
        {
            if (s_defaultInstance == null)
            {
                s_defaultInstance = new TaiwanCalendar();
            }
            return (s_defaultInstance);
        }

        internal static readonly DateTime calendarMinValue = new DateTime(1912, 1, 1);


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

        // Return the type of the Taiwan calendar.
        //

        public TaiwanCalendar()
        {
            try
            {
                new CultureInfo("zh-TW");
            }
            catch (ArgumentException e)
            {
                throw new TypeInitializationException(this.GetType().ToString(), e);
            }
            helper = new GregorianCalendarHelper(this, taiwanEraInfo);
        }

        internal override CalendarId ID
        {
            get
            {
                return CalendarId.TAIWAN;
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


        public override int[] Eras
        {
            get
            {
                return (helper.Eras);
            }
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 99;

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

        // For Taiwan calendar, four digit year is not used.
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
    }
}

