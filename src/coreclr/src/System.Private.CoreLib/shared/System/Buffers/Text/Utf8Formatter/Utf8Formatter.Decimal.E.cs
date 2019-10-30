// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        private static bool TryFormatDecimalE(ref Number.NumberBuffer number, Span<byte> destination, out int bytesWritten, byte precision, byte exponentSymbol)
        {
            const int NumExponentDigits = 3;

            int scale = number.Scale;
            ReadOnlySpan<byte> digits = number.Digits;

            int numBytesNeeded =
                ((number.IsNegative) ? 1 : 0) // minus sign
                + 1  // digits before the decimal point (exactly 1)
                + ((precision == 0) ? 0 : (precision + 1)) // period and the digits after the decimal point
                + 2  // 'E' or 'e' followed by '+' or '-'
                + NumExponentDigits; // exponent digits

            if (destination.Length < numBytesNeeded)
            {
                bytesWritten = 0;
                return false;
            }

            int dstIndex = 0;
            int srcIndex = 0;
            if (number.IsNegative)
            {
                destination[dstIndex++] = Utf8Constants.Minus;
            }

            //
            // Emit exactly one digit before the decimal point.
            //
            int exponent;
            byte firstDigit = digits[srcIndex];
            if (firstDigit == 0)
            {
                destination[dstIndex++] = (byte)'0';  // Special case: number before the decimal point is exactly 0: Number does not store the zero in this case.
                exponent = 0;
            }
            else
            {
                destination[dstIndex++] = firstDigit;
                srcIndex++;
                exponent = scale - 1;
            }

            if (precision > 0)
            {
                destination[dstIndex++] = Utf8Constants.Period;

                //
                // Emit digits after the decimal point.
                //
                int numDigitsEmitted = 0;
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

            // Emit the exponent symbol
            destination[dstIndex++] = exponentSymbol;
            if (exponent >= 0)
            {
                destination[dstIndex++] = Utf8Constants.Plus;
            }
            else
            {
                destination[dstIndex++] = Utf8Constants.Minus;
                exponent = -exponent;
            }

            Debug.Assert(exponent < Number.DecimalPrecision, "If you're trying to reuse this routine for double/float, you'll need to review the code carefully for Decimal-specific assumptions.");

            // Emit exactly three digits for the exponent.
            destination[dstIndex++] = (byte)'0'; // The exponent for Decimal can never exceed 28 (let alone 99)
            destination[dstIndex++] = (byte)((exponent / 10) + '0');
            destination[dstIndex++] = (byte)((exponent % 10) + '0');

            Debug.Assert(dstIndex == numBytesNeeded);
            bytesWritten = numBytesNeeded;
            return true;
        }
    }
}
