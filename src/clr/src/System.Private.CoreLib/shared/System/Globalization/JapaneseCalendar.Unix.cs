// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        private static EraInfo[]? GetJapaneseEras()
        {
            if (GlobalizationMode.Invariant)
            {
                return null;
            }

            string[]? eraNames;
            if (!CalendarData.EnumCalendarInfo("ja-JP", CalendarId.JAPAN, CalendarDataType.EraNames, out eraNames))
            {
                return null;
            }

            string[]? abbrevEnglishEraNames;
            if (!CalendarData.EnumCalendarInfo("en", CalendarId.JAPAN, CalendarDataType.AbbrevEraNames, out abbrevEnglishEraNames))
            {
                return null;
            }

            List<EraInfo> eras = new List<EraInfo>();
            int lastMaxYear = GregorianCalendar.MaxYear;

            int latestEra = Interop.Globalization.GetLatestJapaneseEra();
            for (int i = latestEra; i >= 0; i--)
            {
                DateTime dt;
                if (!GetJapaneseEraStartDate(i, out dt))
                {
                    return null;
                }

                if (dt < s_calendarMinValue)
                {
                    // only populate the Eras that are valid JapaneseCalendar date times
                    break;
                }

                eras.Add(new EraInfo(i, dt.Year, dt.Month, dt.Day, dt.Year - 1, 1, lastMaxYear - dt.Year + 1,
                    eraNames![i], GetAbbreviatedEraName(eraNames, i), abbrevEnglishEraNames![i]));

                lastMaxYear = dt.Year;
            }

            // remap the Era numbers, now that we know how many there will be
            for (int i = 0; i < eras.Count; i++)
            {
                eras[i].era = eras.Count - i;
            }

            return eras.ToArray();
        }

        // PAL Layer ends here

        private static string GetAbbreviatedEraName(string[] eraNames, int eraIndex)
        {
            // This matches the behavior on Win32 - only returning the first character of the era name.
            // See Calendar.EraAsString(Int32) - https://msdn.microsoft.com/en-us/library/windows/apps/br206751.aspx
            return eraNames[eraIndex].Substring(0, 1);
        }

        private static bool GetJapaneseEraStartDate(int era, out DateTime dateTime)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            dateTime = default(DateTime);

            int startYear;
            int startMonth;
            int startDay;
            bool result = Interop.Globalization.GetJapaneseEraStartDate(
                era,
                out startYear,
                out startMonth,
                out startDay);

            if (result)
            {
                dateTime = new DateTime(startYear, startMonth, startDay);
            }

            return result;
        }
    }
}
