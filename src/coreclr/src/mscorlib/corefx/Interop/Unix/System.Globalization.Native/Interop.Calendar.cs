// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendars")]
        internal static extern int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendarInfo")]
        internal static extern ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, [Out] StringBuilder result, int resultCapacity);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
        internal static extern bool EnumCalendarInfo(EnumCalendarInfoCallback callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);

        [DllImport(Libraries.GlobalizationInterop, EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
        internal static extern int GetLatestJapaneseEra();

        [DllImport(Libraries.GlobalizationInterop, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
        internal static extern bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
