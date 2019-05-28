// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Internal.Win32;

namespace System.Globalization
{
    public partial class JapaneseCalendar : Calendar
    {
        private const string JapaneseErasHive = @"System\CurrentControlSet\Control\Nls\Calendars\Japanese\Eras";

        // We know about 4 built-in eras, however users may add additional era(s) from the
        // registry, by adding values to HKLM\SYSTEM\CurrentControlSet\Control\Nls\Calendars\Japanese\Eras
        //
        // Registry values look like:
        //      yyyy.mm.dd=era_abbrev_english_englishabbrev
        //
        // Where yyyy.mm.dd is the registry value name, and also the date of the era start.
        // yyyy, mm, and dd are the year, month & day the era begins (4, 2 & 2 digits long)
        // era is the Japanese Era name
        // abbrev is the Abbreviated Japanese Era Name
        // english is the English name for the Era (unused)
        // englishabbrev is the Abbreviated English name for the era.
        // . is a delimiter, but the value of . doesn't matter.
        // '_' marks the space between the japanese era name, japanese abbreviated era name
        //     english name, and abbreviated english names.
        private static EraInfo[]? GetJapaneseEras()
        {
            // Look in the registry key and see if we can find any ranges
            int iFoundEras = 0;
            EraInfo[]? registryEraRanges = null;

            try
            {
                // Need to access registry
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(JapaneseErasHive))
                {
                    // Abort if we didn't find anything
                    if (key == null) return null;

                    // Look up the values in our reg key
                    string[] valueNames = key.GetValueNames();
                    if (valueNames != null && valueNames.Length > 0)
                    {
                        registryEraRanges = new EraInfo[valueNames.Length];

                        // Loop through the registry and read in all the values
                        for (int i = 0; i < valueNames.Length; i++)
                        {
                            // See if the era is a valid date
                            EraInfo? era = GetEraFromValue(valueNames[i], key.GetValue(valueNames[i])?.ToString());

                            // continue if not valid
                            if (era == null) continue;

                            // Remember we found one.
                            registryEraRanges[iFoundEras] = era;
                            iFoundEras++;
                        }
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                // If we weren't allowed to read, then just ignore the error
                return null;
            }
            catch (System.IO.IOException)
            {
                // If key is being deleted just ignore the error
                return null;
            }
            catch (System.UnauthorizedAccessException)
            {
                // Registry access rights permissions, just ignore the error
                return null;
            }

            //
            // If we didn't have valid eras, then fail
            // should have at least 4 eras
            //
            if (iFoundEras < 4) return null;

            //
            // Now we have eras, clean them up.
            //
            // Clean up array length
            Array.Resize(ref registryEraRanges, iFoundEras);

            // Sort them
            Array.Sort(registryEraRanges!, CompareEraRanges); // TODO-NULLABLE: Remove ! when nullable attributes are respected

            // Clean up era information
            for (int i = 0; i < registryEraRanges!.Length; i++) // TODO-NULLABLE: Remove ! when nullable attributes are respected
            {
                // eras count backwards from length to 1 (and are 1 based indexes into string arrays)
                registryEraRanges[i].era = registryEraRanges.Length - i;

                // update max era year
                if (i == 0)
                {
                    // First range is 'til the end of the calendar
                    registryEraRanges[0].maxEraYear = GregorianCalendar.MaxYear - registryEraRanges[0].yearOffset;
                }
                else
                {
                    // Rest are until the next era (remember most recent era is first in array)
                    registryEraRanges[i].maxEraYear = registryEraRanges[i - 1].yearOffset + 1 - registryEraRanges[i].yearOffset;
                }
            }

            // Return our ranges
            return registryEraRanges;
        }

        //
        // Compare two era ranges, eg just the ticks
        // Remember the era array is supposed to be in reverse chronological order
        //
        private static int CompareEraRanges(EraInfo a, EraInfo b)
        {
            return b.ticks.CompareTo(a.ticks);
        }

        //
        // GetEraFromValue
        //
        // Parse the registry value name/data pair into an era
        //
        // Registry values look like:
        //      yyyy.mm.dd=era_abbrev_english_englishabbrev
        //
        // Where yyyy.mm.dd is the registry value name, and also the date of the era start.
        // yyyy, mm, and dd are the year, month & day the era begins (4, 2 & 2 digits long)
        // era is the Japanese Era name
        // abbrev is the Abbreviated Japanese Era Name
        // english is the English name for the Era (unused)
        // englishabbrev is the Abbreviated English name for the era.
        // . is a delimiter, but the value of . doesn't matter.
        // '_' marks the space between the japanese era name, japanese abbreviated era name
        //     english name, and abbreviated english names.
        private static EraInfo? GetEraFromValue(string? value, string? data)
        {
            // Need inputs
            if (value == null || data == null) return null;

            //
            // Get Date
            //
            // Need exactly 10 characters in name for date
            // yyyy.mm.dd although the . can be any character
            if (value.Length != 10) return null;

            int year;
            int month;
            int day;

            ReadOnlySpan<char> valueSpan = value.AsSpan();
            if (!int.TryParse(valueSpan.Slice(0, 4), NumberStyles.None, NumberFormatInfo.InvariantInfo, out year) ||
                !int.TryParse(valueSpan.Slice(5, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out month) ||
                !int.TryParse(valueSpan.Slice(8, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out day))
            {
                // Couldn't convert integer, fail
                return null;
            }

            //
            // Get Strings
            //
            // Needs to be a certain length e_a_E_A at least (7 chars, exactly 4 groups)
            string[] names = data.Split('_');

            // Should have exactly 4 parts
            // 0 - Era Name
            // 1 - Abbreviated Era Name
            // 2 - English Era Name
            // 3 - Abbreviated English Era Name
            if (names.Length != 4) return null;

            // Each part should have data in it
            if (names[0].Length == 0 ||
                names[1].Length == 0 ||
                names[2].Length == 0 ||
                names[3].Length == 0)
                return null;

            //
            // Now we have an era we can build
            // Note that the era # and max era year need cleaned up after sorting
            // Don't use the full English Era Name (names[2])
            //
            return new EraInfo(0, year, month, day, year - 1, 1, 0,
                                names[0], names[1], names[3]);
        }

        // PAL Layer ends here

        private static readonly string[] s_japaneseErasEnglishNames = new string[] { "M", "T", "S", "H", "R" };

        private static string GetJapaneseEnglishEraName(int era)
        {
            Debug.Assert(era > 0);
            return era <= s_japaneseErasEnglishNames.Length ? s_japaneseErasEnglishNames[era - 1] : " ";
        }
    }
}
