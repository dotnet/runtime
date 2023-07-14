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
    // CalendarData_EraNames = 13, // ?? DateTimeFormatInfo.GetEraName  ?? date.toLocaleDateString("pl-PL", { era: "long"})
    // CalendarData_AbbrevEraNames = 14, // ?? DateTimeFormatInfo.GetAbbreviatedEraName ?? date.toLocaleDateString("pl-PL", { era: "short"})
//==================================================================
    // CalendarData_Uninitialized = 0,
    // CalendarData_NativeName = 1, // is it still present? there are some chances ICU has it; public: NativeCalendarName
    // usage: https://github.com/search?q=.NativeCalendarName+language%3AC%23+AND+NOT+%28path%3ASystem.Private.CoreLib%29&type=code
    // CalendarData_MonthDay = 2, // event.toLocaleDateString("pl-PL", {month: "long", day: "numeric"}); public: MonthDayPattern
    // CalendarData_ShortDates = 3, // event.toLocaleDateString("bg-BG", {dateStyle: "short"}); public: ShortDatePattern
    // CalendarData_LongDates = 4, // FULL = THIS + GetLocaleTimeFormat(shortFormat: false)
    // CalendarData_YearMonths = 5,
    // CalendarData_DayNames = 6, // event.toLocaleDateString("pl-PL", { weekday: "long" }); public: DayNames
    // CalendarData_AbbrevDayNames = 7, // event.toLocaleDateString("pl-PL", { weekday: "short" }), public: AbbreviatedDayNames
    // CalendarData_MonthNames = 8, // event.toLocaleDateString("pl-PL", { month: "long" }); public: MonthNames
    // CalendarData_AbbrevMonthNames = 9, // event.toLocaleDateString("pl-PL", { month: "short" }); public: AbbreviatedMonthNames
    // CalendarData_SuperShortDayNames = 10, // event.toLocaleDateString("pl-PL", { weekday: "narrow" }); public: ShortestDayNames
    // CalendarData_MonthGenitiveNames = 11, // data.toLocaleDateString("pl-PL", {dateStyle: "long"}) with the month day pattern
    // CalendarData_AbbrevMonthGenitiveNames = 12, // data.toLocaleDateString("pl-PL", {dateStyle: "medium"}) with the month day pattern
    // CalendarData_EraNames = 13, // ?? DateTimeFormatInfo.GetEraName  ?? date.toLocaleDateString("pl-PL", { era: "long"})
    // CalendarData_AbbrevEraNames = 14, // ?? DateTimeFormatInfo.GetAbbreviatedEraName ?? date.toLocaleDateString("pl-PL", { era: "short"})
        private const int CALENDAR_INFO_BUFFER_LEN = 1000;
        private unsafe bool JSLoadCalendarDataFromBrowser(string localeName, CalendarId calendarId)
        {
            char* buffer = stackalloc char[CALENDAR_INFO_BUFFER_LEN];
            int exception;
            object exResult;
            int resultLength = Interop.JsGlobalization.GetCalendarInfo(localeName, calendarId, buffer, CALENDAR_INFO_BUFFER_LEN, out exception, out exResult);
            if (exception != 0)
                throw new Exception((string)exResult);
            string result = new string(buffer, 0, resultLength);
            string[] subresults = result.Split("##");
            if (subresults.Length < 2)
                throw new Exception("CalendarInfo recieved from the Browser is in icorrect format.");
            // JS always has one result per locale, so even arrays are initialized with one element
            this.saYearMonths = new string[] { subresults[0] };
            this.sMonthDay = subresults[1];
            this.saLongDates = new string[] { subresults[2] };
            this.saShortDates = new string[] { subresults[3] };
            this.saEraNames = new string[] { subresults[4] };
            this.saAbbrevEraNames = new string[] { subresults[5] };
            this.saDayNames = subresults[6].Split("||");
            this.saAbbrevDayNames = subresults[7].Split("||");
            this.saSuperShortDayNames = subresults[8].Split("||");
            this.saMonthNames = ResizeMonthsArray(subresults[9].Split("||"));
            this.saAbbrevMonthNames = ResizeMonthsArray(subresults[10].Split("||"));
            this.saMonthGenitiveNames = ResizeMonthsArray(subresults[11].Split("||"));
            this.saAbbrevMonthGenitiveNames = ResizeMonthsArray(subresults[12].Split("||"));
            return true;

            static string[] ResizeMonthsArray(string[] months)
            {
                if (months.Length == 13)
                    return months;
                // most calendars have 12 months and then we expect the 13th month to be empty
                string[] resized = new string[13];
                resized[12] = "";
                months.CopyTo(resized, 0);
                return resized;
            }
        }
    }
}
