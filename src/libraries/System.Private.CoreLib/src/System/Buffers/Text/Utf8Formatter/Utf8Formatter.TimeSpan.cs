// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a TimeSpan as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     c/t/T (default) [-][d.]hh:mm:ss[.fffffff]              (constant format)
        ///     G               [-]d:hh:mm:ss.fffffff                  (general long)
        ///     g               [-][d:][h]h:mm:ss[.f[f[f[f[f[f[f]]]]]] (general short)
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(TimeSpan value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            char symbol = FormattingHelpers.GetSymbolOrDefault(format, 'c');

            switch (symbol)
            {
                case 'c':
                case 'G':
                case 'g':
                    break;

                case 't':
                case 'T':
                    symbol = 'c';
                    break;

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }

            // First, calculate how large an output buffer is needed to hold the entire output.

            int requiredOutputLength = 8; // start with "hh:mm:ss" and adjust as necessary

            uint fraction;
            ulong totalSecondsRemaining;
            {
                // Turn this into a non-negative TimeSpan if possible.
                var ticks = value.Ticks;
                if (ticks < 0)
                {
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

                totalSecondsRemaining = FormattingHelpers.DivMod((ulong)Math.Abs(value.Ticks), TimeSpan.TicksPerSecond, out ulong fraction64);
                fraction = (uint)fraction64;
            }

        AfterComputeFraction:

            int fractionDigits = 0;
            if (symbol == 'c')
            {
                // Only write out the fraction if it's non-zero, and in that
                // case write out the entire fraction (all digits).
                if (fraction != 0)
                {
                    fractionDigits = Utf8Constants.DateTimeNumFractionDigits;
                }
            }
            else if (symbol == 'G')
            {
                // Always write out the fraction, even if it's zero.
                fractionDigits = Utf8Constants.DateTimeNumFractionDigits;
            }
            else
            {
                // Only write out the fraction if it's non-zero, and in that
                // case write out only the most significant digits.
                if (fraction != 0)
                {
                    fractionDigits = Utf8Constants.DateTimeNumFractionDigits - FormattingHelpers.CountDecimalTrailingZeros(fraction, out fraction);
                }
            }

            Debug.Assert(fraction < 10_000_000);

            // If we're going to write out a fraction, also need to write the leading decimal.
            if (fractionDigits != 0)
            {
                requiredOutputLength += fractionDigits + 1;
            }

            ulong totalMinutesRemaining = 0;
            ulong seconds = 0;
            if (totalSecondsRemaining > 0)
            {
                // Only compute minutes if the TimeSpan has an absolute value of >= 1 minute.
                totalMinutesRemaining = FormattingHelpers.DivMod(totalSecondsRemaining, 60 /* seconds per minute */, out seconds);
            }

            Debug.Assert(seconds < 60);

            ulong totalHoursRemaining = 0;
            ulong minutes = 0;
            if (totalMinutesRemaining > 0)
            {
                // Only compute hours if the TimeSpan has an absolute value of >= 1 hour.
                totalHoursRemaining = FormattingHelpers.DivMod(totalMinutesRemaining, 60 /* minutes per hour */, out minutes);
            }

            Debug.Assert(minutes < 60);

            // At this point, we can switch over to 32-bit divmod since the data has shrunk far enough.
            Debug.Assert(totalHoursRemaining <= uint.MaxValue);

            uint days = 0;
            uint hours = 0;
            if (totalHoursRemaining > 0)
            {
                // Only compute days if the TimeSpan has an absolute value of >= 1 day.
                days = FormattingHelpers.DivMod((uint)totalHoursRemaining, 24 /* hours per day */, out hours);
            }

            Debug.Assert(hours < 24);

            int hourDigits = 2;
            if (hours < 10 && symbol == 'g')
            {
                // Only writing a one-digit hour, not a two-digit hour
                hourDigits--;
                requiredOutputLength--;
            }

            int dayDigits = 0;
            if (days == 0)
            {
                if (symbol == 'G')
                {
                    requiredOutputLength += 2; // for the leading "0:"
                    dayDigits = 1;
                }
            }
            else
            {
                dayDigits = FormattingHelpers.CountDigits(days);
                requiredOutputLength += dayDigits + 1; // for the leading "d:" (or "d.")
            }

            if (value.Ticks < 0)
            {
                requiredOutputLength++; // for the leading '-' sign
            }

            if (destination.Length < requiredOutputLength)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = requiredOutputLength;

            int idx = 0;

            // Write leading '-' if necessary
            if (value.Ticks < 0)
            {
                destination[idx++] = Utf8Constants.Minus;
            }

            // Write day (and separator) if necessary
            if (dayDigits > 0)
            {
                FormattingHelpers.WriteDigits(days, destination.Slice(idx, dayDigits));
                idx += dayDigits;
                destination[idx++] = (symbol == 'c') ? Utf8Constants.Period : Utf8Constants.Colon;
            }

            // Write "[h]h:mm:ss"
            FormattingHelpers.WriteDigits(hours, destination.Slice(idx, hourDigits));
            idx += hourDigits;
            destination[idx++] = Utf8Constants.Colon;
            FormattingHelpers.WriteDigits((uint)minutes, destination.Slice(idx, 2));
            idx += 2;
            destination[idx++] = Utf8Constants.Colon;
            FormattingHelpers.WriteDigits((uint)seconds, destination.Slice(idx, 2));
            idx += 2;

            // Write fraction (and separator) if necessary
            if (fractionDigits > 0)
            {
                destination[idx++] = Utf8Constants.Period;
                FormattingHelpers.WriteDigits(fraction, destination.Slice(idx, fractionDigits));
                idx += fractionDigits;
            }

            // And we're done!

            Debug.Assert(idx == requiredOutputLength);
            return true;
        }
    }
}
