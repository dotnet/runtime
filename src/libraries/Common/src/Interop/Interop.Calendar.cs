// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetCalendars", CharSet = CharSet.Unicode)]
        internal static partial int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetCalendarInfo", CharSet = CharSet.Unicode)]
        internal static unsafe partial ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity);

        internal static unsafe bool EnumCalendarInfo(delegate* unmanaged<char*, IntPtr, void> callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context)
        {
            return EnumCalendarInfo((IntPtr)callback, localeName, calendarId, calendarDataType, context);
        }

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EnumCalendarInfo", CharSet = CharSet.Unicode)]
        // We skip the following DllImport because of 'Parsing function pointer types in signatures is not supported.' for some targeted
        // platforms (for example, WASM build).
        private static unsafe partial bool EnumCalendarInfo(IntPtr callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
        internal static partial int GetLatestJapaneseEra();

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
        internal static partial bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
