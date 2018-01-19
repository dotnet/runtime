// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static bool TryFormatDateTimeG(DateTime value, TimeSpan offset, Span<byte> buffer, out int bytesWritten)
        {
            const int MinimumBytesNeeded = 19;

            int bytesRequired = MinimumBytesNeeded;

            if (offset != Utf8Constants.s_nullUtcOffset)
            {
                bytesRequired += 7; // Space['+'|'-']hh:mm
            }

            if (buffer.Length < bytesRequired)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = bytesRequired;

            // Hoist most of the bounds checks on buffer.
            { var unused = buffer[MinimumBytesNeeded - 1]; }

            // TODO: Introduce an API which can parse DateTime instances efficiently, pulling out
            // all their properties (Month, Day, etc.) in one shot. This would help avoid the
            // duplicate work that implicitly results from calling these properties individually.

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Month, buffer, 0);
            buffer[2] = Utf8Constants.Slash;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Day, buffer, 3);
            buffer[5] = Utf8Constants.Slash;

            FormattingHelpers.WriteFourDecimalDigits((uint)value.Year, buffer, 6);
            buffer[10] = Utf8Constants.Space;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Hour, buffer, 11);
            buffer[13] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Minute, buffer, 14);
            buffer[16] = Utf8Constants.Colon;

            FormattingHelpers.WriteTwoDecimalDigits((uint)value.Second, buffer, 17);

            if (offset != Utf8Constants.s_nullUtcOffset)
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

                FormattingHelpers.WriteTwoDecimalDigits((uint)offset.Minutes, buffer, 24);
                buffer[23] = Utf8Constants.Colon;
                FormattingHelpers.WriteTwoDecimalDigits((uint)offset.Hours, buffer, 21);
                buffer[20] = sign;
                buffer[19] = Utf8Constants.Space;
            }

            return true;
        }
    }
}
