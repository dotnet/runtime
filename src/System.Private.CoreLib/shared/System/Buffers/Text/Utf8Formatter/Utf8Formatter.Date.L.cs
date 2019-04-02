// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        // Rfc1123 - lowercase
        //
        //   01234567890123456789012345678
        //   -----------------------------
        //   tue, 03 jan 2017 08:08:05 gmt
        //
        private static bool TryFormatDateTimeL(DateTime value, Span<byte> destination, out int bytesWritten)
        {
            // Writing the check in this fashion elides all bounds checks on 'buffer'
            // for the remainder of the method.
            if ((uint)28 >= (uint)destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            uint dayAbbrev = s_dayAbbreviationsLowercase[(int)value.DayOfWeek];

            destination[0] = (byte)dayAbbrev;
            dayAbbrev >>= 8;
            destination[1] = (byte)dayAbbrev;
            dayAbbrev >>= 8;
            destination[2] = (byte)dayAbbrev;
            destination[3] = Utf8Constants.Comma;
            destination[4] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Day, destination, 5);
            destination[7] = Utf8Constants.Space;

            uint monthAbbrev = s_monthAbbreviationsLowercase[value.Month - 1];
            destination[8] = (byte)monthAbbrev;
            monthAbbrev >>= 8;
            destination[9] = (byte)monthAbbrev;
            monthAbbrev >>= 8;
            destination[10] = (byte)monthAbbrev;
            destination[11] = Utf8Constants.Space;

            FormattingHelpers.WriteFourDecimalDigits((uint)value.Year, destination, 12);
            destination[16] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Hour, destination, 17);
            destination[19] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Minute, destination, 20);
            destination[22] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Second, destination, 23);
            destination[25] = Utf8Constants.Space;

            destination[26] = GMT1Lowercase;
            destination[27] = GMT2Lowercase;
            destination[28] = GMT3Lowercase;

            bytesWritten = 29;
            return true;
        }
    }
}
