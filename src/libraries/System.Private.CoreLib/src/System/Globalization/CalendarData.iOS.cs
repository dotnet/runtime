// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal sealed partial class CalendarData
    {
        private bool LoadCalendarDataFromNative(string localeName, CalendarId calendarId)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            this.sNativeName = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.NativeName);
            this.sMonthDay = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.MonthDay);
            this.saShortDates = new string [] { GetCalendarInfoNative(localeName, calendarId, CalendarDataType.ShortDates) };
            this.saLongDates = new string [] { GetCalendarInfoNative(localeName, calendarId, CalendarDataType.LongDates) };
            this.saYearMonths = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.YearMonths).Split("||");
            this.saDayNames = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.DayNames).Split("||");
            this.saAbbrevDayNames = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.AbbrevDayNames).Split("||");
            this.saSuperShortDayNames = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.SuperShortDayNames).Split("||");

            string? leapHebrewMonthName = null;
            this.saMonthNames = NormalizeMonthArray(GetCalendarInfoNative(localeName, calendarId, CalendarDataType.MonthNames).Split("||"), calendarId, ref leapHebrewMonthName);
            if (leapHebrewMonthName != null)
            {
                Debug.Assert(this.saMonthNames != null);

                // In Hebrew calendar, get the leap month name Adar II and override the non-leap month 7
                Debug.Assert(calendarId == CalendarId.HEBREW && saMonthNames.Length == 13);
                saLeapYearMonthNames = (string[]) saMonthNames.Clone();
                saLeapYearMonthNames[6] = leapHebrewMonthName;

                // The returned data has 6th month name as 'Adar I' and 7th month name as 'Adar'
                // We need to adjust that in the list used with non-leap year to have 6th month as 'Adar' and 7th month as 'Adar II'
                // note that when formatting non-leap year dates, 7th month shouldn't get used at all.
                saMonthNames[5] = saMonthNames[6];
                saMonthNames[6] = leapHebrewMonthName;

            }
            this.saAbbrevMonthNames = NormalizeMonthArray(GetCalendarInfoNative(localeName, calendarId, CalendarDataType.AbbrevMonthNames).Split("||"), calendarId, ref leapHebrewMonthName);
            this.saMonthGenitiveNames = NormalizeMonthArray(GetCalendarInfoNative(localeName, calendarId, CalendarDataType.MonthGenitiveNames).Split("||"), calendarId, ref leapHebrewMonthName);
            this.saAbbrevMonthGenitiveNames = NormalizeMonthArray(GetCalendarInfoNative(localeName, calendarId, CalendarDataType.AbbrevMonthGenitiveNames).Split("||"), calendarId, ref leapHebrewMonthName);

            this.saEraNames = NormalizeEraNames(calendarId, GetCalendarInfoNative(localeName, calendarId, CalendarDataType.EraNames).Split("||"));
            this.saAbbrevEraNames = Array.Empty<string>();//NormalizeEraNames(calendarId, GetCalendarInfoNative(localeName, calendarId, CalendarDataType.AbbrevEraNames).Split("||"));

            return this.sNativeName != null && this.saShortDates != null && this.saLongDates != null && this.saYearMonths != null &&
                   this.saDayNames != null && this.saAbbrevDayNames != null && this.saSuperShortDayNames != null && this.saMonthNames != null &&
                   this.saAbbrevMonthNames != null && this.saMonthGenitiveNames != null && this.saAbbrevMonthGenitiveNames != null &&
                   this.saEraNames != null && this.saAbbrevEraNames != null;
        }

        private static string[] NormalizeEraNames(CalendarId calendarId, string[]? eraNames)
        {
            // .NET expects that only the Japanese calendars have more than 1 era.
            // So for other calendars, only return the latest era.
            if (calendarId != CalendarId.JAPAN && calendarId != CalendarId.JAPANESELUNISOLAR && eraNames?.Length > 0)
            {
                string[] latestEraName = new string[] { eraNames![eraNames.Length - 1] };
                eraNames = latestEraName;
            }

            return eraNames ?? Array.Empty<string>();
        }

        private static string[] NormalizeMonthArray(string[] months, CalendarId calendarId, ref string? leapHebrewMonthName)
        {
            if (months.Length == 13)
                return months;

            string[] normalizedMonths = new string[13];
            // the month-name arrays are expected to have 13 elements.  If only returns 12, add an
            // extra empty string to fill the array.
            if (months.Length == 12)
            {
                normalizedMonths[12] = "";
                months.CopyTo(normalizedMonths, 0);
                return normalizedMonths;
            }

            if (months.Length > 13)
            {
                Debug.Assert(calendarId == CalendarId.HEBREW && months.Length == 14);

                if (calendarId == CalendarId.HEBREW)
                {
                    leapHebrewMonthName = months[13];
                }
                for (int i = 0; i < 13; i++)
                {
                    normalizedMonths[i] = months[i];
                }
                return normalizedMonths;
            }

            return normalizedMonths;
        }

        private static unsafe string GetCalendarInfoNative(string localeName, CalendarId calendarId, CalendarDataType calendarDataType)
        {
            Debug.Assert(localeName != null);

            return Interop.Globalization.GetCalendarInfoNative(localeName, calendarId, calendarDataType);
        }
    }
}
