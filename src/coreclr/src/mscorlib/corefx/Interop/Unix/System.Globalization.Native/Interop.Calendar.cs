// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        internal delegate void EnumCalendarInfoCallback(
           [MarshalAs(UnmanagedType.LPWStr)] string calendarString,
           IntPtr context);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal static extern int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal static extern CalendarDataResult GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, [Out] StringBuilder result, int resultCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumCalendarInfo(EnumCalendarInfoCallback callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);

        [DllImport(Libraries.GlobalizationInterop)]
        internal static extern int GetLatestJapaneseEra();

        [DllImport(Libraries.GlobalizationInterop)]
        internal static extern bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
