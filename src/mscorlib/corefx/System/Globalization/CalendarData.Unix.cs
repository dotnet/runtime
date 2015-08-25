// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    // needs to be kept in sync with CalendarDataType in System.Globalization.Native
    internal enum CalendarDataType
    {
        Uninitialized = 0,
        NativeName = 1,
        MonthDay = 2,
        ShortDates = 3,
        LongDates = 4,
        YearMonths = 5,
        DayNames = 6,
        AbbrevDayNames = 7,
        MonthNames = 8,
        AbbrevMonthNames = 9,
        SuperShortDayNames = 10,
        MonthGenitiveNames = 11,
        AbbrevMonthGenitiveNames = 12,
        EraNames = 13,
        AbbrevEraNames = 14,
    }

    // needs to be kept in sync with CalendarDataResult in System.Globalization.Native
    internal enum CalendarDataResult
    {
        Success = 0,
        UnknownError = 1,
        InsufficentBuffer = 2,
    }

    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystem(String localeName, CalendarId calendarId)
        {
            bool result = true;
            result &= GetCalendarInfo(localeName, calendarId, CalendarDataType.NativeName, out this.sNativeName);
            result &= GetCalendarInfo(localeName, calendarId, CalendarDataType.MonthDay, out this.sMonthDay);

            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.ShortDates, out this.saShortDates);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.LongDates, out this.saLongDates);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.YearMonths, out this.saYearMonths);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.DayNames, out this.saDayNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevDayNames, out this.saAbbrevDayNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.MonthNames, out this.saMonthNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevMonthNames, out this.saAbbrevMonthNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.SuperShortDayNames, out this.saSuperShortDayNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.MonthGenitiveNames, out this.saMonthGenitiveNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevMonthGenitiveNames, out this.saAbbrevMonthGenitiveNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.EraNames, out this.saEraNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevEraNames, out this.saAbbrevEraNames);

            return result;
        }

        internal static int GetTwoDigitYearMax(CalendarId calendarId)
        {
            // There is no user override for this value on Linux or in ICU.
            // So just return -1 to use the hard-coded defaults.
            return -1;
        }

        // Call native side to figure out which calendars are allowed
        internal static int GetCalendars(string localeName, bool useUserOverride, CalendarId[] calendars)
        {
            // TODO: Implement this.
            return 0;
        }

        private static bool SystemSupportsTaiwaneseCalendar()
        {
            return true;
        }

        // PAL Layer ends here

        private static bool GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string calendarString)
        {
            calendarString = null;

            const int initialStringSize = 80;
            const int maxDoubleAttempts = 5;

            for (int i = 0; i < maxDoubleAttempts; i++)
            {
                StringBuilder stringBuilder = StringBuilderCache.Acquire((int)(initialStringSize * Math.Pow(2, i)));

                CalendarDataResult result = Interop.GlobalizationInterop.GetCalendarInfo(
                    localeName,
                    calendarId,
                    dataType,
                    stringBuilder,
                    stringBuilder.Capacity);

                if (result == CalendarDataResult.Success)
                {
                    calendarString = StringBuilderCache.GetStringAndRelease(stringBuilder);
                    return true;
                }
                else
                {
                    StringBuilderCache.Release(stringBuilder);

                    if (result != CalendarDataResult.InsufficentBuffer)
                    {
                        return false;
                    }

                    // else, it is an InsufficentBuffer error, so loop and increase the string size
                }
            }

            return false;
        }

        private static bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] calendarData)
        {
            calendarData = null;

            List<string> calendarDataList = new List<string>();
            GCHandle context = GCHandle.Alloc(calendarDataList);
            try
            {
                bool result = Interop.GlobalizationInterop.EnumCalendarInfo(EnumCalendarInfoCallback, localeName, calendarId, dataType, (IntPtr)context);
                if (result)
                {
                    calendarData = calendarDataList.ToArray();
                }

                return result;
            }
            finally
            {
                context.Free();
            }
        }

        private static void EnumCalendarInfoCallback(string calendarString, IntPtr context)
        {
            List<string> calendarDataList = (List<string>)((GCHandle)context).Target;
            calendarDataList.Add(calendarString);
        }
    }
}
