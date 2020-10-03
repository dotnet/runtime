// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        private static readonly string [] s_abbreviatedEnglishEraNames = { "M", "T", "S", "H", "R" };

        private static EraInfo[]? IcuGetJapaneseEras()
        {
            if (GlobalizationMode.Invariant)
            {
                return null;
            }

            Debug.Assert(!GlobalizationMode.UseNls);

            string[]? eraNames;
            if (!CalendarData.EnumCalendarInfo("ja-JP", CalendarId.JAPAN, CalendarDataType.EraNames, out eraNames))
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

                eras.Add(new EraInfo(i, dt.Year, dt.Month, dt.Day, dt.Year - 1, 1, lastMaxYear - dt.Year + 1, eraNames![i], GetAbbreviatedEraName(eraNames, i), ""));

                lastMaxYear = dt.Year;
            }

            string[] abbrevEnglishEraNames;
            if (!CalendarData.EnumCalendarInfo("ja", CalendarId.JAPAN, CalendarDataType.AbbrevEraNames, out abbrevEnglishEraNames!))
            {
                // Failed to get English names. fallback to hardcoded data.
                abbrevEnglishEraNames = s_abbreviatedEnglishEraNames;
            }

            // Check if we are getting the English Name at the end of the returned list.
            // ICU usually return long list including all Era names written in Japanese characters except the recent eras which actually we support will be returned in English.
            // We have the following check as older ICU versions doesn't carry the English names (e.g. ICU version 50).
            if (abbrevEnglishEraNames[abbrevEnglishEraNames.Length - 1].Length == 0 || abbrevEnglishEraNames[abbrevEnglishEraNames.Length - 1][0] > '\u007F')
            {
                // Couldn't get English names.
                abbrevEnglishEraNames = s_abbreviatedEnglishEraNames;
            }

            int startIndex = abbrevEnglishEraNames == s_abbreviatedEnglishEraNames ? eras.Count - 1 : abbrevEnglishEraNames.Length - 1;

            Debug.Assert(abbrevEnglishEraNames == s_abbreviatedEnglishEraNames || eras.Count <= abbrevEnglishEraNames.Length);

            // remap the Era numbers, now that we know how many there will be
            for (int i = 0; i < eras.Count; i++)
            {
                eras[i].era = eras.Count - i;
                if (startIndex < abbrevEnglishEraNames.Length)
                {
                    eras[i].englishEraName = abbrevEnglishEraNames[startIndex];
                }
                startIndex--;
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

            dateTime = default;

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
