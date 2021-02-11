// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        //
        // 'G' format for DateTime.
        //
        //    0123456789012345678
        //    ---------------------------------
        //    05/25/2017 10:30:15
        //
        //  Also handles the default ToString() format for DateTimeOffset
        //
        //    01234567890123456789012345
        //    --------------------------
        //    05/25/2017 10:30:15 -08:00
        //
        private static bool TryFormatDateTimeG(DateTime value, TimeSpan offset, Span<byte> destination, out int bytesWritten)
        {
            const int MinimumBytesNeeded = 19;

            int bytesRequired = MinimumBytesNeeded;

            if (offset != Utf8Constants.NullUtcOffset)
            {
                bytesRequired += 7; // Space['+'|'-']hh:mm
            }

            if (destination.Length < bytesRequired)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = bytesRequired;

            // Hoist most of the bounds checks on buffer.
            { var unused = destination[MinimumBytesNeeded - 1]; }

            value.GetDate(out int year, out int month, out int day);
            value.GetTime(out int hour, out int minute, out int second);

            FormattingHelpers.WriteTwoDecimalDigits((uint)month, destination, 0);
            destination[2] = Utf8Constants.Slash;

            FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 3);
            destination[5] = Utf8Constants.Slash;

            FormattingHelpers.WriteFourDecimalDigits((uint)year, destination, 6);
            destination[10] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 11);
            destination[13] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 14);
            destination[16] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 17);

            if (offset != Utf8Constants.NullUtcOffset)
            {
                int offsetTotalMinutes = (int)(offset.Ticks / TimeSpan.TicksPerMinute);
                byte sign;

                if (offsetTotalMinutes < 0)
                {
                    sign = Utf8Constants.Minus;
                    offsetTotalMinutes = -offsetTotalMinutes;
                }
                else
                {
                    sign = Utf8Constants.Plus;
                }

                int offsetHours = Math.DivRem(offsetTotalMinutes, 60, out int offsetMinutes);

                // Writing the value backward allows the JIT to optimize by
                // performing a single bounds check against buffer.

                FormattingHelpers.WriteTwoDecimalDigits((uint)offsetMinutes, destination, 24);
                destination[23] = Utf8Constants.Colon;
                FormattingHelpers.WriteTwoDecimalDigits((uint)offsetHours, destination, 21);
                destination[20] = sign;
                destination[19] = Utf8Constants.Space;
            }

            return true;
        }
    }
}
