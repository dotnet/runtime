// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendars")]
        internal static extern int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendarInfo")]
        internal static extern unsafe ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
        internal static extern unsafe bool EnumCalendarInfo(delegate* <char*, IntPtr, void> callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
        internal static extern int GetLatestJapaneseEra();

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
        internal static extern bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
