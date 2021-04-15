// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonWriterHelper
    {
        private static readonly StandardFormat s_dateTimeStandardFormat = new StandardFormat('O');

        public static void WriteDateTimeTrimmed(Span<byte> buffer, DateTime value, out int bytesWritten)
        {
            Span<byte> tempSpan = stackalloc byte[JsonConstants.MaximumFormatDateTimeOffsetLength];
            bool result = Utf8Formatter.TryFormat(value, tempSpan, out bytesWritten, s_dateTimeStandardFormat);
            Debug.Assert(result);
            TrimDateTimeOffset(tempSpan.Slice(0, bytesWritten), out bytesWritten);
            tempSpan.Slice(0, bytesWritten).CopyTo(buffer);
        }

        public static void WriteDateTimeOffsetTrimmed(Span<byte> buffer, DateTimeOffset value, out int bytesWritten)
        {
            Span<byte> tempSpan = stackalloc byte[JsonConstants.MaximumFormatDateTimeOffsetLength];
            bool result = Utf8Formatter.TryFormat(value, tempSpan, out bytesWritten, s_dateTimeStandardFormat);
            Debug.Assert(result);
            TrimDateTimeOffset(tempSpan.Slice(0, bytesWritten), out bytesWritten);
            tempSpan.Slice(0, bytesWritten).CopyTo(buffer);
        }

        //
        // Trims roundtrippable DateTime(Offset) input.
        // If the milliseconds part of the date is zero, we omit the fraction part of the date,
        // else we write the fraction up to 7 decimal places with no trailing zeros. i.e. the output format is
        // YYYY-MM-DDThh:mm:ss[.s]TZD where TZD = Z or +-hh:mm.
        // e.g.
        //   ---------------------------------
        //   2017-06-12T05:30:45.768-07:00
        //   2017-06-12T05:30:45.00768Z           (Z is short for "+00:00" but also distinguishes DateTimeKind.Utc from DateTimeKind.Local)
        //   2017-06-12T05:30:45                  (interpreted as local time wrt to current time zone)
        public static void TrimDateTimeOffset(Span<byte> buffer, out int bytesWritten)
        {
            const int maxLenNoOffset = JsonConstants.MaximumFormatDateTimeLength;
            const int maxLenWithZ = maxLenNoOffset + 1;
            const int maxLenWithOffset = JsonConstants.MaximumFormatDateTimeOffsetLength;

            // Assert buffer is the right length for:
            // YYYY-MM-DDThh:mm:ss.fffffff (JsonConstants.MaximumFormatDateTimeLength)
            // YYYY-MM-DDThh:mm:ss.fffffffZ (JsonConstants.MaximumFormatDateTimeLength + 1)
            // YYYY-MM-DDThh:mm:ss.fffffff(+|-)hh:mm (JsonConstants.MaximumFormatDateTimeOffsetLength)
            Debug.Assert(buffer.Length == maxLenNoOffset ||
                buffer.Length == maxLenWithZ ||
                buffer.Length == maxLenWithOffset);

            // Find the position after the last significant digit in seconds fraction.
            int curIndex;
            if (buffer[maxLenNoOffset - 1] == '0')
            if (buffer[maxLenNoOffset - 2] == '0')
            if (buffer[maxLenNoOffset - 3] == '0')
            if (buffer[maxLenNoOffset - 4] == '0')
            if (buffer[maxLenNoOffset - 5] == '0')
            if (buffer[maxLenNoOffset - 6] == '0')
            if (buffer[maxLenNoOffset - 7] == '0')
                // All digits are 0 so we can skip over the period also
                curIndex = maxLenNoOffset - 7 - 1;
            else curIndex = maxLenNoOffset - 6;
            else curIndex = maxLenNoOffset - 5;
            else curIndex = maxLenNoOffset - 4;
            else curIndex = maxLenNoOffset - 3;
            else curIndex = maxLenNoOffset - 2;
            else curIndex = maxLenNoOffset - 1;
            else
            {
                // When there is nothing to trim we are done.
                bytesWritten = buffer.Length;
                return;
            }

            // We are either trimming a DateTimeOffset, or a DateTime with
            // DateTimeKind.Local or DateTimeKind.Utc
            if (buffer.Length == maxLenNoOffset)
            {
                bytesWritten = curIndex;
            }
            else
            {
                // Write offset

                if (buffer.Length == maxLenWithOffset)
                {
                    // We have a Non-UTC offset i.e. (+|-)hh:mm

                    // Write offset characters left to right to prevent overwriting owerselfs
                    buffer[curIndex] = buffer[maxLenNoOffset];
                    buffer[curIndex + 1] = buffer[maxLenNoOffset + 1];
                    buffer[curIndex + 2] = buffer[maxLenNoOffset + 2];
                    buffer[curIndex + 3] = buffer[maxLenNoOffset + 3];
                    buffer[curIndex + 4] = buffer[maxLenNoOffset + 4];
                    buffer[curIndex + 5] = buffer[maxLenNoOffset + 5];
                    bytesWritten = curIndex + 6;
                }
                else
                {
                    Debug.Assert(buffer[maxLenWithZ - 1] == 'Z');

                    buffer[curIndex] = (byte)'Z';
                    bytesWritten = curIndex + 1;
                }
            }
        }
    }
}
