// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a TimeSpan at the start of a Utf8 string.
        /// </summary>
        /// <param name="source">The Utf8 string to parse</param>
        /// <param name="value">Receives the parsed value</param>
        /// <param name="bytesConsumed">On a successful parse, receives the length in bytes of the substring that was parsed </param>
        /// <param name="standardFormat">Expected format of the Utf8 string</param>
        /// <returns>
        /// true for success. "bytesConsumed" contains the length in bytes of the substring that was parsed.
        /// false if the string was not syntactically valid or an overflow or underflow occurred. "bytesConsumed" is set to 0. 
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
        public static bool TryParse(ReadOnlySpan<byte> source, out TimeSpan value, out int bytesConsumed, char standardFormat = default)
        {
            switch (standardFormat)
            {
                case default(char):
                case 'c':
                case 't':
                case 'T':
                    return TryParseTimeSpanC(source, out value, out bytesConsumed);

                case 'G':
                    return TryParseTimeSpanBigG(source, out value, out bytesConsumed);

                case 'g':
                    return TryParseTimeSpanLittleG(source, out value, out bytesConsumed);

                default:
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }
        }

        /// <summary>
        /// Parse the fraction portion of a TimeSpan. Must be 1..7 digits. If fewer than 7, zeroes are implied to the right. If more than 7, the TimeSpan
        /// parser rejects the string (even if the extra digits are all zeroes.)
        /// </summary>
        private static bool TryParseTimeSpanFraction(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
        {
            int srcIndex = 0;

            if (srcIndex == source.Length)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            uint digit = source[srcIndex] - 48u; // '0'
            if (digit > 9)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }
            srcIndex++;

            uint fraction = digit;
            int digitCount = 1;

            while (srcIndex != source.Length)
            {
                digit = source[srcIndex] - 48u; // '0'
                if (digit > 9)
                    break;
                srcIndex++;
                digitCount++;
                if (digitCount > Utf8Constants.DateTimeNumFractionDigits)
                {
                    // Yes, TimeSpan fraction parsing is that picky.
                    value = default;
                    bytesConsumed = 0;
                    return false;
                }
                fraction = 10 * fraction + digit;
            }

            switch (digitCount)
            {
                case 7:
                    break;

                case 6:
                    fraction *= 10;
                    break;

                case 5:
                    fraction *= 100;
                    break;

                case 4:
                    fraction *= 1000;
                    break;

                case 3:
                    fraction *= 10000;
                    break;

                case 2:
                    fraction *= 100000;
                    break;

                default:
                    Debug.Assert(digitCount == 1);
                    fraction *= 1000000;
                    break;
            }

            value = fraction;
            bytesConsumed = srcIndex;
            return true;
        }

        /// <summary>
        /// Overflow-safe TryCreateTimeSpan
        /// </summary>
        private static bool TryCreateTimeSpan(bool isNegative, uint days, uint hours, uint minutes, uint seconds, uint fraction, out TimeSpan timeSpan)
        {
            const long MaxMilliSeconds = long.MaxValue / TimeSpan.TicksPerMillisecond;
            const long MinMilliSeconds = long.MinValue / TimeSpan.TicksPerMillisecond;

            Debug.Assert(days >= 0 && hours >= 0 && minutes >= 0 && seconds >= 00 && fraction >= 0);
            if (hours > 23 || minutes > 59 || seconds > 59)
            {
                timeSpan = default;
                return false;
            }

            Debug.Assert(fraction <= Utf8Constants.MaxDateTimeFraction); // This value comes from TryParseTimeSpanFraction() which already rejects any fraction string longer than 7 digits.

            long millisecondsWithoutFraction = (((long)days) * 3600 * 24 + ((long)hours) * 3600 + ((long)minutes) * 60 + seconds) * 1000;

            long ticks;
            if (isNegative)
            {
                millisecondsWithoutFraction = -millisecondsWithoutFraction;
                if (millisecondsWithoutFraction < MinMilliSeconds)
                {
                    timeSpan = default;
                    return false;
                }

                long ticksWithoutFraction = millisecondsWithoutFraction * TimeSpan.TicksPerMillisecond;
                if (ticksWithoutFraction < long.MinValue + fraction)
                {
                    timeSpan = default;
                    return false;
                }

                ticks = ticksWithoutFraction - fraction;
            }
            else
            {
                if (millisecondsWithoutFraction > MaxMilliSeconds)
                {
                    timeSpan = default;
                    return false;
                }

                long ticksWithoutFraction = millisecondsWithoutFraction * TimeSpan.TicksPerMillisecond;
                if (ticksWithoutFraction > long.MaxValue - fraction)
                {
                    timeSpan = default;
                    return false;
                }

                ticks = ticksWithoutFraction + fraction;
            }

            timeSpan = new TimeSpan(ticks);
            return true;
        }
    }
}
