// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            if (destination.Length <= 28)
            {
                bytesWritten = 0;
                return false;
            }

            value.GetDate(out int year, out int month, out int day);
            value.GetTime(out int hour, out int minute, out int second);

            FormattingHelpers.CopyFourBytes("sun,mon,tue,wed,thu,fri,sat,"u8.Slice(4 * (int)value.DayOfWeek), destination);
            destination[4] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 5);
            destination[7] = Utf8Constants.Space;

            FormattingHelpers.CopyFourBytes("jan feb mar apr may jun jul aug sep oct nov dec "u8.Slice(4 * (month - 1)), destination.Slice(8));

            FormattingHelpers.WriteFourDecimalDigits((uint)year, destination, 12);
            destination[16] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 17);
            destination[19] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 20);
            destination[22] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 23);

            FormattingHelpers.CopyFourBytes(" gmt"u8, destination.Slice(25));

            bytesWritten = 29;
            return true;
        }
    }
}
