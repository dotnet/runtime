// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
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
        private static bool TryFormatDateTimeO(DateTime value, TimeSpan offset, Span<byte> destination, out int bytesWritten)
        {
            const int MinimumBytesNeeded = 27;

            int bytesRequired = MinimumBytesNeeded;
            DateTimeKind kind = DateTimeKind.Local;

            if (offset == Utf8Constants.NullUtcOffset)
            {
                kind = value.Kind;
                if (kind == DateTimeKind.Local)
                {
                    offset = TimeZoneInfo.Local.GetUtcOffset(value);
                    bytesRequired += 6;
                }
                else if (kind == DateTimeKind.Utc)
                {
                    bytesRequired++;
                }
            }
            else
            {
                bytesRequired += 6;
            }

            if (destination.Length < bytesRequired)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = bytesRequired;

            // Hoist most of the bounds checks on buffer.
            { _ = destination[MinimumBytesNeeded - 1]; }

            value.GetDate(out int year, out int month, out int day);
            value.GetTimePrecise(out int hour, out int minute, out int second, out int ticks);

            FormattingHelpers.WriteFourDecimalDigits((uint)year, destination, 0);
            destination[4] = Utf8Constants.Minus;

            FormattingHelpers.WriteTwoDecimalDigits((uint)month, destination, 5);
            destination[7] = Utf8Constants.Minus;

            FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 8);
            destination[10] = TimeMarker;

            FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 11);
            destination[13] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 14);
            destination[16] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 17);
            destination[19] = Utf8Constants.Period;

            FormattingHelpers.WriteDigits((uint)ticks, destination.Slice(20, 7));

            if (kind == DateTimeKind.Local)
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

                FormattingHelpers.WriteTwoDecimalDigits((uint)offsetMinutes, destination, 31);
                destination[30] = Utf8Constants.Colon;
                FormattingHelpers.WriteTwoDecimalDigits((uint)offsetHours, destination, 28);
                destination[27] = sign;
            }
            else if (kind == DateTimeKind.Utc)
            {
                destination[27] = UtcMarker;
            }

            return true;
        }
    }
}
