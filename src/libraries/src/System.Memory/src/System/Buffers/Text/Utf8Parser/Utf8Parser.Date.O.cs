// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        //
        // Roundtrippable format. One of
        //
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12T05:30:45.7680000-07:00
        //   2017-06-12T05:30:45.7680000Z           (Z is short for "+00:00" but also distinguishes DateTimeKind.Utc from DateTimeKind.Local)
        //   2017-06-12T05:30:45.7680000            (interpreted as local time wrt to current time zone)
        //
        private static bool TryParseDateTimeOffsetO(ReadOnlySpan<byte> text, out DateTimeOffset value, out int bytesConsumed, out DateTimeKind kind)
        {
            if (text.Length < 27)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int year;
            {
                uint digit1 = text[0] - 48u; // '0'
                uint digit2 = text[1] - 48u; // '0'
                uint digit3 = text[2] - 48u; // '0'
                uint digit4 = text[3] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9 || digit3 > 9 || digit4 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                year = (int)(digit1 * 1000 + digit2 * 100 + digit3 * 10 + digit4);
            }

            if (text[4] != Utf8Constants.Hyphen)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int month;
            {
                uint digit1 = text[5] - 48u; // '0'
                uint digit2 = text[6] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                month = (int)(digit1 * 10 + digit2);
            }

            if (text[7] != Utf8Constants.Hyphen)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int day;
            {
                uint digit1 = text[8] - 48u; // '0'
                uint digit2 = text[9] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                day = (int)(digit1 * 10 + digit2);
            }

            if (text[10] != 'T')
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int hour;
            {
                uint digit1 = text[11] - 48u; // '0'
                uint digit2 = text[12] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                hour = (int)(digit1 * 10 + digit2);
            }

            if (text[13] != Utf8Constants.Colon)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int minute;
            {
                uint digit1 = text[14] - 48u; // '0'
                uint digit2 = text[15] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                minute = (int)(digit1 * 10 + digit2);
            }

            if (text[16] != Utf8Constants.Colon)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int second;
            {
                uint digit1 = text[17] - 48u; // '0'
                uint digit2 = text[18] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                second = (int)(digit1 * 10 + digit2);
            }

            if (text[19] != Utf8Constants.Period)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int fraction;
            {
                uint digit1 = text[20] - 48u; // '0'
                uint digit2 = text[21] - 48u; // '0'
                uint digit3 = text[22] - 48u; // '0'
                uint digit4 = text[23] - 48u; // '0'
                uint digit5 = text[24] - 48u; // '0'
                uint digit6 = text[25] - 48u; // '0'
                uint digit7 = text[26] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9 || digit3 > 9 || digit4 > 9 || digit5 > 9 || digit6 > 9 || digit7 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                fraction = (int)(digit1 * 1000000 + digit2 * 100000 + digit3 * 10000 + digit4 * 1000 + digit5 * 100 + digit6 * 10 + digit7);
            }

            byte offsetChar = (text.Length <= 27) ? default : text[27];
            if (offsetChar != 'Z' && offsetChar != Utf8Constants.Plus && offsetChar != Utf8Constants.Minus)
            {
                if (!TryCreateDateTimeOffsetInterpretingDataAsLocalTime(year: year, month: month, day: day, hour: hour, minute: minute, second: second, fraction: fraction, out value))
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }
                bytesConsumed = 27;
                kind = DateTimeKind.Unspecified;
                return true;
            }

            if (offsetChar == 'Z')
            {
                // Same as specifying an offset of "+00:00", except that DateTime's Kind gets set to UTC rather than Local
                if (!TryCreateDateTimeOffset(year: year, month: month, day: day, hour: hour, minute: minute, second: second, fraction: fraction, offsetNegative: false, offsetHours: 0, offsetMinutes: 0, out value))
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                bytesConsumed = 28;
                kind = DateTimeKind.Utc;
                return true;
            }

            Debug.Assert(offsetChar == Utf8Constants.Plus || offsetChar == Utf8Constants.Minus);
            if (text.Length < 33)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int offsetHours;
            {
                uint digit1 = text[28] - 48u; // '0'
                uint digit2 = text[29] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                offsetHours = (int)(digit1 * 10 + digit2);
            }

            if (text[30] != Utf8Constants.Colon)
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            int offsetMinutes;
            {
                uint digit1 = text[31] - 48u; // '0'
                uint digit2 = text[32] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    bytesConsumed = 0;
                    kind = default;
                    return false;
                }

                offsetMinutes = (int)(digit1 * 10 + digit2);
            }

            if (!TryCreateDateTimeOffset(year: year, month: month, day: day, hour: hour, minute: minute, second: second, fraction: fraction, offsetNegative: offsetChar == Utf8Constants.Minus, offsetHours: offsetHours, offsetMinutes: offsetMinutes, out value))
            {
                value = default;
                bytesConsumed = 0;
                kind = default;
                return false;
            }

            bytesConsumed = 33;
            kind = DateTimeKind.Local;
            return true;
        }
    }
}
