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

            sNativeName = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.NativeName);
            sMonthDay = GetCalendarInfoNative(localeName, calendarId, CalendarDataType.MonthDay);
            EnumDatePatterns(localeName, calendarId, CalendarDataType.ShortDates, out this.saShortDates!);
            EnumDatePatterns(localeName, calendarId, CalendarDataType.LongDates, out this.saLongDates!);
            EnumDatePatterns(localeName, calendarId, CalendarDataType.YearMonths, out this.saYearMonths!);
            EnumCalendarInfo(localeName, calendarId, CalendarDataType.DayNames, out this.saDayNames!);
            EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevDayNames, out this.saAbbrevDayNames!);
            EnumCalendarInfo(localeName, calendarId, CalendarDataType.SuperShortDayNames, out this.saSuperShortDayNames!);

            string? leapHebrewMonthName = null;
            EnumMonthNames(localeName, calendarId, CalendarDataType.MonthNames, out this.saMonthNames!, ref leapHebrewMonthName);
            if (leapHebrewMonthName != null)
            {
                Debug.Assert(saMonthNames != null);

                // In Hebrew calendar, get the leap month name Adar II and override the non-leap month 7
                Debug.Assert(calendarId == CalendarId.HEBREW && saMonthNames.Length == 13);
                saLeapYearMonthNames = (string[])saMonthNames.Clone();
                saLeapYearMonthNames[6] = leapHebrewMonthName;

                // The returned data has 6th month name as 'Adar I' and 7th month name as 'Adar'
                // We need to adjust that in the list used with non-leap year to have 6th month as 'Adar' and 7th month as 'Adar II'
                // note that when formatting non-leap year dates, 7th month shouldn't get used at all.
                saMonthNames[5] = saMonthNames[6];
                saMonthNames[6] = leapHebrewMonthName;

            }
            EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthNames, out this.saAbbrevMonthNames!, ref leapHebrewMonthName);
            EnumMonthNames(localeName, calendarId, CalendarDataType.MonthGenitiveNames, out this.saMonthGenitiveNames!, ref leapHebrewMonthName);
            EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthGenitiveNames, out this.saAbbrevMonthGenitiveNames!, ref leapHebrewMonthName);

            EnumEraNames(localeName, calendarId, CalendarDataType.EraNames, out this.saEraNames!);
            EnumEraNames(localeName, calendarId, CalendarDataType.AbbrevEraNames, out this.saAbbrevEraNames!);

            return sNativeName != null && saShortDates != null && saLongDates != null && saYearMonths != null &&
                   saDayNames != null && saAbbrevDayNames != null && saSuperShortDayNames != null && saMonthNames != null &&
                   saAbbrevMonthNames != null && saMonthGenitiveNames != null && saAbbrevMonthGenitiveNames != null &&
                   saEraNames != null && saAbbrevEraNames != null;
        }

        private static string GetCalendarInfoNative(string localeName, CalendarId calendarId, CalendarDataType calendarDataType)
        {
            Debug.Assert(localeName != null);

            return Interop.Globalization.GetCalendarInfoNative(localeName, calendarId, calendarDataType);
        }
    }
}
