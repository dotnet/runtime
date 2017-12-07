// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Decimal at the start of a Utf8 string.
        /// </summary>
        /// <param name="text">The Utf8 string to parse</param>
        /// <param name="value">Receives the parsed value</param>
        /// <param name="bytesConsumed">On a successful parse, receives the length in bytes of the substring that was parsed </param>
        /// <param name="standardFormat">Expected format of the Utf8 string</param>
        /// <returns>
        /// true for success. "bytesConsumed" contains the length in bytes of the substring that was parsed.
        /// false if the string was not syntactically valid or an overflow or underflow occurred. "bytesConsumed" is set to 0. 
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
        public static bool TryParse(ReadOnlySpan<byte> text, out decimal value, out int bytesConsumed, char standardFormat = default)
        {
            ParseNumberOptions options;
            switch (standardFormat)
            {
                case (default):
                case 'G':
                case 'g':
                case 'E':
                case 'e':
                    options = ParseNumberOptions.AllowExponent;
                    break;

                case 'F':
                case 'f':
                    options = default;
                    break;

                default:
                    return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);
            }

            NumberBuffer number = default;
            if (!TryParseNumber(text, ref number, out bytesConsumed, options, out bool textUsedExponentNotation))
            {
                value = default;
                return false;
            }

            if ((!textUsedExponentNotation) && (standardFormat == 'E' || standardFormat == 'e'))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            // More compat with .NET behavior - whether or not a 0 keeps the negative sign depends on whether it an "integer" 0 or a "fractional" 0
            if (number.Digits[0] == 0 && number.Scale == 0)
            {
                number.IsNegative = false;
            }

            value = default;
            if (!Number.NumberBufferToDecimal(ref number, ref value))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            return true;
        }
    }
}
