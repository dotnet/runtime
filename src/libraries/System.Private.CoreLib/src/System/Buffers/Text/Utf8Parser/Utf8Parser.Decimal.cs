// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Decimal at the start of a Utf8 string.
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
        public static unsafe bool TryParse(ReadOnlySpan<byte> source, out decimal value, out int bytesConsumed, char standardFormat = default)
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
                    return ParserHelpers.TryParseThrowFormatException(out value, out bytesConsumed);
            }

            Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, stackalloc byte[Number.DecimalNumberBufferLength]);

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

            value = default;

            if (!Number.TryNumberToDecimal(ref number, ref value))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            return true;
        }
    }
}
