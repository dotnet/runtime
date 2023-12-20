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
        "yyy"   "D3"        year with leading zeroes              2000
        "yyyy"  "D4"        year with leading zeroes              2000
        "yyyyy" "D5"        year with leading zeroes              02000
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

        private const int FormatOMinLength = 27, FormatOMaxLength = 33;
        private const int FormatInvariantGMinLength = 19, FormatInvariantGMaxLength = 26;
        internal const int FormatRLength = 29;
        private const int FormatSLength = 19;
        private const int FormatuLength = 20;

        private const int DEFAULT_ALL_DATETIMES_SIZE = 132;

        internal static readonly DateTimeFormatInfo InvariantFormatInfo = CultureInfo.InvariantCulture.DateTimeFormat;
        private static readonly string[] s_invariantAbbreviatedMonthNames = InvariantFormatInfo.AbbreviatedMonthNames;
        private static readonly string[] s_invariantAbbreviatedDayNames = InvariantFormatInfo.AbbreviatedDayNames;

        internal static string[] fixedNumberFormats = new string[] {
            "0",
            "00",
            "000",
            "0000",
            "00000",
            "000000",
            "0000000",
        };

        /// <summary>Format the positive integer value to a string and prefix with assigned length of leading zero.</summary>
        /// <typeparam name="TChar">The type of the character.</typeparam>
        /// <param name="outputBuffer">The buffer into which to write the digits.</param>
        /// <param name="value">The value to format</param>
        /// <param name="minimumLength">
        /// The minimum length for formatted number. If the number of digits in the value is less than this length, it will be padded with leading zeros.
        /// </param>
        internal static unsafe void FormatDigits<TChar>(ref ValueListBuilder<TChar> outputBuffer, int value, int minimumLength) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(value >= 0, "DateTimeFormat.FormatDigits(): value >= 0");
            Debug.Assert(minimumLength <= 16);

            switch (minimumLength)
            {
                case 1 when value < 10:
                    outputBuffer.Append(TChar.CreateTruncating(value + '0'));
                    break;

                case 2 when value < 100:
                    fixed (TChar* ptr = &MemoryMarshal.GetReference(outputBuffer.AppendSpan(2)))
                    {
                        Number.WriteTwoDigits((uint)value, ptr);
                    }
                    break;

                case 4 when value < 10000:
                    fixed (TChar* ptr = &MemoryMarshal.GetReference(outputBuffer.AppendSpan(4)))
                    {
                        Number.WriteFourDigits((uint)value, ptr);
                    }
                    break;

                default:
                    TChar* buffer = stackalloc TChar[16];
                    TChar* p = Number.UInt32ToDecChars(buffer + 16, (uint)value, minimumLength);
                    outputBuffer.Append(new ReadOnlySpan<TChar>(p, (int)(buffer + 16 - p)));
                    break;
            }
        }

        internal static int ParseRepeatPattern(ReadOnlySpan<char> format, int pos, char patternChar)
        {
            int index = pos + 1;
            while ((uint)index < (uint)format.Length && format[index] == patternChar)
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
        internal static int ParseQuoteString<TChar>(scoped ReadOnlySpan<char> format, int pos, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
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
                        result.Append(TChar.CastFrom(format[pos++]));
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
                    AppendChar(ref result, ch);
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
            DateTime dateTime, scoped ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
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
                        hour12 = dateTime.Hour;
                        if (hour12 > 12)
                        {
                            hour12 -= 12;
                        }
                        else if (hour12 == 0)
                        {
                            hour12 = 12;
                        }
                        FormatDigits(ref result, hour12, Math.Min(tokenLen, 2));
                        break;

                    case 'H':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(ref result, dateTime.Hour, Math.Min(tokenLen, 2));
                        break;

                    case 'm':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(ref result, dateTime.Minute, Math.Min(tokenLen, 2));
                        break;

                    case 's':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(ref result, dateTime.Second, Math.Min(tokenLen, 2));
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
                                    if (result.Length > 0 && result[^1] == TChar.CastFrom('.'))
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
                            result.Append(dateTime.Hour < 12 ? dtfi.AMDesignatorTChar<TChar>() : dtfi.PMDesignatorTChar<TChar>());
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
                        // tokenLen == 1 : Month as digits with no leading zero.
                        // tokenLen == 2 : Month as digits with leading zero for single-digit months.
                        // tokenLen == 3 : Month as a three-letter abbreviation.
                        // tokenLen >= 4 : Month as its full name.
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
                        // yyy/yyyy/yyyyy/... : Print year value.  With leading zeros.

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
                            FormatDigits(ref result, year, Math.Min(tokenLen, 2));
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
                                FormatDigits(ref result, year, tokenLen);
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
                        result.Append(dtfi.TimeSeparatorTChar<TChar>());
                        tokenLen = 1;
                        break;

                    case '/':
                        result.Append(dtfi.DateSeparatorTChar<TChar>());
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
                            result.Append(TChar.CastFrom(nextChar));
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
                        // NOTENOTE : we can remove this rule if we enforce the enforced quote character rule.
                        // That is, if we ask everyone to use single quote or double quote to insert characters,
                        // then we can remove this default block.
                        AppendChar(ref result, ch);
                        tokenLen = 1;
                        break;
                }
                i += tokenLen;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AppendChar<TChar>(ref ValueListBuilder<TChar> result, char ch) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (typeof(TChar) == typeof(char) || char.IsAscii(ch))
            {
                result.Append(TChar.CastFrom(ch));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));
                var r = new Rune(ch);
                r.EncodeToUtf8(MemoryMarshal.AsBytes(result.AppendSpan(r.Utf8SequenceLength)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendString<TChar>(ref ValueListBuilder<TChar> result, scoped ReadOnlySpan<char> s) where TChar : unmanaged, IUtfChar<TChar>
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

        internal static void FormatFraction<TChar>(ref ValueListBuilder<TChar> result, int fraction, ReadOnlySpan<char> fractionFormat) where TChar : unmanaged, IUtfChar<TChar>
        {
            Span<TChar> chars = stackalloc TChar[11];
            int charCount;
            bool formatted = typeof(TChar) == typeof(char) ?
                fraction.TryFormat(MemoryMarshal.Cast<TChar, char>(chars), out charCount, fractionFormat, CultureInfo.InvariantCulture) :
                fraction.TryFormat(MemoryMarshal.Cast<TChar, byte>(chars), out charCount, fractionFormat, CultureInfo.InvariantCulture);
            Debug.Assert(charCount != 0);
            result.Append(chars.Slice(0, charCount));
        }

        // output the 'z' family of formats, which output a the offset from UTC, e.g. "-07:30"
        private static unsafe void FormatCustomizedTimeZone<TChar>(DateTime dateTime, TimeSpan offset, int tokenLen, bool timeOnly, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
        {
            // See if the instance already has an offset
            bool dateTimeFormat = offset.Ticks == NullOffset;
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
                result.Append(TChar.CastFrom('+'));
            }
            else
            {
                result.Append(TChar.CastFrom('-'));
                offset = offset.Negate(); // get a positive offset, so that you don't need a separate code path for the negative numbers.
            }

            if (tokenLen <= 1)
            {
                // 'z' format e.g "-7"
                (int tens, int ones) = Math.DivRem(offset.Hours, 10);
                if (tens != 0)
                {
                    result.Append(TChar.CastFrom('0' + tens));
                }
                result.Append(TChar.CastFrom('0' + ones));
            }
            else if (tokenLen == 2)
            {
                // 'zz' format e.g "-07"
                fixed (TChar* p = &MemoryMarshal.GetReference(result.AppendSpan(2)))
                {
                    Number.WriteTwoDigits((uint)offset.Hours, p);
                }
            }
            else
            {
                Debug.Assert(tokenLen >= 3);
                fixed (TChar* p = &MemoryMarshal.GetReference(result.AppendSpan(5)))
                {
                    Number.WriteTwoDigits((uint)offset.Hours, p);
                    p[2] = TChar.CastFrom(':');
                    Number.WriteTwoDigits((uint)offset.Minutes, p + 3);
                }
            }
        }

        // output the 'K' format, which is for round-tripping the data
        private static unsafe void FormatCustomizedRoundripTimeZone<TChar>(DateTime dateTime, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
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
                        result.Append(TChar.CastFrom('Z'));
                        return;
                    default:
                        // If the kind is unspecified, we output nothing here
                        return;
                }
            }
            if (offset.Ticks >= 0)
            {
                result.Append(TChar.CastFrom('+'));
            }
            else
            {
                result.Append(TChar.CastFrom('-'));
                // get a positive offset, so that you don't need a separate code path for the negative numbers.
                offset = offset.Negate();
            }

            fixed (TChar* hoursMinutes = &MemoryMarshal.GetReference(result.AppendSpan(5)))
            {
                Number.WriteTwoDigits((uint)offset.Hours, hoursMinutes);
                hoursMinutes[2] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)offset.Minutes, hoursMinutes + 3);
            }
        }

        internal static string ExpandStandardFormatToCustomPattern(char format, DateTimeFormatInfo dtfi) =>
            format switch
            {
                'd' => dtfi.ShortDatePattern, // Short Date
                'D' => dtfi.LongDatePattern, // Long Date
                'f' => dtfi.LongDatePattern + " " + dtfi.ShortTimePattern, // Full (long date + short time)
                'F' => dtfi.FullDateTimePattern, // Full (long date + long time)
                'g' => dtfi.GeneralShortTimePattern, // General (short date + short time)
                'G' => dtfi.GeneralLongTimePattern, // General (short date + long time)
                'm' or 'M' => dtfi.MonthDayPattern, // Month/Day Date
                'o' or 'O' => RoundtripFormat, // Roundtrip Format
                'r' or 'R' => dtfi.RFC1123Pattern, // RFC 1123 Standard
                's' => dtfi.SortableDateTimePattern, // Sortable without Time Zone Info
                't' => dtfi.ShortTimePattern, // Short Time
                'T' => dtfi.LongTimePattern, // Long Time
                'u' => dtfi.UniversalSortableDateTimePattern, // Universal with Sortable format
                'U' => dtfi.FullDateTimePattern, // Universal with Full (long date + long time) format
                'y' or 'Y' => dtfi.YearMonthPattern, // Year/Month Date
                _ => throw new FormatException(SR.Format_InvalidString),
            };

        internal static string Format(DateTime dateTime, string? format, IFormatProvider? provider) =>
            Format(dateTime, format, provider, new TimeSpan(NullOffset));

        internal static string Format(DateTime dateTime, string? format, IFormatProvider? provider, TimeSpan offset)
        {
            DateTimeFormatInfo dtfi;

            if (string.IsNullOrEmpty(format))
            {
                dtfi = DateTimeFormatInfo.GetInstance(provider);

                if (offset.Ticks == NullOffset) // default DateTime.ToString case
                {
                    if (IsTimeOnlySpecialCase(dateTime, dtfi))
                    {
                        string str = string.FastAllocateString(FormatSLength);
                        TryFormatS(dateTime, new Span<char>(ref str.GetRawStringData(), str.Length), out int charsWritten);
                        Debug.Assert(charsWritten == FormatSLength);
                        return str;
                    }
                    else if (ReferenceEquals(dtfi, DateTimeFormatInfo.InvariantInfo))
                    {
                        string str = string.FastAllocateString(FormatInvariantGMinLength);
                        TryFormatInvariantG(dateTime, offset, new Span<char>(ref str.GetRawStringData(), str.Length), out int charsWritten);
                        Debug.Assert(charsWritten == FormatInvariantGMinLength);
                        return str;
                    }
                    else
                    {
                        format = dtfi.GeneralLongTimePattern; // "G"
                    }
                }
                else // default DateTimeOffset.ToString case
                {
                    if (IsTimeOnlySpecialCase(dateTime, dtfi))
                    {
                        format = RoundtripDateTimeUnfixed;
                        dtfi = DateTimeFormatInfo.InvariantInfo;
                    }
                    else if (ReferenceEquals(dtfi, DateTimeFormatInfo.InvariantInfo))
                    {
                        string str = string.FastAllocateString(FormatInvariantGMaxLength);
                        TryFormatInvariantG(dateTime, offset, new Span<char>(ref str.GetRawStringData(), str.Length), out int charsWritten);
                        Debug.Assert(charsWritten == FormatInvariantGMaxLength);
                        return str;
                    }
                    else
                    {
                        format = dtfi.DateTimeOffsetPattern;
                    }
                }
            }
            else if (format.Length == 1)
            {
                int charsWritten;
                string str;
                switch (format[0])
                {
                    // Round trip format
                    case 'o' or 'O':
                        Span<char> span = stackalloc char[FormatOMaxLength];
                        TryFormatO(dateTime, offset, span, out charsWritten);
                        Debug.Assert(charsWritten is >= FormatOMinLength and <= FormatOMaxLength);
                        return span.Slice(0, charsWritten).ToString();

                    // RFC1123 format
                    case 'r' or 'R':
                        str = string.FastAllocateString(FormatRLength);
                        TryFormatR(dateTime, offset, new Span<char>(ref str.GetRawStringData(), str.Length), out charsWritten);
                        Debug.Assert(charsWritten == str.Length);
                        return str;

                    // Sortable format
                    case 's':
                        str = string.FastAllocateString(FormatSLength);
                        TryFormatS(dateTime, new Span<char>(ref str.GetRawStringData(), str.Length), out charsWritten);
                        Debug.Assert(charsWritten == str.Length);
                        return str;

                    // Universal time in sortable format
                    case 'u':
                        str = string.FastAllocateString(FormatuLength);
                        TryFormatu(dateTime, offset, new Span<char>(ref str.GetRawStringData(), str.Length), out charsWritten);
                        Debug.Assert(charsWritten == str.Length);
                        return str;

                    // Universal time in culture dependent format
                    case 'U':
                        dtfi = DateTimeFormatInfo.GetInstance(provider);
                        PrepareFormatU(ref dateTime, ref dtfi, offset);
                        format = dtfi.FullDateTimePattern;
                        break;

                    // All other standard formats
                    default:
                        dtfi = DateTimeFormatInfo.GetInstance(provider);
                        format = ExpandStandardFormatToCustomPattern(format[0], dtfi);
                        break;
                }
            }
            else
            {
                dtfi = DateTimeFormatInfo.GetInstance(provider);
            }

            var vlb = new ValueListBuilder<char>(stackalloc char[256]);
            FormatCustomized(dateTime, format, dtfi, offset, ref vlb);
            string resultString = vlb.AsSpan().ToString();
            vlb.Dispose();
            return resultString;
        }

        internal static bool TryFormat<TChar>(DateTime dateTime, Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) where TChar : unmanaged, IUtfChar<TChar> =>
            TryFormat(dateTime, destination, out charsWritten, format, provider, new TimeSpan(NullOffset));

        internal static bool TryFormat<TChar>(DateTime dateTime, Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider, TimeSpan offset) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            DateTimeFormatInfo dtfi;

            if (format.IsEmpty)
            {
                dtfi = DateTimeFormatInfo.GetInstance(provider);

                if (offset.Ticks == NullOffset) // default DateTime.ToString case
                {
                    if (IsTimeOnlySpecialCase(dateTime, dtfi))
                    {
                        return TryFormatS(dateTime, destination, out charsWritten);
                    }
                    else if (ReferenceEquals(dtfi, DateTimeFormatInfo.InvariantInfo))
                    {
                        return TryFormatInvariantG(dateTime, offset, destination, out charsWritten);
                    }
                    else
                    {
                        format = dtfi.GeneralLongTimePattern; // "G"
                    }
                }
                else // default DateTimeOffset.ToString case
                {
                    if (IsTimeOnlySpecialCase(dateTime, dtfi))
                    {
                        format = RoundtripDateTimeUnfixed;
                        dtfi = DateTimeFormatInfo.InvariantInfo;
                    }
                    else if (ReferenceEquals(dtfi, DateTimeFormatInfo.InvariantInfo))
                    {
                        return TryFormatInvariantG(dateTime, offset, destination, out charsWritten);
                    }
                    else
                    {
                        format = dtfi.DateTimeOffsetPattern;
                    }
                }
            }
            else if (format.Length == 1)
            {
                switch (format[0])
                {
                    // Round trip format
                    case 'o' or 'O':
                        return TryFormatO(dateTime, offset, destination, out charsWritten);

                    // RFC1123 format
                    case 'r' or 'R':
                        return TryFormatR(dateTime, offset, destination, out charsWritten);

                    // Sortable format
                    case 's':
                        return TryFormatS(dateTime, destination, out charsWritten);

                    // Universal time in sortable format
                    case 'u':
                        return TryFormatu(dateTime, offset, destination, out charsWritten);

                    // Universal time in culture dependent format
                    case 'U':
                        dtfi = DateTimeFormatInfo.GetInstance(provider);
                        PrepareFormatU(ref dateTime, ref dtfi, offset);
                        format = dtfi.FullDateTimePattern;
                        break;

                    // All other standard formats
                    default:
                        dtfi = DateTimeFormatInfo.GetInstance(provider);
                        format = ExpandStandardFormatToCustomPattern(format[0], dtfi);
                        break;
                }
            }
            else
            {
                dtfi = DateTimeFormatInfo.GetInstance(provider);
            }

            var vlb = new ValueListBuilder<TChar>(destination);
            FormatCustomized(dateTime, format, dtfi, offset, ref vlb);
            bool success = Unsafe.AreSame(ref MemoryMarshal.GetReference(destination), ref MemoryMarshal.GetReference(vlb.AsSpan()));
            if (success)
            {
                // The reference inside of the builder is still the destination.  That means the builder didn't need to grow to beyond
                // the space in the destination, which means the formatting operation was successful and fully wrote the data to
                // the destination.  All we need to do now is store how much was written.
                charsWritten = vlb.Length;
            }
            else
            {
                // The reference inside of the builder is no longer the destination.  That means the builder needed to grow beyond
                // the builder.  However, it's possible it grew unnecessarily, e.g. when formatting a fraction it might grow but then
                // realize it didn't need to write any data and remove a preceding period. As such, we need to try to copy the data
                // just in case it does actually fit.
                success = vlb.TryCopyTo(destination, out charsWritten);
            }
            vlb.Dispose();
            return success;
        }

        /// <summary>Check whether this DateTime needs to be treated specially as time-only for formatting purposes.</summary>
        /// <remarks>This is only relevant when no format is specified.</remarks>
        private static bool IsTimeOnlySpecialCase(DateTime dateTime, DateTimeFormatInfo dtfi) =>
            // If the time is less than 1 day, consider it as time of day. This is a workaround for VB,
            // since they use ticks less then one day to be time of day.  In cultures which use calendar
            // other than Gregorian calendar, these alternative calendar may not support ticks less than
            // a day. For example, Japanese calendar only supports date after 1868/9/8. This will pose a
            // problem when people in VB get the time of day, and use it to call ToString(), which will
            // use the general format (short date + long time). Since Japanese calendar does not support
            // Gregorian year 0001, an exception will be thrown when we try to get the Japanese year for
            // Gregorian year 0001. Therefore, the workaround allows them to call ToString() for time of
            // day from a DateTime by formatting as ISO 8601 format.
            dateTime.Ticks < Calendar.TicksPerDay &&
            dtfi.Calendar.ID is
                CalendarId.JAPAN or
                CalendarId.TAIWAN or
                CalendarId.HIJRI or
                CalendarId.HEBREW or
                CalendarId.JULIAN or
                CalendarId.UMALQURA or
                CalendarId.PERSIAN;

        /// <summary>For handling the "U" format, update the DateTime and DateTimeFormatInfo appropriately.</summary>
        private static void PrepareFormatU(ref DateTime dateTime, ref DateTimeFormatInfo dtfi, TimeSpan offset)
        {
            if (offset.Ticks != NullOffset)
            {
                // This format is not supported by DateTimeOffset
                throw new FormatException(SR.Format_InvalidString);
            }

            // Universal time is always in Gregorian calendar. Ensure Gregorian is used.
            // Change the Calendar to be Gregorian Calendar.
            if (dtfi.Calendar.GetType() != typeof(GregorianCalendar))
            {
                dtfi = (DateTimeFormatInfo)dtfi.Clone();
                dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
            }
            dateTime = dateTime.ToUniversalTime();
        }

        internal static bool IsValidCustomDateOnlyFormat(ReadOnlySpan<char> format, bool throwOnError)
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


        internal static bool IsValidCustomTimeOnlyFormat(ReadOnlySpan<char> format, bool throwOnError)
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
        internal static unsafe bool TryFormatTimeOnlyO<TChar>(int hour, int minute, int second, long fraction, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < 16)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 16;

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteTwoDigits((uint)hour, dest);
                dest[2] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 3);
                dest[5] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 6);
                dest[8] = TChar.CastFrom('.');
                Number.WriteDigits((uint)fraction, dest + 9, 7);
            }

            return true;
        }

        //   012345678901234567890123456789012
        //   ---------------------------------
        //   05:30:45
        internal static unsafe bool TryFormatTimeOnlyR<TChar>(int hour, int minute, int second, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < 8)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 8;

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteTwoDigits((uint)hour, dest);
                dest[2] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 3);
                dest[5] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 6);
            }

            return true;
        }

        // Roundtrippable format. One of
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12
        internal static unsafe bool TryFormatDateOnlyO<TChar>(int year, int month, int day, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < 10)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 10;

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteFourDigits((uint)year, dest);
                dest[4] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)month, dest + 5);
                dest[7] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)day, dest + 8);
            }

            return true;
        }

        // Rfc1123
        //   01234567890123456789012345678
        //   -----------------------------
        //   Tue, 03 Jan 2017
        internal static unsafe bool TryFormatDateOnlyR<TChar>(DayOfWeek dayOfWeek, int year, int month, int day, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < 16)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 16;

            Debug.Assert((uint)dayOfWeek < 7);
            string dayAbbrev = s_invariantAbbreviatedDayNames[(int)dayOfWeek];
            Debug.Assert(dayAbbrev.Length == 3);

            string monthAbbrev = s_invariantAbbreviatedMonthNames[month - 1];
            Debug.Assert(monthAbbrev.Length == 3);

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                char c = dayAbbrev[2]; // remove bounds checks on remaining dayAbbrev accesses
                dest[0] = TChar.CastFrom(dayAbbrev[0]);
                dest[1] = TChar.CastFrom(dayAbbrev[1]);
                dest[2] = TChar.CastFrom(c);
                dest[3] = TChar.CastFrom(',');
                dest[4] = TChar.CastFrom(' ');
                Number.WriteTwoDigits((uint)day, dest + 5);
                dest[7] = TChar.CastFrom(' ');
                c = monthAbbrev[2]; // remove bounds checks on remaining monthAbbrev accesses
                dest[8] = TChar.CastFrom(monthAbbrev[0]);
                dest[9] = TChar.CastFrom(monthAbbrev[1]);
                dest[10] = TChar.CastFrom(c);
                dest[11] = TChar.CastFrom(' ');
                Number.WriteFourDigits((uint)year, dest + 12);
            }

            return true;
        }

        // Roundtrippable format. One of
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12T05:30:45.7680000-07:00
        //   2017-06-12T05:30:45.7680000Z           (Z is short for "+00:00" but also distinguishes DateTimeKind.Utc from DateTimeKind.Local)
        //   2017-06-12T05:30:45.7680000            (interpreted as local time wrt to current time zone)
        internal static unsafe bool TryFormatO<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            int charsRequired = FormatOMinLength;
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

            dateTime.GetDate(out int year, out int month, out int day);
            dateTime.GetTimePrecise(out int hour, out int minute, out int second, out int tick);

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteFourDigits((uint)year, dest);
                dest[4] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)month, dest + 5);
                dest[7] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)day, dest + 8);
                dest[10] = TChar.CastFrom('T');
                Number.WriteTwoDigits((uint)hour, dest + 11);
                dest[13] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 14);
                dest[16] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 17);
                dest[19] = TChar.CastFrom('.');
                Number.WriteDigits((uint)tick, dest + 20, 7);

                if (kind == DateTimeKind.Local)
                {
                    int offsetTotalMinutes = (int)(offset.Ticks / TimeSpan.TicksPerMinute);

                    char sign = '+';
                    if (offsetTotalMinutes < 0)
                    {
                        sign = '-';
                        offsetTotalMinutes = -offsetTotalMinutes;
                    }

                    (int offsetHours, int offsetMinutes) = Math.DivRem(offsetTotalMinutes, 60);

                    dest[27] = TChar.CastFrom(sign);
                    Number.WriteTwoDigits((uint)offsetHours, dest + 28);
                    dest[30] = TChar.CastFrom(':');
                    Number.WriteTwoDigits((uint)offsetMinutes, dest + 31);
                }
                else if (kind == DateTimeKind.Utc)
                {
                    dest[27] = TChar.CastFrom('Z');
                }
            }

            return true;
        }

        // Sortable format. Offset and Kind are ignored.
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12T05:30:45
        internal static unsafe bool TryFormatS<TChar>(DateTime dateTime, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < FormatSLength)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = FormatSLength;

            dateTime.GetDate(out int year, out int month, out int day);
            dateTime.GetTime(out int hour, out int minute, out int second);

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteFourDigits((uint)year, dest);
                dest[4] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)month, dest + 5);
                dest[7] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)day, dest + 8);
                dest[10] = TChar.CastFrom('T');
                Number.WriteTwoDigits((uint)hour, dest + 11);
                dest[13] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 14);
                dest[16] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 17);
            }

            return true;
        }

        // Sortable universal format. Kind is ignored.
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12 05:30:45Z
        internal static unsafe bool TryFormatu<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < FormatuLength)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = FormatuLength;

            if (offset.Ticks != NullOffset)
            {
                dateTime -= offset;
            }

            dateTime.GetDate(out int year, out int month, out int day);
            dateTime.GetTime(out int hour, out int minute, out int second);

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteFourDigits((uint)year, dest);
                dest[4] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)month, dest + 5);
                dest[7] = TChar.CastFrom('-');
                Number.WriteTwoDigits((uint)day, dest + 8);
                dest[10] = TChar.CastFrom(' ');
                Number.WriteTwoDigits((uint)hour, dest + 11);
                dest[13] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 14);
                dest[16] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 17);
                dest[19] = TChar.CastFrom('Z');
            }

            return true;
        }

        // Rfc1123
        //   01234567890123456789012345678
        //   -----------------------------
        //   Tue, 03 Jan 2017 08:08:05 GMT
        internal static unsafe bool TryFormatR<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (destination.Length < FormatRLength)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = FormatRLength;

            if (offset.Ticks != NullOffset)
            {
                // Convert to UTC invariants.
                dateTime -= offset;
            }

            dateTime.GetDate(out int year, out int month, out int day);
            dateTime.GetTime(out int hour, out int minute, out int second);

            string dayAbbrev = s_invariantAbbreviatedDayNames[(int)dateTime.DayOfWeek];
            Debug.Assert(dayAbbrev.Length == 3);

            string monthAbbrev = s_invariantAbbreviatedMonthNames[month - 1];
            Debug.Assert(monthAbbrev.Length == 3);

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                char c = dayAbbrev[2]; // remove bounds checks on remaining dayAbbrev accesses
                dest[0] = TChar.CastFrom(dayAbbrev[0]);
                dest[1] = TChar.CastFrom(dayAbbrev[1]);
                dest[2] = TChar.CastFrom(c);
                dest[3] = TChar.CastFrom(',');
                dest[4] = TChar.CastFrom(' ');
                Number.WriteTwoDigits((uint)day, dest + 5);
                dest[7] = TChar.CastFrom(' ');
                c = monthAbbrev[2]; // remove bounds checks on remaining monthAbbrev accesses
                dest[8] = TChar.CastFrom(monthAbbrev[0]);
                dest[9] = TChar.CastFrom(monthAbbrev[1]);
                dest[10] = TChar.CastFrom(c);
                dest[11] = TChar.CastFrom(' ');
                Number.WriteFourDigits((uint)year, dest + 12);
                dest[16] = TChar.CastFrom(' ');
                Number.WriteTwoDigits((uint)hour, dest + 17);
                dest[19] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 20);
                dest[22] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 23);
                dest[25] = TChar.CastFrom(' ');
                dest[26] = TChar.CastFrom('G');
                dest[27] = TChar.CastFrom('M');
                dest[28] = TChar.CastFrom('T');
            }

            return true;
        }

        // 'G' format for DateTime when using the invariant culture
        //    0123456789012345678
        //    ---------------------------------
        //    05/25/2017 10:30:15
        //
        //  Also default "" format for DateTimeOffset when using the invariant culture
        //    01234567890123456789012345
        //    --------------------------
        //    05/25/2017 10:30:15 -08:00
        internal static unsafe bool TryFormatInvariantG<TChar>(DateTime value, TimeSpan offset, Span<TChar> destination, out int bytesWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            int bytesRequired = FormatInvariantGMinLength;
            if (offset.Ticks != NullOffset)
            {
                bytesRequired += 7; // Space['+'|'-']hh:mm
            }

            if (destination.Length < bytesRequired)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = bytesRequired;

            value.GetDate(out int year, out int month, out int day);
            value.GetTime(out int hour, out int minute, out int second);

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteTwoDigits((uint)month, dest);
                dest[2] = TChar.CastFrom('/');
                Number.WriteTwoDigits((uint)day, dest + 3);
                dest[5] = TChar.CastFrom('/');
                Number.WriteFourDigits((uint)year, dest + 6);
                dest[10] = TChar.CastFrom(' ');

                Number.WriteTwoDigits((uint)hour, dest + 11);
                dest[13] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minute, dest + 14);
                dest[16] = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)second, dest + 17);

                if (offset.Ticks != NullOffset)
                {
                    int offsetMinutes = (int)(offset.Ticks / TimeSpan.TicksPerMinute);
                    TChar sign = TChar.CastFrom('+');
                    if (offsetMinutes < 0)
                    {
                        sign = TChar.CastFrom('-');
                        offsetMinutes = -offsetMinutes;
                    }
                    (int offsetHours, offsetMinutes) = Math.DivRem(offsetMinutes, 60);

                    dest[19] = TChar.CastFrom(' ');
                    dest[20] = sign;
                    Number.WriteTwoDigits((uint)offsetHours, dest + 21);
                    dest[23] = TChar.CastFrom(':');
                    Number.WriteTwoDigits((uint)offsetMinutes, dest + 24);
                }
            }

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
