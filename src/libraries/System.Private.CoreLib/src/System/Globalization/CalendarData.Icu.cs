// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

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

    internal sealed partial class CalendarData
    {
        private bool IcuLoadCalendarDataFromSystem(string localeName, CalendarId calendarId)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            bool result = true;

            // these can return null but are later replaced with String.Empty or other non-nullable value
            result &= GetCalendarInfo(localeName, calendarId, CalendarDataType.NativeName, out this.sNativeName!);
            result &= GetCalendarInfo(localeName, calendarId, CalendarDataType.MonthDay, out this.sMonthDay!);

            if (this.sMonthDay != null)
            {
                this.sMonthDay = NormalizeDatePattern(this.sMonthDay);
            }

            result &= EnumDatePatterns(localeName, calendarId, CalendarDataType.ShortDates, out this.saShortDates!);
            result &= EnumDatePatterns(localeName, calendarId, CalendarDataType.LongDates, out this.saLongDates!);
            result &= EnumDatePatterns(localeName, calendarId, CalendarDataType.YearMonths, out this.saYearMonths!);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.DayNames, out this.saDayNames!);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevDayNames, out this.saAbbrevDayNames!);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.SuperShortDayNames, out this.saSuperShortDayNames!);

            string? leapHebrewMonthName = null;
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.MonthNames, out this.saMonthNames!, ref leapHebrewMonthName);
            if (leapHebrewMonthName != null)
            {
                Debug.Assert(this.saMonthNames != null);

                // In Hebrew calendar, get the leap month name Adar II and override the non-leap month 7
                Debug.Assert(calendarId == CalendarId.HEBREW && saMonthNames.Length == 13);
                saLeapYearMonthNames = (string[]) saMonthNames.Clone();
                saLeapYearMonthNames[6] = leapHebrewMonthName;

                // The returned data from ICU has 6th month name as 'Adar I' and 7th month name as 'Adar'
                // We need to adjust that in the list used with non-leap year to have 6th month as 'Adar' and 7th month as 'Adar II'
                // note that when formatting non-leap year dates, 7th month shouldn't get used at all.
                saMonthNames[5] = saMonthNames[6];
                saMonthNames[6] = leapHebrewMonthName;

            }
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthNames, out this.saAbbrevMonthNames!, ref leapHebrewMonthName);
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.MonthGenitiveNames, out this.saMonthGenitiveNames!, ref leapHebrewMonthName);
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthGenitiveNames, out this.saAbbrevMonthGenitiveNames!, ref leapHebrewMonthName);

            result &= EnumEraNames(localeName, calendarId, CalendarDataType.EraNames, out this.saEraNames!);
            result &= EnumEraNames(localeName, calendarId, CalendarDataType.AbbrevEraNames, out this.saAbbrevEraNames!);

            return result;
        }

        internal static int IcuGetTwoDigitYearMax(CalendarId calendarId)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            // There is no user override for this value on Linux or in ICU.
            // So just return -1 to use the hard-coded defaults.
            return -1;
        }

        // Call native side to figure out which calendars are allowed
        internal static int IcuGetCalendars(string localeName, CalendarId[] calendars)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            // NOTE: there are no 'user overrides' on Linux
            int count = Interop.Globalization.GetCalendars(localeName, calendars, calendars.Length);

            // ensure there is at least 1 calendar returned
            if (count == 0 && calendars.Length > 0)
            {
                calendars[0] = CalendarId.GREGORIAN;
                count = 1;
            }

            return count;
        }

        private static bool IcuSystemSupportsTaiwaneseCalendar()
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            return true;
        }

        // PAL Layer ends here

        private static unsafe bool GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string? calendarString)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return Interop.CallStringMethod(
                static (buffer, locale, id, type) =>
                {
                    fixed (char* bufferPtr = buffer)
                    {
                        return Interop.Globalization.GetCalendarInfo(locale, id, type, bufferPtr, buffer.Length);
                    }
                },
                localeName,
                calendarId,
                dataType,
                out calendarString);
        }

        private static bool EnumDatePatterns(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[]? datePatterns)
        {
            datePatterns = null;

            IcuEnumCalendarsData callbackContext = default;
            callbackContext.Results = new List<string>();
            callbackContext.DisallowDuplicates = true;
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
            if (result)
            {
                List<string> datePatternsList = callbackContext.Results;

                for (int i = 0; i < datePatternsList.Count; i++)
                {
                    datePatternsList[i] = NormalizeDatePattern(datePatternsList[i]);
                }

                if (dataType == CalendarDataType.ShortDates)
                    FixDefaultShortDatePattern(datePatternsList);

                datePatterns = datePatternsList.ToArray();
            }

            return result;
        }

        // FixDefaultShortDatePattern will convert the default short date pattern from using 'yy' to using 'yyyy'
        // And will ensure the original pattern still exist in the list.
        // doing that will have the short date pattern format the year as 4-digit number and not just 2-digit number.
        // Example: June 5, 2018 will be formatted to something like 6/5/2018 instead of 6/5/18 fro en-US culture.
        private static void FixDefaultShortDatePattern(List<string> shortDatePatterns)
        {
            if (shortDatePatterns.Count == 0)
                return;

            string s = shortDatePatterns[0];

            // We are not expecting any pattern have length more than 100.
            // We have to do this check to prevent stack overflow as we allocate the buffer on the stack.
            if (s.Length > 100)
                return;

            Span<char> modifiedPattern = stackalloc char[s.Length + 2];
            int index = 0;

            while (index < s.Length)
            {
                if (s[index] == '\'')
                {
                    do
                    {
                        modifiedPattern[index] = s[index];
                        index++;
                    } while (index < s.Length && s[index] != '\'');

                    if (index >= s.Length)
                        return;
                }
                else if (s[index] == 'y')
                {
                    modifiedPattern[index] = 'y';
                    break;
                }

                modifiedPattern[index] = s[index];
                index++;
            }

            if (index >= s.Length - 1 || s[index + 1] != 'y')
            {
                // not a 'yy' pattern
                return;
            }

            if (index + 2 < s.Length && s[index + 2] == 'y')
            {
                // we have 'yyy' then nothing to do
                return;
            }

            // we are sure now we have 'yy' pattern

            Debug.Assert(index + 3 < modifiedPattern.Length);

            modifiedPattern[index + 1] = 'y'; // second y
            modifiedPattern[index + 2] = 'y'; // third y
            modifiedPattern[index + 3] = 'y'; // fourth y

            index += 2;

            // Now, copy the rest of the pattern to the destination buffer
            while (index < s.Length)
            {
                modifiedPattern[index + 2] = s[index];
                index++;
            }

            shortDatePatterns[0] = modifiedPattern.ToString();

            for (int i = 1; i < shortDatePatterns.Count; i++)
            {
                if (shortDatePatterns[i] == shortDatePatterns[0])
                {
                    // Found match in the list to the new constructed pattern, then replace it with the original modified pattern
                    shortDatePatterns[i] = s;
                    return;
                }
            }

            // if we come here means the newly constructed pattern not found on the list, then add the original pattern
            shortDatePatterns.Add(s);
        }

        /// <summary>
        /// The ICU date format characters are not exactly the same as the .NET date format characters.
        /// NormalizeDatePattern will take in an ICU date pattern and return the equivalent .NET date pattern.
        /// </summary>
        /// <remarks>
        /// see Date Field Symbol Table in http://userguide.icu-project.org/formatparse/datetime
        /// and https://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx
        /// </remarks>
        private static string NormalizeDatePattern(string input)
        {
            StringBuilder destination = StringBuilderCache.Acquire(input.Length);

            int index = 0;
            while (index < input.Length)
            {
                switch (input[index])
                {
                    case '\'':
                        // single quotes escape characters, like 'de' in es-SP
                        // so read verbatim until the next single quote
                        destination.Append(input[index++]);
                        while (index < input.Length)
                        {
                            char current = input[index++];
                            destination.Append(current);
                            if (current == '\'')
                            {
                                break;
                            }
                        }
                        break;
                    case 'E':
                    case 'e':
                    case 'c':
                        // 'E' in ICU is the day of the week, which maps to 3 or 4 'd's in .NET
                        // 'e' in ICU is the local day of the week, which has no representation in .NET, but
                        // maps closest to 3 or 4 'd's in .NET
                        // 'c' in ICU is the stand-alone day of the week, which has no representation in .NET, but
                        // maps closest to 3 or 4 'd's in .NET
                        NormalizeDayOfWeek(input, destination, ref index);
                        break;
                    case 'L':
                    case 'M':
                        // 'L' in ICU is the stand-alone name of the month,
                        // which maps closest to 'M' in .NET since it doesn't support stand-alone month names in patterns
                        // 'M' in both ICU and .NET is the month,
                        // but ICU supports 5 'M's, which is the super short month name
                        int occurrences = CountOccurrences(input, input[index], ref index);
                        if (occurrences > 4)
                        {
                            // 5 'L's or 'M's in ICU is the super short name, which maps closest to MMM in .NET
                            occurrences = 3;
                        }
                        destination.Append('M', occurrences);
                        break;
                    case 'G':
                        // 'G' in ICU is the era, which maps to 'g' in .NET
                        occurrences = CountOccurrences(input, 'G', ref index);

                        // it doesn't matter how many 'G's, since .NET only supports 'g' or 'gg', and they
                        // have the same meaning
                        destination.Append('g');
                        break;
                    case 'y':
                        // a single 'y' in ICU is the year with no padding or trimming.
                        // a single 'y' in .NET is the year with 1 or 2 digits
                        // so convert any single 'y' to 'yyyy'
                        occurrences = CountOccurrences(input, 'y', ref index);
                        if (occurrences == 1)
                        {
                            occurrences = 4;
                        }
                        destination.Append('y', occurrences);
                        break;
                    default:
                        const string unsupportedDateFieldSymbols = "YuUrQqwWDFg";
                        Debug.Assert(!unsupportedDateFieldSymbols.Contains(input[index]),
                            $"Encountered an unexpected date field symbol '{input[index]}' from ICU which has no known corresponding .NET equivalent.");

                        destination.Append(input[index++]);
                        break;
                }
            }

            return StringBuilderCache.GetStringAndRelease(destination);
        }

        private static void NormalizeDayOfWeek(string input, StringBuilder destination, ref int index)
        {
            char dayChar = input[index];
            int occurrences = CountOccurrences(input, dayChar, ref index);
            occurrences = Math.Max(occurrences, 3);
            if (occurrences > 4)
            {
                // 5 and 6 E/e/c characters in ICU is the super short names, which maps closest to ddd in .NET
                occurrences = 3;
            }

            destination.Append('d', occurrences);
        }

        private static int CountOccurrences(string input, char value, ref int index)
        {
            int startIndex = index;
            while (index < input.Length && input[index] == value)
            {
                index++;
            }

            return index - startIndex;
        }

        private static bool EnumMonthNames(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[]? monthNames, ref string? leapHebrewMonthName)
        {
            monthNames = null;

            IcuEnumCalendarsData callbackContext = default;
            callbackContext.Results = new List<string>();
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
            if (result)
            {
                // the month-name arrays are expected to have 13 elements.  If ICU only returns 12, add an
                // extra empty string to fill the array.
                if (callbackContext.Results.Count == 12)
                {
                    callbackContext.Results.Add(string.Empty);
                }

                if (callbackContext.Results.Count > 13)
                {
                    Debug.Assert(calendarId == CalendarId.HEBREW && callbackContext.Results.Count == 14);

                    if (calendarId == CalendarId.HEBREW)
                    {
                        leapHebrewMonthName = callbackContext.Results[13];
                    }
                    callbackContext.Results.RemoveRange(13, callbackContext.Results.Count - 13);
                }

                monthNames = callbackContext.Results.ToArray();
            }

            return result;
        }

        private static bool EnumEraNames(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[]? eraNames)
        {
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, out eraNames);

            // .NET expects that only the Japanese calendars have more than 1 era.
            // So for other calendars, only return the latest era.
            if (calendarId != CalendarId.JAPAN && calendarId != CalendarId.JAPANESELUNISOLAR && eraNames?.Length > 0)
            {
                string[] latestEraName = new string[] { eraNames![eraNames.Length - 1] };
                eraNames = latestEraName;
            }

            return result;
        }

        internal static bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[]? calendarData)
        {
            calendarData = null;

            IcuEnumCalendarsData callbackContext = default;
            callbackContext.Results = new List<string>();
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
            if (result)
            {
                calendarData = callbackContext.Results.ToArray();
            }

            return result;
        }

        private static unsafe bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, ref IcuEnumCalendarsData callbackContext)
        {
            return Interop.Globalization.EnumCalendarInfo(&EnumCalendarInfoCallback, localeName, calendarId, dataType, (IntPtr)Unsafe.AsPointer(ref callbackContext));
        }

        [UnmanagedCallersOnly]
        private static unsafe void EnumCalendarInfoCallback(char* calendarStringPtr, IntPtr context)
        {
            try
            {
                ReadOnlySpan<char> calendarStringSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(calendarStringPtr);
                ref IcuEnumCalendarsData callbackContext = ref Unsafe.As<byte, IcuEnumCalendarsData>(ref *(byte*)context);

                if (callbackContext.DisallowDuplicates)
                {
                    foreach (string existingResult in callbackContext.Results)
                    {
                        if (string.CompareOrdinal(calendarStringSpan, existingResult) == 0)
                        {
                            // the value is already in the results, so don't add it again
                            return;
                        }
                    }
                }

                callbackContext.Results.Add(calendarStringSpan.ToString());
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
                // we ignore the managed exceptions here because EnumCalendarInfoCallback will get called from the native code.
                // If we don't ignore the exception here that can cause the runtime to fail fast.
            }
        }

        private struct IcuEnumCalendarsData
        {
            public List<string> Results;
            public bool DisallowDuplicates;
        }
    }
}
