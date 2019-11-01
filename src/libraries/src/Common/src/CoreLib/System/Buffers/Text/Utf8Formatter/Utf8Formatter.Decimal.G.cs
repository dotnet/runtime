// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        private static bool TryFormatDecimalG(ref Number.NumberBuffer number, Span<byte> destination, out int bytesWritten)
        {
            int scale = number.Scale;
            ReadOnlySpan<byte> digits = number.Digits;
            int numDigits = number.DigitsCount;

            bool isFraction = scale < numDigits;
            int numBytesNeeded;
            if (isFraction)
            {
                numBytesNeeded = numDigits + 1;  // A fraction. Must include one for the decimal point.
                if (scale <= 0)
                {
                    numBytesNeeded += 1 + (-scale); // A fraction of the form 0.ddd. Need to emit the non-stored 0 before the decimal point plus (-scale) leading 0's after the decimal point.
                }
            }
            else
            {
                numBytesNeeded = ((scale <= 0) ? 1 : scale); // An integral. Just emit the digits before the decimal point (minimum 1) and no decimal point.
            }

            if (number.IsNegative)
            {
                numBytesNeeded++; // And the minus sign.
            }

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

            if (isFraction)
            {
                destination[dstIndex++] = Utf8Constants.Period;

                //
                // Emit digits after the decimal point.
                //
                if (scale < 0)
                {
                    int numLeadingZeroesToEmit = -scale;
                    for (int i = 0; i < numLeadingZeroesToEmit; i++)
                    {
                        destination[dstIndex++] = (byte)'0';
                    }
                }

                byte digit;
                while ((digit = digits[srcIndex++]) != 0)
                {
                    destination[dstIndex++] = digit;
                }
            }

            Debug.Assert(dstIndex == numBytesNeeded);

            bytesWritten = numBytesNeeded;
            return true;
        }
    }
}
