// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    internal partial class CalendarData
    {
        // Get native two digit year max
        internal static int NlsGetTwoDigitYearMax(CalendarId calendarId)
        {
            Debug.Assert(GlobalizationMode.UseNls);

            return GlobalizationMode.Invariant ? Invariant.iTwoDigitYearMax :
                    CallGetCalendarInfoEx(null, calendarId, CAL_ITWODIGITYEARMAX, out int twoDigitYearMax) ?
                        twoDigitYearMax :
                        -1;
        }

        // Call native side to figure out which calendars are allowed
        internal static int NlsGetCalendars(string localeName, bool useUserOverride, CalendarId[] calendars)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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
                Interop.Kernel32.EnumCalendarInfoExEx(EnumCalendarsCallback, localeName, ENUM_ALL_CALENDARS, null, CAL_ICALINTVALUE, Unsafe.AsPointer(ref data));
            }

            // Copy to the output array
            for (int i = 0; i < Math.Min(calendars.Length, data.calendars.Count); i++)
                calendars[i] = (CalendarId)data.calendars[i];

            // Now we have a list of data, return the count
            return data.calendars.Count;
        }

        private static bool NlsSystemSupportsTaiwaneseCalendar()
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            // Taiwanese calendar get listed as one of the optional zh-TW calendars only when having zh-TW UI
            return CallGetCalendarInfoEx("zh-TW", CalendarId.TAIWAN, CAL_SCALNAME, out string _);
        }

        // PAL Layer ends here

        private const uint CAL_RETURN_NUMBER = 0x20000000;
        private const uint CAL_SCALNAME = 0x00000002;
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

        private static bool CallGetCalendarInfoEx(string? localeName, CalendarId calendar, uint calType, out int data)
        {
            return Interop.Kernel32.GetCalendarInfoEx(localeName, (uint)calendar, IntPtr.Zero, calType | CAL_RETURN_NUMBER, IntPtr.Zero, 0, out data) != 0;
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
        // [UnmanagedCallersOnly(CallingConvention = CallingConvention.StdCall)]
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

        //
        // struct to help our calendar data enumaration callback
        //
        public struct NlsEnumCalendarsData
        {
            public int userOverride;   // user override value (if found)
            public List<int> calendars;      // list of calendars found so far
        }

        // [UnmanagedCallersOnly(CallingConvention = CallingConvention.StdCall)]
        private static unsafe Interop.BOOL EnumCalendarsCallback(char* lpCalendarInfoString, uint calendar, IntPtr reserved, void* lParam)
        {
            ref NlsEnumCalendarsData context = ref Unsafe.As<byte, NlsEnumCalendarsData>(ref *(byte*)lParam);
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
    }
}
