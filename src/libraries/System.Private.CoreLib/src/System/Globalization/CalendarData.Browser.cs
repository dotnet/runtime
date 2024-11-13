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
        private const int CALENDAR_INFO_BUFFER_LEN = 1000;
        private unsafe bool JSLoadCalendarDataFromBrowser(string localeName, CalendarId calendarId)
        {
            ReadOnlySpan<char> localeNameSpan = localeName.AsSpan();
            fixed (char* pLocaleName = &MemoryMarshal.GetReference(localeNameSpan))
            {
                char* buffer = stackalloc char[CALENDAR_INFO_BUFFER_LEN];
                nint exceptionPtr = Interop.JsGlobalization.GetCalendarInfo(pLocaleName, localeNameSpan.Length, calendarId, buffer, CALENDAR_INFO_BUFFER_LEN, out int resultLength);
                Helper.MarshalAndThrowIfException(exceptionPtr);
                string result = new string(buffer, 0, resultLength);
                string[] subresults = result.Split("##");
                if (subresults.Length < 14)
                    throw new Exception("CalendarInfo recieved from the Browser is in icorrect format.");
                // JS always has one result per locale, so even arrays are initialized with one element
                this.sNativeName = string.IsNullOrEmpty(subresults[0]) ? ((CalendarId)calendarId).ToString() : subresults[0]; // this is EnglishName, not NativeName but it's the best we can do
                this.saYearMonths = new string[] { subresults[1] };
                this.sMonthDay = subresults[2];
                this.saLongDates = new string[] { subresults[3] };
                this.saShortDates = new string[] { subresults[4] };
                this.saEraNames = new string[] { subresults[5] };
                this.saAbbrevEraNames = new string[] { subresults[6] };
                this.saDayNames = subresults[7].Split("||");
                this.saAbbrevDayNames = subresults[8].Split("||");
                this.saSuperShortDayNames = subresults[9].Split("||");
                this.saMonthNames = ResizeMonthsArray(subresults[10].Split("||"));
                this.saAbbrevMonthNames = ResizeMonthsArray(subresults[11].Split("||"));
                this.saMonthGenitiveNames = ResizeMonthsArray(subresults[12].Split("||"));
                this.saAbbrevMonthGenitiveNames = ResizeMonthsArray(subresults[13].Split("||"));
                return true;
            }

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
