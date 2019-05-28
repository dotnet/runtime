// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystem(string localeName, CalendarId calendarId)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            bool ret = true;

            uint useOverrides = this.bUseUserOverrides ? 0 : CAL_NOUSEROVERRIDE;

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

            // Numbers
            ret &= CallGetCalendarInfoEx(localeName, calendarId, CAL_ITWODIGITYEARMAX | useOverrides, out this.iTwoDigitYearMax);

            // Strings
            ret &= CallGetCalendarInfoEx(localeName, calendarId, CAL_SCALNAME, out this.sNativeName);
            ret &= CallGetCalendarInfoEx(localeName, calendarId, CAL_SMONTHDAY | useOverrides, out this.sMonthDay);

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

        // Get native two digit year max
        internal static int GetTwoDigitYearMax(CalendarId calendarId)
        {
            if (GlobalizationMode.Invariant)
            {
                return Invariant.iTwoDigitYearMax;
            }

            int twoDigitYearMax = -1;

            if (!CallGetCalendarInfoEx(null, calendarId, (uint)CAL_ITWODIGITYEARMAX, out twoDigitYearMax))
            {
                twoDigitYearMax = -1;
            }

            return twoDigitYearMax;
        }

        // Call native side to figure out which calendars are allowed
        internal static int GetCalendars(string localeName, bool useUserOverride, CalendarId[] calendars)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            EnumCalendarsData data = new EnumCalendarsData();
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
                Interop.Kernel32.EnumCalendarInfoExEx(EnumCalendarsCallback, localeName, ENUM_ALL_CALENDARS, null, CAL_ICALINTVALUE, Unsafe.AsPointer(ref data));
            }

            // Copy to the output array
            for (int i = 0; i < Math.Min(calendars.Length, data.calendars.Count); i++)
                calendars[i] = (CalendarId)data.calendars[i];

            // Now we have a list of data, return the count
            return data.calendars.Count;
        }

        private static bool SystemSupportsTaiwaneseCalendar()
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            string data;
            // Taiwanese calendar get listed as one of the optional zh-TW calendars only when having zh-TW UI 
            return CallGetCalendarInfoEx("zh-TW", CalendarId.TAIWAN, CAL_SCALNAME, out data);
        }

        // PAL Layer ends here

        private const uint CAL_RETURN_NUMBER = 0x20000000;
        private const uint CAL_RETURN_GENITIVE_NAMES = 0x10000000;
        private const uint CAL_NOUSEROVERRIDE = 0x80000000;
        private const uint CAL_SCALNAME = 0x00000002;
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
        private const uint CAL_ICALINTVALUE = 0x00000001;
        private const uint CAL_ITWODIGITYEARMAX = 0x00000030;

        private const uint ENUM_ALL_CALENDARS = 0xffffffff;

        private const uint LOCALE_SSHORTDATE = 0x0000001F;
        private const uint LOCALE_SLONGDATE = 0x00000020;
        private const uint LOCALE_SYEARMONTH = 0x00001006;
        private const uint LOCALE_ICALENDARTYPE = 0x00001009;

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
            string data;

            // Gregorian-US isn't always available in the OS, however it is the same for all locales
            switch (calendar)
            {
                case CalendarId.GREGORIAN_US:
                    // See if this works
                    if (!CallGetCalendarInfoEx(localeName, calendar, CAL_SCALNAME, out data))
                    {
                        // Failed, set it to a locale (fa-IR) that's alway has Gregorian US available in the OS
                        localeName = "fa-IR";
                    }
                    // See if that works
                    if (!CallGetCalendarInfoEx(localeName, calendar, CAL_SCALNAME, out data))
                    {
                        // Failed again, just use en-US with the gregorian calendar
                        localeName = "en-US";
                        calendar = CalendarId.GREGORIAN;
                    }
                    break;
                case CalendarId.TAIWAN:
                    // Taiwan calendar data is not always in all language version of OS due to Geopolical reasons.
                    // It is only available in zh-TW localized versions of Windows.
                    // Let's check if OS supports it.  If not, fallback to Greogrian localized for Taiwan calendar.
                    if (!SystemSupportsTaiwaneseCalendar())
                    {
                        calendar = CalendarId.GREGORIAN;
                    }
                    break;
            }
        }

        private static bool CallGetCalendarInfoEx(string? localeName, CalendarId calendar, uint calType, out int data)
        {
            return (Interop.Kernel32.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType | CAL_RETURN_NUMBER, IntPtr.Zero, 0, out data) != 0);
        }

        private static unsafe bool CallGetCalendarInfoEx(string localeName, CalendarId calendar, uint calType, out string data)
        {
            const int BUFFER_LENGTH = 80;

            // The maximum size for values returned from GetCalendarInfoEx is 80 characters.
            char* buffer = stackalloc char[BUFFER_LENGTH];

            int ret = Interop.Kernel32.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType, (IntPtr)buffer, BUFFER_LENGTH, IntPtr.Zero);
            if (ret > 0)
            {
                if (buffer[ret - 1] == '\0')
                {
                    ret--; // don't include the null termination in the string
                }
                data = new string(buffer, 0, ret);
                return true;
            }
            data = "";
            return false;
        }

        // Context for EnumCalendarInfoExEx callback.
        private struct EnumData
        {
            public string? userOverride;
            public List<string>? strings;
        }

        // EnumCalendarInfoExEx callback itself.
        // [NativeCallable(CallingConvention = CallingConvention.StdCall)]
        private static unsafe Interop.BOOL EnumCalendarInfoCallback(char* lpCalendarInfoString, uint calendar, IntPtr pReserved, void* lParam)
        {
            ref EnumData context = ref Unsafe.As<byte, EnumData>(ref *(byte*)lParam);
            try
            {
                string calendarInfo = new string(lpCalendarInfoString);

                // If we had a user override, check to make sure this differs
                if (context.userOverride != calendarInfo)
                {
                    Debug.Assert(context.strings != null);
                    context.strings.Add(calendarInfo);
                }

                return Interop.BOOL.TRUE;
            }
            catch (Exception)
            {
                return Interop.BOOL.FALSE;
            }
        }

        private static unsafe bool CallEnumCalendarInfo(string localeName, CalendarId calendar, uint calType, uint lcType, out string[]? data)
        {
            EnumData context = new EnumData();
            context.userOverride = null;
            context.strings = new List<string>();
            // First call GetLocaleInfo if necessary
            if (((lcType != 0) && ((lcType & CAL_NOUSEROVERRIDE) == 0)) &&
                // Get user locale, see if it matches localeName.
                // Note that they should match exactly, including letter case
                GetUserDefaultLocaleName() == localeName)
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

            unsafe
            {
                // Now call the enumeration API. Work is done by our callback function
                Interop.Kernel32.EnumCalendarInfoExEx(EnumCalendarInfoCallback, localeName, (uint)calendar, null, calType, Unsafe.AsPointer(ref context));
            }

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

        //
        // struct to help our calendar data enumaration callback
        //
        private struct EnumCalendarsData
        {
            public int userOverride;   // user override value (if found)
            public List<int> calendars;      // list of calendars found so far
        }

        // [NativeCallable(CallingConvention = CallingConvention.StdCall)]
        private static unsafe Interop.BOOL EnumCalendarsCallback(char* lpCalendarInfoString, uint calendar, IntPtr reserved, void* lParam)
        {
            ref EnumCalendarsData context = ref Unsafe.As<byte, EnumCalendarsData>(ref *(byte*)lParam);
            try
            {
                // If we had a user override, check to make sure this differs
                if (context.userOverride != calendar)
                    context.calendars.Add((int)calendar);

                return Interop.BOOL.TRUE;
            }
            catch (Exception)
            {
                return Interop.BOOL.FALSE;
            }
        }

        private static unsafe string GetUserDefaultLocaleName()
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int result;
            char* localeName = stackalloc char[Interop.Kernel32.LOCALE_NAME_MAX_LENGTH];
            result = CultureData.GetLocaleInfoEx(Interop.Kernel32.LOCALE_NAME_USER_DEFAULT, Interop.Kernel32.LOCALE_SNAME, localeName, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH);

            return result <= 0 ? "" : new string(localeName, 0, result - 1); // exclude the null termination
        }
    }
}
