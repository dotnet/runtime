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
        ///     G (default)   True/False
        ///     l             true/false
        /// </remarks>
        /// <exceptions>
        /// <cref>System.FormatException</cref> if the format is not valid for this data type.
        /// </exceptions>
        public static bool TryParse(ReadOnlySpan<byte> text, out bool value, out int bytesConsumed, char standardFormat = default)
        {
            if (!(standardFormat == default(char) || standardFormat == 'G' || standardFormat == 'l'))
                return ThrowHelper.TryParseThrowFormatException(out value, out bytesConsumed);

            if (text.Length >= 4)
            {
                if ((text[0] == 'T' || text[0] == 't') &&
                    (text[1] == 'R' || text[1] == 'r') &&
                    (text[2] == 'U' || text[2] == 'u') &&
                    (text[3] == 'E' || text[3] == 'e'))
                {
                    bytesConsumed = 4;
                    value = true;
                    return true;
                }
                if (text.Length >= 5)
                {
                    if ((text[0] == 'F' || text[0] == 'f') &&
                        (text[1] == 'A' || text[1] == 'a') &&
                        (text[2] == 'L' || text[2] == 'l') &&
                        (text[3] == 'S' || text[3] == 's') &&
                        (text[4] == 'E' || text[4] == 'e'))
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
