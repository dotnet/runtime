// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        private static bool TryFormatDecimalF(ref Number.NumberBuffer number, Span<byte> destination, out int bytesWritten, byte precision)
        {
            int scale = number.Scale;
            ReadOnlySpan<byte> digits = number.Digits;

            int numBytesNeeded =
                ((number.IsNegative) ? 1 : 0) // minus sign
                + ((scale <= 0) ? 1 : scale)  // digits before the decimal point (minimum 1)
                + ((precision == 0) ? 0 : (precision + 1)); // if specified precision != 0, the decimal point and the digits after the decimal point (padded with zeroes if needed)

            if (destination.Length < numBytesNeeded)
            {
                bytesWritten = 0;
                return false;
            }

            int srcIndex = 0;
            int dstIndex = 0;
            if (number.IsNegative)
            {
                destination[dstIndex++] = Utf8Constants.Minus;
            }

            //
            // Emit digits before the decimal point.
            //
            if (scale <= 0)
            {
                destination[dstIndex++] = (byte)'0';  // The integer portion is 0 and not stored. The formatter, however, needs to emit it.
            }
            else
            {
                while (srcIndex < scale)
                {
                    byte digit = digits[srcIndex];
                    if (digit == 0)
                    {
                        int numTrailingZeroes = scale - srcIndex;
                        for (int i = 0; i < numTrailingZeroes; i++)
                        {
                            destination[dstIndex++] = (byte)'0';
                        }
                        break;
                    }

                    destination[dstIndex++] = digit;
                    srcIndex++;
                }
            }

            if (precision > 0)
            {
                destination[dstIndex++] = Utf8Constants.Period;

                //
                // Emit digits after the decimal point.
                //
                int numDigitsEmitted = 0;
                if (scale < 0)
                {
                    int numLeadingZeroesToEmit = Math.Min((int)precision, -scale);
                    for (int i = 0; i < numLeadingZeroesToEmit; i++)
                    {
                        destination[dstIndex++] = (byte)'0';
                    }
                    numDigitsEmitted += numLeadingZeroesToEmit;
                }

                while (numDigitsEmitted < precision)
                {
                    byte digit = digits[srcIndex];
                    if (digit == 0)
                    {
                        while (numDigitsEmitted++ < precision)
                        {
                            destination[dstIndex++] = (byte)'0';
                        }
                        break;
                    }
                    destination[dstIndex++] = digit;
                    srcIndex++;
                    numDigitsEmitted++;
                }
            }

            Debug.Assert(dstIndex == numBytesNeeded);
            bytesWritten = numBytesNeeded;
            return true;
        }
    }
}
