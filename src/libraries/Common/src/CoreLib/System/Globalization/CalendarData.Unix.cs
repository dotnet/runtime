// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
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

    internal partial class CalendarData
    {
        private bool LoadCalendarDataFromSystem(String localeName, CalendarId calendarId)
        {
            bool result = true;
            result &= GetCalendarInfo(localeName, calendarId, CalendarDataType.NativeName, out this.sNativeName);
            result &= GetCalendarInfo(localeName, calendarId, CalendarDataType.MonthDay, out this.sMonthDay);
            this.sMonthDay = NormalizeDatePattern(this.sMonthDay);

            result &= EnumDatePatterns(localeName, calendarId, CalendarDataType.ShortDates, out this.saShortDates);
            result &= EnumDatePatterns(localeName, calendarId, CalendarDataType.LongDates, out this.saLongDates);
            result &= EnumDatePatterns(localeName, calendarId, CalendarDataType.YearMonths, out this.saYearMonths);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.DayNames, out this.saDayNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.AbbrevDayNames, out this.saAbbrevDayNames);
            result &= EnumCalendarInfo(localeName, calendarId, CalendarDataType.SuperShortDayNames, out this.saSuperShortDayNames);
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.MonthNames, out this.saMonthNames);
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthNames, out this.saAbbrevMonthNames);
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.MonthGenitiveNames, out this.saMonthGenitiveNames);
            result &= EnumMonthNames(localeName, calendarId, CalendarDataType.AbbrevMonthGenitiveNames, out this.saAbbrevMonthGenitiveNames);
            result &= EnumEraNames(localeName, calendarId, CalendarDataType.EraNames, out this.saEraNames);
            result &= EnumEraNames(localeName, calendarId, CalendarDataType.AbbrevEraNames, out this.saAbbrevEraNames);

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
            Debug.Assert(!GlobalizationMode.Invariant);

            // NOTE: there are no 'user overrides' on Linux
            int count = Interop.GlobalizationInterop.GetCalendars(localeName, calendars, calendars.Length);

            // ensure there is at least 1 calendar returned
            if (count == 0 && calendars.Length > 0)
            {
                calendars[0] = CalendarId.GREGORIAN;
                count = 1;
            }

            return count;
        }

        private static bool SystemSupportsTaiwaneseCalendar()
        {
            return true;
        }

        // PAL Layer ends here

        private static bool GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string calendarString)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return Interop.CallStringMethod(
                (locale, calId, type, stringBuilder) =>
                    Interop.GlobalizationInterop.GetCalendarInfo(
                        locale,
                        calId,
                        type,
                        stringBuilder,
                        stringBuilder.Capacity),
                localeName,
                calendarId,
                dataType,
                out calendarString);
        }

        private static bool EnumDatePatterns(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] datePatterns)
        {
            datePatterns = null;

            EnumCalendarsData callbackContext = new EnumCalendarsData();
            callbackContext.Results = new List<string>();
            callbackContext.DisallowDuplicates = true;
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
            if (result)
            {
                List<string> datePatternsList = callbackContext.Results;

                datePatterns = new string[datePatternsList.Count];
                for (int i = 0; i < datePatternsList.Count; i++)
                {
                    datePatterns[i] = NormalizeDatePattern(datePatternsList[i]);
                }
            }

            return result;
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
                        Debug.Assert(unsupportedDateFieldSymbols.IndexOf(input[index]) == -1,
                            string.Format(CultureInfo.InvariantCulture,
                                "Encountered an unexpected date field symbol '{0}' from ICU which has no known corresponding .NET equivalent.", 
                                input[index]));

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

        private static bool EnumMonthNames(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] monthNames)
        {
            monthNames = null;

            EnumCalendarsData callbackContext = new EnumCalendarsData();
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

                monthNames = callbackContext.Results.ToArray();
            }

            return result;
        }

        private static bool EnumEraNames(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] eraNames)
        {
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, out eraNames);

            // .NET expects that only the Japanese calendars have more than 1 era.
            // So for other calendars, only return the latest era.
            if (calendarId != CalendarId.JAPAN && calendarId != CalendarId.JAPANESELUNISOLAR && eraNames.Length > 0)
            {
                string[] latestEraName = new string[] { eraNames[eraNames.Length - 1] };
                eraNames = latestEraName;
            }

            return result;
        }

        internal static bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, out string[] calendarData)
        {
            calendarData = null;

            EnumCalendarsData callbackContext = new EnumCalendarsData();
            callbackContext.Results = new List<string>();
            bool result = EnumCalendarInfo(localeName, calendarId, dataType, ref callbackContext);
            if (result)
            {
                calendarData = callbackContext.Results.ToArray();
            }

            return result;
        }

        private static unsafe bool EnumCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType dataType, ref EnumCalendarsData callbackContext)
        {
            return Interop.GlobalizationInterop.EnumCalendarInfo(EnumCalendarInfoCallback, localeName, calendarId, dataType, (IntPtr)Unsafe.AsPointer(ref callbackContext));
        }

        private static unsafe void EnumCalendarInfoCallback(string calendarString, IntPtr context)
        {
            try
            {
                ref EnumCalendarsData callbackContext = ref Unsafe.As<byte, EnumCalendarsData>(ref *(byte*)context);

                if (callbackContext.DisallowDuplicates)
                {
                    foreach (string existingResult in callbackContext.Results)
                    {
                        if (string.Equals(calendarString, existingResult, StringComparison.Ordinal))
                        {
                            // the value is already in the results, so don't add it again
                            return;
                        }
                    }
                }

                callbackContext.Results.Add(calendarString);
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
                // we ignore the managed exceptions here because EnumCalendarInfoCallback will get called from the native code.
                // If we don't ignore the exception here that can cause the runtime to fail fast.
            }
        }

        private struct EnumCalendarsData
        {
            public List<string> Results;
            public bool DisallowDuplicates;
        }
    }
}
