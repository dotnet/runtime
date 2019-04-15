// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        //
        // ToString() format for DateTimeOffset. Does not have a corresponding format symbol but it
        // is the "G" format postpended with the UTC offset.
        //
        // 01234567890123456789012345
        // --------------------------
        // 05/25/2017 10:30:15 -08:00
        //
        private static bool TryParseDateTimeOffsetDefault(ReadOnlySpan<byte> source, out DateTimeOffset value, out int bytesConsumed)
        {
            if (source.Length < 26)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            if (!TryParseDateTimeG(source, out DateTime dateTime, out _, out _))
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            if (source[19] != Utf8Constants.Space)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            byte sign = source[20];
            if (sign != Utf8Constants.Plus && sign != Utf8Constants.Minus)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            int offsetHours;
            {
                uint digit1 = source[21] - 48u; // '0'
                uint digit2 = source[22] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    return false;
                }

                offsetHours = (int)(digit1 * 10 + digit2);
            }

            if (source[23] != Utf8Constants.Colon)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            int offsetMinutes;
            {
                uint digit1 = source[24] - 48u; // '0'
                uint digit2 = source[25] - 48u; // '0'

                if (digit1 > 9 || digit2 > 9)
                {
                    bytesConsumed = 0;
                    value = default;
                    return false;
                }

                offsetMinutes = (int)(digit1 * 10 + digit2);
            }

            if (!TryCreateDateTimeOffset(dateTime: dateTime, offsetNegative: sign == Utf8Constants.Minus, offsetHours: offsetHours, offsetMinutes: offsetMinutes, out value))
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            bytesConsumed = 26;
            return true;
        }
    }
}
