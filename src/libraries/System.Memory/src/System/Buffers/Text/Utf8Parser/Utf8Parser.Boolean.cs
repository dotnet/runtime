// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        /// <summary>
        /// Parses a Boolean at the start of a Utf8 string.
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
        ///     G (default)   True/False
        ///     l             true/false
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryParse(ReadOnlySpan<byte> source, out bool value, out int bytesConsumed, char standardFormat = default)
        {
            if (!(standardFormat == default(char) || standardFormat == 'G' || standardFormat == 'l'))
                return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);

            if (source.Length >= 4)
            {
                if ((source[0] == 'T' || source[0] == 't') &&
                    (source[1] == 'R' || source[1] == 'r') &&
                    (source[2] == 'U' || source[2] == 'u') &&
                    (source[3] == 'E' || source[3] == 'e'))
                {
                    bytesConsumed = 4;
                    value = true;
                    return true;
                }
                if (source.Length >= 5)
                {
                    if ((source[0] == 'F' || source[0] == 'f') &&
                        (source[1] == 'A' || source[1] == 'a') &&
                        (source[2] == 'L' || source[2] == 'l') &&
                        (source[3] == 'S' || source[3] == 's') &&
                        (source[4] == 'E' || source[4] == 'e'))
                    {
                        bytesConsumed = 5;
                        value = false;
                        return true;
                    }
                }
            }
            bytesConsumed = 0;
            value = default;
            return false;
        }
    }
}
