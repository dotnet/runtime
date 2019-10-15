// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Text.Unicode.Tests
{
    public partial class Utf8Tests
    {
        private const string X_UTF8 = "58"; // U+0058 LATIN CAPITAL LETTER X, 1 byte
        private const string X_UTF16 = "X";

        private const string Y_UTF8 = "59"; // U+0058 LATIN CAPITAL LETTER Y, 1 byte
        private const string Y_UTF16 = "Y";

        private const string Z_UTF8 = "5A"; // U+0058 LATIN CAPITAL LETTER Z, 1 byte
        private const string Z_UTF16 = "Z";

        private const string E_ACUTE_UTF8 = "C3A9"; // U+00E9 LATIN SMALL LETTER E WITH ACUTE, 2 bytes
        private const string E_ACUTE_UTF16 = "\u00E9";

        private const string EURO_SYMBOL_UTF8 = "E282AC"; // U+20AC EURO SIGN, 3 bytes
        private const string EURO_SYMBOL_UTF16 = "\u20AC";

        private const string REPLACEMENT_CHAR_UTF8 = "EFBFBD"; // U+FFFD REPLACEMENT CHAR, 3 bytes
        private const string REPLACEMENT_CHAR_UTF16 = "\uFFFD";

        private const string GRINNING_FACE_UTF8 = "F09F9880"; // U+1F600 GRINNING FACE, 4 bytes
        private const string GRINNING_FACE_UTF16 = "\U0001F600";

        private const string WOMAN_CARTWHEELING_MEDSKIN_UTF16 = "\U0001F938\U0001F3FD\u200D\u2640\uFE0F"; // U+1F938 U+1F3FD U+200D U+2640 U+FE0F WOMAN CARTWHEELING: MEDIUM SKIN TONE

        // All valid scalars [ U+0000 .. U+D7FF ] and [ U+E000 .. U+10FFFF ].
        private static readonly IEnumerable<Rune> s_allValidScalars = Enumerable.Range(0x0000, 0xD800).Concat(Enumerable.Range(0xE000, 0x110000 - 0xE000)).Select(value => new Rune(value));

        private static readonly ReadOnlyMemory<char> s_allScalarsAsUtf16;
        private static readonly ReadOnlyMemory<byte> s_allScalarsAsUtf8;

        static Utf8Tests()
        {
            List<char> allScalarsAsUtf16 = new List<char>();
            List<byte> allScalarsAsUtf8 = new List<byte>();

            foreach (Rune rune in s_allValidScalars)
            {
                allScalarsAsUtf16.AddRange(ToUtf16(rune));
                allScalarsAsUtf8.AddRange(ToUtf8(rune));
            }

            s_allScalarsAsUtf16 = allScalarsAsUtf16.ToArray().AsMemory();
            s_allScalarsAsUtf8 = allScalarsAsUtf8.ToArray().AsMemory();
        }

        /*
         * COMMON UTILITIES FOR UNIT TESTS
         */

        public static byte[] DecodeHex(ReadOnlySpan<char> inputHex)
        {
            Assert.True(Regex.IsMatch(inputHex.ToString(), "^([0-9a-fA-F]{2})*$"), "Input must be an even number of hex characters.");

            byte[] retVal = new byte[inputHex.Length / 2];
            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = byte.Parse(inputHex.Slice(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            }
            return retVal;
        }

        // !! IMPORTANT !!
        // Don't delete this implementation, as we use it as a reference to make sure the framework's
        // transcoding logic is correct.
        public static byte[] ToUtf8(Rune rune)
        {
            Assert.True(Rune.IsValid(rune.Value), $"Rune with value U+{(uint)rune.Value:X4} is not well-formed.");

            if (rune.Value < 0x80)
            {
                return new[]
                {
                    (byte)rune.Value
                };
            }
            else if (rune.Value < 0x0800)
            {
                return new[]
                {
                    (byte)((rune.Value >> 6) | 0xC0),
                    (byte)((rune.Value & 0x3F) | 0x80)
                };
            }
            else if (rune.Value < 0x10000)
            {
                return new[]
                {
                    (byte)((rune.Value >> 12) | 0xE0),
                    (byte)(((rune.Value >> 6) & 0x3F) | 0x80),
                    (byte)((rune.Value & 0x3F) | 0x80)
                };
            }
            else
            {
                return new[]
                {
                    (byte)((rune.Value >> 18) | 0xF0),
                    (byte)(((rune.Value >> 12) & 0x3F) | 0x80),
                    (byte)(((rune.Value >> 6) & 0x3F) | 0x80),
                    (byte)((rune.Value & 0x3F) | 0x80)
                };
            }
        }

        // !! IMPORTANT !!
        // Don't delete this implementation, as we use it as a reference to make sure the framework's
        // transcoding logic is correct.
        private static char[] ToUtf16(Rune rune)
        {
            Assert.True(Rune.IsValid(rune.Value), $"Rune with value U+{(uint)rune.Value:X4} is not well-formed.");

            if (rune.IsBmp)
            {
                return new[]
                {
                    (char)rune.Value
                };
            }
            else
            {
                return new[]
                {
                    (char)((rune.Value >> 10) + 0xD800 - 0x40),
                    (char)((rune.Value & 0x03FF) + 0xDC00)
                };
            }
        }
    }
}
