// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    internal sealed partial class CalendarData
    {
        private const uint CAL_ICALINTVALUE = 0x00000001;
        private const uint CAL_RETURN_GENITIVE_NAMES = 0x10000000;
        private const uint CAL_NOUSEROVERRIDE = 0x80000000;
        private const uint CAL_SMONTHDAY = 0x00000038;
        private const uint CAL_SSHORTDATE = 0x00000005;
        private const uint CAL_SLONGDATE = 0x00000006;
        private const uint CAL_SYEARMONTH = 0x0000002f;
        private const uint CAL_SDAYNAME7 = 0x0000000d;
        private const uint CAL_SABBREVDAYNAME7 = 0x00000014;
        private const uint CAL_SMONTHNAME1 = 0x00000015;
        private const uint CAL_SABBREVMONTHNAME1 = 0x00000022;
        private const uint CAL_SSHORTESTDAYNAME7 = 0x00000037;
        private const uint CAL_SERASTRING = 0x00000004;
        private const uint CAL_SABBREVERASTRING = 0x00000039;

        private const uint ENUM_ALL_CALENDARS = 0xffffffff;

        private const uint LOCALE_ICALENDARTYPE = 0x00001009;
        private const uint LOCALE_SSHORTDATE = 0x0000001F;
        private const uint LOCALE_SLONGDATE = 0x00000020;
        private const uint LOCALE_SYEARMONTH = 0x00001006;

        private bool LoadCalendarDataFromSystemCore(string localeName, CalendarId calendarId)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            if (GlobalizationMode.UseNls)
            {
                return NlsLoadCalendarDataFromSystem(localeName, calendarId);
            }

            // If running using ICU on Windows we should honor user overrides using NLS and the rest from ICU
            bool result = IcuLoadCalendarDataFromSystem(localeName, calendarId);

            if (result && bUseUserOverrides)
            {
                NormalizeCalendarId(ref calendarId, ref localeName);
                result &= CallGetCalendarInfoEx(localeName, calendarId, CAL_ITWODIGITYEARMAX, out this.iTwoDigitYearMax);

                // They want user overrides, see if the user calendar matches the input calendar
                CalendarId userCalendar = (CalendarId)CultureData.GetLocaleInfoExInt(localeName, LOCALE_ICALENDARTYPE);

                // If the calendars were the same, see if the locales were the same
                if (userCalendar == calendarId)
                {
                    string? shortDateOverride = CultureData.ReescapeWin32String(CultureData.GetLocaleInfoEx(localeName, LOCALE_SSHORTDATE));
                    string? longDateOverride = CultureData.ReescapeWin32String(CultureData.GetLocaleInfoEx(localeName, LOCALE_SLONGDATE));
                    InsertOrSwapOverride(shortDateOverride, ref this.saShortDates);
                    InsertOrSwapOverride(longDateOverride, ref this.saLongDates);
                }
            }

            return result;
        }

        private void InsertOrSwapOverride(string? value, ref string[] destination)
        {
            if (value == null)
                return;

            for (int i = 0; i < destination.Length; i++)
            {
                if (destination[i] == value)
                {
                    if (i > 0)
                    {
                        string tmp = destination[0];
                        destination[0] = value;
                        destination[i] = tmp;
                    }

                    return;
                }
            }

            string[] newArray = new string[destination.Length + 1];
            newArray[0] = value;
            Array.Copy(destination, 0, newArray, 1, destination.Length);
            destination = newArray;
        }

        private bool NlsLoadCalendarDataFromSystem(string localeName, CalendarId calendarId)
        {
            bool ret = true;

            uint useOverrides = bUseUserOverrides ? 0 : CAL_NOUSEROVERRIDE;

            NormalizeCalendarId(ref calendarId, ref localeName);

            // Numbers
            ret &= CallGetCalendarInfoEx(localeName, calendarId, CAL_ITWODIGITYEARMAX | useOverrides, out this.iTwoDigitYearMax);

            // Strings
            ret &= CallGetCalendarInfoEx(localeName, calendarId, CAL_SCALNAME, out this.sNativeName);
            ret &= CallGetCalendarInfoEx(localeName, calendarId, CAL_SMONTHDAY, out this.sMonthDay);

            // String Arrays
            // Formats
            ret &= CallEnumCalendarInfo(localeName, calendarId, CAL_SSHORTDATE, LOCALE_SSHORTDATE | useOverrides, out this.saShortDates!);
            ret &= CallEnumCalendarInfo(localeName, calendarId, CAL_SLONGDATE, LOCALE_SLONGDATE | useOverrides, out this.saLongDates!);

            // Get the YearMonth pattern.
            ret &= CallEnumCalendarInfo(localeName, calendarId, CAL_SYEARMONTH, LOCALE_SYEARMONTH, out this.saYearMonths!);

            // Day & Month Names
            // These are all single calType entries, 1 per day, so we have to make 7 or 13 calls to collect all the names

            // Day
            // Note that we're off-by-one since managed starts on sunday and windows starts on monday
            ret &= GetCalendarDayInfo(localeName, calendarId, CAL_SDAYNAME7, out this.saDayNames);
            ret &= GetCalendarDayInfo(localeName, calendarId, CAL_SABBREVDAYNAME7, out this.saAbbrevDayNames);

            // Month names
            ret &= GetCalendarMonthInfo(localeName, calendarId, CAL_SMONTHNAME1, out this.saMonthNames);
            ret &= GetCalendarMonthInfo(localeName, calendarId, CAL_SABBREVMONTHNAME1, out this.saAbbrevMonthNames);

            //
            // The following LCTYPE are not supported in some platforms.  If the call fails,
            // don't return a failure.
            //
            GetCalendarDayInfo(localeName, calendarId, CAL_SSHORTESTDAYNAME7, out this.saSuperShortDayNames);

            // Gregorian may have genitive month names
            if (calendarId == CalendarId.GREGORIAN)
            {
                GetCalendarMonthInfo(localeName, calendarId, CAL_SMONTHNAME1 | CAL_RETURN_GENITIVE_NAMES, out this.saMonthGenitiveNames);
                GetCalendarMonthInfo(localeName, calendarId, CAL_SABBREVMONTHNAME1 | CAL_RETURN_GENITIVE_NAMES, out this.saAbbrevMonthGenitiveNames);
            }

            // Calendar Parts Names
            // This doesn't get always get localized names for gregorian (not available in windows < 7)
            // so: eg: coreclr on win < 7 won't get these
            CallEnumCalendarInfo(localeName, calendarId, CAL_SERASTRING, 0, out this.saEraNames!);
            CallEnumCalendarInfo(localeName, calendarId, CAL_SABBREVERASTRING, 0, out this.saAbbrevEraNames!);

            //
            // Calendar Era Info
            // Note that calendar era data (offsets, etc) is hard coded for each calendar since this
            // data is implementation specific and not dynamic (except perhaps Japanese)
            //

            // Clean up the escaping of the formats
            this.saShortDates = CultureData.ReescapeWin32Strings(this.saShortDates)!;
            this.saLongDates = CultureData.ReescapeWin32Strings(this.saLongDates)!;
            this.saYearMonths = CultureData.ReescapeWin32Strings(this.saYearMonths)!;
            this.sMonthDay = CultureData.ReescapeWin32String(this.sMonthDay)!;

            return ret;
        }

        private static void NormalizeCalendarId(ref CalendarId calendarId, ref string localeName)
        {
            //
            // Windows doesn't support some calendars right now, so remap those.
            //
            switch (calendarId)
            {
                case CalendarId.JAPANESELUNISOLAR:    // Data looks like Japanese
                    calendarId = CalendarId.JAPAN;
                    break;
                case CalendarId.JULIAN:               // Data looks like gregorian US
                case CalendarId.CHINESELUNISOLAR:     // Algorithmic, so actual data is irrelevent
                case CalendarId.SAKA:                 // reserved to match Office but not implemented in our code, so data is irrelevent
                case CalendarId.LUNAR_ETO_CHN:        // reserved to match Office but not implemented in our code, so data is irrelevent
                case CalendarId.LUNAR_ETO_KOR:        // reserved to match Office but not implemented in our code, so data is irrelevent
                case CalendarId.LUNAR_ETO_ROKUYOU:    // reserved to match Office but not implemented in our code, so data is irrelevent
                case CalendarId.KOREANLUNISOLAR:      // Algorithmic, so actual data is irrelevent
                case CalendarId.TAIWANLUNISOLAR:      // Algorithmic, so actual data is irrelevent
                    calendarId = CalendarId.GREGORIAN_US;
                    break;
            }

            //
            // Special handling for some special calendar due to OS limitation.
            // This includes calendar like Taiwan calendar, UmAlQura calendar, etc.
            //
            CheckSpecialCalendar(ref calendarId, ref localeName);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // For calendars like Gregorain US/Taiwan/UmAlQura, they are not available
        // in all OS or all localized versions of OS.
        // If OS does not support these calendars, we will fallback by using the
        // appropriate fallback calendar and locale combination to retrieve data from OS.
        //
        // Parameters:
        //  __deref_inout pCalendarInt:
        //    Pointer to the calendar ID. This will be updated to new fallback calendar ID if needed.
        //  __in_out pLocaleNameStackBuffer
        //    Pointer to the StackSString object which holds the locale name to be checked.
        //    This will be updated to new fallback locale name if needed.
        //
        ////////////////////////////////////////////////////////////////////////
        private static void CheckSpecialCalendar(ref CalendarId calendar, ref string localeName)
        {
            // Gregorian-US isn't always available in the OS, however it is the same for all locales
            switch (calendar)
            {
                case CalendarId.GREGORIAN_US:
                    // See if this works
                    if (!CallGetCalendarInfoEx(localeName, calendar, CAL_SCALNAME, out string _))
                    {
                        // Failed, set it to a locale (fa-IR) that's alway has Gregorian US available in the OS
                        localeName = "fa-IR";

                        // See if that works
                        if (!CallGetCalendarInfoEx(localeName, calendar, CAL_SCALNAME, out string _))
                        {
                            // Failed again, just use en-US with the gregorian calendar
                            localeName = "en-US";
                            calendar = CalendarId.GREGORIAN;
                        }
                    }
                    break;
                case CalendarId.TAIWAN:
                    // Taiwan calendar data is not always in all language version of OS due to Geopolical reasons.
                    // It is only available in zh-TW localized versions of Windows.
                    // Let's check if OS supports it.  If not, fallback to Greogrian localized for Taiwan calendar.
                    if (!NlsSystemSupportsTaiwaneseCalendar())
                    {
                        calendar = CalendarId.GREGORIAN;
                    }
                    break;
            }
        }

        private static unsafe bool CallEnumCalendarInfo(string localeName, CalendarId calendar, uint calType, uint lcType, out string[]? data)
        {
            EnumData context = default;
            context.userOverride = null;
            context.strings = new List<string>();
            // First call GetLocaleInfo if necessary
            if ((lcType != 0) && ((lcType & CAL_NOUSEROVERRIDE) == 0))
            {
                // They want user overrides, see if the user calendar matches the input calendar
                CalendarId userCalendar = (CalendarId)CultureData.GetLocaleInfoExInt(localeName, LOCALE_ICALENDARTYPE);

                // If the calendars were the same, see if the locales were the same
                if (userCalendar == calendar)
                {
                    // They matched, get the user override since locale & calendar match
                    string? res = CultureData.GetLocaleInfoEx(localeName, lcType);

                    // if it succeeded remember the override for the later callers
                    if (res != null)
                    {
                        // Remember this was the override (so we can look for duplicates later in the enum function)
                        context.userOverride = res;

                        // Add to the result strings.
                        context.strings.Add(res);
                    }
                }
            }

            // Now call the enumeration API. Work is done by our callback function
            Interop.Kernel32.EnumCalendarInfoExEx(&EnumCalendarInfoCallback, localeName, (uint)calendar, null, calType, Unsafe.AsPointer(ref context));

            // Now we have a list of data, fail if we didn't find anything.
            Debug.Assert(context.strings != null);
            if (context.strings.Count == 0)
            {
                data = null;
                return false;
            }

            string[] output = context.strings.ToArray();

            if (calType == CAL_SABBREVERASTRING || calType == CAL_SERASTRING)
            {
                // Eras are enumerated backwards.  (oldest era name first, but
                // Japanese calendar has newest era first in array, and is only
                // calendar with multiple eras)
                Array.Reverse(output, 0, output.Length);
            }

            data = output;

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // Get the native day names
        //
        // NOTE: There's a disparity between .NET & windows day orders, the input day should
        //           start with Sunday
        //
        // Parameters:
        //      OUT pOutputStrings      The output string[] value.
        //
        ////////////////////////////////////////////////////////////////////////
        private static bool GetCalendarDayInfo(string localeName, CalendarId calendar, uint calType, out string[] outputStrings)
        {
            bool result = true;

            //
            // We'll need a new array of 7 items
            //
            string[] results = new string[7];

            // Get each one of them
            for (int i = 0; i < 7; i++, calType++)
            {
                result &= CallGetCalendarInfoEx(localeName, calendar, calType, out results[i]);

                // On the first iteration we need to go from CAL_SDAYNAME7 to CAL_SDAYNAME1, so subtract 7 before the ++ happens
                // This is because the framework starts on sunday and windows starts on monday when counting days
                if (i == 0)
                    calType -= 7;
            }

            outputStrings = results;

            return result;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // Get the native month names
        //
        // Parameters:
        //      OUT pOutputStrings      The output string[] value.
        //
        ////////////////////////////////////////////////////////////////////////
        private static bool GetCalendarMonthInfo(string localeName, CalendarId calendar, uint calType, out string[] outputStrings)
        {
            //
            // We'll need a new array of 13 items
            //
            string[] results = new string[13];

            // Get each one of them
            for (int i = 0; i < 13; i++, calType++)
            {
                if (!CallGetCalendarInfoEx(localeName, calendar, calType, out results[i]))
                    results[i] = "";
            }

            outputStrings = results;

            return true;
        }

        internal static int GetCalendarsCore(string localeName, bool useUserOverride, CalendarId[] calendars)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            if (GlobalizationMode.UseNls)
            {
                return NlsGetCalendars(localeName, useUserOverride, calendars);
            }

            int count = IcuGetCalendars(localeName, calendars);

            if (useUserOverride)
            {
                // They want user overrides, see if the user calendar matches the input calendar
                int userCalendar = CultureData.GetLocaleInfoExInt(localeName, LOCALE_ICALENDARTYPE);

                if (userCalendar != 0 && (CalendarId)userCalendar != calendars[0])
                {
                    CalendarId userOverride = (CalendarId)userCalendar;
                    for (int i = 1; i < calendars.Length; i++)
                    {
                        if (calendars[i] == userOverride)
                        {
                            CalendarId tmp = calendars[0];
                            calendars[0] = userOverride;
                            calendars[i] = tmp;
                            return count;
                        }
                    }

                    // We didn't find it, we insert it at the beginning of the array. If calendar's array is full, we drop the last element.
                    count = count < calendars.Length ? count + 1 : count;
                    Span<CalendarId> tmpSpan = stackalloc CalendarId[count]; // should be 23 max.
                    tmpSpan[0] = userOverride;
                    calendars.AsSpan(0, count - 1).CopyTo(tmpSpan.Slice(1));
                    tmpSpan.CopyTo(calendars);
                }
            }

            return count;
        }

        private static int NlsGetCalendars(string localeName, bool useUserOverride, CalendarId[] calendars)
        {
            NlsEnumCalendarsData data = default;
            data.userOverride = 0;
            data.calendars = new List<int>();

            // First call GetLocaleInfo if necessary
            if (useUserOverride)
            {
                // They want user overrides, see if the user calendar matches the input calendar
                int userCalendar = CultureData.GetLocaleInfoExInt(localeName, LOCALE_ICALENDARTYPE);

                // If we got a default, then use it as the first calendar
                if (userCalendar != 0)
                {
                    data.userOverride = userCalendar;
                    data.calendars.Add(userCalendar);
                }
            }

            unsafe
            {
                Interop.Kernel32.EnumCalendarInfoExEx(&EnumCalendarsCallback, localeName, ENUM_ALL_CALENDARS, null, CAL_ICALINTVALUE, Unsafe.AsPointer(ref data));
            }

            // Copy to the output array
            for (int i = 0; i < Math.Min(calendars.Length, data.calendars.Count); i++)
                calendars[i] = (CalendarId)data.calendars[i];

            // Now we have a list of data, return the count
            return data.calendars.Count;
        }
    }
}
