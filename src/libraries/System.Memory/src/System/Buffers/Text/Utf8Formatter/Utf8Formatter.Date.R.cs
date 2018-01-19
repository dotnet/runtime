// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        // Rfc1123
        //
        //   01234567890123456789012345678
        //   -----------------------------
        //   Tue, 03 Jan 2017 08:08:05 GMT
        //
        private static bool TryFormatDateTimeR(DateTime value, Span<byte> buffer, out int bytesWritten)
        {
            // Writing the check in this fashion elides all bounds checks on 'buffer'
            // for the remainder of the method.
            if ((uint)28 >= (uint)buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }

            var dayAbbrev = DayAbbreviations[(int)value.DayOfWeek];

            buffer[0] = (byte)dayAbbrev;
            dayAbbrev >>= 8;
            buffer[1] = (byte)dayAbbrev;
            dayAbbrev >>= 8;
            buffer[2] = (byte)dayAbbrev;
            buffer[3] = Utf8Constants.Comma;
            buffer[4] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Day, buffer, 5);
            buffer[7] = Utf8Constants.Space;

            var monthAbbrev = MonthAbbreviations[value.Month - 1];
            buffer[8] = (byte)monthAbbrev;
            monthAbbrev >>= 8;
            buffer[9] = (byte)monthAbbrev;
            monthAbbrev >>= 8;
            buffer[10] = (byte)monthAbbrev;
            buffer[11] = Utf8Constants.Space;

            FormattingHelpers.WriteFourDecimalDigits((uint)value.Year, buffer, 12);
            buffer[16] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Hour, buffer, 17);
            buffer[19] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Minute, buffer, 20);
            buffer[22] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Second, buffer, 23);
            buffer[25] = Utf8Constants.Space;

            buffer[26] = GMT1;
            buffer[27] = GMT2;
            buffer[28] = GMT3;

            bytesWritten = 29;
            return true;
        }
    }
}
