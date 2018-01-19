// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static bool TryFormatDateTimeO(DateTime value, TimeSpan offset, Span<byte> buffer, out int bytesWritten)
        {
            const int MinimumBytesNeeded = 27;

            int bytesRequired = MinimumBytesNeeded;
            DateTimeKind kind = DateTimeKind.Local;

            if (offset == Utf8Constants.s_nullUtcOffset)
            {
                kind = value.Kind;
                if (kind == DateTimeKind.Local)
                {
                    offset = TimeZoneInfo.Local.GetUtcOffset(value);
                    bytesRequired += 6;
                }
                else if (kind == DateTimeKind.Utc)
                {
                    bytesRequired += 1;
                }
            }
            else
            {
                bytesRequired += 6;
            }

            if (buffer.Length < bytesRequired)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = bytesRequired;

            // Hoist most of the bounds checks on buffer.
            { var unused = buffer[MinimumBytesNeeded - 1]; }

            FormattingHelpers.WriteFourDecimalDigits((uint)value.Year, buffer, 0);
            buffer[4] = Utf8Constants.Minus;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Month, buffer, 5);
            buffer[7] = Utf8Constants.Minus;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Day, buffer, 8);
            buffer[10] = TimeMarker;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Hour, buffer, 11);
            buffer[13] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Minute, buffer, 14);
            buffer[16] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Second, buffer, 17);
            buffer[19] = Utf8Constants.Period;

            FormattingHelpers.WriteDigits((uint)((ulong)value.Ticks % (ulong)TimeSpan.TicksPerSecond), buffer.Slice(20, 7));

            if (kind == DateTimeKind.Local)
            {
                byte sign;

                if (offset < default(TimeSpan) /* a "const" version of TimeSpan.Zero */)
                {
                    sign = Utf8Constants.Minus;
                    offset = TimeSpan.FromTicks(-offset.Ticks);
                }
                else
                {
                    sign = Utf8Constants.Plus;
                }

                // Writing the value backward allows the JIT to optimize by
                // performing a single bounds check against buffer.

                FormattingHelpers.WriteTwoDecimalDigits((uint)offset.Minutes, buffer, 31);
                buffer[30] = Utf8Constants.Colon;
                FormattingHelpers.WriteTwoDecimalDigits((uint)offset.Hours, buffer, 28);
                buffer[27] = sign;

            }
            else if (kind == DateTimeKind.Utc)
            {
                buffer[27] = UtcMarker;
            }

            return true;
        }
    }
}
