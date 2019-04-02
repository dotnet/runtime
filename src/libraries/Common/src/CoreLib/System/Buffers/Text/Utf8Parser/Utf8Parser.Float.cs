// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers.Binary;

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
        public static unsafe bool TryParse(ReadOnlySpan<byte> source, out float value, out int bytesConsumed, char standardFormat = default)
        {
            byte* pDigits = stackalloc byte[Number.SingleNumberBufferLength];
            Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.FloatingPoint, pDigits, Number.SingleNumberBufferLength);

            if (TryParseNormalAsFloatingPoint(source, ref number, out bytesConsumed, standardFormat))
            {
                value = Number.NumberToSingle(ref number);
                return true;
            }

            return TryParseAsSpecialFloatingPoint(source, float.PositiveInfinity, float.NegativeInfinity, float.NaN, out value, out bytesConsumed);
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
        public static unsafe bool TryParse(ReadOnlySpan<byte> source, out double value, out int bytesConsumed, char standardFormat = default)
        {
            byte* pDigits = stackalloc byte[Number.DoubleNumberBufferLength];
            Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.FloatingPoint, pDigits, Number.DoubleNumberBufferLength);

            if (TryParseNormalAsFloatingPoint(source, ref number, out bytesConsumed, standardFormat))
            {
                value = Number.NumberToDouble(ref number);
                return true;
            }

            return TryParseAsSpecialFloatingPoint(source, double.PositiveInfinity, double.NegativeInfinity, double.NaN, out value, out bytesConsumed);
        }

        //
        // Attempt to parse the regular floating points (the ones without names like "Infinity" and "NaN")
        //
        private static bool TryParseNormalAsFloatingPoint(ReadOnlySpan<byte> source, ref Number.NumberBuffer number, out int bytesConsumed, char standardFormat)
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
                    return ParserHelpers.TryParseThrowFormatException(out bytesConsumed);
            }
            if (!TryParseNumber(source, ref number, out bytesConsumed, options, out bool textUsedExponentNotation))
            {
                return false;
            }
            if ((!textUsedExponentNotation) && (standardFormat == 'E' || standardFormat == 'e'))
            {
                bytesConsumed = 0;
                return false;
            }
            return true;
        }

        //
        // Assuming the text doesn't look like a normal floating point, we attempt to parse it as one the special floating point values.
        //
        private static bool TryParseAsSpecialFloatingPoint<T>(ReadOnlySpan<byte> source, T positiveInfinity, T negativeInfinity, T nan, out T value, out int bytesConsumed) where T : struct
        {            
            int srcIndex = 0;
            int remaining = source.Length;
            bool isNegative = false;

            // We need at least 4 characters to process a sign
            if (remaining >= 4)
            {
                byte c = source[srcIndex];

                switch (c)
                {
                    case Utf8Constants.Minus:
                    {
                        isNegative = true;
                        goto case Utf8Constants.Plus;
                    }

                    case Utf8Constants.Plus:
                    {
                        srcIndex++;
                        remaining--;
                        break;
                    }
                }
            }

            // We can efficiently do an ASCII IsLower check by xor'ing with the expected
            // result and validating that it returns either 0 or exactly 0x20 (which is the
            // delta between lowercase and uppercase ASCII characters).

            if (remaining >= 3)
            {
                if ((((source[srcIndex] ^ (byte)('n')) & ~0x20) == 0) &&
                    (((source[srcIndex + 1] ^ (byte)('a')) & ~0x20) == 0) &&
                    (((source[srcIndex + 2] ^ (byte)('n')) & ~0x20) == 0))
                {
                    value = nan;
                    bytesConsumed = 3 + srcIndex;
                    return true;
                }

                if (remaining >= 8)
                {
                    const int infi = 0x69666E69;
                    int diff = (BinaryPrimitives.ReadInt32LittleEndian(source.Slice(srcIndex)) ^ infi);

                    if ((diff & ~0x20202020) == 0)
                    {
                        const int nity = 0x7974696E;
                        diff = (BinaryPrimitives.ReadInt32LittleEndian(source.Slice(srcIndex + 4)) ^ nity);

                        if ((diff & ~0x20202020) == 0)
                        {
                            value = isNegative ? negativeInfinity : positiveInfinity;
                            bytesConsumed = 8 + srcIndex;
                            return true;
                        }
                    }
                }
            }

            value = default;
            bytesConsumed = 0;
            return false;
        }
    }
}
