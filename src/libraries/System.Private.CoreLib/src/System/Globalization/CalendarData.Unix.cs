using System;
using System.Collections.Generic;
using System.Text;

namespace System.Globalization
{
    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystemCore(string localeName, CalendarId calendarId, bool isUserDefaultLocale) =>
            IcuLoadCalendarDataFromSystem(localeName, calendarId);
     }
}
