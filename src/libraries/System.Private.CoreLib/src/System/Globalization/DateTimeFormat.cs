// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    /*
     Customized format patterns:
     P.S. Format in the table below is the internal number format used to display the pattern.

     Patterns   Format      Description                           Example
     =========  ==========  ===================================== ========
        "h"     "0"         hour (12-hour clock)w/o leading zero  3
        "hh"    "00"        hour (12-hour clock)with leading zero 03
        "hh*"   "00"        hour (12-hour clock)with leading zero 03

        "H"     "0"         hour (24-hour clock)w/o leading zero  8
        "HH"    "00"        hour (24-hour clock)with leading zero 08
        "HH*"   "00"        hour (24-hour clock)                  08

        "m"     "0"         minute w/o leading zero
        "mm"    "00"        minute with leading zero
        "mm*"   "00"        minute with leading zero

        "s"     "0"         second w/o leading zero
        "ss"    "00"        second with leading zero
        "ss*"   "00"        second with leading zero

        "f"     "0"         second fraction (1 digit)
        "ff"    "00"        second fraction (2 digit)
        "fff"   "000"       second fraction (3 digit)
        "ffff"  "0000"      second fraction (4 digit)
        "fffff" "00000"         second fraction (5 digit)
        "ffffff"    "000000"    second fraction (6 digit)
        "fffffff"   "0000000"   second fraction (7 digit)

        "F"     "0"         second fraction (up to 1 digit)
        "FF"    "00"        second fraction (up to 2 digit)
        "FFF"   "000"       second fraction (up to 3 digit)
        "FFFF"  "0000"      second fraction (up to 4 digit)
        "FFFFF" "00000"         second fraction (up to 5 digit)
        "FFFFFF"    "000000"    second fraction (up to 6 digit)
        "FFFFFFF"   "0000000"   second fraction (up to 7 digit)

        "t"                 first character of AM/PM designator   A
        "tt"                AM/PM designator                      AM
        "tt*"               AM/PM designator                      PM

        "d"     "0"         day w/o leading zero                  1
        "dd"    "00"        day with leading zero                 01
        "ddd"               short weekday name (abbreviation)     Mon
        "dddd"              full weekday name                     Monday
        "dddd*"             full weekday name                     Monday


        "M"     "0"         month w/o leading zero                2
        "MM"    "00"        month with leading zero               02
        "MMM"               short month name (abbreviation)       Feb
        "MMMM"              full month name                       February
        "MMMM*"             full month name                       February

        "y"     "0"         two digit year (year % 100) w/o leading zero           0
        "yy"    "00"        two digit year (year % 100) with leading zero          00
        "yyy"   "D3"        year                                  2000
        "yyyy"  "D4"        year                                  2000
        "yyyyy" "D5"        year                                  2000
        ...

        "z"     "+0;-0"     timezone offset w/o leading zero      -8
        "zz"    "+00;-00"   timezone offset with leading zero     -08
        "zzz"      "+00;-00" for hour offset, "00" for minute offset  full timezone offset   -07:30
        "zzz*"  "+00;-00" for hour offset, "00" for minute offset   full timezone offset   -08:00

        "K"    -Local       "zzz", e.g. -08:00
               -Utc         "'Z'", representing UTC
               -Unspecified ""
               -DateTimeOffset      "zzzzz" e.g -07:30:15

        "g*"                the current era name                  A.D.

        ":"                 time separator                        : -- DEPRECATED - Insert separator directly into pattern (eg: "H.mm.ss")
        "/"                 date separator                        /-- DEPRECATED - Insert separator directly into pattern (eg: "M-dd-yyyy")
        "'"                 quoted string                         'ABC' will insert ABC into the formatted string.
        '"'                 quoted string                         "ABC" will insert ABC into the formatted string.
        "%"                 used to quote a single pattern characters      E.g.The format character "%y" is to print two digit year.
        "\"                 escaped character                     E.g. '\d' insert the character 'd' into the format string.
        other characters    insert the character into the format string.

    Pre-defined format characters:
        (U) to indicate Universal time is used.
        (G) to indicate Gregorian calendar is used.

        Format              Description                             Real format                             Example
        =========           =================================       ======================                  =======================
        "d"                 short date                              culture-specific                        10/31/1999
        "D"                 long data                               culture-specific                        Sunday, October 31, 1999
        "f"                 full date (long date + short time)      culture-specific                        Sunday, October 31, 1999 2:00 AM
        "F"                 full date (long date + long time)       culture-specific                        Sunday, October 31, 1999 2:00:00 AM
        "g"                 general date (short date + short time)  culture-specific                        10/31/1999 2:00 AM
        "G"                 general date (short date + long time)   culture-specific                        10/31/1999 2:00:00 AM
        "m"/"M"             Month/Day date                          culture-specific                        October 31
(G)     "o"/"O"             Round Trip XML                          "yyyy-MM-ddTHH:mm:ss.fffffffK"          1999-10-31 02:00:00.0000000Z
(G)     "r"/"R"             RFC 1123 date,                          "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"   Sun, 31 Oct 1999 10:00:00 GMT
(G)     "s"                 Sortable format, based on ISO 8601.     "yyyy-MM-dd'T'HH:mm:ss"                 1999-10-31T02:00:00
                                                                    ('T' for local time)
        "t"                 short time                              culture-specific                        2:00 AM
        "T"                 long time                               culture-specific                        2:00:00 AM
(G)     "u"                 Universal time with sortable format,    "yyyy'-'MM'-'dd HH':'mm':'ss'Z'"        1999-10-31 10:00:00Z
                            based on ISO 8601.
(U)     "U"                 Universal time with full                culture-specific                        Sunday, October 31, 1999 10:00:00 AM
                            (long date + long time) format
                            "y"/"Y"             Year/Month day                          culture-specific                        October, 1999

    */

    // This class contains only static members and does not require the serializable attribute.
    internal static class DateTimeFormat
    {
        internal const int MaxSecondsFractionDigits = 7;
        internal const long NullOffset = long.MinValue;

        internal const string AllStandardFormats = "dDfFgGmMoOrRstTuUyY";

        internal const string RoundtripFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";
        internal const string RoundtripDateTimeUnfixed = "yyyy'-'MM'-'ddTHH':'mm':'ss zzz";

        private const int DEFAULT_ALL_DATETIMES_SIZE = 132;

        internal static readonly DateTimeFormatInfo InvariantFormatInfo = CultureInfo.InvariantCulture.DateTimeFormat;

        internal static string[] fixedNumberFormats = new string[] {
            "0",
            "00",
            "000",
            "0000",
            "00000",
            "000000",
            "0000000",
        };

        ////////////////////////////////////////////////////////////////////////////
        //
        // Format the positive integer value to a string and prefix with assigned
        // length of leading zero.
        //
        // Parameters:
        //  value: The value to format
        //  len: The maximum length for leading zero.
        //  If the digits of the value is greater than len, no leading zero is added.
        //
        // Notes:
        //  The function can format to int.MaxValue.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static void FormatDigits<TChar>(ref ValueListBuilder<TChar> outputBuffer, int value, int len) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(value >= 0, "DateTimeFormat.FormatDigits(): value >= 0");
            FormatDigits(ref outputBuffer, value, len, false);
        }

        internal static unsafe void FormatDigits<TChar>(ref ValueListBuilder<TChar> outputBuffer, int value, int len, bool overrideLengthLimit) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(value >= 0, "DateTimeFormat.FormatDigits(): value >= 0");

            // Limit the use of this function to be two-digits, so that we have the same behavior
            // as RTM bits.
            if (!overrideLengthLimit && len > 2)
            {
                len = 2;
            }

            TChar* buffer = stackalloc TChar[16];
            TChar* p = buffer + 16;
            int n = value;
            do
            {
                *--p = TChar.CreateTruncating(n % 10 + '0');
                n /= 10;
            } while ((n != 0) && (p > buffer));

            int digits = (int)(buffer + 16 - p);

            // If the repeat count is greater than 0, we're trying
            // to emulate the "00" format, so we have to prepend
            // a zero if the string only has one character.
            while ((digits < len) && (p > buffer))
            {
                *--p = TChar.CreateTruncating('0');
                digits++;
            }
            new ReadOnlySpan<TChar>(p, digits).CopyTo(outputBuffer.AppendSpan(digits));
        }

        internal static int ParseRepeatPattern(ReadOnlySpan<char> format, int pos, char patternChar)
        {
            int len = format.Length;
            int index = pos + 1;
            while ((index < len) && (format[index] == patternChar))
            {
                index++;
            }
            return index - pos;
        }

        private static string FormatDayOfWeek(int dayOfWeek, int repeat, DateTimeFormatInfo dtfi)
        {
            Debug.Assert(dayOfWeek >= 0 && dayOfWeek <= 6, "dayOfWeek >= 0 && dayOfWeek <= 6");
            if (repeat == 3)
            {
                return dtfi.GetAbbreviatedDayName((DayOfWeek)dayOfWeek);
            }
            // Call dtfi.GetDayName() here, instead of accessing DayNames property, because we don't
            // want a clone of DayNames, which will hurt perf.
            return dtfi.GetDayName((DayOfWeek)dayOfWeek);
        }

        private static string FormatMonth(int month, int repeatCount, DateTimeFormatInfo dtfi)
        {
            Debug.Assert(month >= 1 && month <= 12, "month >=1 && month <= 12");
            if (repeatCount == 3)
            {
                return dtfi.GetAbbreviatedMonthName(month);
            }
            // Call GetMonthName() here, instead of accessing MonthNames property, because we don't
            // want a clone of MonthNames, which will hurt perf.
            return dtfi.GetMonthName(month);
        }

        //
        //  FormatHebrewMonthName
        //
        //  Action: Return the Hebrew month name for the specified DateTime.
        //  Returns: The month name string for the specified DateTime.
        //  Arguments:
        //        time   the time to format
        //        month  The month is the value of HebrewCalendar.GetMonth(time).
        //        repeat Return abbreviated month name if repeat=3, or full month name if repeat=4
        //        dtfi    The DateTimeFormatInfo which uses the Hebrew calendars as its calendar.
        //  Exceptions: None.
        //

        /* Note:
            If DTFI is using Hebrew calendar, GetMonthName()/GetAbbreviatedMonthName() will return month names like this:
            1   Hebrew 1st Month
            2   Hebrew 2nd Month
            ..  ...
            6   Hebrew 6th Month
            7   Hebrew 6th Month II (used only in a leap year)
            8   Hebrew 7th Month
            9   Hebrew 8th Month
            10  Hebrew 9th Month
            11  Hebrew 10th Month
            12  Hebrew 11th Month
            13  Hebrew 12th Month

            Therefore, if we are in a regular year, we have to increment the month name if month is greater or equal to 7.
        */
        private static string FormatHebrewMonthName(DateTime time, int month, int repeatCount, DateTimeFormatInfo dtfi)
        {
            Debug.Assert(repeatCount != 3 || repeatCount != 4, "repeateCount should be 3 or 4");
            if (dtfi.Calendar.IsLeapYear(dtfi.Calendar.GetYear(time)))
            {
                // This month is in a leap year
                return dtfi.InternalGetMonthName(month, MonthNameStyles.LeapYear, repeatCount == 3);
            }
            // This is in a regular year.
            if (month >= 7)
            {
                month++;
            }
            if (repeatCount == 3)
            {
                return dtfi.GetAbbreviatedMonthName(month);
            }
            return dtfi.GetMonthName(month);
        }

        //
        // The pos should point to a quote character. This method will
        // append to the result StringBuilder the string enclosed by the quote character.
        //
        internal static int ParseQuoteString<TChar>(scoped ReadOnlySpan<char> format, int pos, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            //
            // NOTE : pos will be the index of the quote character in the 'format' string.
            //
            int formatLen = format.Length;
            int beginPos = pos;
            char quoteChar = format[pos++]; // Get the character used to quote the following string.

            bool foundQuote = false;
            while (pos < formatLen)
            {
                char ch = format[pos++];
                if (ch == quoteChar)
                {
                    foundQuote = true;
                    break;
                }
                else if (ch == '\\')
                {
                    // The following are used to support escaped character.
                    // Escaped character is also supported in the quoted string.
                    // Therefore, someone can use a format like "'minute:' mm\"" to display:
                    //  minute: 45"
                    // because the second double quote is escaped.
                    if (pos < formatLen)
                    {
                        result.Append(TChar.CreateTruncating(format[pos++]));
                    }
                    else
                    {
                        //
                        // This means that '\' is at the end of the formatting string.
                        //
                        throw new FormatException(SR.Format_InvalidString);
                    }
                }
                else
                {
                    result.Append(TChar.CreateTruncating(ch));
                }
            }

            if (!foundQuote)
            {
                // Here we can't find the matching quote.
                throw new FormatException(SR.Format(SR.Format_BadQuote, quoteChar));
            }

            //
            // Return the character count including the begin/end quote characters and enclosed string.
            //
            return pos - beginPos;
        }

        //
        // Get the next character at the index of 'pos' in the 'format' string.
        // Return value of -1 means 'pos' is already at the end of the 'format' string.
        // Otherwise, return value is the int value of the next character.
        //
        internal static int ParseNextChar(ReadOnlySpan<char> format, int pos)
        {
            if ((uint)(pos + 1) >= (uint)format.Length)
            {
                return -1;
            }
            return format[pos + 1];
        }

        //
        //  IsUseGenitiveForm
        //
        //  Actions: Check the format to see if we should use genitive month in the formatting.
        //      Starting at the position (index) in the (format) string, look back and look ahead to
        //      see if there is "d" or "dd".  In the case like "d MMMM" or "MMMM dd", we can use
        //      genitive form.  Genitive form is not used if there is more than two "d".
        //  Arguments:
        //      format      The format string to be scanned.
        //      index       Where we should start the scanning.  This is generally where "M" starts.
        //      tokenLen    The len of the current pattern character.  This indicates how many "M" that we have.
        //      patternToMatch  The pattern that we want to search. This generally uses "d"
        //
        private static bool IsUseGenitiveForm(ReadOnlySpan<char> format, int index, int tokenLen, char patternToMatch)
        {
            int i;
            int repeat = 0;
            //
            // Look back to see if we can find "d" or "ddd"
            //

            // Find first "d".
            for (i = index - 1; i >= 0 && format[i] != patternToMatch; i--) {  /*Do nothing here */ }

            if (i >= 0)
            {
                // Find a "d", so look back to see how many "d" that we can find.
                while (--i >= 0 && format[i] == patternToMatch)
                {
                    repeat++;
                }
                //
                // repeat == 0 means that we have one (patternToMatch)
                // repeat == 1 means that we have two (patternToMatch)
                //
                if (repeat <= 1)
                {
                    return true;
                }
                // Note that we can't just stop here.  We may find "ddd" while looking back, and we have to look
                // ahead to see if there is "d" or "dd".
            }

            //
            // If we can't find "d" or "dd" by looking back, try look ahead.
            //

            // Find first "d"
            for (i = index + tokenLen; i < format.Length && format[i] != patternToMatch; i++) { /* Do nothing here */ }

            if (i < format.Length)
            {
                repeat = 0;
                // Find a "d", so continue the walk to see how may "d" that we can find.
                while (++i < format.Length && format[i] == patternToMatch)
                {
                    repeat++;
                }
                //
                // repeat == 0 means that we have one (patternToMatch)
                // repeat == 1 means that we have two (patternToMatch)
                //
                if (repeat <= 1)
                {
                    return true;
                }
            }
            return false;
        }

        //
        //  FormatCustomized
        //
        //  Actions: Format the DateTime instance using the specified format.
        //
        private static void FormatCustomized<TChar>(
            DateTime dateTime, scoped ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Calendar cal = dtfi.Calendar;

            // This is a flag to indicate if we are formatting the dates using Hebrew calendar.
            bool isHebrewCalendar = (cal.ID == CalendarId.HEBREW);
            bool isJapaneseCalendar = (cal.ID == CalendarId.JAPAN);
            // This is a flag to indicate if we are formatting hour/minute/second only.
            bool bTimeOnly = true;

            int i = 0;
            int tokenLen, hour12;

            while (i < format.Length)
            {
                char ch = format[i];
                int nextChar;
                switch (ch)
                {
                    case 'g':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        AppendString(ref result, dtfi.GetEraName(cal.GetEra(dateTime)));
                        break;
                    case 'h':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        hour12 = dateTime.Hour % 12;
                        if (hour12 == 0)
                        {
                            hour12 = 12;
                        }
                        FormatDigits(ref result, hour12, tokenLen);
                        break;
                    case 'H':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(ref result, dateTime.Hour, tokenLen);
                        break;
                    case 'm':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(ref result, dateTime.Minute, tokenLen);
                        break;
                    case 's':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(ref result, dateTime.Second, tokenLen);
                        break;
                    case 'f':
                    case 'F':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (tokenLen <= MaxSecondsFractionDigits)
                        {
                            long fraction = (dateTime.Ticks % Calendar.TicksPerSecond);
                            fraction /= (long)Math.Pow(10, 7 - tokenLen);
                            if (ch == 'f')
                            {
                                FormatFraction(ref result, (int)fraction, fixedNumberFormats[tokenLen - 1]);
                            }
                            else
                            {
                                int effectiveDigits = tokenLen;
                                while (effectiveDigits > 0)
                                {
                                    if (fraction % 10 == 0)
                                    {
                                        fraction /= 10;
                                        effectiveDigits--;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (effectiveDigits > 0)
                                {
                                    FormatFraction(ref result, (int)fraction, fixedNumberFormats[effectiveDigits - 1]);
                                }
                                else
                                {
                                    // No fraction to emit, so see if we should remove decimal also.
                                    if (result.Length > 0 && result[result.Length - 1] == TChar.CreateTruncating('.'))
                                    {
                                        result.Length--;
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new FormatException(SR.Format_InvalidString);
                        }
                        break;
                    case 't':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (tokenLen == 1)
                        {
                            string designator = dateTime.Hour < 12 ? dtfi.AMDesignator : dtfi.PMDesignator;
                            if (designator.Length >= 1)
                            {
                                AppendChar(ref result, designator[0]);
                            }
                        }
                        else
                        {
                            AppendString(ref result, dateTime.Hour < 12 ? dtfi.AMDesignator : dtfi.PMDesignator);
                        }
                        break;
                    case 'd':
                        //
                        // tokenLen == 1 : Day of month as digits with no leading zero.
                        // tokenLen == 2 : Day of month as digits with leading zero for single-digit months.
                        // tokenLen == 3 : Day of week as a three-letter abbreviation.
                        // tokenLen >= 4 : Day of week as its full name.
                        //
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (tokenLen <= 2)
                        {
                            int day = cal.GetDayOfMonth(dateTime);
                            if (isHebrewCalendar)
                            {
                                // For Hebrew calendar, we need to convert numbers to Hebrew text for yyyy, MM, and dd values.
                                HebrewNumber.Append(ref result, day);
                            }
                            else
                            {
                                FormatDigits(ref result, day, tokenLen);
                            }
                        }
                        else
                        {
                            int dayOfWeek = (int)cal.GetDayOfWeek(dateTime);
                            AppendString(ref result, FormatDayOfWeek(dayOfWeek, tokenLen, dtfi));
                        }
                        bTimeOnly = false;
                        break;
                    case 'M':
                        //
                        // tokenLen == 1 : Month as digits with no leading zero.
                        // tokenLen == 2 : Month as digits with leading zero for single-digit months.
                        // tokenLen == 3 : Month as a three-letter abbreviation.
                        // tokenLen >= 4 : Month as its full name.
                        //
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        int month = cal.GetMonth(dateTime);
                        if (tokenLen <= 2)
                        {
                            if (isHebrewCalendar)
                            {
                                // For Hebrew calendar, we need to convert numbers to Hebrew text for yyyy, MM, and dd values.
                                HebrewNumber.Append(ref result, month);
                            }
                            else
                            {
                                FormatDigits(ref result, month, tokenLen);
                            }
                        }
                        else
                        {
                            if (isHebrewCalendar)
                            {
                                AppendString(ref result, FormatHebrewMonthName(dateTime, month, tokenLen, dtfi));
                            }
                            else
                            {
                                if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
                                {
                                    AppendString(ref result,
                                        dtfi.InternalGetMonthName(
                                            month,
                                            IsUseGenitiveForm(format, i, tokenLen, 'd') ? MonthNameStyles.Genitive : MonthNameStyles.Regular,
                                            tokenLen == 3));
                                }
                                else
                                {
                                    AppendString(ref result, FormatMonth(month, tokenLen, dtfi));
                                }
                            }
                        }
                        bTimeOnly = false;
                        break;
                    case 'y':
                        // Notes about OS behavior:
                        // y: Always print (year % 100). No leading zero.
                        // yy: Always print (year % 100) with leading zero.
                        // yyy/yyyy/yyyyy/... : Print year value.  No leading zero.

                        int year = cal.GetYear(dateTime);
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (isJapaneseCalendar &&
                            !LocalAppContextSwitches.FormatJapaneseFirstYearAsANumber &&
                            year == 1 &&
                            ((i + tokenLen < format.Length && format[i + tokenLen] == DateTimeFormatInfoScanner.CJKYearSuff) ||
                            (i + tokenLen < format.Length - 1 && format[i + tokenLen] == '\'' && format[i + tokenLen + 1] == DateTimeFormatInfoScanner.CJKYearSuff)))
                        {
                            // We are formatting a Japanese date with year equals 1 and the year number is followed by the year sign \u5e74
                            // In Japanese dates, the first year in the era is not formatted as a number 1 instead it is formatted as \u5143 which means
                            // first or beginning of the era.
                            AppendChar(ref result, DateTimeFormatInfo.JapaneseEraStart[0]);
                        }
                        else if (dtfi.HasForceTwoDigitYears)
                        {
                            FormatDigits(ref result, year, tokenLen <= 2 ? tokenLen : 2);
                        }
                        else if (cal.ID == CalendarId.HEBREW)
                        {
                            HebrewNumber.Append(ref result, year);
                        }
                        else
                        {
                            if (tokenLen <= 2)
                            {
                                FormatDigits(ref result, year % 100, tokenLen);
                            }
                            else if (tokenLen <= 16) // FormatDigits has an implicit 16-digit limit
                            {
                                FormatDigits(ref result, year, tokenLen, overrideLengthLimit: true);
                            }
                            else
                            {
                                AppendString(ref result, year.ToString("D" + tokenLen.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
                            }
                        }
                        bTimeOnly = false;
                        break;
                    case 'z':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatCustomizedTimeZone(dateTime, offset, tokenLen, bTimeOnly, ref result);
                        break;
                    case 'K':
                        tokenLen = 1;
                        FormatCustomizedRoundripTimeZone(dateTime, offset, ref result);
                        break;
                    case ':':
                        AppendString(ref result, dtfi.TimeSeparator);
                        tokenLen = 1;
                        break;
                    case '/':
                        AppendString(ref result, dtfi.DateSeparator);
                        tokenLen = 1;
                        break;
                    case '\'':
                    case '\"':
                        tokenLen = ParseQuoteString(format, i, ref result);
                        break;
                    case '%':
                        // Optional format character.
                        // For example, format string "%d" will print day of month
                        // without leading zero.  Most of the cases, "%" can be ignored.
                        nextChar = ParseNextChar(format, i);
                        // nextChar will be -1 if we have already reached the end of the format string.
                        // Besides, we will not allow "%%" to appear in the pattern.
                        if (nextChar >= 0 && nextChar != '%')
                        {
                            char nextCharChar = (char)nextChar;
                            FormatCustomized(dateTime, new ReadOnlySpan<char>(in nextCharChar), dtfi, offset, ref result);
                            tokenLen = 2;
                        }
                        else
                        {
                            //
                            // This means that '%' is at the end of the format string or
                            // "%%" appears in the format string.
                            //
                            throw new FormatException(SR.Format_InvalidString);
                        }
                        break;
                    case '\\':
                        // Escaped character.  Can be used to insert a character into the format string.
                        // For example, "\d" will insert the character 'd' into the string.
                        //
                        // NOTENOTE : we can remove this format character if we enforce the enforced quote
                        // character rule.
                        // That is, we ask everyone to use single quote or double quote to insert characters,
                        // then we can remove this character.
                        //
                        nextChar = ParseNextChar(format, i);
                        if (nextChar >= 0)
                        {
                            result.Append(TChar.CreateTruncating(nextChar));
                            tokenLen = 2;
                        }
                        else
                        {
                            //
                            // This means that '\' is at the end of the formatting string.
                            //
                            throw new FormatException(SR.Format_InvalidString);
                        }
                        break;
                    default:
                        // NOTENOTE : we can remove this rule if we enforce the enforced quote
                        // character rule.
                        // That is, if we ask everyone to use single quote or double quote to insert characters,
                        // then we can remove this default block.
                        result.Append(TChar.CreateTruncating(ch));
                        tokenLen = 1;
                        break;
                }
                i += tokenLen;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AppendChar<TChar>(ref ValueListBuilder<TChar> result, char ch) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (typeof(TChar) == typeof(char) || char.IsAscii(ch))
            {
                result.Append(TChar.CreateTruncating(ch));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));
                var r = new Rune(ch);
                r.EncodeToUtf8(MemoryMarshal.AsBytes(result.AppendSpan(r.Utf8SequenceLength)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendString<TChar>(ref ValueListBuilder<TChar> result, scoped ReadOnlySpan<char> s) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (typeof(TChar) == typeof(char))
            {
                result.Append(MemoryMarshal.Cast<char, TChar>(s));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));
                Encoding.UTF8.GetBytes(s, MemoryMarshal.Cast<TChar, byte>(result.AppendSpan(Encoding.UTF8.GetByteCount(s))));
            }
        }

        internal static void FormatFraction<TChar>(ref ValueListBuilder<TChar> result, int fraction, ReadOnlySpan<char> fractionFormat) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // TODO https://github.com/dotnet/runtime/issues/84527: Update when Int32 implements IUtf8SpanFormattable
            Span<char> chars = stackalloc char[11];
            fraction.TryFormat(chars, out int charsWritten, fractionFormat, CultureInfo.InvariantCulture);
            Debug.Assert(charsWritten != 0);
            AppendString(ref result, chars.Slice(0, charsWritten));
        }

        // output the 'z' family of formats, which output a the offset from UTC, e.g. "-07:30"
        private static void FormatCustomizedTimeZone<TChar>(DateTime dateTime, TimeSpan offset, int tokenLen, bool timeOnly, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // See if the instance already has an offset
            bool dateTimeFormat = (offset.Ticks == NullOffset);
            if (dateTimeFormat)
            {
                // No offset. The instance is a DateTime and the output should be the local time zone

                if (timeOnly && dateTime.Ticks < Calendar.TicksPerDay)
                {
                    // For time only format and a time only input, the time offset on 0001/01/01 is less
                    // accurate than the system's current offset because of daylight saving time.
                    offset = TimeZoneInfo.GetLocalUtcOffset(DateTime.Now, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                }
                else if (dateTime.Kind == DateTimeKind.Utc)
                {
                    offset = default; // TimeSpan.Zero
                }
                else
                {
                    offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                }
            }
            if (offset.Ticks >= 0)
            {
                result.Append(TChar.CreateTruncating('+'));
            }
            else
            {
                result.Append(TChar.CreateTruncating('-'));
                // get a positive offset, so that you don't need a separate code path for the negative numbers.
                offset = offset.Negate();
            }

            if (tokenLen <= 1)
            {
                // 'z' format e.g "-7"
                (int tens, int ones) = Math.DivRem(offset.Hours, 10);
                if (tens != 0)
                {
                    result.Append(TChar.CreateTruncating('0' + tens));
                }
                result.Append(TChar.CreateTruncating('0' + ones));
            }
            else
            {
                // 'zz' or longer format e.g "-07"
                FormattingHelpers.WriteTwoDigits((uint)offset.Hours, result.AppendSpan(2), 0);
                if (tokenLen >= 3)
                {
                    result.Append(TChar.CreateTruncating(':'));
                    FormattingHelpers.WriteTwoDigits((uint)offset.Minutes, result.AppendSpan(2), 0);
                }
            }
        }

        // output the 'K' format, which is for round-tripping the data
        private static void FormatCustomizedRoundripTimeZone<TChar>(DateTime dateTime, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // The objective of this format is to round trip the data in the type
            // For DateTime it should round-trip the Kind value and preserve the time zone.
            // DateTimeOffset instance, it should do so by using the internal time zone.

            if (offset.Ticks == NullOffset)
            {
                // source is a date time, so behavior depends on the kind.
                switch (dateTime.Kind)
                {
                    case DateTimeKind.Local:
                        // This should output the local offset, e.g. "-07:30"
                        offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                        // fall through to shared time zone output code
                        break;
                    case DateTimeKind.Utc:
                        // The 'Z' constant is a marker for a UTC date
                        result.Append(TChar.CreateTruncating('Z'));
                        return;
                    default:
                        // If the kind is unspecified, we output nothing here
                        return;
                }
            }
            if (offset.Ticks >= 0)
            {
                result.Append(TChar.CreateTruncating('+'));
            }
            else
            {
                result.Append(TChar.CreateTruncating('-'));
                // get a positive offset, so that you don't need a separate code path for the negative numbers.
                offset = offset.Negate();
            }

            Span<TChar> hoursMinutes = result.AppendSpan(5);
            FormattingHelpers.WriteTwoDigits((uint)offset.Hours, hoursMinutes, 0);
            hoursMinutes[2] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)offset.Minutes, hoursMinutes, 3);
        }

        internal static string GetRealFormat(ReadOnlySpan<char> format, DateTimeFormatInfo dtfi)
        {
            string realFormat;

            switch (format[0])
            {
                case 'd':       // Short Date
                    realFormat = dtfi.ShortDatePattern;
                    break;
                case 'D':       // Long Date
                    realFormat = dtfi.LongDatePattern;
                    break;
                case 'f':       // Full (long date + short time)
                    realFormat = dtfi.LongDatePattern + " " + dtfi.ShortTimePattern;
                    break;
                case 'F':       // Full (long date + long time)
                    realFormat = dtfi.FullDateTimePattern;
                    break;
                case 'g':       // General (short date + short time)
                    realFormat = dtfi.GeneralShortTimePattern;
                    break;
                case 'G':       // General (short date + long time)
                    realFormat = dtfi.GeneralLongTimePattern;
                    break;
                case 'm':
                case 'M':       // Month/Day Date
                    realFormat = dtfi.MonthDayPattern;
                    break;
                case 'o':
                case 'O':
                    realFormat = RoundtripFormat;
                    break;
                case 'r':
                case 'R':       // RFC 1123 Standard
                    realFormat = dtfi.RFC1123Pattern;
                    break;
                case 's':       // Sortable without Time Zone Info
                    realFormat = dtfi.SortableDateTimePattern;
                    break;
                case 't':       // Short Time
                    realFormat = dtfi.ShortTimePattern;
                    break;
                case 'T':       // Long Time
                    realFormat = dtfi.LongTimePattern;
                    break;
                case 'u':       // Universal with Sortable format
                    realFormat = dtfi.UniversalSortableDateTimePattern;
                    break;
                case 'U':       // Universal with Full (long date + long time) format
                    realFormat = dtfi.FullDateTimePattern;
                    break;
                case 'y':
                case 'Y':       // Year/Month Date
                    realFormat = dtfi.YearMonthPattern;
                    break;
                default:
                    throw new FormatException(SR.Format_InvalidString);
            }
            return realFormat;
        }

        // Expand a pre-defined format string (like "D" for long date) to the real format that
        // we are going to use in the date time parsing.
        // This method also convert the dateTime if necessary (e.g. when the format is in Universal time),
        // and change dtfi if necessary (e.g. when the format should use invariant culture).
        //
        private static string ExpandPredefinedFormat(ReadOnlySpan<char> format, ref DateTime dateTime, ref DateTimeFormatInfo dtfi, TimeSpan offset)
        {
            switch (format[0])
            {
                case 'o':
                case 'O':       // Round trip format
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 'r':
                case 'R':       // RFC 1123 Standard
                case 'u':       // Universal time in sortable format.
                    if (offset.Ticks != NullOffset)
                    {
                        // Convert to UTC invariants mean this will be in range
                        dateTime -= offset;
                    }
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 's':       // Sortable without Time Zone Info
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 'U':       // Universal time in culture dependent format.
                    if (offset.Ticks != NullOffset)
                    {
                        // This format is not supported by DateTimeOffset
                        throw new FormatException(SR.Format_InvalidString);
                    }
                    // Universal time is always in Gregorian calendar.
                    //
                    // Change the Calendar to be Gregorian Calendar.
                    //
                    dtfi = (DateTimeFormatInfo)dtfi.Clone();
                    if (dtfi.Calendar.GetType() != typeof(GregorianCalendar))
                    {
                        dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
                    }
                    dateTime = dateTime.ToUniversalTime();
                    break;
            }
            return GetRealFormat(format, dtfi);
        }

        internal static string Format(DateTime dateTime, string? format, IFormatProvider? provider)
        {
            return Format(dateTime, format, provider, new TimeSpan(NullOffset));
        }

        internal static string Format(DateTime dateTime, string? format, IFormatProvider? provider, TimeSpan offset)
        {
            if (format != null && format.Length == 1)
            {
                // Optimize for these standard formats that are not affected by culture.
                switch ((char)(format[0] | 0x20))
                {
                    // Round trip format
                    case 'o':
                        const int MinFormatOLength = 27, MaxFormatOLength = 33;
                        Span<char> span = stackalloc char[MaxFormatOLength];
                        TryFormatO(dateTime, offset, span, out int ochars);
                        Debug.Assert(ochars >= MinFormatOLength && ochars <= MaxFormatOLength);
                        return span.Slice(0, ochars).ToString();

                    // RFC1123
                    case 'r':
                        const int FormatRLength = 29;
                        string str = string.FastAllocateString(FormatRLength);
                        TryFormatR(dateTime, offset, new Span<char>(ref str.GetRawStringData(), str.Length), out int rchars);
                        Debug.Assert(rchars == str.Length);
                        return str;
                }
            }

            var vlb = new ValueListBuilder<char>(stackalloc char[256]);
            FormatIntoBuilder(dateTime, format, DateTimeFormatInfo.GetInstance(provider), offset, ref vlb);
            string resultString = vlb.AsSpan().ToString();
            vlb.Dispose();
            return resultString;
        }

        internal static bool TryFormat<TChar>(DateTime dateTime, Span<TChar> destination, out int written, ReadOnlySpan<char> format, IFormatProvider? provider) where TChar : unmanaged, IBinaryInteger<TChar> =>
            TryFormat(dateTime, destination, out written, format, provider, new TimeSpan(NullOffset));

        internal static bool TryFormat<TChar>(DateTime dateTime, Span<TChar> destination, out int written, ReadOnlySpan<char> format, IFormatProvider? provider, TimeSpan offset) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (format.Length == 1)
            {
                // Optimize for these standard formats that are not affected by culture.
                switch ((char)(format[0] | 0x20))
                {
                    // Round trip format
                    case 'o':
                        return TryFormatO(dateTime, offset, destination, out written);

                    // RFC1123
                    case 'r':
                        return TryFormatR(dateTime, offset, destination, out written);
                }
            }

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[256]);
            FormatIntoBuilder(dateTime, format, DateTimeFormatInfo.GetInstance(provider), offset, ref vlb);
            bool copied = vlb.TryCopyTo(destination, out written);
            vlb.Dispose();
            return copied;
        }

        private static void FormatIntoBuilder<TChar>(DateTime dateTime, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(dtfi != null);
            if (format.Length == 0)
            {
                bool timeOnlySpecialCase = false;
                if (dateTime.Ticks < Calendar.TicksPerDay)
                {
                    // If the time is less than 1 day, consider it as time of day.
                    // Just print out the short time format.
                    //
                    // This is a workaround for VB, since they use ticks less then one day to be
                    // time of day.  In cultures which use calendar other than Gregorian calendar, these
                    // alternative calendar may not support ticks less than a day.
                    // For example, Japanese calendar only supports date after 1868/9/8.
                    // This will pose a problem when people in VB get the time of day, and use it
                    // to call ToString(), which will use the general format (short date + long time).
                    // Since Japanese calendar does not support Gregorian year 0001, an exception will be
                    // thrown when we try to get the Japanese year for Gregorian year 0001.
                    // Therefore, the workaround allows them to call ToString() for time of day from a DateTime by
                    // formatting as ISO 8601 format.
                    switch (dtfi.Calendar.ID)
                    {
                        case CalendarId.JAPAN:
                        case CalendarId.TAIWAN:
                        case CalendarId.HIJRI:
                        case CalendarId.HEBREW:
                        case CalendarId.JULIAN:
                        case CalendarId.UMALQURA:
                        case CalendarId.PERSIAN:
                            timeOnlySpecialCase = true;
                            dtfi = DateTimeFormatInfo.InvariantInfo;
                            break;
                    }
                }
                if (offset.Ticks == NullOffset)
                {
                    // Default DateTime.ToString case.
                    format = timeOnlySpecialCase ? "s" : "G";
                }
                else
                {
                    // Default DateTimeOffset.ToString case.
                    format = timeOnlySpecialCase ? RoundtripDateTimeUnfixed : dtfi.DateTimeOffsetPattern;
                }
            }

            if (format.Length == 1)
            {
                format = ExpandPredefinedFormat(format, ref dateTime, ref dtfi, offset);
            }

            FormatCustomized(dateTime, format, dtfi, offset, ref result);
        }

        internal static bool IsValidCustomDateFormat(ReadOnlySpan<char> format, bool throwOnError)
        {
            int i = 0;

            while (i < format.Length)
            {
                switch (format[i])
                {
                    case '\\':
                        if (i == format.Length - 1)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException(SR.Format_InvalidString);
                            }

                            return false;
                        }

                        i += 2;
                        break;

                    case '\'':
                    case '"':
                        char quoteChar = format[i++];
                        while (i < format.Length && format[i] != quoteChar)
                        {
                            i++;
                        }

                        if (i >= format.Length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException(SR.Format(SR.Format_BadQuote, quoteChar));
                            }

                            return false;
                        }

                        i++;
                        break;

                    case ':':
                    case 't':
                    case 'f':
                    case 'F':
                    case 'h':
                    case 'H':
                    case 'm':
                    case 's':
                    case 'z':
                    case 'K':
                        // reject non-date formats
                        if (throwOnError)
                        {
                            throw new FormatException(SR.Format_InvalidString);
                        }

                        return false;

                    default:
                        i++;
                        break;
                }
            }

            return true;
        }


        internal static bool IsValidCustomTimeFormat(ReadOnlySpan<char> format, bool throwOnError)
        {
            int length = format.Length;
            int i = 0;

            while (i < length)
            {
                switch (format[i])
                {
                    case '\\':
                        if (i == length - 1)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException(SR.Format_InvalidString);
                            }

                            return false;
                        }

                        i += 2;
                        break;

                    case '\'':
                    case '"':
                        char quoteChar = format[i++];
                        while (i < length && format[i] != quoteChar)
                        {
                            i++;
                        }

                        if (i >= length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException(SR.Format(SR.Format_BadQuote, quoteChar));
                            }

                            return false;
                        }

                        i++;
                        break;

                    case 'd':
                    case 'M':
                    case 'y':
                    case '/':
                    case 'z':
                    case 'k':
                        if (throwOnError)
                        {
                            throw new FormatException(SR.Format_InvalidString);
                        }

                        return false;

                    default:
                        i++;
                        break;
                }
            }

            return true;
        }

        //   012345678901234567890123456789012
        //   ---------------------------------
        //   05:30:45.7680000
        internal static bool TryFormatTimeOnlyO<TChar>(int hour, int minute, int second, long fraction, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (destination.Length < 16)
            {
                return false;
            }

            FormattingHelpers.WriteTwoDigits((uint)hour, destination, 0);
            destination[2] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)minute, destination, 3);
            destination[5] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)second, destination, 6);
            destination[8] = TChar.CreateTruncating('.');
            FormattingHelpers.WriteDigits((uint)fraction, destination.Slice(9, 7));

            return true;
        }

        //   012345678901234567890123456789012
        //   ---------------------------------
        //   05:30:45
        internal static bool TryFormatTimeOnlyR<TChar>(int hour, int minute, int second, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (destination.Length < 8)
            {
                return false;
            }

            FormattingHelpers.WriteTwoDigits((uint)hour, destination, 0);
            destination[2] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)minute, destination, 3);
            destination[5] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)second, destination, 6);

            return true;
        }

        // Roundtrippable format. One of
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12
        internal static bool TryFormatDateOnlyO<TChar>(int year, int month, int day, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (destination.Length < 10)
            {
                return false;
            }

            FormattingHelpers.WriteFourDigits((uint)year, destination, 0);
            destination[4] = TChar.CreateTruncating('-');
            FormattingHelpers.WriteTwoDigits((uint)month, destination, 5);
            destination[7] = TChar.CreateTruncating('-');
            FormattingHelpers.WriteTwoDigits((uint)day, destination, 8);
            return true;
        }

        // Rfc1123
        //   01234567890123456789012345678
        //   -----------------------------
        //   Tue, 03 Jan 2017
        internal static bool TryFormatDateOnlyR<TChar>(DayOfWeek dayOfWeek, int year, int month, int day, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert((uint)dayOfWeek < 7);

            if (destination.Length < 16)
            {
                return false;
            }

            if (typeof(TChar) == typeof(char))
            {
                Span<char> dest = MemoryMarshal.Cast<TChar, char>(destination);

                FormattingHelpers.CopyFour("Sun,Mon,Tue,Wed,Thu,Fri,Sat,".AsSpan(4 * (int)dayOfWeek), dest);
                dest[4] = ' ';
                FormattingHelpers.WriteTwoDigits((uint)day, dest, 5);
                dest[7] = ' ';
                FormattingHelpers.CopyFour("Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec ".AsSpan(4 * (month - 1)), dest.Slice(8));
                FormattingHelpers.WriteFourDigits((uint)year, dest, 12);
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));
                Span<byte> dest = MemoryMarshal.Cast<TChar, byte>(destination);

                FormattingHelpers.CopyFour("Sun,Mon,Tue,Wed,Thu,Fri,Sat,"u8.Slice(4 * (int)dayOfWeek), dest);
                dest[4] = (byte)' ';
                FormattingHelpers.WriteTwoDigits((uint)day, dest, 5);
                dest[7] = (byte)' ';
                FormattingHelpers.CopyFour("Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec "u8.Slice(4 * (month - 1)), dest.Slice(8));
                FormattingHelpers.WriteFourDigits((uint)year, dest, 12);
            }

            return true;
        }

        // Roundtrippable format. One of
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12T05:30:45.7680000-07:00
        //   2017-06-12T05:30:45.7680000Z           (Z is short for "+00:00" but also distinguishes DateTimeKind.Utc from DateTimeKind.Local)
        //   2017-06-12T05:30:45.7680000            (interpreted as local time wrt to current time zone)
        internal static bool TryFormatO<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            const int MinimumBytesNeeded = 27;

            int charsRequired = MinimumBytesNeeded;
            DateTimeKind kind = DateTimeKind.Local;

            if (offset.Ticks == NullOffset)
            {
                kind = dateTime.Kind;
                if (kind == DateTimeKind.Local)
                {
                    offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
                    charsRequired += 6;
                }
                else if (kind == DateTimeKind.Utc)
                {
                    charsRequired++;
                }
            }
            else
            {
                charsRequired += 6;
            }

            if (destination.Length < charsRequired)
            {
                charsWritten = 0;
                return false;
            }
            charsWritten = charsRequired;

            // Hoist most of the bounds checks on destination.
            { _ = destination[MinimumBytesNeeded - 1]; }

            dateTime.GetDate(out int year, out int month, out int day);
            dateTime.GetTimePrecise(out int hour, out int minute, out int second, out int tick);

            FormattingHelpers.WriteFourDigits((uint)year, destination, 0);
            destination[4] = TChar.CreateTruncating('-');
            FormattingHelpers.WriteTwoDigits((uint)month, destination, 5);
            destination[7] = TChar.CreateTruncating('-');
            FormattingHelpers.WriteTwoDigits((uint)day, destination, 8);
            destination[10] = TChar.CreateTruncating('T');
            FormattingHelpers.WriteTwoDigits((uint)hour, destination, 11);
            destination[13] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)minute, destination, 14);
            destination[16] = TChar.CreateTruncating(':');
            FormattingHelpers.WriteTwoDigits((uint)second, destination, 17);
            destination[19] = TChar.CreateTruncating('.');
            FormattingHelpers.WriteDigits((uint)tick, destination.Slice(20, 7));

            if (kind == DateTimeKind.Local)
            {
                int offsetTotalMinutes = (int)(offset.Ticks / TimeSpan.TicksPerMinute);

                char sign = '+';
                if (offsetTotalMinutes < 0)
                {
                    sign = '-';
                    offsetTotalMinutes = -offsetTotalMinutes;
                }

                int offsetHours = Math.DivRem(offsetTotalMinutes, 60, out int offsetMinutes);

                // Writing the value backward allows the JIT to optimize by
                // performing a single bounds check against buffer.
                FormattingHelpers.WriteTwoDigits((uint)offsetMinutes, destination, 31);
                destination[30] = TChar.CreateTruncating(':');
                FormattingHelpers.WriteTwoDigits((uint)offsetHours, destination, 28);
                destination[27] = TChar.CreateTruncating(sign);
            }
            else if (kind == DateTimeKind.Utc)
            {
                destination[27] = TChar.CreateTruncating('Z');
            }

            return true;
        }

        // Rfc1123
        //   01234567890123456789012345678
        //   -----------------------------
        //   Tue, 03 Jan 2017 08:08:05 GMT
        internal static bool TryFormatR<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (destination.Length <= 28)
            {
                charsWritten = 0;
                return false;
            }

            if (offset.Ticks != NullOffset)
            {
                // Convert to UTC invariants.
                dateTime -= offset;
            }

            dateTime.GetDate(out int year, out int month, out int day);
            dateTime.GetTime(out int hour, out int minute, out int second);

            if (typeof(TChar) == typeof(char))
            {
                Span<char> dest = MemoryMarshal.Cast<TChar, char>(destination);

                FormattingHelpers.CopyFour("Sun,Mon,Tue,Wed,Thu,Fri,Sat,".AsSpan(4 * (int)dateTime.DayOfWeek), dest);
                dest[4] = ' ';
                FormattingHelpers.WriteTwoDigits((uint)day, dest, 5);
                dest[7] = ' ';
                FormattingHelpers.CopyFour("Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec ".AsSpan(4 * (month - 1)), dest.Slice(8));
                FormattingHelpers.WriteFourDigits((uint)year, dest, 12);
                dest[16] = ' ';
                FormattingHelpers.WriteTwoDigits((uint)hour, dest, 17);
                dest[19] = ':';
                FormattingHelpers.WriteTwoDigits((uint)minute, dest, 20);
                dest[22] = ':';
                FormattingHelpers.WriteTwoDigits((uint)second, dest, 23);
                FormattingHelpers.CopyFour(" GMT", dest.Slice(25));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));
                Span<byte> dest = MemoryMarshal.Cast<TChar, byte>(destination);

                FormattingHelpers.CopyFour("Sun,Mon,Tue,Wed,Thu,Fri,Sat,"u8.Slice(4 * (int)dateTime.DayOfWeek), dest);
                dest[4] = (byte)' ';
                FormattingHelpers.WriteTwoDigits((uint)day, dest, 5);
                dest[7] = (byte)' ';
                FormattingHelpers.CopyFour("Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec "u8.Slice(4 * (month - 1)), dest.Slice(8));
                FormattingHelpers.WriteFourDigits((uint)year, dest, 12);
                dest[16] = (byte)' ';
                FormattingHelpers.WriteTwoDigits((uint)hour, dest, 17);
                dest[19] = (byte)':';
                FormattingHelpers.WriteTwoDigits((uint)minute, dest, 20);
                dest[22] = (byte)':';
                FormattingHelpers.WriteTwoDigits((uint)second, dest, 23);
                FormattingHelpers.CopyFour(" GMT"u8, dest.Slice(25));
            }

            charsWritten = 29;
            return true;
        }

        internal static string[] GetAllDateTimes(DateTime dateTime, char format, DateTimeFormatInfo dtfi)
        {
            Debug.Assert(dtfi != null);
            string[] allFormats;
            string[] results;

            switch (format)
            {
                case 'd':
                case 'D':
                case 'f':
                case 'F':
                case 'g':
                case 'G':
                case 'm':
                case 'M':
                case 't':
                case 'T':
                case 'y':
                case 'Y':
                    allFormats = dtfi.GetAllDateTimePatterns(format);
                    results = new string[allFormats.Length];
                    for (int i = 0; i < allFormats.Length; i++)
                    {
                        results[i] = Format(dateTime, allFormats[i], dtfi);
                    }
                    break;
                case 'U':
                    DateTime universalTime = dateTime.ToUniversalTime();
                    allFormats = dtfi.GetAllDateTimePatterns(format);
                    results = new string[allFormats.Length];
                    for (int i = 0; i < allFormats.Length; i++)
                    {
                        results[i] = Format(universalTime, allFormats[i], dtfi);
                    }
                    break;
                //
                // The following ones are special cases because these patterns are read-only in
                // DateTimeFormatInfo.
                //
                case 'r':
                case 'R':
                case 'o':
                case 'O':
                case 's':
                case 'u':
                    results = new string[] { Format(dateTime, char.ToString(format), dtfi) };
                    break;
                default:
                    throw new FormatException(SR.Format_InvalidString);
            }
            return results;
        }

        internal static string[] GetAllDateTimes(DateTime dateTime, DateTimeFormatInfo dtfi)
        {
            List<string> results = new List<string>(DEFAULT_ALL_DATETIMES_SIZE);

            foreach (char standardFormat in AllStandardFormats)
            {
                foreach (string dateTimes in GetAllDateTimes(dateTime, standardFormat, dtfi))
                {
                    results.Add(dateTimes);
                }
            }

            return results.ToArray();
        }
    }
}
