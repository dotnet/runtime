// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

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

            bytesWritten = MinimumBytesNeeded;
            DateTimeKind kind = DateTimeKind.Local;

            if (offset == Utf8Constants.s_nullUtcOffset)
            {
                kind = value.Kind;
                if (kind == DateTimeKind.Local)
                {
                    offset = TimeZoneInfo.Local.GetUtcOffset(value);
                    bytesWritten += 6;
                }
                else if (kind == DateTimeKind.Utc)
                {
                    bytesWritten += 1;
                }
            }
            else
            {
                bytesWritten += 6;
            }

            if (buffer.Length < bytesWritten)
            {
                bytesWritten = 0;
                return false;
            }

            ref byte utf8Bytes = ref MemoryMarshal.GetReference(buffer);

            FormattingHelpers.WriteDigits(value.Year, 4, ref utf8Bytes, 0);
            Unsafe.Add(ref utf8Bytes, 4) = Utf8Constants.Minus;

            FormattingHelpers.WriteDigits(value.Month, 2, ref utf8Bytes, 5);
            Unsafe.Add(ref utf8Bytes, 7) = Utf8Constants.Minus;

            FormattingHelpers.WriteDigits(value.Day, 2, ref utf8Bytes, 8);
            Unsafe.Add(ref utf8Bytes, 10) = TimeMarker;

            FormattingHelpers.WriteDigits(value.Hour, 2, ref utf8Bytes, 11);
            Unsafe.Add(ref utf8Bytes, 13) = Utf8Constants.Colon;

            FormattingHelpers.WriteDigits(value.Minute, 2, ref utf8Bytes, 14);
            Unsafe.Add(ref utf8Bytes, 16) = Utf8Constants.Colon;

            FormattingHelpers.WriteDigits(value.Second, 2, ref utf8Bytes, 17);
            Unsafe.Add(ref utf8Bytes, 19) = Utf8Constants.Period;

            FormattingHelpers.DivMod(value.Ticks, TimeSpan.TicksPerSecond, out long fraction);
            FormattingHelpers.WriteDigits(fraction, Utf8Constants.DateTimeNumFractionDigits, ref utf8Bytes, 20);

            if (kind == DateTimeKind.Local)
            {
                int hours = offset.Hours;
                byte sign = Utf8Constants.Plus;

                if (offset.Hours < 0)
                {
                    hours = -offset.Hours;
                    sign = Utf8Constants.Minus;
                }

                Unsafe.Add(ref utf8Bytes, 27) = sign;
                FormattingHelpers.WriteDigits(hours, 2, ref utf8Bytes, 28);
                Unsafe.Add(ref utf8Bytes, 30) = Utf8Constants.Colon;
                int offsetMinutes = Math.Abs(offset.Minutes);
                FormattingHelpers.WriteDigits(offsetMinutes, 2, ref utf8Bytes, 31);
            }
            else if (kind == DateTimeKind.Utc)
            {
                Unsafe.Add(ref utf8Bytes, 27) = UtcMarker;
            }

            return true;
        }
    }
}
