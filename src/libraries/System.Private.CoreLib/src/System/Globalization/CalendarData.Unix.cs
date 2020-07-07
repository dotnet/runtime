// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystemCore(string localeName, CalendarId calendarId) =>
            IcuLoadCalendarDataFromSystem(localeName, calendarId);

        internal static int GetCalendarsCore(string localeName, bool useUserOverride, CalendarId[] calendars) =>
            IcuGetCalendars(localeName, calendars);
     }
}
