// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonWriterHelper
    {
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
            // Assert buffer is the right length for:
            // YYYY-MM-DDThh:mm:ss.fffffff (JsonConstants.MaximumFormatDateTimeLength)
            // YYYY-MM-DDThh:mm:ss.fffffffZ (JsonConstants.MaximumFormatDateTimeLength + 1)
            // YYYY-MM-DDThh:mm:ss.fffffff(+|-)hh:mm (JsonConstants.MaximumFormatDateTimeOffsetLength)
            Debug.Assert(buffer.Length == JsonConstants.MaximumFormatDateTimeLength ||
                buffer.Length == (JsonConstants.MaximumFormatDateTimeLength + 1) ||
                buffer.Length == JsonConstants.MaximumFormatDateTimeOffsetLength);

            if (buffer.Length == JsonConstants.MaximumFormatDateTimeOffsetLength)
            {
                Debug.Assert(buffer[30] == JsonConstants.Colon);
            }

            uint digit7 = buffer[26] - (uint)'0';
            uint digit6 = buffer[25] - (uint)'0';
            uint digit5 = buffer[24] - (uint)'0';
            uint digit4 = buffer[23] - (uint)'0';
            uint digit3 = buffer[22] - (uint)'0';
            uint digit2 = buffer[21] - (uint)'0';
            uint digit1 = buffer[20] - (uint)'0';
            uint fraction = (digit1 * 1_000_000) + (digit2 * 100_000) + (digit3 * 10_000) + (digit4 * 1_000) + (digit5 * 100) + (digit6 * 10) + digit7;

            // The period's index
            int curIndex = 19;

            if (fraction > 0)
            {
                int numFractionDigits = 7;

                // Remove trailing zeros
                while (true)
                {
                    uint quotient = DivMod(fraction, 10, out uint remainder);
                    if (remainder != 0)
                    {
                        break;
                    }
                    fraction = quotient;
                    numFractionDigits--;
                }

                // The last fraction digit's index will be (the period's index plus one) + (the number of fraction digits minus one)
                int fractionEnd = 19 + numFractionDigits;

                // Write fraction
                // Leading zeros are written because the fraction becomes zero when it's their turn
                for (int i = fractionEnd; i > curIndex; i--)
                {
                    buffer[i] = (byte)((fraction % 10) + (uint)'0');
                    fraction /= 10;
                }

                curIndex = fractionEnd + 1;
            }

            bytesWritten = curIndex;

            // We are either trimming a DateTimeOffset, or a DateTime with
            // DateTimeKind.Local or DateTimeKind.Utc
            if (buffer.Length > JsonConstants.MaximumFormatDateTimeLength)
            {
                // Write offset

                buffer[curIndex] = buffer[27];

                // curIndex is at one of 'Z', '+', or '-'
                bytesWritten = curIndex + 1;

                // We have a Non-UTC offset i.e. (+|-)hh:mm
                if (buffer.Length == JsonConstants.MaximumFormatDateTimeOffsetLength)
                {
                    // Last index of the offset
                    int bufferEnd = curIndex + 5;

                    // Cache offset characters to prevent them from being overwritten
                    // The second minute digit is never at risk
                    byte offsetMinDigit1 = buffer[31];
                    byte offsetHourDigit2 = buffer[29];
                    byte offsetHourDigit1 = buffer[28];

                    // Write offset characters
                    buffer[bufferEnd] = buffer[32];
                    buffer[bufferEnd - 1] = offsetMinDigit1;
                    buffer[bufferEnd - 2] = JsonConstants.Colon;
                    buffer[bufferEnd - 3] = offsetHourDigit2;
                    buffer[bufferEnd - 4] = offsetHourDigit1;

                    // bytes written is the last index of the offset + 1
                    bytesWritten = bufferEnd + 1;
                }
            }
        }

        // We don't have access to System.Buffers.Text.FormattingHelpers.DivMod,
        // so this is a copy of the implementation.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint DivMod(uint numerator, uint denominator, out uint modulo)
        {
            uint div = numerator / denominator;
            modulo = numerator - (div * denominator);
            return div;
        }
    }
}
