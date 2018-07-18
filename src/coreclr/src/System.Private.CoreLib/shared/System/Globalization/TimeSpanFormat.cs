// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Text;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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


        /// <summary>Main method called from TimeSpan.ToString.</summary>
        internal static string Format(TimeSpan value, string format, IFormatProvider formatProvider)
        {
            return IsFormatC(format) ? // special-case to optimize the default TimeSpan format
                FormatC(value) : // formatProvider ignored, as "c" is invariant
                StringBuilderCache.GetStringAndRelease(FormatToBuilder(value, format, formatProvider));
        }

        /// <summary>Main method called from TimeSpan.TryFormat.</summary>
        internal static bool TryFormat(TimeSpan value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider formatProvider)
        {
            if (IsFormatC(format)) // special-case to optimize the default TimeSpan format
            {
                return TryFormatC(value, destination, out charsWritten); // formatProvider ignored, as "c" is invariant
            }

            StringBuilder sb = FormatToBuilder(value, format, formatProvider);

            if (sb.Length <= destination.Length)
            {
                sb.CopyTo(0, destination, sb.Length);
                charsWritten = sb.Length;
                StringBuilderCache.Release(sb);
                return true;
            }

            charsWritten = 0;
            StringBuilderCache.Release(sb);
            return false;
        }

        private static StringBuilder FormatToBuilder(TimeSpan value, ReadOnlySpan<char> format, IFormatProvider formatProvider)
        {
            // Standard formats other than 'c'/'t'/'T', which should have already been handled.
            if (format.Length == 1)
            {
                char f = format[0];
                switch (f)
                {
                    case 'g':
                    case 'G':
                        DateTimeFormatInfo dtfi = DateTimeFormatInfo.GetInstance(formatProvider);
                        return FormatG(
                            value, 
                            format: value.Ticks < 0 ? dtfi.FullTimeSpanNegativePattern : dtfi.FullTimeSpanPositivePattern,
                            full: f == 'G');

                    default:
                        throw new FormatException(SR.Format_InvalidString);
                }
            }

            // Custom formats
            return FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider), result: null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFormatC(ReadOnlySpan<char> format) =>
            format.Length == 0 ||
            (format.Length == 1 && (format[0] == 'c' || (format[0] | 0x20) == 't'));

        internal static string FormatC(TimeSpan value)
        {
            Span<char> destination = stackalloc char[26]; // large enough for any "c" TimeSpan
            TryFormatC(value, destination, out int charsWritten);
            return new string(destination.Slice(0, charsWritten));
        }

        private static bool TryFormatC(TimeSpan value, Span<char> destination, out int charsWritten)
        {
            // First, calculate how large an output buffer is needed to hold the entire output.
            int requiredOutputLength = 8; // start with "hh:mm:ss" and adjust as necessary

            uint fraction;
            ulong totalSecondsRemaining;
            {
                // Turn this into a non-negative TimeSpan if possible.
                long ticks = value.Ticks;
                if (ticks < 0)
                {
                    requiredOutputLength = 9; // requiredOutputLength + 1 for the leading '-' sign
                    ticks = -ticks;
                    if (ticks < 0)
                    {
                        Debug.Assert(ticks == long.MinValue /* -9223372036854775808 */);

                        // We computed these ahead of time; they're straight from the decimal representation of Int64.MinValue.
                        fraction = 4775808;
                        totalSecondsRemaining = 922337203685;
                        goto AfterComputeFraction;
                    }
                }

                totalSecondsRemaining = Math.DivRem((ulong)ticks, TimeSpan.TicksPerSecond, out ulong fraction64);
                fraction = (uint)fraction64;
            }

        AfterComputeFraction:
            // Only write out the fraction if it's non-zero, and in that
            // case write out the entire fraction (all digits).
            int fractionDigits = 0;
            if (fraction != 0)
            {
                Debug.Assert(fraction < 10_000_000);
                fractionDigits = DateTimeFormat.MaxSecondsFractionDigits;
                requiredOutputLength += fractionDigits + 1; // If we're going to write out a fraction, also need to write the leading decimal.
            }

            ulong totalMinutesRemaining = 0, seconds = 0;
            if (totalSecondsRemaining > 0)
            {
                // Only compute minutes if the TimeSpan has an absolute value of >= 1 minute.
                totalMinutesRemaining = Math.DivRem(totalSecondsRemaining, 60 /* seconds per minute */, out seconds);
                Debug.Assert(seconds < 60);
            }

            ulong totalHoursRemaining = 0, minutes = 0;
            if (totalMinutesRemaining > 0)
            {
                // Only compute hours if the TimeSpan has an absolute value of >= 1 hour.
                totalHoursRemaining = Math.DivRem(totalMinutesRemaining, 60 /* minutes per hour */, out minutes);
                Debug.Assert(minutes < 60);
            }

            // At this point, we can switch over to 32-bit DivRem since the data has shrunk far enough.
            Debug.Assert(totalHoursRemaining <= uint.MaxValue);

            uint days = 0, hours = 0;
            if (totalHoursRemaining > 0)
            {
                // Only compute days if the TimeSpan has an absolute value of >= 1 day.
                days = Math.DivRem((uint)totalHoursRemaining, 24 /* hours per day */, out hours);
                Debug.Assert(hours < 24);
            }

            int dayDigits = 0;
            if (days > 0)
            {
                dayDigits = FormattingHelpers.CountDigits(days);
                Debug.Assert(dayDigits <= 8);
                requiredOutputLength += dayDigits + 1; // for the leading "d."
            }

            if (destination.Length < requiredOutputLength)
            {
                charsWritten = 0;
                return false;
            }

            // Write leading '-' if necessary
            int idx = 0;
            if (value.Ticks < 0)
            {
                destination[idx++] = '-';
            }

            // Write day and separator, if necessary
            if (dayDigits != 0)
            {
                WriteDigits(days, destination.Slice(idx, dayDigits));
                idx += dayDigits;
                destination[idx++] = '.';
            }

            // Write "hh:mm:ss"
            WriteTwoDigits(hours, destination.Slice(idx));
            idx += 2;
            destination[idx++] = ':';
            WriteTwoDigits((uint)minutes, destination.Slice(idx));
            idx += 2;
            destination[idx++] = ':';
            WriteTwoDigits((uint)seconds, destination.Slice(idx));
            idx += 2;

            // Write fraction and separator, if necessary
            if (fractionDigits != 0)
            {
                destination[idx++] = '.';
                WriteDigits(fraction, destination.Slice(idx, fractionDigits));
                idx += fractionDigits;
            }

            Debug.Assert(idx == requiredOutputLength);
            charsWritten = requiredOutputLength;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteTwoDigits(uint value, Span<char> buffer)
        {
            Debug.Assert(buffer.Length >= 2);
            uint temp = '0' + value;
            value /= 10;
            buffer[1] = (char)(temp - (value * 10));
            buffer[0] = (char)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteDigits(uint value, Span<char> buffer)
        {
            Debug.Assert(buffer.Length > 0);

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                uint temp = '0' + value;
                value /= 10;
                buffer[i] = (char)(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = (char)('0' + value);
        }

        /// <summary>Format the TimeSpan instance using the specified format.</summary>
        private static StringBuilder FormatG(TimeSpan value, ReadOnlySpan<char> format, bool full)
        {
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

            FormatLiterals literal = new FormatLiterals();
            literal.Init(format, full);

            if (fraction != 0)
            {
                // truncate the partial second to the specified length
                fraction = (int)(fraction / TimeSpanParse.Pow10(DateTimeFormat.MaxSecondsFractionDigits - literal.ff));
            }

            //  full: [-]dd.hh:mm:ss.fffffff
            // !full: [-][d.]hh:mm:ss[.fffffff] 

            StringBuilder sb = StringBuilderCache.Acquire(InternalGlobalizationHelper.StringBuilderDefaultCapacity);
            sb.Append(literal.Start);                           // [-]
            if (full || day != 0)
            {
                sb.Append(day);                                 // [dd]
                sb.Append(literal.DayHourSep);                  // [.]
            }                                                   //
            AppendNonNegativeInt32(sb, hours, literal.hh);      // hh
            sb.Append(literal.HourMinuteSep);                   // :
            AppendNonNegativeInt32(sb, minutes, literal.mm);    // mm
            sb.Append(literal.MinuteSecondSep);                 // :
            AppendNonNegativeInt32(sb, seconds, literal.ss);    // ss
            if (!full)
            {
                int effectiveDigits = literal.ff;
                while (effectiveDigits > 0 && fraction % 10 == 0)
                {
                    fraction = fraction / 10;
                    effectiveDigits--;
                }
                if (effectiveDigits > 0)
                {
                    sb.Append(literal.SecondFractionSep);           // [.FFFFFFF]
                    sb.Append((fraction).ToString(DateTimeFormat.fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                }
            }
            else
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
                            StringBuilder origStringBuilder = FormatCustomized(value, MemoryMarshal.CreateReadOnlySpan<char>(ref nextCharChar, 1), dtfi, result);
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
