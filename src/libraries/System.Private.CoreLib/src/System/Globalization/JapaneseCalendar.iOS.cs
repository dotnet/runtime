// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        private static EraInfo[]? GetJapaneseErasNative()
        {
            if (GlobalizationMode.Invariant)
            {
                return null;
            }

            Debug.Assert(!GlobalizationMode.UseNls);

            string[]? eraNames;
            eraNames = Interop.Globalization.GetCalendarInfoNative("ja-JP", CalendarId.JAPAN, CalendarDataType.EraNames).Split("||");
            if (eraNames.Length == 0)
            {
                return null;
            }

            List<EraInfo> eras = new List<EraInfo>();
            int lastMaxYear = GregorianCalendar.MaxYear;
            int latestEra = Interop.Globalization.GetLatestJapaneseEraNative();

            for (int i = latestEra; i >= 0; i--)
            {
                DateTime dt;
                if (!GetJapaneseEraStartDateNative(i, out dt))
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

            string[] abbrevEnglishEraNames = Interop.Globalization.GetCalendarInfoNative("ja", CalendarId.JAPAN, CalendarDataType.AbbrevEraNames).Split("||");
            if (abbrevEnglishEraNames.Length == 0)
            {
                // Failed to get English names. fallback to hardcoded data.
                abbrevEnglishEraNames = s_abbreviatedEnglishEraNames;
            }

            // Check if we are getting the English Name at the end of the returned list.
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

        private static bool GetJapaneseEraStartDateNative(int era, out DateTime dateTime)
        {
            dateTime = default;

            int startYear;
            int startMonth;
            int startDay;
            bool result = Interop.Globalization.GetJapaneseEraStartDateNative(era, out startYear, out startMonth, out startDay);
            if (result)
            {
                dateTime = new DateTime(startYear, startMonth, startDay);
            }

            return result;
        }
    }
}
