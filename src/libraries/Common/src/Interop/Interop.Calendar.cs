// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        internal unsafe delegate void EnumCalendarInfoCallback(
           char* calendarString,
           IntPtr context);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendars")]
        internal static extern int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendarInfo")]
        internal static extern unsafe ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity);

#if TARGET_BROWSER
        // Temp workaround for pinvoke callbacks for Mono-Wasm-Interpreter
        // https://github.com/dotnet/runtime/issues/39100
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
        internal static extern bool EnumCalendarInfo(IntPtr callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
        internal static extern bool EnumCalendarInfo(EnumCalendarInfoCallback callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);
#endif

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
        internal static extern int GetLatestJapaneseEra();

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
        internal static extern bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
