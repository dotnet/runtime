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
            throw new NotImplementedException();            
        }

        // Get native two digit year max
        internal static int GetTwoDigitYearMax(CalendarId calendarId)
        {
            // TODO: Implement this.
            throw new NotImplementedException();            
        }

        internal static CalendarData GetCalendarData(CalendarId calendarId)
        {
            // TODO: Implement this.
            throw new NotImplementedException();            
        }

        // Call native side to figure out which calendars are allowed
        internal static int GetCalendars(String localeName, bool useUserOverride, CalendarId[] calendars)
        {
            // TODO: Implement this.
            throw new NotImplementedException();            
        }

        private static bool SystemSupportsTaiwaneseCalendar()
        {
            // TODO: Implement this.
            throw new NotImplementedException();            
        }

        // PAL Layer ends here
    }
}