// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

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

            bytesWritten = MinimumBytesNeeded;
            if (offset != Utf8Constants.NullUtcOffset)
            {
                bytesWritten += 7; // Space['+'|'-']hh:ss
            }

            if (buffer.Length < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref buffer.DangerousGetPinnableReference();

            FormattingHelpers.WriteDigits(value.Month, 2, ref utf8Bytes, 0);
            Unsafe.Add(ref utf8Bytes, 2) = Utf8Constants.Slash;

            FormattingHelpers.WriteDigits(value.Day, 2, ref utf8Bytes, 3);
            Unsafe.Add(ref utf8Bytes, 5) = Utf8Constants.Slash;

            FormattingHelpers.WriteDigits(value.Year, 4, ref utf8Bytes, 6);
            Unsafe.Add(ref utf8Bytes, 10) = Utf8Constants.Space;

            FormattingHelpers.WriteDigits(value.Hour, 2, ref utf8Bytes, 11);
            Unsafe.Add(ref utf8Bytes, 13) = Utf8Constants.Colon;

            FormattingHelpers.WriteDigits(value.Minute, 2, ref utf8Bytes, 14);
            Unsafe.Add(ref utf8Bytes, 16) = Utf8Constants.Colon;

            FormattingHelpers.WriteDigits(value.Second, 2, ref utf8Bytes, 17);

            if (offset != Utf8Constants.NullUtcOffset)
            {
                Unsafe.Add(ref utf8Bytes, 19) = Utf8Constants.Space;

                int offsetHours = offset.Hours;
                if (offsetHours >= 0)
                {
                    Unsafe.Add(ref utf8Bytes, 20) = Utf8Constants.Plus;
                    FormattingHelpers.WriteDigits(offsetHours, 2, ref utf8Bytes, 21);
                }
                else
                {
                    Unsafe.Add(ref utf8Bytes, 20) = Utf8Constants.Minus;
                    FormattingHelpers.WriteDigits(-offsetHours, 2, ref utf8Bytes, 21);
                }

                int offsetMinutes = Math.Abs(offset.Minutes);
                Unsafe.Add(ref utf8Bytes, 23) = Utf8Constants.Colon;
                FormattingHelpers.WriteDigits(offsetMinutes, 2, ref utf8Bytes, 24);
            }

            return true;
        }
    }
}
