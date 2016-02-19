// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    internal static class UriHelper
    {
        const char c_DummyChar = (char)0xFFFF;
        private static readonly char[] HexUpperChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };


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

        private static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = HexUpperChars[(ch & 0xf0) >> 4];
            to[pos++] = HexUpperChars[ch & 0xf];
        }

        public static bool IsHexEncoding(string pattern, int index)
        {
            if ((pattern.Length - index) < 3)
            {
                return false;
            }
            if ((pattern[index] == '%') && EscapedAscii(pattern[index + 1], pattern[index + 2]) != c_DummyChar)
            {
                return true;
            }
            return false;
        }

        public static string HexEscape(char character)
        {
            if (character > '\xff')
            {
                throw new ArgumentOutOfRangeException(nameof(character));
            }
            char[] chars = new char[3];
            int pos = 0;
            EscapeAsciiChar(character, chars, ref pos);
            return new string(chars);
        }

        public static char HexUnescape(string pattern, ref int index)
        {
            if ((index < 0) || (index >= pattern.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if ((pattern[index] == '%')
                && (pattern.Length - index >= 3))
            {
                char ret = EscapedAscii(pattern[index + 1], pattern[index + 2]);
                if (ret != c_DummyChar)
                {
                    index += 3;
                    return ret;
                }
            }
            return pattern[index++];
        }

        public static bool IsDefaultPort(Uri uri)
        {
            Uri defaultPortUri = new Uri(uri.Scheme + "://" + uri.Host);
            return defaultPortUri.Port == uri.Port;
        }
    }
}
