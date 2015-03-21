// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace System.Globalization
{
    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystem(String localeName, CalendarId calendarId)
        {
            // TODO: Implement this.
            return false;
        }

        // Get native two digit year max
        internal static int GetTwoDigitYearMax(CalendarId calendarId)
        {
            // TODO: Implement this.
            return -1;
        }

        internal static CalendarData GetCalendarData(CalendarId calendarId)
        {
            // TODO: Implement this.
            return new CalendarData("", calendarId, false);
        }

        // Call native side to figure out which calendars are allowed
        internal static int GetCalendars(String localeName, bool useUserOverride, CalendarId[] calendars)
        {
            // TODO: Implement this.
            return 0;
        }

        private static bool SystemSupportsTaiwaneseCalendar()
        {
            // TODO: Implement this.
            return false;
        }

        // PAL Layer ends here
    }
}