// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    internal sealed partial class CalendarData
    {
        private bool LoadCalendarDataFromSystemCore(string localeName, CalendarId calendarId) =>
            IcuLoadCalendarDataFromSystem(localeName, calendarId);

#pragma warning disable IDE0060
        internal static int GetCalendarsCore(string localeName, bool useUserOverride, CalendarId[] calendars) =>
            IcuGetCalendars(localeName, calendars);
#pragma warning restore IDE0060
     }
}
