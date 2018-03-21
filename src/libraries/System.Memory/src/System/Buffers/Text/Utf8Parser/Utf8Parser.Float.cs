// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Single at the start of a Utf8 string.
        /// </summary>
        /// <param name="source">The Utf8 string to parse</param>
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
        public static bool TryParse(ReadOnlySpan<byte> source, out float value, out int bytesConsumed, char standardFormat = default)
        {
            if (TryParseNormalAsFloatingPoint(source, out double d, out bytesConsumed, standardFormat))
            {
                value = (float)d;
                if (float.IsInfinity(value))
                {
                    value = default;
                    bytesConsumed = 0;
                    return false;
                }
                return true;
            }

            return TryParseAsSpecialFloatingPoint<float>(source, float.PositiveInfinity, float.NegativeInfinity, float.NaN, out value, out bytesConsumed);
        }

        /// <summary>
        /// Parses a Double at the start of a Utf8 string.
        /// </summary>
        /// <param name="source">The Utf8 string to parse</param>
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
        public static bool TryParse(ReadOnlySpan<byte> source, out double value, out int bytesConsumed, char standardFormat = default)
        {
            if (TryParseNormalAsFloatingPoint(source, out value, out bytesConsumed, standardFormat))
                return true;

            return TryParseAsSpecialFloatingPoint<double>(source, double.PositiveInfinity, double.NegativeInfinity, double.NaN, out value, out bytesConsumed);
        }

        //
        // Attempt to parse the regular floating points (the ones without names like "Infinity" and "NaN")
        //
        private static bool TryParseNormalAsFloatingPoint(ReadOnlySpan<byte> source, out double value, out int bytesConsumed, char standardFormat)
        {
            ParseNumberOptions options;
            switch (standardFormat)
            {
                case default(char):
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
            if (!TryParseNumber(source, ref number, out bytesConsumed, options, out bool textUsedExponentNotation))
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

            if (number.Digits[0] == 0)
            {
                number.IsNegative = false;
            }

            if (!Number.NumberBufferToDouble(ref number, out value))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            return true;
        }

        //
        // Assuming the text doesn't look like a normal floating point, we attempt to parse it as one the special floating point values.
        //
        private static bool TryParseAsSpecialFloatingPoint<T>(ReadOnlySpan<byte> source, T positiveInfinity, T negativeInfinity, T nan, out T value, out int bytesConsumed)
        {
            if (source.Length >= 8 &&
                source[0] == 'I' && source[1] == 'n' && source[2] == 'f' && source[3] == 'i' &&
                source[4] == 'n' && source[5] == 'i' && source[6] == 't' && source[7] == 'y')
            {
                value = positiveInfinity;
                bytesConsumed = 8;
                return true;
            }

            if (source.Length >= 9 &&
                source[0] == Utf8Constants.Minus &&
                source[1] == 'I' && source[2] == 'n' && source[3] == 'f' && source[4] == 'i' &&
                source[5] == 'n' && source[6] == 'i' && source[7] == 't' && source[8] == 'y')
            {
                value = negativeInfinity;
                bytesConsumed = 9;
                return true;
            }

            if (source.Length >= 3 &&
                source[0] == 'N' && source[1] == 'a' && source[2] == 'N')
            {
                value = nan;
                bytesConsumed = 3;
                return true;
            }

            value = default;
            bytesConsumed = 0;
            return false;
        }
    }
}
