// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Formatter
    {
        /// <summary>
        /// Formats a Decimal as a UTF8 string.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="destination">Buffer to write the UTF8-formatted value to</param>
        /// <param name="bytesWritten">Receives the length of the formatted text in bytes</param>
        /// <param name="format">The standard format to use</param>
        /// <returns>
        /// true for success. "bytesWritten" contains the length of the formatted text in bytes.
        /// false if buffer was too short. Iteratively increase the size of the buffer and retry until it succeeds. 
        /// </returns>
        /// <remarks>
        /// Formats supported:
        ///     G/g  (default)  
        ///     F/f             12.45       Fixed point
        ///     E/e             1.245000e1  Exponential
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static unsafe bool TryFormat(decimal value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            if (format.IsDefault)
            {
                format = 'G';
            }

            switch (format.Symbol)
            {
                case 'g':
                case 'G':
                    {
                        if (format.Precision != StandardFormat.NoPrecision)
                            throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);

                        byte* pDigits = stackalloc byte[Number.DecimalNumberBufferLength];
                        Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, pDigits, Number.DecimalNumberBufferLength);

                        Number.DecimalToNumber(ref value, ref number);
                        if (number.Digits[0] == 0)	
                        {	
                            number.IsNegative = false; // For Decimals, -0 must print as normal 0.	
                        }
                        bool success = TryFormatDecimalG(ref number, destination, out bytesWritten);
#if DEBUG
                        // This DEBUG segment exists to close a code coverage hole inside TryFormatDecimalG(). Because we don't call RoundNumber() on this path, we have no way to feed
                        // TryFormatDecimalG() a number where trailing zeros before the decimal point have been cropped. So if the chance comes up, we'll crop the zeroes
                        // ourselves and make a second call to ensure we get the same outcome.
                        if (success)
                        {
                            Span<byte> digits = number.Digits;
                            int numDigits = number.DigitsCount;
                            if (numDigits != 0 && number.Scale == numDigits && digits[numDigits - 1] == '0')
                            {
                                while (numDigits != 0 && digits[numDigits - 1] == '0')
                                {
                                    digits[numDigits - 1] = 0;
                                    numDigits--;
                                }

                                number.DigitsCount = numDigits;
                                number.CheckConsistency();

                                byte[] buffer2 = new byte[destination.Length];
                                bool success2 = TryFormatDecimalG(ref number, buffer2, out int bytesWritten2);
                                Debug.Assert(success2);
                                Debug.Assert(bytesWritten2 == bytesWritten);
                                for (int i = 0; i < bytesWritten; i++)
                                {
                                    Debug.Assert(destination[i] == buffer2[i]);
                                }
                            }

                        }
#endif // DEBUG
                        return success;
                    }

                case 'f':
                case 'F':
                    {
                        byte* pDigits = stackalloc byte[Number.DecimalNumberBufferLength];
                        Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, pDigits, Number.DecimalNumberBufferLength);

                        Number.DecimalToNumber(ref value, ref number);
                        byte precision = (format.Precision == StandardFormat.NoPrecision) ? (byte)2 : format.Precision;
                        Number.RoundNumber(ref number, number.Scale + precision);
                        Debug.Assert((number.Digits[0] != 0) || !number.IsNegative);   // For Decimals, -0 must print as normal 0. As it happens, Number.RoundNumber already ensures this invariant.
                        return TryFormatDecimalF(ref number, destination, out bytesWritten, precision);
                    }

                case 'e':
                case 'E':
                    {
                        byte* pDigits = stackalloc byte[Number.DecimalNumberBufferLength];
                        Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, pDigits, Number.DecimalNumberBufferLength);

                        Number.DecimalToNumber(ref value, ref number);
                        byte precision = (format.Precision == StandardFormat.NoPrecision) ? (byte)6 : format.Precision;
                        Number.RoundNumber(ref number, precision + 1);
                        Debug.Assert((number.Digits[0] != 0) || !number.IsNegative);   // For Decimals, -0 must print as normal 0. As it happens, Number.RoundNumber already ensures this invariant.
                        return TryFormatDecimalE(ref number, destination, out bytesWritten, precision, exponentSymbol: (byte)format.Symbol);
                    }

                default:
                    return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
            }
        }
    }
}
