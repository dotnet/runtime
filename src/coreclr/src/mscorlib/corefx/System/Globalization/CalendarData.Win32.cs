// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace System.Globalization
{
    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystem(String localeName, CalendarId calendarId)
        {
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
            ret &= CallEnumCalendarInfo(localeName, calendarId, CAL_SSHORTDATE, LOCALE_SSHORTDATE | useOverrides, out this.saShortDates);
            ret &= CallEnumCalendarInfo(localeName, calendarId, CAL_SLONGDATE, LOCALE_SLONGDATE | useOverrides, out this.saLongDates);

            // Get the YearMonth pattern.
            ret &= CallEnumCalendarInfo(localeName, calendarId, CAL_SYEARMONTH, LOCALE_SYEARMONTH, out this.saYearMonths);

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
            CallEnumCalendarInfo(localeName, calendarId, CAL_SERASTRING, 0, out this.saEraNames);
            CallEnumCalendarInfo(localeName, calendarId, CAL_SABBREVERASTRING, 0, out this.saAbbrevEraNames);

            //
            // Calendar Era Info
            // Note that calendar era data (offsets, etc) is hard coded for each calendar since this
            // data is implementation specific and not dynamic (except perhaps Japanese)
            //

            // Clean up the escaping of the formats
            this.saShortDates = CultureData.ReescapeWin32Strings(this.saShortDates);
            this.saLongDates = CultureData.ReescapeWin32Strings(this.saLongDates);
            this.saYearMonths = CultureData.ReescapeWin32Strings(this.saYearMonths);
            this.sMonthDay = CultureData.ReescapeWin32String(this.sMonthDay);

            return ret;
        }

        // Get native two digit year max
        internal static int GetTwoDigitYearMax(CalendarId calendarId)
        {
            int twoDigitYearMax = -1;

            if (!CallGetCalendarInfoEx(null, calendarId, (uint)CAL_ITWODIGITYEARMAX, out twoDigitYearMax))
            {
                twoDigitYearMax = -1;
            }

            return twoDigitYearMax;
        }

        internal static CalendarData GetCalendarData(CalendarId calendarId)
        {
            //
            // Get a calendar.
            // Unfortunately we depend on the locale in the OS, so we need a locale
            // no matter what.  So just get the appropriate calendar from the 
            // appropriate locale here
            //

            // Get a culture name
            // TODO: NLS Arrowhead Arrowhead - note that this doesn't handle the new calendars (lunisolar, etc)
            String culture = CalendarIdToCultureName(calendarId);

            // Return our calendar
            return CultureInfo.GetCultureInfo(culture).m_cultureData.GetCalendar(calendarId);
        }

        // Call native side to figure out which calendars are allowed
        internal static int GetCalendars(String localeName, bool useUserOverride, CalendarId[] calendars)
        {
            EnumCalendarsData data = new EnumCalendarsData();
            data.userOverride = 0;
            data.calendars = new LowLevelList<int>();

            // First call GetLocaleInfo if necessary
            if (useUserOverride)
            {
                // They want user overrides, see if the user calendar matches the input calendar
                int userCalendar = Interop.mincore.GetLocaleInfoExInt(localeName, LOCALE_ICALENDARTYPE);

                // If we got a default, then use it as the first calendar
                if (userCalendar != 0)
                {
                    data.userOverride = userCalendar;
                    data.calendars.Add(userCalendar);
                }
            }

            GCHandle contextHandle = GCHandle.Alloc(data);

            try
            {
                EnumCalendarInfoExExCallback callback = new EnumCalendarInfoExExCallback(EnumCalendarsCallback);
                Interop.mincore_private.LParamCallbackContext context = new Interop.mincore_private.LParamCallbackContext();
                context.lParam = (IntPtr)contextHandle;

                // Now call the enumeration API. Work is done by our callback function
                Interop.mincore_private.EnumCalendarInfoExEx(callback, localeName, ENUM_ALL_CALENDARS, null, CAL_ICALINTVALUE, context);
            }
            finally
            {
                contextHandle.Free();
            }

            // Copy to the output array
            for (int i = 0; i < Math.Min(calendars.Length, data.calendars.Count); i++)
                calendars[i] = (CalendarId)data.calendars[i];

            // Now we have a list of data, return the count
            return data.calendars.Count;
        }

        private static bool SystemSupportsTaiwaneseCalendar()
        {
            string data;
            // Taiwanese calendar get listed as one of the optional zh-TW calendars only when having zh-TW UI 
            return CallGetCalendarInfoEx("zh-TW", CalendarId.TAIWAN, CAL_SCALNAME, out data);
        }

        // PAL Layer ends here

        const uint CAL_RETURN_NUMBER = 0x20000000;
        const uint CAL_RETURN_GENITIVE_NAMES = 0x10000000;
        const uint CAL_NOUSEROVERRIDE = 0x80000000;
        const uint CAL_SCALNAME = 0x00000002;
        const uint CAL_SMONTHDAY = 0x00000038;
        const uint CAL_SSHORTDATE = 0x00000005;
        const uint CAL_SLONGDATE = 0x00000006;
        const uint CAL_SYEARMONTH = 0x0000002f;
        const uint CAL_SDAYNAME7 = 0x0000000d;
        const uint CAL_SABBREVDAYNAME7 = 0x00000014;
        const uint CAL_SMONTHNAME1 = 0x00000015;
        const uint CAL_SABBREVMONTHNAME1 = 0x00000022;
        const uint CAL_SSHORTESTDAYNAME7 = 0x00000037;
        const uint CAL_SERASTRING = 0x00000004;
        const uint CAL_SABBREVERASTRING = 0x00000039;
        const uint CAL_ICALINTVALUE = 0x00000001;
        const uint CAL_ITWODIGITYEARMAX = 0x00000030;

        const uint ENUM_ALL_CALENDARS = 0xffffffff;

        const uint LOCALE_SSHORTDATE = 0x0000001F;
        const uint LOCALE_SLONGDATE = 0x00000020;
        const uint LOCALE_SYEARMONTH = 0x00001006;
        const uint LOCALE_ICALENDARTYPE = 0x00001009;

        private static String CalendarIdToCultureName(CalendarId calendarId)
        {
            switch (calendarId)
            {
                case CalendarId.GREGORIAN_US:
                    return "fa-IR";             // "fa-IR" Iran

                case CalendarId.JAPAN:
                    return "ja-JP";             // "ja-JP" Japan

                case CalendarId.TAIWAN:
                    return "zh-TW";             // zh-TW Taiwan

                case CalendarId.KOREA:
                    return "ko-KR";             // "ko-KR" Korea

                case CalendarId.HIJRI:
                case CalendarId.GREGORIAN_ARABIC:
                case CalendarId.UMALQURA:
                    return "ar-SA";             // "ar-SA" Saudi Arabia

                case CalendarId.THAI:
                    return "th-TH";             // "th-TH" Thailand

                case CalendarId.HEBREW:
                    return "he-IL";             // "he-IL" Israel

                case CalendarId.GREGORIAN_ME_FRENCH:
                    return "ar-DZ";             // "ar-DZ" Algeria

                case CalendarId.GREGORIAN_XLIT_ENGLISH:
                case CalendarId.GREGORIAN_XLIT_FRENCH:
                    return "ar-IQ";             // "ar-IQ"; Iraq

                default:
                    // Default to gregorian en-US
                    break;
            }

            return "en-US";
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

        private static bool CallGetCalendarInfoEx(string localeName, CalendarId calendar, uint calType, out int data)
        {
            return (Interop.mincore.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType | CAL_RETURN_NUMBER, IntPtr.Zero, 0, out data) != 0);
        }

        private static unsafe bool CallGetCalendarInfoEx(string localeName, CalendarId calendar, uint calType, out string data)
        {
            const int BUFFER_LENGTH = 80;

            // The maximum size for values returned from GetCalendarInfoEx is 80 characters.
            char* buffer = stackalloc char[BUFFER_LENGTH];

            int ret = Interop.mincore.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType, (IntPtr)buffer, BUFFER_LENGTH, IntPtr.Zero);
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
        class EnumData
        {
            public string userOverride;
            public LowLevelList<string> strings;
        }

        // EnumCalendarInfoExEx callback itself.
        static unsafe bool EnumCalendarInfoCallback(IntPtr lpCalendarInfoString, uint calendar, IntPtr pReserved, Interop.mincore_private.LParamCallbackContext contextHandle)
        {
            EnumData context = (EnumData)((GCHandle)contextHandle.lParam).Target;
            try
            {
                string calendarInfo = new string((char*)lpCalendarInfoString);

                // If we had a user override, check to make sure this differs
                if (context.userOverride != calendarInfo)
                    context.strings.Add(calendarInfo);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static unsafe bool CallEnumCalendarInfo(string localeName, CalendarId calendar, uint calType, uint lcType, out string[] data)
        {
            EnumData context = new EnumData();
            context.userOverride = null;
            context.strings = new LowLevelList<string>();

            // First call GetLocaleInfo if necessary
            if (((lcType != 0) && ((lcType & CAL_NOUSEROVERRIDE) == 0)) &&
                // Get user locale, see if it matches localeName.
                // Note that they should match exactly, including letter case
                GetUserDefaultLocaleName() == localeName)
            {
                // They want user overrides, see if the user calendar matches the input calendar
                CalendarId userCalendar = (CalendarId)Interop.mincore.GetLocaleInfoExInt(localeName, LOCALE_ICALENDARTYPE);

                // If the calendars were the same, see if the locales were the same
                if (userCalendar == calendar)
                {
                    // They matched, get the user override since locale & calendar match
                    string res = Interop.mincore.GetLocaleInfoEx(localeName, lcType);

                    // if it succeeded remember the override for the later callers
                    if (res != "")
                    {
                        // Remember this was the override (so we can look for duplicates later in the enum function)
                        context.userOverride = res;

                        // Add to the result strings.
                        context.strings.Add(res);
                    }
                }
            }

            GCHandle contextHandle = GCHandle.Alloc(context);
            try
            {
                EnumCalendarInfoExExCallback callback = new EnumCalendarInfoExExCallback(EnumCalendarInfoCallback);
                Interop.mincore_private.LParamCallbackContext ctx = new Interop.mincore_private.LParamCallbackContext();
                ctx.lParam = (IntPtr)contextHandle;

                // Now call the enumeration API. Work is done by our callback function
                Interop.mincore_private.EnumCalendarInfoExEx(callback, localeName, (uint)calendar, null, calType, ctx);
            }
            finally
            {
                contextHandle.Free();
            }

            // Now we have a list of data, fail if we didn't find anything.
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
        // NOTE: There's a disparity between .Net & windows day orders, the input day should
        //           start with Sunday
        //
        // Parameters:
        //      OUT pOutputStrings      The output string[] value.
        //
        ////////////////////////////////////////////////////////////////////////
        static bool GetCalendarDayInfo(string localeName, CalendarId calendar, uint calType, out string[] outputStrings)
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
        static bool GetCalendarMonthInfo(string localeName, CalendarId calendar, uint calType, out string[] outputStrings)
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
        class EnumCalendarsData
        {
            public int userOverride;   // user override value (if found)
            public LowLevelList<int> calendars;      // list of calendars found so far
        }

        static bool EnumCalendarsCallback(IntPtr lpCalendarInfoString, uint calendar, IntPtr reserved, Interop.mincore_private.LParamCallbackContext cxt)
        {
            EnumCalendarsData context = (EnumCalendarsData)((GCHandle)cxt.lParam).Target;
            try
            {
                // If we had a user override, check to make sure this differs
                if (context.userOverride != calendar)
                    context.calendars.Add((int)calendar);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static unsafe String GetUserDefaultLocaleName()
        {
            const int LOCALE_NAME_MAX_LENGTH = 85;
            const uint LOCALE_SNAME = 0x0000005c;
            const string LOCALE_NAME_USER_DEFAULT = null;

            int result;
            char* localeName = stackalloc char[LOCALE_NAME_MAX_LENGTH];
            result = Interop.mincore.GetLocaleInfoEx(LOCALE_NAME_USER_DEFAULT, LOCALE_SNAME, localeName, LOCALE_NAME_MAX_LENGTH);

            return result <= 0 ? "" : new String(localeName, 0, result - 1); // exclude the null termination
        }
    }
}