// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetCalendarsNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int GetCalendarsNative(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetCalendarInfoNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial string GetCalendarInfoNative(string localeName, CalendarId calendarId, CalendarDataType calendarDataType);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLatestJapaneseEraNative")]
        internal static partial int GetLatestJapaneseEraNative();

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDateNative", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetJapaneseEraStartDateNative(int era, out int startYear, out int startMonth, out int startDay);

    }
}
