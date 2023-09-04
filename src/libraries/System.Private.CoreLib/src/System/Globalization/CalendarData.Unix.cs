// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    internal sealed partial class CalendarData
    {
        private bool LoadCalendarDataFromSystemCore(string localeName, CalendarId calendarId)
        {
#if TARGET_BROWSER
            return GlobalizationMode.Hybrid ?
                JSLoadCalendarDataFromBrowser(localeName, calendarId) :
                IcuLoadCalendarDataFromSystem(localeName, calendarId);
#elif TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
            return GlobalizationMode.Hybrid ?
                LoadCalendarDataFromNative(localeName, calendarId) :
                IcuLoadCalendarDataFromSystem(localeName, calendarId);
#else
            return IcuLoadCalendarDataFromSystem(localeName, calendarId);
#endif
        }

#pragma warning disable IDE0060
        internal static int GetCalendarsCore(string localeName, bool useUserOverride, CalendarId[] calendars) =>
            IcuGetCalendars(localeName, calendars);

        internal static int GetTwoDigitYearMax(CalendarId calendarId)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            // There is no user override for this value on Linux or in ICU.
            // So just return -1 to use the hard-coded defaults.
            return GlobalizationMode.Invariant ? Invariant.iTwoDigitYearMax : -1;
        }
#pragma warning restore IDE0060
    }
}
