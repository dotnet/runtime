// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

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
        private static unsafe bool TryFormatDateTimeG(DateTime value, TimeSpan offset, Span<byte> destination, out int bytesWritten)
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

            value.GetDate(out int year, out int month, out int day);
            value.GetTime(out int hour, out int minute, out int second);

            fixed (byte* dest = &MemoryMarshal.GetReference(destination))
            {
                Number.WriteTwoDigits((uint)month, dest);
                dest[2] = Utf8Constants.Slash;

                Number.WriteTwoDigits((uint)day, dest + 3);
                dest[5] = Utf8Constants.Slash;

                Number.WriteFourDigits((uint)year, dest + 6);
                dest[10] = Utf8Constants.Space;

                Number.WriteTwoDigits((uint)hour, dest + 11);
                dest[13] = Utf8Constants.Colon;

                Number.WriteTwoDigits((uint)minute, dest + 14);
                dest[16] = Utf8Constants.Colon;

                Number.WriteTwoDigits((uint)second, dest + 17);

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

                    dest[19] = Utf8Constants.Space;
                    dest[20] = sign;
                    Number.WriteTwoDigits((uint)offsetHours, dest + 21);
                    dest[23] = Utf8Constants.Colon;
                    Number.WriteTwoDigits((uint)offsetMinutes, dest + 24);
                }
            }

            return true;
        }
    }
}
