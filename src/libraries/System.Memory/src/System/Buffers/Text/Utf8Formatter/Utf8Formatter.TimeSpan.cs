// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a TimeSpan as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="buffer">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     c/t/T (default) [-][d.]hh:mm:ss[.fffffff]             (constant format)
        ///     G               [-]d:hh:mm:ss.fffffff                 (general long)
        ///     g               [-][d:]h:mm:ss[.f[f[f[f[f[f[f[]]]]]]] (general short)
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryFormat(TimeSpan value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default)
        {
            char symbol = format.IsDefault ? 'c' : format.Symbol;

            switch (symbol)
            {
                case 'G':
                case 'g':
                case 'c':
                case 't':
                case 'T':
                    {
                        bool longForm = (symbol == 'G');
                        bool constant = (symbol == 't' || symbol == 'T' || symbol == 'c');

                        long ticks = value.Ticks;
                        int days = (int)FormattingHelpers.DivMod(ticks, TimeSpan.TicksPerDay, out long timeLeft);

                        bool showSign = false;
                        if (ticks < 0)
                        {
                            showSign = true;
                            days = -days;
                            timeLeft = -timeLeft;
                        }

                        int hours = (int)FormattingHelpers.DivMod(timeLeft, TimeSpan.TicksPerHour, out timeLeft);
                        int minutes = (int)FormattingHelpers.DivMod(timeLeft, TimeSpan.TicksPerMinute, out timeLeft);
                        int seconds = (int)FormattingHelpers.DivMod(timeLeft, TimeSpan.TicksPerSecond, out long fraction);

                        int dayDigits = 0;
                        int hourDigits = (constant || longForm || hours > 9) ? 2 : 1;
                        int fractionDigits = 0;

                        bytesWritten = hourDigits + 6; // [h]h:mm:ss
                        if (showSign)
                            bytesWritten += 1;  // [-]
                        if (longForm || days > 0)
                        {
                            dayDigits = FormattingHelpers.CountDigits(days);
                            bytesWritten += dayDigits + 1; // [d'.']
                        }
                        if (longForm || fraction > 0)
                        {
                            fractionDigits = (longForm || constant) ? Utf8Constants.DateTimeNumFractionDigits : FormattingHelpers.CountFractionDigits(fraction);
                            bytesWritten += fractionDigits + 1; // ['.'fffffff] or ['.'FFFFFFF] for short-form
                        }

                        if (buffer.Length < bytesWritten)
                        {
                            bytesWritten = 0;
                            return false;
                        }

                        ref byte utf8Bytes = ref buffer.DangerousGetPinnableReference();
                        int idx = 0;

                        if (showSign)
                            Unsafe.Add(ref utf8Bytes, idx++) = Utf8Constants.Minus;

                        if (dayDigits > 0)
                        {
                            idx += FormattingHelpers.WriteDigits(days, dayDigits, ref utf8Bytes, idx);
                            Unsafe.Add(ref utf8Bytes, idx++) = constant ? Utf8Constants.Period : Utf8Constants.Colon;
                        }

                        idx += FormattingHelpers.WriteDigits(hours, hourDigits, ref utf8Bytes, idx);
                        Unsafe.Add(ref utf8Bytes, idx++) = Utf8Constants.Colon;

                        idx += FormattingHelpers.WriteDigits(minutes, 2, ref utf8Bytes, idx);
                        Unsafe.Add(ref utf8Bytes, idx++) = Utf8Constants.Colon;

                        idx += FormattingHelpers.WriteDigits(seconds, 2, ref utf8Bytes, idx);

                        if (fractionDigits > 0)
                        {
                            Unsafe.Add(ref utf8Bytes, idx++) = Utf8Constants.Period;
                            idx += FormattingHelpers.WriteFractionDigits(fraction, fractionDigits, ref utf8Bytes, idx);
                        }

                        return true;
                    }

                default:
                    return ThrowHelper.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
