// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
namespace System.Globalization {
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    internal static class TimeSpanFormat {

        [System.Security.SecuritySafeCritical]  // auto-generated
        private static String IntToString(int n, int digits) {
            return ParseNumbers.IntToString(n, 10, digits, '0', 0);
        }

        internal static readonly FormatLiterals PositiveInvariantFormatLiterals  = TimeSpanFormat.FormatLiterals.InitInvariant(false /*isNegative*/);
        internal static readonly FormatLiterals NegativeInvariantFormatLiterals  = TimeSpanFormat.FormatLiterals.InitInvariant(true  /*isNegative*/);

        internal enum Pattern {
            None    = 0,
            Minimum = 1,
            Full    = 2,
        }  

        //
        //  Format
        //
        //  Actions: Main method called from TimeSpan.ToString
        // 
        internal static String Format(TimeSpan value, String format, IFormatProvider formatProvider) {
            if (format == null || format.Length == 0)
                format = "c";

            // standard formats
            if (format.Length == 1) {               
                char f = format[0];

                if (f == 'c' || f == 't' || f == 'T')
                    return FormatStandard(value, true, format, Pattern.Minimum);
                if (f == 'g' || f == 'G') {
                    Pattern pattern;
                    DateTimeFormatInfo dtfi = DateTimeFormatInfo.GetInstance(formatProvider);

                    if (value._ticks < 0)
                        format = dtfi.FullTimeSpanNegativePattern;
                    else
                        format = dtfi.FullTimeSpanPositivePattern;
                    if (f == 'g')
                        pattern = Pattern.Minimum;
                    else
                        pattern = Pattern.Full;
                  
                    return FormatStandard(value, false, format, pattern);
                }
                throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            return FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider));
        }

        //
        //  FormatStandard
        //
        //  Actions: Format the TimeSpan instance using the specified format.
        // 
        private static String FormatStandard(TimeSpan value, bool isInvariant, String format, Pattern pattern) {
            StringBuilder sb = StringBuilderCache.Acquire();
            int day = (int)(value._ticks / TimeSpan.TicksPerDay);
            long time = value._ticks % TimeSpan.TicksPerDay;

            if (value._ticks < 0) {
                day = -day;
                time = -time;
            }
            int hours    = (int)(time / TimeSpan.TicksPerHour % 24);
            int minutes  = (int)(time / TimeSpan.TicksPerMinute % 60);
            int seconds  = (int)(time / TimeSpan.TicksPerSecond % 60);
            int fraction = (int)(time % TimeSpan.TicksPerSecond);

            FormatLiterals literal;
            if (isInvariant) {
                if (value._ticks < 0)
                    literal = NegativeInvariantFormatLiterals;
                else
                    literal = PositiveInvariantFormatLiterals;
            }
            else {
                literal = new FormatLiterals();
                literal.Init(format, pattern == Pattern.Full);
            }
            if (fraction != 0) { // truncate the partial second to the specified length
                fraction = (int)((long)fraction / (long)Math.Pow(10, DateTimeFormat.MaxSecondsFractionDigits - literal.ff));
            }

            // Pattern.Full: [-]dd.hh:mm:ss.fffffff
            // Pattern.Minimum: [-][d.]hh:mm:ss[.fffffff] 

            sb.Append(literal.Start);                           // [-]
            if (pattern == Pattern.Full || day != 0) {          //
                sb.Append(day);                                 // [dd]
                sb.Append(literal.DayHourSep);                  // [.]
            }                                                   //
            sb.Append(IntToString(hours, literal.hh));          // hh
            sb.Append(literal.HourMinuteSep);                   // :
            sb.Append(IntToString(minutes, literal.mm));        // mm
            sb.Append(literal.MinuteSecondSep);                 // :
            sb.Append(IntToString(seconds, literal.ss));        // ss
            if (!isInvariant && pattern == Pattern.Minimum) {
                int effectiveDigits = literal.ff;
                while (effectiveDigits > 0) {
                    if (fraction % 10 == 0) {
                        fraction = fraction / 10;
                        effectiveDigits--;
                    }
                    else {
                        break;
                    }
                }
                if (effectiveDigits > 0) {
                    sb.Append(literal.SecondFractionSep);           // [.FFFFFFF]
                    sb.Append((fraction).ToString(DateTimeFormat.fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                }
            }
            else if (pattern == Pattern.Full || fraction != 0) {
                sb.Append(literal.SecondFractionSep);           // [.]
                sb.Append(IntToString(fraction, literal.ff));   // [fffffff]
            }                                                   //
            sb.Append(literal.End);                             //

            return StringBuilderCache.GetStringAndRelease(sb);
        }




        //
        //  FormatCustomized
        //
        //  Actions: Format the TimeSpan instance using the specified format.
        // 
        internal static String FormatCustomized(TimeSpan value, String format, DateTimeFormatInfo dtfi) {                      

            Contract.Assert(dtfi != null, "dtfi == null");

            int day = (int)(value._ticks / TimeSpan.TicksPerDay);
            long time = value._ticks % TimeSpan.TicksPerDay;

            if (value._ticks < 0) {
                day = -day;
                time = -time;
            }
            int hours    = (int)(time / TimeSpan.TicksPerHour % 24);
            int minutes  = (int)(time / TimeSpan.TicksPerMinute % 60);
            int seconds  = (int)(time / TimeSpan.TicksPerSecond % 60);
            int fraction = (int)(time % TimeSpan.TicksPerSecond);

            long tmp = 0;
            int i = 0;
            int tokenLen;
            StringBuilder result = StringBuilderCache.Acquire();
            
            while (i < format.Length) {
                char ch = format[i];
                int nextChar;
                switch (ch) {
                    case 'h':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, hours, tokenLen);
                        break;
                    case 'm':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, minutes, tokenLen);
                        break;
                    case 's':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, seconds, tokenLen);
                        break;
                    case 'f':
                        //
                        // The fraction of a second in single-digit precision. The remaining digits are truncated. 
                        //
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));

                        tmp = (long)fraction;
                        tmp /= (long)Math.Pow(10, DateTimeFormat.MaxSecondsFractionDigits - tokenLen);
                        result.Append((tmp).ToString(DateTimeFormat.fixedNumberFormats[tokenLen - 1], CultureInfo.InvariantCulture));
                        break;
                    case 'F':
                        //
                        // Displays the most significant digit of the seconds fraction. Nothing is displayed if the digit is zero.
                        //
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));

                        tmp = (long)fraction;
                        tmp /= (long)Math.Pow(10, DateTimeFormat.MaxSecondsFractionDigits - tokenLen);
                        int effectiveDigits = tokenLen;
                        while (effectiveDigits > 0) {
                            if (tmp % 10 == 0) {
                                tmp = tmp / 10;
                                effectiveDigits--;
                            }
                            else {
                                break;
                            }
                        }
                        if (effectiveDigits > 0) {
                            result.Append((tmp).ToString(DateTimeFormat.fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                        }
                        break;
                    case 'd':
                        //
                        // tokenLen == 1 : Day as digits with no leading zero.
                        // tokenLen == 2+: Day as digits with leading zero for single-digit days.
                        //
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 8)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, day, tokenLen, true);
                        break;
                    case '\'':
                    case '\"':
                        tokenLen = DateTimeFormat.ParseQuoteString(format, i, result); 
                        break;
                    case '%':
                        // Optional format character.
                        // For example, format string "%d" will print day 
                        // Most of the cases, "%" can be ignored.
                        nextChar = DateTimeFormat.ParseNextChar(format, i);
                        // nextChar will be -1 if we already reach the end of the format string.
                        // Besides, we will not allow "%%" appear in the pattern.
                        if (nextChar >= 0 && nextChar != (int)'%') {
                            result.Append(TimeSpanFormat.FormatCustomized(value, ((char)nextChar).ToString(), dtfi));
                            tokenLen = 2;
                        }
                        else
                        {
                            //
                            // This means that '%' is at the end of the format string or
                            // "%%" appears in the format string.
                            //
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        }
                        break;
                    case '\\':
                        // Escaped character.  Can be used to insert character into the format string.
                        // For example, "\d" will insert the character 'd' into the string.
                        //
                        nextChar = DateTimeFormat.ParseNextChar(format, i);
                        if (nextChar >= 0)
                        {
                            result.Append(((char)nextChar));
                            tokenLen = 2;
                        } 
                        else
                        {
                            //
                            // This means that '\' is at the end of the formatting string.
                            //
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        }
                        break;
                    default:
                        throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                }
                i += tokenLen;
            }
            return StringBuilderCache.GetStringAndRelease(result);

        }




        internal struct FormatLiterals {
            internal String Start {
                get {
                    return literals[0];
                }
            }
            internal String DayHourSep {
                get {
                    return literals[1];
                }
            }
            internal String HourMinuteSep {
                get {
                    return literals[2];
                }
            }
            internal String MinuteSecondSep {
                get {
                    return literals[3];
                }
            }
            internal String SecondFractionSep {
                get {
                    return literals[4];
                }
            }
            internal String End {
                get {
                    return literals[5];
                }
            }
            internal String AppCompatLiteral;
            internal int dd;
            internal int hh;
            internal int mm;
            internal int ss;
            internal int ff;  

            private String[] literals;


            /* factory method for static invariant FormatLiterals */
            internal static FormatLiterals InitInvariant(bool isNegative) {
                FormatLiterals x = new FormatLiterals();
                x.literals = new String[6];
                x.literals[0] = isNegative ? "-" : String.Empty;
                x.literals[1] = ".";
                x.literals[2] = ":";
                x.literals[3] = ":";
                x.literals[4] = ".";
                x.literals[5] = String.Empty;  
                x.AppCompatLiteral = ":."; // MinuteSecondSep+SecondFractionSep;       
                x.dd = 2;
                x.hh = 2;
                x.mm = 2;
                x.ss = 2;
                x.ff = DateTimeFormat.MaxSecondsFractionDigits;
                return x;
            }

            // For the "v1" TimeSpan localized patterns, the data is simply literal field separators with
            // the constants guaranteed to include DHMSF ordered greatest to least significant.
            // Once the data becomes more complex than this we will need to write a proper tokenizer for
            // parsing and formatting
            internal void Init(String format, bool useInvariantFieldLengths) {
                literals = new String[6];
                for (int i = 0; i < literals.Length; i++) 
                    literals[i] = String.Empty;
                dd = 0;
                hh = 0;
                mm = 0;
                ss = 0;
                ff = 0;

                StringBuilder sb = StringBuilderCache.Acquire();
                bool inQuote = false;
                char quote = '\'';
                int  field = 0;

                for (int i = 0; i < format.Length; i++) {
                    switch (format[i]) {
                        case '\'':
                        case '\"':
                            if (inQuote && (quote == format[i])) {
                                /* we were in a quote and found a matching exit quote, so we are outside a quote now */
                                Contract.Assert(field >= 0 && field <= 5, "field >= 0 && field <= 5");
                                if (field >= 0 && field <= 5) {
                                    literals[field] = sb.ToString();
                                    sb.Length = 0;
                                    inQuote = false;
                                }
                                else {                                   
                                    return; // how did we get here?
                                }
                            }
                            else if (!inQuote) {
                                /* we are at the start of a new quote block */
                                quote = format[i];
                                inQuote = true;
                            }
                            else {
                                /* we were in a quote and saw the other type of quote character, so we are still in a quote */
                            }
                            break;
                        case '%':
                            Contract.Assert(false, "Unexpected special token '%', Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                            goto default;
                        case '\\':
                            if (!inQuote) {
                                i++; /* skip next character that is escaped by this backslash or percent sign */
                                break;
                            }
                            goto default;
                        case 'd':
                            if (!inQuote) {
                                Contract.Assert((field == 0 && sb.Length == 0) || field == 1,
                                                "field == 0 || field == 1, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 1; // DayHourSep
                                dd++;
                            }
                            break;
                        case 'h':
                            if (!inQuote) {
                                Contract.Assert((field == 1 && sb.Length == 0) || field == 2,
                                                "field == 1 || field == 2, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 2; // HourMinuteSep
                                hh++;
                            }
                            break;
                        case 'm':
                            if (!inQuote) {
                                Contract.Assert((field == 2 && sb.Length == 0) || field == 3,
                                                "field == 2 || field == 3, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 3; // MinuteSecondSep
                                mm++;
                            }
                            break;
                        case 's':
                            if (!inQuote) {
                                Contract.Assert((field == 3 && sb.Length == 0) || field == 4,
                                                "field == 3 || field == 4, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 4; // SecondFractionSep
                                ss++;
                            }
                            break;
                        case 'f':
                        case 'F':
                            if (!inQuote) {
                                Contract.Assert((field == 4 && sb.Length == 0) || field == 5,
                                                "field == 4 || field == 5, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 5; // End
                                ff++;
                            }
                            break;
                        default:
                            sb.Append(format[i]);
                            break;
                    }
                }

                Contract.Assert(field == 5);
                AppCompatLiteral = MinuteSecondSep + SecondFractionSep;

                Contract.Assert(0 < dd && dd < 3, "0 < dd && dd < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < hh && hh < 3, "0 < hh && hh < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < mm && mm < 3, "0 < mm && mm < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < ss && ss < 3, "0 < ss && ss < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < ff && ff < 8, "0 < ff && ff < 8, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");

                if (useInvariantFieldLengths) {
                    dd = 2;
                    hh = 2;
                    mm = 2;
                    ss = 2;
                    ff = DateTimeFormat.MaxSecondsFractionDigits;
                }
                else {
                    if (dd < 1 || dd > 2) dd = 2;   // The DTFI property has a problem. let's try to make the best of the situation.
                    if (hh < 1 || hh > 2) hh = 2;
                    if (mm < 1 || mm > 2) mm = 2;
                    if (ss < 1 || ss > 2) ss = 2;
                    if (ff < 1 || ff > 7) ff = 7;
                }
                StringBuilderCache.Release(sb);
            }
        } //end of struct FormatLiterals
    }
}
