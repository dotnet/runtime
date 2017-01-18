// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace System.Net.Tests
{
    public partial class WebUtilityTests
    {
        // HtmlEncode + HtmlDecode
        public static IEnumerable<object[]> HtmlDecode_TestData()
        {
            // Needs decoding
            yield return new object[] { "Hello! &apos;&quot;&lt;&amp;&gt;\u2665&hearts;\u00E7&#xe7;&#231;", "Hello! '\"<&>\u2665\u2665\u00E7\u00E7\u00E7" };
            yield return new object[] { "&#xD7FF;&#xd7ff;", "\uD7FF\uD7FF" };
            yield return new object[] { "&#xE000;&#xe000;", "\uE000\uE000" };
            yield return new object[] { "&#97;&#98;&#99;", "abc" };

            // Surrogate pairs
            yield return new object[] { "&#65536;", "\uD800\uDC00" };
            yield return new object[] { "a&#65536;b", "a\uD800\uDC00b" };
            yield return new object[] { "&#144308;", char.ConvertFromUtf32(144308) };

            // Invalid encoding
            yield return new object[] { "&", "&" };
            yield return new object[] { "&#", "&#" };
            yield return new object[] { "&#x", "&#x" };
            yield return new object[] { "&abc", "&abc" };
            yield return new object[] { "&abc;", "&abc;" };
            yield return new object[] { "&#65536", "&#65536" };
            yield return new object[] { "&#xD7FF", "&#xD7FF" };
            yield return new object[] { "&#xG123;", "&#xG123;" };
            yield return new object[] { "&#xD800;", "&#xD800;" };
            yield return new object[] { "&#xDFFF;", "&#xDFFF;" };
            yield return new object[] { "&#1114112;", "&#1114112;" };
            yield return new object[] { "&#x110000;", "&#x110000;" };
            yield return new object[] { "&#4294967296;", "&#4294967296;" };
            yield return new object[] { "&#x100000000;", "&#x100000000;" };

            // High BMP non-chars
            yield return new object[] { "\uFFFD", "\uFFFD" };
            yield return new object[] { "\uFFFE", "\uFFFE" };
            yield return new object[] { "\uFFFF", "\uFFFF" };

            // Basic
            yield return new object[] { "Hello, world!", "Hello, world!" };
            yield return new object[] { "Hello, world! \"<>\u2665\u00E7", "Hello, world! \"<>\u2665\u00E7" };
            yield return new object[] { "    ", "    " };

            // Empty
            yield return new object[] { "", "" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(HtmlDecode_TestData))]
        public static void HtmlDecode(string value, string expected)
        {
            Assert.Equal(expected, WebUtility.HtmlDecode(value));
        }

        public static IEnumerable<object[]> HtmlEncode_TestData()
        {
            // Single quotes need to be encoded as &#39; rather than &apos; since &#39; is valid both for
            // HTML and XHTML, but &apos; is valid only for XHTML.
            // For more info: http://fishbowl.pastiche.org/2003/07/01/the_curse_of_apos/
            yield return new object[] { "'", "&#39;" };

            yield return new object[] { "Hello! '\"<&>\u2665\u00E7 World", "Hello! &#39;&quot;&lt;&amp;&gt;\u2665&#231; World" };
            yield return new object[] { "<>\"\\&", "&lt;&gt;&quot;\\&amp;" };
            yield return new object[] { "\u00A0", "&#160;" };
            yield return new object[] { "\u00FF", "&#255;" };
            yield return new object[] { "\u0100", "\u0100" };
            yield return new object[] { "\u0021\u0023\u003D\u003F", "!#=?" };

            // Surrogate pairs - default strict settings
            yield return new object[] { char.ConvertFromUtf32(144308), "&#144308;" };
            yield return new object[] { "\uD800\uDC00", "&#65536;" };
            yield return new object[] { "a\uD800\uDC00b", "a&#65536;b" };

            // High BMP non-chars
            yield return new object[] { "\uFFFD", "\uFFFD" };
            yield return new object[] { "\uFFFE", "\uFFFE" };
            yield return new object[] { "\uFFFF", "\uFFFF" };

            // Lone high surrogate
            yield return new object[] { "\uD800", "\uFFFD" };
            yield return new object[] { "\uD800a", "\uFFFDa" };

            // Lone low surrogate
            yield return new object[] { "\uDC00", "\uFFFD" };
            yield return new object[] { "\uDC00a", "\uFFFDa" };

            // Invalid surrogate pair
            yield return new object[] { "\uD800\uD800", "\uFFFD\uFFFD" }; // High, high
            yield return new object[] { "\uDC00\uD800", "\uFFFD\uFFFD" }; // Low, high
            yield return new object[] { "\uDC00\uDC00", "\uFFFD\uFFFD" }; // Low, low

            // Basic
            yield return new object[] { "Hello, world!", "Hello, world!" };
            yield return new object[] { "    ", "    " };

            // Empty string
            yield return new object[] { "", "" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(HtmlEncode_TestData))]
        public static void HtmlEncode(string value, string expected)
        {
            Assert.Equal(expected, WebUtility.HtmlEncode(value));
        }

        // Shared test data for UrlEncode + Decode and their ToBytes counterparts

        public static IEnumerable<Tuple<string, string>> UrlDecode_SharedTestData()
        {
            // Escaping needed - case insensitive hex
            yield return Tuple.Create("%2F%5C%22%09Hello!+%E2%99%A5%3F%2F%5C%22%09World!+%E2%99%A5%3F%E2%99%A5", "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665");
            yield return Tuple.Create("%2f%5c%22%09Hello!+%e2%99%a5%3f%2f%5c%22%09World!+%e2%99%a5%3F%e2%99%a5", "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665");

            // Unecessary escaping
            yield return Tuple.Create("%61%62%63", "abc");
            yield return Tuple.Create("\u1234%61%62%63\u1234", "\u1234abc\u1234");

            // Surrogate pair
            yield return Tuple.Create("%F0%90%8F%BF", "\uD800\uDFFF");
            yield return Tuple.Create("\uD800\uDFFF", "\uD800\uDFFF");

            // Spaces
            yield return Tuple.Create("abc+def", "abc def");
            yield return Tuple.Create("++++", "    ");
            yield return Tuple.Create("    ", "    ");

            // No decoding needed
            yield return Tuple.Create("abc", "abc");
            yield return Tuple.Create("", "");
            yield return Tuple.Create("Hello, world", "Hello, world");
            yield return Tuple.Create("\u1234\u2345", "\u1234\u2345");
            yield return Tuple.Create("abc\u1234\u2345def\u1234", "abc\u1234\u2345def\u1234");

            // Invalid percent encoding
            yield return Tuple.Create("%", "%");
            yield return Tuple.Create("%A", "%A");
            yield return Tuple.Create("%\01", "%\01");
            yield return Tuple.Create("%1\0", "%1\0");
            yield return Tuple.Create("%g1", "%g1");
            yield return Tuple.Create("%1g", "%1g");
            yield return Tuple.Create("%G1", "%G1");
            yield return Tuple.Create("%1G", "%1G");
        }

        public static IEnumerable<Tuple<string, string>> UrlEncode_SharedTestData()
        {
            // RFC 3986 requires returned hex-encoded chars to be uppercase
            yield return Tuple.Create("/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665", "%2F%5C%22%09Hello!+%E2%99%A5%3F%2F%5C%22%09World!+%E2%99%A5%3F%E2%99%A5");
            yield return Tuple.Create("'", "%27");
            yield return Tuple.Create("\uD800\uDFFF", "%F0%90%8F%BF"); // Surrogate pairs should be encoded as 4 bytes together

            // No encoding needed
            yield return Tuple.Create("abc", "abc");
            yield return Tuple.Create("", "");

            // Spaces
            yield return Tuple.Create("abc def", "abc+def");
            yield return Tuple.Create("    ", "++++");
            yield return Tuple.Create("++++", "%2B%2B%2B%2B");

            // Tests for stray surrogate chars (all should be encoded as U+FFFD)            
            yield return Tuple.Create("\uD800", "%EF%BF%BD"); // High surrogate
            yield return Tuple.Create("\uDC00", "%EF%BF%BD"); // Low surrogate

            yield return Tuple.Create("\uDC00\uD800", "%EF%BF%BD%EF%BF%BD"); // Low + high
            yield return Tuple.Create("\uD900\uDA00", "%EF%BF%BD%EF%BF%BD"); // High + high
            yield return Tuple.Create("\uDE00\uDF00", "%EF%BF%BD%EF%BF%BD"); // Low + low

            yield return Tuple.Create("!\uDB00@", "!%EF%BF%BD%40"); // Non-surrogate + high + non-surrogate
            yield return Tuple.Create("#\uDD00$", "%23%EF%BF%BD%24"); // Non-surrogate + low + non-surrogate
        }

        public static IEnumerable<object[]> UrlEncodeDecode_Roundtrip_SharedTestData()
        {
            yield return new object[] { "'" };
            yield return new object[] { "http://www.microsoft.com" };
            yield return new object[] { "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665" };
            yield return new object[] { "\uD800\uDFFF" }; // Surrogate pairs

            yield return new object[] { CharRange('\uE000', '\uF8FF') }; // BMP private use chars
            yield return new object[] { CharRange('\uFDD0', '\uFDEF') }; // Low BMP non-chars
            yield return new object[] { "\uFFFE\uFFFF" }; // High BMP non-chars

            yield return new object[] { CharRange('\0', '\u001F') }; // C0 controls
            yield return new object[] { CharRange('\u0080', '\u009F') }; // C1 controls

            yield return new object[] { CharRange('\u202A', '\u202E') }; // BIDI embedding and override
            yield return new object[] { CharRange('\u2066', '\u2069') }; // BIDI isolate

            yield return new object[] { "\uFEFF" }; // BOM
        }

        // UrlEncode + UrlDecode

        public static IEnumerable<object[]> UrlDecode_TestData()
        {
            foreach (var tuple in UrlDecode_SharedTestData())
                yield return new object[] { tuple.Item1, tuple.Item2 };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(UrlDecode_TestData))]
        public static void UrlDecode(string encodedValue, string expected)
        {
            Assert.Equal(expected, WebUtility.UrlDecode(encodedValue));
        }

        public static IEnumerable<object[]> UrlEncode_TestData()
        {
            foreach (var tuple in UrlEncode_SharedTestData())
                yield return new object[] { tuple.Item1, tuple.Item2 };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(UrlEncode_TestData))]
        public static void UrlEncode(string value, string expected)
        {
            Assert.Equal(expected, WebUtility.UrlEncode(value));
        }

        [Theory]
        [MemberData(nameof(UrlEncodeDecode_Roundtrip_SharedTestData))]
        public static void UrlEncodeDecode_Roundtrip(string value)
        {
            string encoded = WebUtility.UrlEncode(value);
            Assert.Equal(value, WebUtility.UrlDecode(encoded));
        }

        [Fact]
        public static void UrlEncodeDecode_Roundtrip_AstralPlanes()
        {
            // These were separated out of the UrlEncodeDecode_Roundtrip_SharedTestData member data
            // due to the CharRange calls resulting in giant (several megabyte) strings.  Since these
            // values become part of the test names, they're resulting in gigantic logs.  To avoid that,
            // they've been separated out of the theory.

            // Astral plane private use chars
            UrlEncodeDecode_Roundtrip(CharRange(0xF0000, 0xFFFFD));
            UrlEncodeDecode_Roundtrip(CharRange(0x100000, 0x10FFFD));

            // Astral plane non-chars
            UrlEncodeDecode_Roundtrip(CharRange(0x2FFFE, 0x10FFFF));
            UrlEncodeDecode_Roundtrip("\U0001FFFE");
            UrlEncodeDecode_Roundtrip("\U0001FFFF");
        }

        // UrlEncode + DecodeToBytes

        public static IEnumerable<object[]> UrlDecodeToBytes_TestData()
        {
            foreach (var tuple in UrlDecode_SharedTestData())
            {
                byte[] input = Encoding.UTF8.GetBytes(tuple.Item1);
                byte[] output = Encoding.UTF8.GetBytes(tuple.Item2);
                yield return new object[] { input, 0, input.Length, output };
            }

            // Ranges
            byte[] bytes = new byte[] { 97, 37, 67, 50, 37, 56, 48, 98 };
            yield return new object[] { bytes, 1, 6, new byte[] { 194, 128 } };
            yield return new object[] { bytes, 7, 1, new byte[] { 98 } };
            yield return new object[] { bytes, 0, 0, new byte[0] };
            yield return new object[] { bytes, 8, 0, new byte[0] };

            // Empty
            yield return new object[] { new byte[0], 0, 0, new byte[0] };

            // Null
            yield return new object[] { null, 0, 0, null };
            yield return new object[] { null, int.MinValue, 0, null };
            yield return new object[] { null, int.MaxValue, 0, null };
        }

        [Theory]
        [MemberData(nameof(UrlDecodeToBytes_TestData))]
        public static void UrlDecodeToBytes(byte[] value, int offset, int count, byte[] expected)
        {
            byte[] actual = WebUtility.UrlDecodeToBytes(value, offset, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void UrlDecodeToBytes_NullBytes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("bytes", () => WebUtility.UrlDecodeToBytes(null, 0, 1));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public static void UrlDecodeToBytes_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset)
        {
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => WebUtility.UrlDecodeToBytes(new byte[1], offset, 1));
        }

        [Theory]
        [InlineData(1, 0, -1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(3, 2, 2)]
        public static void UrlDecodeToBytes_InvalidCount_ThrowsArgumentOutOfRangeException(int byteCount, int offset, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count", () => WebUtility.UrlDecodeToBytes(new byte[byteCount], offset, count));
        }
        
        public static IEnumerable<object[]> UrlEncodeToBytes_TestData()
        {
            foreach (var tuple in UrlEncode_SharedTestData())
            {
                byte[] input = Encoding.UTF8.GetBytes(tuple.Item1);
                byte[] output = Encoding.UTF8.GetBytes(tuple.Item2);
                yield return new object[] { input, 0, input.Length, output };
            }

            // Nothing to encode
            yield return new object[] { new byte[] { 97 }, 0, 1, new byte[] { 97 } };
            yield return new object[] { new byte[] { 97 }, 1, 0, new byte[0] };
            yield return new object[] { new byte[] { 97, 98, 99 }, 0, 3, new byte[] { 97, 98, 99 } };
            yield return new object[] { new byte[] { 97, 98, 99 }, 1, 2, new byte[] { 98, 99 } };
            yield return new object[] { new byte[] { 97, 98, 99 }, 1, 1, new byte[] { 98 } };
            yield return new object[] { new byte[] { 97, 98, 99, 100 }, 1, 2, new byte[] { 98, 99 } };
            yield return new object[] { new byte[] { 97, 98, 99, 100 }, 2, 2, new byte[] { 99, 100 } };

            // Mixture of ASCII and non-URL safe chars (full and in a range)
            yield return new object[] { new byte[] { 97, 225, 136, 180, 98 }, 0, 5, new byte[] { 97, 37, 69, 49, 37, 56, 56, 37, 66, 52, 98 } };
            yield return new object[] { new byte[] { 97, 225, 136, 180, 98 }, 1, 3, new byte[] { 37, 69, 49, 37, 56, 56, 37, 66, 52 } };

            // Empty
            yield return new object[] { new byte[0], 0, 0, new byte[0] };

            // Null
            yield return new object[] { null, 0, 0, null };
            yield return new object[] { null, int.MinValue, 0, null };
            yield return new object[] { null, int.MaxValue, 0, null };
        }

        [Theory]
        [MemberData(nameof(UrlEncodeToBytes_TestData))]
        public static void UrlEncodeToBytes(byte[] value, int offset, int count, byte[] expected)
        {
            byte[] actual = WebUtility.UrlEncodeToBytes(value, offset, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void UrlEncodeToBytes_NullBytes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("bytes", () => WebUtility.UrlEncodeToBytes(null, 0, 1));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public static void UrlEncodeToBytes_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset)
        {
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => WebUtility.UrlEncodeToBytes(new byte[1], offset, 0));
        }

        [Theory]
        [InlineData(1, 0, -1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(3, 2, 2)]
        public static void UrlEncodeToBytes_InvalidCount_ThrowsArgumentOutOfRangeExceptioh(int byteCount, int offset, int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>("count", () => WebUtility.UrlEncodeToBytes(new byte[byteCount], offset, count));
        }

        [Theory]
        [MemberData(nameof(UrlEncodeDecode_Roundtrip_SharedTestData))]
        public static void UrlEncodeDecodeToBytes_Roundtrip(string url)
        {
            byte[] input = Encoding.UTF8.GetBytes(url);
            byte[] encoded = WebUtility.UrlEncodeToBytes(input, 0, input.Length);
            Assert.Equal(input, WebUtility.UrlDecodeToBytes(encoded, 0, encoded.Length));
        }

        [Fact]
        public static void UrlEncodeDecodeToBytes_Roundtrip_AstralPlanes()
        {
            // These were separated out of the UrlEncodeDecode_Roundtrip_SharedTestData member data
            // due to the CharRange calls resulting in giant (several megabyte) strings. Since these
            // values become part of the test names, they're resulting in gigantic logs. To avoid that,
            // they've been separated out of the theory.

            // Astral plane private use chars
            UrlEncodeDecodeToBytes_Roundtrip(CharRange(0xF0000, 0xFFFFD));
            UrlEncodeDecodeToBytes_Roundtrip(CharRange(0x100000, 0x10FFFD));

            // Astral plane non-chars
            UrlEncodeDecodeToBytes_Roundtrip(CharRange(0x2FFFE, 0x10FFFF));
            UrlEncodeDecodeToBytes_Roundtrip("\U0001FFFE");
            UrlEncodeDecodeToBytes_Roundtrip("\U0001FFFF");
        }

        [Theory]
        [InlineData("FooBarQuux", 3, 7, "BarQuux")]
        public static void UrlEncodeToBytes_ExcludeIrrelevantData(string value, int offset, int count, string expected)
        {
            byte[] input = Encoding.UTF8.GetBytes(value);
            byte[] encoded = WebUtility.UrlEncodeToBytes(input, offset, count);
            string actual = Encoding.UTF8.GetString(encoded);
            Assert.Equal(expected, actual);
        }
        
        [Fact]
        public static void UrlEncodeToBytes_NoEncodingNeeded_ReturnsNewClonedArray()
        {
            // We have to make sure it always returns a new array, to
            // prevent problems where the input array is changed if
            // the output one is modified.

            byte[] input = Encoding.UTF8.GetBytes("Dont.Need.Encoding");
            byte[] output = WebUtility.UrlEncodeToBytes(input, 0, input.Length);
            Assert.NotSame(input, output);
        }

        [Fact]
        public static void UrlDecodeToBytes_NoDecodingNeeded_ReturnsNewClonedArray()
        {
            byte[] input = Encoding.UTF8.GetBytes("Dont.Need.Decoding");
            byte[] output = WebUtility.UrlDecodeToBytes(input, 0, input.Length);
            Assert.NotSame(input, output);
        }

        public static string CharRange(int start, int end)
        {
            Debug.Assert(start <= end);

            int capacity = end - start + 1;
            var builder = new StringBuilder(capacity);
            for (int i = start; i <= end; i++)
            {
                // 0 -> \0, 65 -> A, 0x10FFFF -> \U0010FFFF
                builder.Append(char.ConvertFromUtf32(i));
            }
            return builder.ToString();
        }
    }
}
