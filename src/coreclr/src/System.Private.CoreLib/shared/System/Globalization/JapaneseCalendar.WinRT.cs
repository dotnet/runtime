// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

using Internal.Runtime.Augments;

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        private static EraInfo[]? GetJapaneseEras()
        {
            int erasCount = WinRTInterop.Callbacks.GetJapaneseEraCount();
            if (erasCount < 4)
            {
                return null;
            }

            EraInfo[] eras = new EraInfo[erasCount];
            int lastMaxYear = GregorianCalendar.MaxYear;

            for (int i = erasCount; i > 0; i--)
            {
                DateTimeOffset dateOffset;

                string? eraName;
                string? abbreviatedEraName;

                if (!GetJapaneseEraInfo(i, out dateOffset, out eraName, out abbreviatedEraName))
                {
                    return null;
                }

                DateTime dt = new DateTime(dateOffset.Ticks);

                eras[erasCount - i] = new EraInfo(i, dt.Year, dt.Month, dt.Day, dt.Year - 1, 1, lastMaxYear - dt.Year + 1,
                                                   eraName!, abbreviatedEraName!, GetJapaneseEnglishEraName(i));    // era #4 start year/month/day, yearOffset, minEraYear

                lastMaxYear = dt.Year;
            }

            return eras;
        }

        // PAL Layer ends here

        private static readonly string[] s_JapaneseErasEnglishNames = new string[] { "M", "T", "S", "H", "R" };

        private static string GetJapaneseEnglishEraName(int era)
        {
            Debug.Assert(era > 0);
            return era <= s_JapaneseErasEnglishNames.Length ? s_JapaneseErasEnglishNames[era - 1] : " ";
        }

        private static bool GetJapaneseEraInfo(int era, out DateTimeOffset dateOffset, out string? eraName, out string? abbreviatedEraName)
        {
            return WinRTInterop.Callbacks.GetJapaneseEraInfo(era, out dateOffset, out eraName, out abbreviatedEraName);
        }
    }
}
