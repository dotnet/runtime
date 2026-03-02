// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexEscapeUnescapeTests
    {
        [Theory]
        [InlineData("Hello", "Hello")]
        [InlineData("#$^*+(){}<>\\|. ", @"\#\$\^\*\+\(\)\{}<>\\\|\.\ ")]
        [InlineData("\n\r\t\f", "\\n\\r\\t\\f")]
        [InlineData(@"\", @"\\")]
        [InlineData("", "")]
        public static void Escape(string str, string expected)
        {
            Assert.Equal(expected, Regex.Escape(str));

            if (expected.Length > 0)
            {
                const int Count = 100;
                Assert.Equal(string.Concat(Enumerable.Repeat(expected, Count)), Regex.Escape(string.Concat(Enumerable.Repeat(str, Count))));
            }
        }

        [Fact]
        public void Escape_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => Regex.Escape(null));
        }

        /// <summary>
        /// Tests that each individual metacharacter is properly escaped.
        /// Metacharacters that should be escaped: \, *, +, ?, |, {, [, (, ), ^, $, ., #, space, \t, \n, \r, \f
        /// </summary>
        [Theory]
        [InlineData("\\", @"\\")]
        [InlineData("*", @"\*")]
        [InlineData("+", @"\+")]
        [InlineData("?", @"\?")]
        [InlineData("|", @"\|")]
        [InlineData("{", @"\{")]
        [InlineData("[", @"\[")]
        [InlineData("(", @"\(")]
        [InlineData(")", @"\)")]
        [InlineData("^", @"\^")]
        [InlineData("$", @"\$")]
        [InlineData(".", @"\.")]
        [InlineData("#", @"\#")]
        [InlineData(" ", @"\ ")]
        [InlineData("\t", @"\t")]
        [InlineData("\n", @"\n")]
        [InlineData("\r", @"\r")]
        [InlineData("\f", @"\f")]
        public static void Escape_IndividualMetacharacters(string str, string expected)
        {
            Assert.Equal(expected, Regex.Escape(str));
        }

        /// <summary>
        /// Tests that characters that are NOT metacharacters are NOT escaped.
        /// Specifically, vertical tab (\v) should NOT be escaped.
        /// </summary>
        [Theory]
        [InlineData("\v")] // vertical tab should NOT be escaped
        [InlineData("a")]
        [InlineData("Z")]
        [InlineData("0")]
        [InlineData("9")]
        [InlineData("-")]
        [InlineData("_")]
        [InlineData(":")]
        [InlineData(";")]
        [InlineData("<")]
        [InlineData(">")]
        [InlineData("=")]
        [InlineData("@")]
        [InlineData("!")]
        [InlineData("~")]
        [InlineData("`")]
        [InlineData("'")]
        [InlineData("\"")]
        [InlineData(",")]
        [InlineData("/")]
        [InlineData("}")]
        [InlineData("]")]
        [InlineData("%")]
        [InlineData("&")]
        public static void Escape_CharactersThatShouldNotBeEscaped(string str)
        {
            Assert.Equal(str, Regex.Escape(str));
        }

        /// <summary>
        /// Tests escape with mixed content containing metacharacters and regular characters.
        /// </summary>
        [Theory]
        [InlineData("a.b", @"a\.b")]
        [InlineData("hello world", @"hello\ world")]
        [InlineData("a*b+c", @"a\*b\+c")]
        [InlineData("(abc)", @"\(abc\)")]
        [InlineData("[abc]", @"\[abc]")]
        [InlineData("{1,2}", @"\{1,2}")]
        [InlineData("a\tb\nc", @"a\tb\nc")]
        [InlineData("a\vb", "a\vb")] // vertical tab should NOT be escaped
        public static void Escape_MixedContent(string str, string expected)
        {
            Assert.Equal(expected, Regex.Escape(str));
        }

        [Theory]
        [InlineData("Hello", "Hello")]
        [InlineData(@"\#\$\^\*\+\(\)\{}<>\\\|\.\ ", "#$^*+(){}<>\\|. ")]
        [InlineData("\\n\\r\\t\\f", "\n\r\t\f")]
        [InlineData(@"\\", @"\")]
        [InlineData(@"\", "")]
        [InlineData("", "")]
        public void Unescape(string str, string expected)
        {
            Assert.Equal(expected, Regex.Unescape(str));

            if (expected.Length > 0)
            {
                const int Count = 100;
                Assert.Equal(string.Concat(Enumerable.Repeat(expected, Count)), Regex.Unescape(string.Concat(Enumerable.Repeat(str, Count))));
            }
        }

        [Fact]
        public void Unscape_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => Regex.Unescape(null));
        }

        /// <summary>
        /// Tests that each individual escape sequence is properly unescaped.
        /// </summary>
        [Theory]
        [InlineData(@"\\", "\\")]
        [InlineData(@"\*", "*")]
        [InlineData(@"\+", "+")]
        [InlineData(@"\?", "?")]
        [InlineData(@"\|", "|")]
        [InlineData(@"\{", "{")]
        [InlineData(@"\[", "[")]
        [InlineData(@"\(", "(")]
        [InlineData(@"\)", ")")]
        [InlineData(@"\^", "^")]
        [InlineData(@"\$", "$")]
        [InlineData(@"\.", ".")]
        [InlineData(@"\#", "#")]
        [InlineData(@"\ ", " ")]
        [InlineData(@"\t", "\t")]
        [InlineData(@"\n", "\n")]
        [InlineData(@"\r", "\r")]
        [InlineData(@"\f", "\f")]
        [InlineData(@"\v", "\v")]
        [InlineData(@"\a", "\a")]
        [InlineData(@"\e", "\x1B")]
        public static void Unescape_IndividualEscapeSequences(string str, string expected)
        {
            Assert.Equal(expected, Regex.Unescape(str));
        }

        /// <summary>
        /// Tests unescape with hex and unicode escape sequences.
        /// </summary>
        [Theory]
        [InlineData(@"\x41", "A")]
        [InlineData(@"\x61", "a")]
        [InlineData(@"\x20", " ")]
        [InlineData(@"\u0041", "A")]
        [InlineData(@"\u0061", "a")]
        [InlineData(@"\u0020", " ")]
        [InlineData(@"\u03B1", "\u03B1")] // Greek alpha
        public static void Unescape_HexAndUnicodeSequences(string str, string expected)
        {
            Assert.Equal(expected, Regex.Unescape(str));
        }

        /// <summary>
        /// Tests unescape with octal escape sequences.
        /// </summary>
        [Theory]
        [InlineData(@"\0", "\0")]
        [InlineData(@"\00", "\0")]
        [InlineData(@"\000", "\0")]
        [InlineData(@"\101", "A")]  // Octal 101 = 65 = 'A'
        [InlineData(@"\141", "a")]  // Octal 141 = 97 = 'a'
        public static void Unescape_OctalSequences(string str, string expected)
        {
            Assert.Equal(expected, Regex.Unescape(str));
        }

        /// <summary>
        /// Tests unescape with control character escape sequences.
        /// </summary>
        [Theory]
        [InlineData(@"\cA", "\x01")]
        [InlineData(@"\cZ", "\x1A")]
        [InlineData(@"\ca", "\x01")]
        [InlineData(@"\cz", "\x1A")]
        public static void Unescape_ControlCharSequences(string str, string expected)
        {
            Assert.Equal(expected, Regex.Unescape(str));
        }

        /// <summary>
        /// Tests that unescaping a string that was escaped produces the original string.
        /// </summary>
        [Theory]
        [InlineData("Hello")]
        [InlineData("a.b*c+d?e")]
        [InlineData("(test)")]
        [InlineData("[abc]")]
        [InlineData("{1,2}")]
        [InlineData("$100")]
        [InlineData("^start")]
        [InlineData("a|b")]
        [InlineData(@"path\to\file")]
        [InlineData("line1\nline2")]
        [InlineData("tab\there")]
        [InlineData("space here")]
        [InlineData("#comment")]
        public static void EscapeUnescape_RoundTrip(string original)
        {
            string escaped = Regex.Escape(original);
            string unescaped = Regex.Unescape(escaped);
            Assert.Equal(original, unescaped);
        }
    }
}
