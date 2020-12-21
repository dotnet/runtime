// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        private const uint CAL_ITWODIGITYEARMAX = 0x00000030;

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
        [UnmanagedCallersOnly]
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
        // struct to help our calendar data enumeration callback
        //
        public struct NlsEnumCalendarsData
        {
            public int userOverride;   // user override value (if found)
            public List<int> calendars;      // list of calendars found so far
        }

        [UnmanagedCallersOnly]
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
