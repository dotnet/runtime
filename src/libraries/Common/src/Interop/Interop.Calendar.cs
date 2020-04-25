// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        internal delegate void EnumCalendarInfoCallback(
           [MarshalAs(UnmanagedType.LPWStr)] string calendarString,
           IntPtr context);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendars")]
#endif
        internal static extern int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendarInfo")]
#endif
        internal static extern unsafe ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
#endif
        internal static extern bool EnumCalendarInfo(EnumCalendarInfoCallback callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
#endif
        internal static extern int GetLatestJapaneseEra();
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
#endif
        internal static extern bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);
    }
}
