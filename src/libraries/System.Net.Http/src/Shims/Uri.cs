// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    // TODO (#5715): Move this class to System.Net.Common.
    internal class UriShim
    {
        private const char c_DummyChar = (char)0xFFFF;
        private static readonly char[] s_hexUpperChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        // Transforms a character into its hexadecimal representation.
        public static string HexEscape(char character)
        {
            if (character > '\xff')
            {
                throw new ArgumentOutOfRangeException(nameof(character));
            }
            char[] chars = new char[3];
            int pos = 0;
            UriShim.EscapeAsciiChar(character, chars, ref pos);
            return new string(chars);
        }

        // HexUnescape
        //
        //  Converts a substring of the form "%XX" to the single character represented
        //  by the hexadecimal value XX. If the substring s[Index] does not conform to
        //  the hex encoding format then the character at s[Index] is returned
        //
        // Inputs:
        //  <argument>  pattern
        //      String from which to read the hexadecimal encoded substring
        //
        //  <argument>  index
        //      Offset within <pattern> from which to start reading the hexadecimal
        //      encoded substring
        //
        // Outputs:
        //  <argument>  index
        //      Incremented to the next character position within the string. This
        //      may be EOS if this was the last character/encoding within <pattern>
        //
        // Returns:
        //  Either the converted character if <pattern>[<index>] was hex encoded, or
        //  the character at <pattern>[<index>]
        //
        // Throws:
        //  ArgumentOutOfRangeException
        public static char HexUnescape(string pattern, ref int index)
        {
            if ((index < 0) || (index >= pattern.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if ((pattern[index] == '%')
                && (pattern.Length - index >= 3))
            {
                char ret = UriShim.EscapedAscii(pattern[index + 1], pattern[index + 2]);
                if (ret != c_DummyChar)
                {
                    index += 3;
                    return ret;
                }
            }
            return pattern[index++];
        }

        // IsHexEncoding
        //
        //  Determines whether a substring has the URI hex encoding format of '%'
        //  followed by 2 hexadecimal characters
        //
        // Inputs:
        //  <argument>  pattern
        //      String to check
        //
        //  <argument>  index
        //      Offset in <pattern> at which to check substring for hex encoding
        //
        // Assumes:
        //  0 <= <index> < <pattern>.Length
        //
        // Returns:
        //  true if <pattern>[<index>] is hex encoded, else false
        //
        // Throws:
        //  Nothing
        public static bool IsHexEncoding(string pattern, int index)
        {
            if ((pattern.Length - index) < 3)
            {
                return false;
            }
            if ((pattern[index] == '%') && UriShim.EscapedAscii(pattern[index + 1], pattern[index + 2]) != c_DummyChar)
            {
                return true;
            }
            return false;
        }

        internal static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = s_hexUpperChars[(ch & 0xf0) >> 4];
            to[pos++] = s_hexUpperChars[ch & 0xf];
        }

        private static char EscapedAscii(char digit, char next)
        {
            if (!(((digit >= '0') && (digit <= '9'))
                || ((digit >= 'A') && (digit <= 'F'))
                || ((digit >= 'a') && (digit <= 'f'))))
            {
                return c_DummyChar;
            }

            int res = (digit <= '9')
                ? ((int)digit - (int)'0')
                : (((digit <= 'F')
                ? ((int)digit - (int)'A')
                : ((int)digit - (int)'a'))
                   + 10);

            if (!(((next >= '0') && (next <= '9'))
                || ((next >= 'A') && (next <= 'F'))
                || ((next >= 'a') && (next <= 'f'))))
            {
                return c_DummyChar;
            }

            return (char)((res << 4) + ((next <= '9')
                    ? ((int)next - (int)'0')
                    : (((next <= 'F')
                        ? ((int)next - (int)'A')
                        : ((int)next - (int)'a'))
                       + 10)));
        }
    }
}
