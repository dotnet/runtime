// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class HttpDateParse
    {
        private const int BASE_DEC  = 10; // base 10

        //
        // Date indicies used to figure out what each entry is.
        //

        private const int DATE_INDEX_DAY_OF_WEEK     = 0;

        private const int DATE_1123_INDEX_DAY        = 1;
        private const int DATE_1123_INDEX_MONTH      = 2;
        private const int DATE_1123_INDEX_YEAR       = 3;
        private const int DATE_1123_INDEX_HRS        = 4;
        private const int DATE_1123_INDEX_MINS       = 5;
        private const int DATE_1123_INDEX_SECS       = 6;

        private const int DATE_ANSI_INDEX_MONTH      = 1;
        private const int DATE_ANSI_INDEX_DAY        = 2;
        private const int DATE_ANSI_INDEX_HRS        = 3;
        private const int DATE_ANSI_INDEX_MINS       = 4;
        private const int DATE_ANSI_INDEX_SECS       = 5;
        private const int DATE_ANSI_INDEX_YEAR       = 6;

        private const int DATE_INDEX_TZ              = 7;

        private const int DATE_INDEX_LAST            = DATE_INDEX_TZ;
        private const int MAX_FIELD_DATE_ENTRIES     = (DATE_INDEX_LAST+1);

        //
        // DATE_TOKEN's DWORD values used to determine what day/month we're on
        //

        private const int DATE_TOKEN_JANUARY      = 1;
        private const int DATE_TOKEN_FEBRUARY     = 2;
        private const int DATE_TOKEN_MARCH        = 3;
        private const int DATE_TOKEN_APRIL        = 4;
        private const int DATE_TOKEN_MAY          = 5;
        private const int DATE_TOKEN_JUNE         = 6;
        private const int DATE_TOKEN_JULY         = 7;
        private const int DATE_TOKEN_AUGUST       = 8;
        private const int DATE_TOKEN_SEPTEMBER    = 9;
        private const int DATE_TOKEN_OCTOBER      = 10;
        private const int DATE_TOKEN_NOVEMBER     = 11;
        private const int DATE_TOKEN_DECEMBER     = 12;

        private const int DATE_TOKEN_LAST_MONTH   = (DATE_TOKEN_DECEMBER+1);

        private const int DATE_TOKEN_SUNDAY       = 0;
        private const int DATE_TOKEN_MONDAY       = 1;
        private const int DATE_TOKEN_TUESDAY      = 2;
        private const int DATE_TOKEN_WEDNESDAY    = 3;
        private const int DATE_TOKEN_THURSDAY     = 4;
        private const int DATE_TOKEN_FRIDAY       = 5;
        private const int DATE_TOKEN_SATURDAY     = 6;

        private const int DATE_TOKEN_LAST_DAY     = (DATE_TOKEN_SATURDAY+1);

        private const int DATE_TOKEN_GMT          = -1000;

        private const int DATE_TOKEN_LAST         = DATE_TOKEN_GMT;

        private const int DATE_TOKEN_ERROR        = (DATE_TOKEN_LAST+1);

        /// <summary>
        /// Looks at the first three chars of a string at the index to determine if we're looking
        /// at a Day of the Week, or Month, or "GMT" string.
        /// </summary>
        /// <returns>
        /// The correct date token (0-6 for day of the week, 1-14 for month, etc.) if successful,
        /// otherwise, DATE_TOKEN_ERROR.
        /// </returns>
        private static int MapDayMonthToDword(string day, int index)
        {
            switch (char.ToUpper(day[index]))
            {
                case 'A':
                    switch (char.ToUpper(day[index + 1]))
                    {
                        case 'P':
                            return DATE_TOKEN_APRIL;
                        case 'U':
                            return DATE_TOKEN_AUGUST;
                    }
                    return DATE_TOKEN_ERROR;

                case 'D':
                    return DATE_TOKEN_DECEMBER;

                case 'F':
                    switch (char.ToUpper(day[index + 1]))
                    {
                        case 'R':
                            return DATE_TOKEN_FRIDAY;
                        case 'E':
                            return DATE_TOKEN_FEBRUARY;
                    }
                    return DATE_TOKEN_ERROR;

                case 'G':
                    return DATE_TOKEN_GMT;

                case 'M':
                    switch (char.ToUpper(day[index + 1]))
                    {
                        case 'O':
                            return DATE_TOKEN_MONDAY;
                        case 'A':
                            switch (char.ToUpper(day[index + 2]))
                            {
                                case 'R':
                                    return DATE_TOKEN_MARCH;
                                case 'Y':
                                    return DATE_TOKEN_MAY;
                            }

                            // fall through to error
                            break;
                    }
                    return DATE_TOKEN_ERROR;

                case 'N':
                    return DATE_TOKEN_NOVEMBER;

                case 'J':
                    switch (char.ToUpper(day[index + 1]))
                    {
                        case 'A':
                            return DATE_TOKEN_JANUARY;

                        case 'U':
                            switch (char.ToUpper(day[index + 2]))
                            {
                                case 'N':
                                    return DATE_TOKEN_JUNE;
                                case 'L':
                                    return DATE_TOKEN_JULY;
                            }

                            // fall through to error
                            break;
                    }
                    return DATE_TOKEN_ERROR;

                case 'O':
                    return DATE_TOKEN_OCTOBER;

                case 'S':
                    switch (char.ToUpper(day[index + 1]))
                    {
                        case 'A':
                            return DATE_TOKEN_SATURDAY;
                        case 'U':
                            return DATE_TOKEN_SUNDAY;
                        case 'E':
                            return DATE_TOKEN_SEPTEMBER;
                    }
                    return DATE_TOKEN_ERROR;

                case 'T':
                    switch (char.ToUpper(day[index + 1]))
                    {
                        case 'U':
                            return DATE_TOKEN_TUESDAY;
                        case 'H':
                            return DATE_TOKEN_THURSDAY;
                    }
                    return DATE_TOKEN_ERROR;

                case 'U':
                    return DATE_TOKEN_GMT;

                case 'W':
                    return DATE_TOKEN_WEDNESDAY;
            }

            return DATE_TOKEN_ERROR;
        }

        /// <summary>
        /// Parses through an ANSI, RFC850, or RFC1123 date format and converts it to a <see cref="DateTime"/>.
        /// </summary>
        public static bool ParseHttpDate(string dateString, out DateTime result)
        {
            int index = 0;
            int i = 0, iLastLettered = -1;
            bool isANSIDateFormat = false;
            int[] dateParseResults = new int[MAX_FIELD_DATE_ENTRIES];

            result = new DateTime();

            //
            // Date Parsing v2 (1 more to go), and here is how it works...
            //  We take a date string and churn through it once, converting
            //  integers to integers, Month,Day, and GMT strings into integers,
            //  and all is then placed IN order in a temp array.
            //
            // At the completion of the parse stage, we simple look at
            //  the data, and then map the results into a new DateTime.
            //
            // The end of the function does something munging and pretting
            //  up of the results to handle the year 2000, and TZ offsets
            //  Note: do we need to fully handle TZs anymore?
            //

            while (index < dateString.Length && i < MAX_FIELD_DATE_ENTRIES)
            {
                if (dateString[index] >= '0' && dateString[index] <= '9')
                {
                    //
                    // we have a numerical entry, scan through it and convent to DWORD
                    //

                    dateParseResults[i] = 0;

                    do
                    {
                        dateParseResults[i] *= BASE_DEC;
                        dateParseResults[i] += (dateString[index] - '0');
                        index++;
                    } while (index < dateString.Length &&
                             dateString[index] >= '0' &&
                             dateString[index] <= '9');

                    i++; // next token
                }
                else if ((dateString[index] >= 'A' && dateString[index] <= 'Z') ||
                         (dateString[index] >= 'a' && dateString[index] <= 'z'))
                {
                    //
                    // we have a string, should be a day, month, or GMT
                    //   lets skim to the end of the string
                    //

                    dateParseResults[i] = MapDayMonthToDword(dateString, index);

                    iLastLettered = i;

                    // We want to ignore the possibility of a time zone such as PST or EST in a non-standard
                    // date format such as "Thu Dec 17 16:01:28 PST 1998" (Notice that the year is _after_ the time zone
                    if ((dateParseResults[i] == DATE_TOKEN_ERROR) &&
                        !(isANSIDateFormat && (i == DATE_ANSI_INDEX_YEAR)))
                    {
                        return false;
                    }

                    //
                    // At this point if we have a vaild string
                    //  at this index, we know for sure that we're
                    //  looking at a ANSI type DATE format.
                    //

                    if (i == DATE_ANSI_INDEX_MONTH)
                    {
                        isANSIDateFormat = true;
                    }

                    //
                    // Read past the end of the current set of alpha characters,
                    //  as MapDayMonthToDword only peeks at a few characters
                    //

                    do
                    {
                        index++;
                    } while ((index < dateString.Length) &&
                             ((dateString[index] >= 'A' && dateString[index] <= 'Z') ||
                              (dateString[index] >= 'a' && dateString[index] <= 'z')));

                    i++; // next token
                }
                else
                {
                    //
                    // For the generic case its either a space, comma, semi-colon, etc.
                    //  the point is we really don't care, nor do we need to waste time
                    //  worring about it (the orginal code did).   The point is we
                    //  care about the actual date information, So we just advance to the
                    //  next lexume.
                    //

                    index++;
                }
            }

            //
            // We're finished parsing the string, now take the parsed tokens
            //  and turn them to the actual structured information we care about.
            //  So we build lpSysTime from the Array, using a local if none is passed in.
            //

            int year;
            int month;
            int day;
            int hour;
            int minute;
            int second;
            int millisecond;

            millisecond = 0;

            if (isANSIDateFormat)
            {
                day = dateParseResults[DATE_ANSI_INDEX_DAY];
                month = dateParseResults[DATE_ANSI_INDEX_MONTH];
                hour = dateParseResults[DATE_ANSI_INDEX_HRS];
                minute = dateParseResults[DATE_ANSI_INDEX_MINS];
                second = dateParseResults[DATE_ANSI_INDEX_SECS];
                if (iLastLettered != DATE_ANSI_INDEX_YEAR)
                {
                    year = dateParseResults[DATE_ANSI_INDEX_YEAR];
                }
                else
                {
                    // This is a fix to get around toString/toGMTstring (where the timezone is
                    // appended at the end. (See above)
                    year = dateParseResults[DATE_INDEX_TZ];
                }
            }
            else
            {
                day = dateParseResults[DATE_1123_INDEX_DAY];
                month = dateParseResults[DATE_1123_INDEX_MONTH];
                year = dateParseResults[DATE_1123_INDEX_YEAR];
                hour = dateParseResults[DATE_1123_INDEX_HRS];
                minute = dateParseResults[DATE_1123_INDEX_MINS];
                second = dateParseResults[DATE_1123_INDEX_SECS];
            }

            //
            // Normalize the year, 90 == 1990, handle the year 2000, 02 == 2002
            //  This is Year 2000 handling folks!!!  We get this wrong and
            //  we all look bad.
            //

            if (year < 100)
            {
                year += ((year < 80) ? 2000 : 1900);
            }

            //
            // if we got misformed time, then plug in the current time
            // !lpszHrs || !lpszMins || !lpszSec
            //

            if ((i < 4) ||
                (day > 31) ||
                (hour > 23) ||
                (minute > 59) ||
                (second > 59))
            {
                return false;
            }

            //
            // Now do the DateTime conversion
            //

            result = new DateTime(year, month, day, hour, minute, second, millisecond);

            //
            // we want the system time to be accurate. This is _suhlow_
            // The time passed in is in the local time zone; we have to convert this into GMT.
            //

            if (iLastLettered == DATE_ANSI_INDEX_YEAR)
            {
                // this should be an unusual case.
                result = result.ToUniversalTime();
            }

            //
            // If we have an Offset to another Time Zone
            //   then convert to appropriate GMT time
            //

            if (i > DATE_INDEX_TZ &&
                dateParseResults[DATE_INDEX_TZ] != DATE_TOKEN_GMT)
            {

                //
                // if we received +/-nnnn as offset (hhmm), modify the output FILETIME
                //

                double offset = dateParseResults[DATE_INDEX_TZ];
                result.AddHours(offset);
            }

            // In the end, we leave it all in LocalTime

            result = result.ToLocalTime();

            return true;
        }
    }
}
