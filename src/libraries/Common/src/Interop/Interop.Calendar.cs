// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetCalendars", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetCalendarInfo", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EnumCalendarInfo", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool EnumCalendarInfo(delegate* unmanaged<char*, IntPtr, void> callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
        internal static partial int GetLatestJapaneseEra();

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
