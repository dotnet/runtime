// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        //
        // 'G' format for DateTime.
        //
        //   0123456789012345678
        //   ---------------------------------
        //   05/25/2017 10:30:15
        //
        private static bool TryParseDateTimeG(ReadOnlySpan<byte> text, out DateTime value, out DateTimeOffset valueAsOffset, out int bytesConsumed)
        {
            if (text.Length < 19)
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            int month;
            {
                uint digit1 = text[0] - 48u; // '0'
                uint digit2 = text[1] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    valueAsOffset = default;
                    return false;
                }

                month = (int)(digit1 * 10 + digit2);
            }

            if (text[2] != Utf8Constants.Slash)
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            int day;
            {
                uint digit1 = text[3] - 48u; // '0'
                uint digit2 = text[4] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    valueAsOffset = default;
                    return false;
                }

                day = (int)(digit1 * 10 + digit2);
            }

            if (text[5] != Utf8Constants.Slash)
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            int year;
            {
                uint digit1 = text[6] - 48u; // '0'
                uint digit2 = text[7] - 48u; // '0'
                uint digit3 = text[8] - 48u; // '0'
                uint digit4 = text[9] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9 || digit3 > 9 || digit4 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    valueAsOffset = default;
                    return false;
                }

                year = (int)(digit1 * 1000 + digit2 * 100 + digit3 * 10 + digit4);
            }

            if (text[10] != Utf8Constants.Space)
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            int hour;
            {
                uint digit1 = text[11] - 48u; // '0'
                uint digit2 = text[12] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    valueAsOffset = default;
                    return false;
                }

                hour = (int)(digit1 * 10 + digit2);
            }

            if (text[13] != Utf8Constants.Colon)
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            int minute;
            {
                uint digit1 = text[14] - 48u; // '0'
                uint digit2 = text[15] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    valueAsOffset = default;
                    return false;
                }

                minute = (int)(digit1 * 10 + digit2);
            }

            if (text[16] != Utf8Constants.Colon)
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            int second;
            {
                uint digit1 = text[17] - 48u; // '0'
                uint digit2 = text[18] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    valueAsOffset = default;
                    return false;
                }

                second = (int)(digit1 * 10 + digit2);
            }

            if (!TryCreateDateTimeOffsetInterpretingDataAsLocalTime(year: year, month: month, day: day, hour: hour, minute: minute, second: second, fraction: 0, out valueAsOffset))
            {
                bytesConsumed = 0;
                value = default;
                valueAsOffset = default;
                return false;
            }

            bytesConsumed = 19;
            value = valueAsOffset.DateTime;
            return true;
        }
    }
}
