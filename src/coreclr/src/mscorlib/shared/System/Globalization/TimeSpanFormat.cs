// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Diagnostics;

namespace System.Globalization
{
    internal static class TimeSpanFormat
    {
        private static unsafe void AppendNonNegativeInt32(StringBuilder sb, int n, int digits)
        {
            Debug.Assert(n >= 0);
            uint value = (uint)n;

            const int MaxUInt32Digits = 10;
            char* buffer = stackalloc char[MaxUInt32Digits];

            int index = 0;
            do
            {
                uint div = value / 10;
                buffer[index++] = (char)(value - (div * 10) + '0');
                value = div;
            }
            while (value != 0);
            Debug.Assert(index <= MaxUInt32Digits);

            for (int i = digits - index; i > 0; --i) sb.Append('0');
            for (int i = index - 1; i >= 0; --i) sb.Append(buffer[i]);
        }

        internal static readonly FormatLiterals PositiveInvariantFormatLiterals = TimeSpanFormat.FormatLiterals.InitInvariant(isNegative: false);
        internal static readonly FormatLiterals NegativeInvariantFormatLiterals = TimeSpanFormat.FormatLiterals.InitInvariant(isNegative: true);

        internal enum Pattern
        {
            None = 0,
            Minimum = 1,
            Full = 2,
        }

        /// <summary>Main method called from TimeSpan.ToString.</summary>
        internal static string Format(TimeSpan value, string format, IFormatProvider formatProvider) =>
            StringBuilderCache.GetStringAndRelease(FormatToBuilder(value, format, formatProvider));

        /// <summary>Main method called from TimeSpan.TryFormat.</summary>
        internal static bool TryFormat(TimeSpan value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider formatProvider)
        {
            StringBuilder sb = FormatToBuilder(value, format, formatProvider);
            if (sb.Length <= destination.Length)
            {
                charsWritten = sb.Length;
                sb.CopyTo(0, destination, sb.Length);
                StringBuilderCache.Release(sb);
                return true;
            }
            else
            {
                StringBuilderCache.Release(sb);
                charsWritten = 0;
                return false;
            }
        }

        private static StringBuilder FormatToBuilder(TimeSpan value, ReadOnlySpan<char> format, IFormatProvider formatProvider)
        {
            if (format.Length == 0)
            {
                format = "c";
            }

            // Standard formats
            if (format.Length == 1)
            {
                char f = format[0];
                switch (f)
                {
                    case 'c':
                    case 't':
                    case 'T':
                        return FormatStandard(
                            value,
                            isInvariant: true,
                            format: format,
                            pattern: Pattern.Minimum);

                    case 'g':
                    case 'G':
                        DateTimeFormatInfo dtfi = DateTimeFormatInfo.GetInstance(formatProvider);
                        return FormatStandard(
                            value, 
                            isInvariant: false,
                            format: value.Ticks < 0 ? dtfi.FullTimeSpanNegativePattern : dtfi.FullTimeSpanPositivePattern,
                            pattern: f == 'g' ? Pattern.Minimum : Pattern.Full);

                    default:
                        throw new FormatException(SR.Format_InvalidString);
                }
            }

            // Custom formats
            return FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider), result: null);
        }

        /// <summary>Format the TimeSpan instance using the specified format.</summary>
        private static StringBuilder FormatStandard(TimeSpan value, bool isInvariant, ReadOnlySpan<char> format, Pattern pattern)
        {
            StringBuilder sb = StringBuilderCache.Acquire(InternalGlobalizationHelper.StringBuilderDefaultCapacity);
            int day = (int)(value.Ticks / TimeSpan.TicksPerDay);
            long time = value.Ticks % TimeSpan.TicksPerDay;

            if (value.Ticks < 0)
            {
                day = -day;
                time = -time;
            }
            int hours = (int)(time / TimeSpan.TicksPerHour % 24);
            int minutes = (int)(time / TimeSpan.TicksPerMinute % 60);
            int seconds = (int)(time / TimeSpan.TicksPerSecond % 60);
            int fraction = (int)(time % TimeSpan.TicksPerSecond);

            FormatLiterals literal;
            if (isInvariant)
            {
                literal = value.Ticks < 0 ?
                    NegativeInvariantFormatLiterals :
                    PositiveInvariantFormatLiterals;
            }
            else
            {
                literal = new FormatLiterals();
                literal.Init(format, pattern == Pattern.Full);
            }

            if (fraction != 0)
            {
                // truncate the partial second to the specified length
                fraction = (int)(fraction / TimeSpanParse.Pow10(DateTimeFormat.MaxSecondsFractionDigits - literal.ff));
            }

            // Pattern.Full: [-]dd.hh:mm:ss.fffffff
            // Pattern.Minimum: [-][d.]hh:mm:ss[.fffffff] 

            sb.Append(literal.Start);                           // [-]
            if (pattern == Pattern.Full || day != 0)
            {
                sb.Append(day);                                 // [dd]
                sb.Append(literal.DayHourSep);                  // [.]
            }                                                   //
            AppendNonNegativeInt32(sb, hours, literal.hh);      // hh
            sb.Append(literal.HourMinuteSep);                   // :
            AppendNonNegativeInt32(sb, minutes, literal.mm);    // mm
            sb.Append(literal.MinuteSecondSep);                 // :
            AppendNonNegativeInt32(sb, seconds, literal.ss);    // ss
            if (!isInvariant && pattern == Pattern.Minimum)
            {
                int effectiveDigits = literal.ff;
                while (effectiveDigits > 0)
                {
                    if (fraction % 10 == 0)
                    {
                        fraction = fraction / 10;
                        effectiveDigits--;
                    }
                    else
                    {
                        break;
                    }
                }
                if (effectiveDigits > 0)
                {
                    sb.Append(literal.SecondFractionSep);           // [.FFFFFFF]
                    sb.Append((fraction).ToString(DateTimeFormat.fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                }
            }
            else if (pattern == Pattern.Full || fraction != 0)
            {
                sb.Append(literal.SecondFractionSep);             // [.]
                AppendNonNegativeInt32(sb, fraction, literal.ff); // [fffffff]
            }
            sb.Append(literal.End);

            return sb;
        }

        /// <summary>Format the TimeSpan instance using the specified format.</summary>
        private static StringBuilder FormatCustomized(TimeSpan value, ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, StringBuilder result)
        {
            Debug.Assert(dtfi != null);

            bool resultBuilderIsPooled = false;
            if (result == null)
            {
                result = StringBuilderCache.Acquire(InternalGlobalizationHelper.StringBuilderDefaultCapacity);
                resultBuilderIsPooled = true;
            }

            int day = (int)(value.Ticks / TimeSpan.TicksPerDay);
            long time = value.Ticks % TimeSpan.TicksPerDay;

            if (value.Ticks < 0)
            {
                day = -day;
                time = -time;
            }
            int hours = (int)(time / TimeSpan.TicksPerHour % 24);
            int minutes = (int)(time / TimeSpan.TicksPerMinute % 60);
            int seconds = (int)(time / TimeSpan.TicksPerSecond % 60);
            int fraction = (int)(time % TimeSpan.TicksPerSecond);

            long tmp = 0;
            int i = 0;
            int tokenLen;

            while (i < format.Length)
            {
                char ch = format[i];
                int nextChar;
                switch (ch)
                {
                    case 'h':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                        {
                            goto default; // to release the builder and throw
                        }
                        DateTimeFormat.FormatDigits(result, hours, tokenLen);
                        break;
                    case 'm':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                        {
                            goto default; // to release the builder and throw
                        }
                        DateTimeFormat.FormatDigits(result, minutes, tokenLen);
                        break;
                    case 's':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                        {
                            goto default; // to release the builder and throw
                        }
                        DateTimeFormat.FormatDigits(result, seconds, tokenLen);
                        break;
                    case 'f':
                        //
                        // The fraction of a second in single-digit precision. The remaining digits are truncated. 
                        //
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits)
                        {
                            goto default; // to release the builder and throw
                        }

                        tmp = fraction;
                        tmp /= TimeSpanParse.Pow10(DateTimeFormat.MaxSecondsFractionDigits - tokenLen);
                        result.Append((tmp).ToString(DateTimeFormat.fixedNumberFormats[tokenLen - 1], CultureInfo.InvariantCulture));
                        break;
                    case 'F':
                        //
                        // Displays the most significant digit of the seconds fraction. Nothing is displayed if the digit is zero.
                        //
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits)
                        {
                            goto default; // to release the builder and throw
                        }

                        tmp = fraction;
                        tmp /= TimeSpanParse.Pow10(DateTimeFormat.MaxSecondsFractionDigits - tokenLen);
                        int effectiveDigits = tokenLen;
                        while (effectiveDigits > 0)
                        {
                            if (tmp % 10 == 0)
                            {
                                tmp = tmp / 10;
                                effectiveDigits--;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (effectiveDigits > 0)
                        {
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
                        {
                            goto default; // to release the builder and throw
                        }

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
                        if (nextChar >= 0 && nextChar != (int)'%')
                        {
                            char nextCharChar = (char)nextChar;
                            StringBuilder origStringBuilder = FormatCustomized(value, ReadOnlySpan<char>.DangerousCreate(null, ref nextCharChar, 1), dtfi, result);
                            Debug.Assert(ReferenceEquals(origStringBuilder, result));
                            tokenLen = 2;
                        }
                        else
                        {
                            //
                            // This means that '%' is at the end of the format string or
                            // "%%" appears in the format string.
                            //
                            goto default; // to release the builder and throw
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
                            goto default; // to release the builder and throw
                        }
                        break;
                    default:
                        // Invalid format string
                        if (resultBuilderIsPooled)
                        {
                            StringBuilderCache.Release(result);
                        }
                        throw new FormatException(SR.Format_InvalidString);
                }
                i += tokenLen;
            }
            return result;
        }

        internal struct FormatLiterals
        {
            internal string AppCompatLiteral;
            internal int dd;
            internal int hh;
            internal int mm;
            internal int ss;
            internal int ff;

            private string[] _literals;

            internal string Start => _literals[0];
            internal string DayHourSep => _literals[1];
            internal string HourMinuteSep => _literals[2];
            internal string MinuteSecondSep => _literals[3];
            internal string SecondFractionSep => _literals[4];
            internal string End => _literals[5];

            /* factory method for static invariant FormatLiterals */
            internal static FormatLiterals InitInvariant(bool isNegative)
            {
                FormatLiterals x = new FormatLiterals();
                x._literals = new string[6];
                x._literals[0] = isNegative ? "-" : string.Empty;
                x._literals[1] = ".";
                x._literals[2] = ":";
                x._literals[3] = ":";
                x._literals[4] = ".";
                x._literals[5] = string.Empty;
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
            internal void Init(ReadOnlySpan<char> format, bool useInvariantFieldLengths)
            {
                dd = hh = mm = ss = ff = 0;
                _literals = new string[6];
                for (int i = 0; i < _literals.Length; i++)
                {
                    _literals[i] = string.Empty;
                }

                StringBuilder sb = StringBuilderCache.Acquire(InternalGlobalizationHelper.StringBuilderDefaultCapacity);
                bool inQuote = false;
                char quote = '\'';
                int field = 0;

                for (int i = 0; i < format.Length; i++)
                {
                    switch (format[i])
                    {
                        case '\'':
                        case '\"':
                            if (inQuote && (quote == format[i]))
                            {
                                /* we were in a quote and found a matching exit quote, so we are outside a quote now */
                                if (field >= 0 && field <= 5)
                                {
                                    _literals[field] = sb.ToString();
                                    sb.Length = 0;
                                    inQuote = false;
                                }
                                else
                                {
                                    Debug.Fail($"Unexpected field value: {field}");
                                    return; // how did we get here?
                                }
                            }
                            else if (!inQuote)
                            {
                                /* we are at the start of a new quote block */
                                quote = format[i];
                                inQuote = true;
                            }
                            else
                            {
                                /* we were in a quote and saw the other type of quote character, so we are still in a quote */
                            }
                            break;
                        case '%':
                            Debug.Fail("Unexpected special token '%', Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                            goto default;
                        case '\\':
                            if (!inQuote)
                            {
                                i++; /* skip next character that is escaped by this backslash or percent sign */
                                break;
                            }
                            goto default;
                        case 'd':
                            if (!inQuote)
                            {
                                Debug.Assert((field == 0 && sb.Length == 0) || field == 1, "field == 0 || field == 1, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 1; // DayHourSep
                                dd++;
                            }
                            break;
                        case 'h':
                            if (!inQuote)
                            {
                                Debug.Assert((field == 1 && sb.Length == 0) || field == 2, "field == 1 || field == 2, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 2; // HourMinuteSep
                                hh++;
                            }
                            break;
                        case 'm':
                            if (!inQuote)
                            {
                                Debug.Assert((field == 2 && sb.Length == 0) || field == 3, "field == 2 || field == 3, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 3; // MinuteSecondSep
                                mm++;
                            }
                            break;
                        case 's':
                            if (!inQuote)
                            {
                                Debug.Assert((field == 3 && sb.Length == 0) || field == 4, "field == 3 || field == 4, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 4; // SecondFractionSep
                                ss++;
                            }
                            break;
                        case 'f':
                        case 'F':
                            if (!inQuote)
                            {
                                Debug.Assert((field == 4 && sb.Length == 0) || field == 5, "field == 4 || field == 5, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 5; // End
                                ff++;
                            }
                            break;
                        default:
                            sb.Append(format[i]);
                            break;
                    }
                }

                Debug.Assert(field == 5);
                AppCompatLiteral = MinuteSecondSep + SecondFractionSep;

                Debug.Assert(0 < dd && dd < 3, "0 < dd && dd < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Debug.Assert(0 < hh && hh < 3, "0 < hh && hh < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Debug.Assert(0 < mm && mm < 3, "0 < mm && mm < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Debug.Assert(0 < ss && ss < 3, "0 < ss && ss < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Debug.Assert(0 < ff && ff < 8, "0 < ff && ff < 8, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");

                if (useInvariantFieldLengths)
                {
                    dd = 2;
                    hh = 2;
                    mm = 2;
                    ss = 2;
                    ff = DateTimeFormat.MaxSecondsFractionDigits;
                }
                else
                {
                    if (dd < 1 || dd > 2) dd = 2;   // The DTFI property has a problem. let's try to make the best of the situation.
                    if (hh < 1 || hh > 2) hh = 2;
                    if (mm < 1 || mm > 2) mm = 2;
                    if (ss < 1 || ss > 2) ss = 2;
                    if (ff < 1 || ff > 7) ff = 7;
                }
                StringBuilderCache.Release(sb);
            }
        }
    }
}
