// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal static class TimeSpanFormat
    {
        internal static readonly FormatLiterals PositiveInvariantFormatLiterals = FormatLiterals.InitInvariant(isNegative: false);
        internal static readonly FormatLiterals NegativeInvariantFormatLiterals = FormatLiterals.InitInvariant(isNegative: true);

        /// <summary>Main method called from TimeSpan.ToString.</summary>
        internal static string Format(TimeSpan value, string? format, IFormatProvider? formatProvider)
        {
            if (string.IsNullOrEmpty(format))
            {
                return FormatC(value); // formatProvider ignored, as "c" is invariant
            }

            if (format.Length == 1)
            {
                char c = format[0];

                if (c == 'c' || (c | 0x20) == 't') // special-case to optimize the default TimeSpan format
                {
                    return FormatC(value); // formatProvider ignored, as "c" is invariant
                }

                if ((c | 0x20) == 'g') // special-case to optimize the remaining 'g'/'G' standard formats
                {
                    return FormatG(value, DateTimeFormatInfo.GetInstance(formatProvider), c == 'G' ? StandardFormat.G : StandardFormat.g);
                }

                throw new FormatException(SR.Format_InvalidString);
            }

            var vlb = new ValueListBuilder<char>(stackalloc char[256]);
            FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider), ref vlb);
            string resultString = vlb.AsSpan().ToString();
            vlb.Dispose();
            return resultString;
        }

        /// <summary>Main method called from TimeSpan.TryFormat.</summary>
        internal static bool TryFormat<TChar>(TimeSpan value, Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (format.Length == 0)
            {
                return TryFormatStandard(value, StandardFormat.C, null, destination, out charsWritten);
            }

            if (format.Length == 1)
            {
                char c = format[0];
                if (c == 'c' || ((c | 0x20) == 't'))
                {
                    return TryFormatStandard(value, StandardFormat.C, null, destination, out charsWritten);
                }
                else
                {
                    StandardFormat sf =
                        c == 'g' ? StandardFormat.g :
                        c == 'G' ? StandardFormat.G :
                        throw new FormatException(SR.Format_InvalidString);
                    return TryFormatStandard(value, sf, DateTimeFormatInfo.GetInstance(formatProvider).DecimalSeparatorTChar<TChar>(), destination, out charsWritten);
                }
            }

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[256]);
            FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider), ref vlb);
            bool result = vlb.TryCopyTo(destination, out charsWritten);
            vlb.Dispose();
            return result;
        }

        internal static string FormatC(TimeSpan value)
        {
            Span<char> destination = stackalloc char[26]; // large enough for any "c" TimeSpan
            TryFormatStandard(value, StandardFormat.C, null, destination, out int charsWritten);
            return new string(destination.Slice(0, charsWritten));
        }

        private static string FormatG(TimeSpan value, DateTimeFormatInfo dtfi, StandardFormat format)
        {
            string decimalSeparator = dtfi.DecimalSeparator;
            int maxLength = 25 + decimalSeparator.Length; // large enough for any "g"/"G" TimeSpan
            Span<char> destination = maxLength < 128 ?
                stackalloc char[maxLength] :
                new char[maxLength]; // the chances of needing this case are almost 0, as DecimalSeparator.Length will basically always == 1
            TryFormatStandard(value, format, decimalSeparator, destination, out int charsWritten);
            return new string(destination.Slice(0, charsWritten));
        }

        internal enum StandardFormat
        {
            C,
            G,
            g
        }

        internal static unsafe bool TryFormatStandard<TChar>(TimeSpan value, StandardFormat format, ReadOnlySpan<TChar> decimalSeparator, Span<TChar> destination, out int written) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(format == StandardFormat.C || format == StandardFormat.G || format == StandardFormat.g);

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

                ulong fraction64;
                (totalSecondsRemaining, fraction64) = Math.DivRem((ulong)ticks, TimeSpan.TicksPerSecond);
                fraction = (uint)fraction64;
            }

        AfterComputeFraction:
            // Only write out the fraction if it's non-zero, and in that
            // case write out the entire fraction (all digits).
            Debug.Assert(fraction < 10_000_000);
            int fractionDigits = 0;
            switch (format)
            {
                case StandardFormat.C:
                    // "c": Write out a fraction only if it's non-zero, and write out all 7 digits of it.
                    if (fraction != 0)
                    {
                        Debug.Assert(decimalSeparator.IsEmpty);
                        fractionDigits = DateTimeFormat.MaxSecondsFractionDigits;
                        requiredOutputLength += fractionDigits + 1; // digits plus leading decimal separator
                    }
                    break;

                case StandardFormat.G:
                    // "G": Write out a fraction regardless of whether it's 0, and write out all 7 digits of it.
                    fractionDigits = DateTimeFormat.MaxSecondsFractionDigits;
                    requiredOutputLength += fractionDigits;
                    requiredOutputLength += decimalSeparator.Length;
                    break;

                default:
                    // "g": Write out a fraction only if it's non-zero, and write out only the most significant digits.
                    Debug.Assert(format == StandardFormat.g);
                    if (fraction != 0)
                    {
                        fractionDigits = DateTimeFormat.MaxSecondsFractionDigits - FormattingHelpers.CountDecimalTrailingZeros(fraction, out fraction);
                        requiredOutputLength += fractionDigits;
                        requiredOutputLength += decimalSeparator.Length;
                    }
                    break;
            }

            ulong totalMinutesRemaining = 0, seconds = 0;
            if (totalSecondsRemaining > 0)
            {
                // Only compute minutes if the TimeSpan has an absolute value of >= 1 minute.
                (totalMinutesRemaining, seconds) = Math.DivRem(totalSecondsRemaining, 60 /* seconds per minute */);
                Debug.Assert(seconds < 60);
            }

            ulong totalHoursRemaining = 0, minutes = 0;
            if (totalMinutesRemaining > 0)
            {
                // Only compute hours if the TimeSpan has an absolute value of >= 1 hour.
                (totalHoursRemaining, minutes) = Math.DivRem(totalMinutesRemaining, 60 /* minutes per hour */);
                Debug.Assert(minutes < 60);
            }

            // At this point, we can switch over to 32-bit DivRem since the data has shrunk far enough.
            Debug.Assert(totalHoursRemaining <= uint.MaxValue);

            uint days = 0, hours = 0;
            if (totalHoursRemaining > 0)
            {
                // Only compute days if the TimeSpan has an absolute value of >= 1 day.
                (days, hours) = Math.DivRem((uint)totalHoursRemaining, 24 /* hours per day */);
                Debug.Assert(hours < 24);
            }

            int hourDigits = 2;
            if (format == StandardFormat.g && hours < 10)
            {
                // "g": Only writing a one-digit hour, rather than expected two-digit hour
                hourDigits = 1;
                requiredOutputLength--;
            }

            int dayDigits = 0;
            if (days > 0)
            {
                dayDigits = FormattingHelpers.CountDigits(days);
                Debug.Assert(dayDigits <= 8);
                requiredOutputLength += dayDigits + 1; // for the leading "d."
            }
            else if (format == StandardFormat.G)
            {
                // "G": has a leading "0:" if days is 0
                requiredOutputLength += 2;
                dayDigits = 1;
            }

            if (destination.Length < requiredOutputLength)
            {
                written = 0;
                return false;
            }

            fixed (TChar* dest = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = dest;

                // Write leading '-' if necessary
                if (value.Ticks < 0)
                {
                    *p++ = TChar.CastFrom('-');
                }

                // Write day and separator, if necessary
                if (dayDigits != 0)
                {
                    Number.WriteDigits(days, p, dayDigits);
                    p += dayDigits;
                    *p++ = TChar.CastFrom(format == StandardFormat.C ? '.' : ':');
                }

                // Write "[h]h:mm:ss
                Debug.Assert(hourDigits == 1 || hourDigits == 2);
                if (hourDigits == 2)
                {
                    Number.WriteTwoDigits(hours, p);
                    p += 2;
                }
                else
                {
                    *p++ = TChar.CastFrom('0' + hours);
                }
                *p++ = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)minutes, p);
                p += 2;
                *p++ = TChar.CastFrom(':');
                Number.WriteTwoDigits((uint)seconds, p);
                p += 2;

                // Write fraction and separator, if necessary
                if (fractionDigits != 0)
                {
                    Debug.Assert(format == StandardFormat.C || !decimalSeparator.IsEmpty);
                    if (format == StandardFormat.C)
                    {
                        *p++ = TChar.CastFrom('.');
                    }
                    else if (decimalSeparator.Length == 1)
                    {
                        *p++ = decimalSeparator[0];
                    }
                    else
                    {
                        decimalSeparator.CopyTo(new Span<TChar>(p, decimalSeparator.Length));
                        p += decimalSeparator.Length;
                    }

                    Number.WriteDigits(fraction, p, fractionDigits);
                    p += fractionDigits;
                }

                Debug.Assert(p - dest == requiredOutputLength);
            }

            written = requiredOutputLength;
            return true;
        }

        /// <summary>Format the TimeSpan instance using the specified format.</summary>
        private static void FormatCustomized<TChar>(TimeSpan value, scoped ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(dtfi != null);

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

            long tmp;
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
                        DateTimeFormat.FormatDigits(ref result, hours, tokenLen);
                        break;
                    case 'm':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                        {
                            goto default; // to release the builder and throw
                        }
                        DateTimeFormat.FormatDigits(ref result, minutes, tokenLen);
                        break;
                    case 's':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                        {
                            goto default; // to release the builder and throw
                        }
                        DateTimeFormat.FormatDigits(ref result, seconds, tokenLen);
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
                        DateTimeFormat.FormatFraction(ref result, (int)tmp, DateTimeFormat.fixedNumberFormats[tokenLen - 1]);
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
                                tmp /= 10;
                                effectiveDigits--;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (effectiveDigits > 0)
                        {
                            DateTimeFormat.FormatFraction(ref result, (int)tmp, DateTimeFormat.fixedNumberFormats[effectiveDigits - 1]);
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

                        DateTimeFormat.FormatDigits(ref result, day, tokenLen);
                        break;
                    case '\'':
                    case '\"':
                        tokenLen = DateTimeFormat.ParseQuoteString(format, i, ref result);
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
                            FormatCustomized(value, new ReadOnlySpan<char>(in nextCharChar), dtfi, ref result);
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
                            result.Append(TChar.CastFrom(nextChar));
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
                        throw new FormatException(SR.Format_InvalidString);
                }
                i += tokenLen;
            }
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
                FormatLiterals x = default;
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

                var sb = new ValueStringBuilder(stackalloc char[256]);
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
                                Debug.Assert(field >= 0 && field <= 5);
                                _literals[field] = sb.AsSpan().ToString();
                                sb.Length = 0;
                                inQuote = false;
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

                sb.Dispose();

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
            }
        }
    }
}
