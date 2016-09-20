// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Net.Tests
{
    public class WebUtilityTests
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

        [Fact]
        public static void HtmlDecode_InvalidUnicode()
        {
            // TODO: add into HtmlDecode_TestData when #7166 is fixed
            // High BMP non-chars
            HtmlDecode("\uFFFD", "\uFFFD");
            HtmlDecode("\uFFFE", "\uFFFE");
            HtmlDecode("\uFFFF", "\uFFFF");
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

            // Surrogate pairs - default strict settings
            yield return new object[] { char.ConvertFromUtf32(144308), "&#144308;" };
            yield return new object[] { "\uD800\uDC00", "&#65536;" };
            yield return new object[] { "a\uD800\uDC00b", "a&#65536;b" };

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

        [Fact]
        public static void HtmlEncode_InvalidUnicode()
        {
            // TODO: add into HtmlEncode_TestData when #7166 is fixed
            // High BMP non-chars
            HtmlEncode("\uFFFD", "\uFFFD");
            HtmlEncode("\uFFFE", "\uFFFE");
            HtmlEncode("\uFFFF", "\uFFFF");
            
            // Lone high surrogate
            HtmlEncode("\uD800", "\uFFFD");
            HtmlEncode("\uD800a", "\uFFFDa");

            // Lone low surrogate
            HtmlEncode("\uDC00", "\uFFFD");
            HtmlEncode("\uDC00a", "\uFFFDa");

            // Invalid surrogate pairs
            HtmlEncode("\uD800\uD800", "\uFFFD\uFFFD"); // High, high
            HtmlEncode("\uDC00\uD800", "\uFFFD\uFFFD"); // Low, high
            HtmlEncode("\uDC00\uDC00", "\uFFFD\uFFFD"); // Low, low
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

            // No escaping needed
            yield return Tuple.Create("abc", "abc");
            yield return Tuple.Create("", "");
            yield return Tuple.Create("Hello, world", "Hello, world");
            yield return Tuple.Create("\u1234\u2345", "\u1234\u2345");
            yield return Tuple.Create("abc\u1234\u2345def\u1234", "abc\u1234\u2345def\u1234");

            // Invalid percent encoding
            yield return Tuple.Create("%", "%");
            yield return Tuple.Create("%A", "%A");
            yield return Tuple.Create("%G1", "%G1");
            yield return Tuple.Create("%1G", "%1G");
        }

        public static IEnumerable<Tuple<string, string>> UrlEncode_SharedTestData()
        {
            // Recent change brings function inline with RFC 3986 to return hex-encoded chars in uppercase
            yield return Tuple.Create("/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665", "%2F%5C%22%09Hello!+%E2%99%A5%3F%2F%5C%22%09World!+%E2%99%A5%3F%E2%99%A5");
            yield return Tuple.Create("'", "%27");
            yield return Tuple.Create("\uD800\uDFFF", "%F0%90%8F%BF"); // Surrogate pairs should be encoded as 4 bytes together

            // No escaping needed
            yield return Tuple.Create("abc", "abc");
            yield return Tuple.Create("", "");

            // Spaces
            yield return Tuple.Create("abc def", "abc+def");
            yield return Tuple.Create("    ", "++++");
            yield return Tuple.Create("++++", "%2B%2B%2B%2B");

            // TODO: Uncomment this block out when dotnet/corefx#7166 is fixed.

            /*
            
            // Tests for stray surrogate chars (all should be encoded as U+FFFD)
            // Relevant GitHub issue: dotnet/corefx#7036
            
            yield return Tuple.Create("\uD800", "%EF%BF%BD"); // High surrogate
            yield return Tuple.Create("\uDC00", "%EF%BF%BD"); // Low surrogate

            yield return Tuple.Create("\uDC00\uD800", "%EF%BF%BD%EF%BF%BD"); // Low + high
            yield return Tuple.Create("\uD900\uDA00", "%EF%BF%BD%EF%BF%BD"); // High + high
            yield return Tuple.Create("\uDE00\uDF00", "%EF%BF%BD%EF%BF%BD"); // Low + low

            yield return Tuple.Create("!\uDB00@", "!%EF%BF%BD%40"); // Non-surrogate + high + non-surrogate
            yield return Tuple.Create("#\uDD00$", "%23%EF%BF%BD%24"); // Non-surrogate + low + non-surrogate
            
            */
        }

        public static IEnumerable<object[]> UrlEncodeDecode_Roundtrip_SharedTestData()
        {
            yield return new object[] { "'" };
            yield return new object[] { "http://www.microsoft.com" };
            yield return new object[] { "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665" };
            yield return new object[] { "\uD800\uDFFF" }; // Surrogate pairs

            yield return new object[] { CharRange('\uE000', '\uF8FF') }; // BMP private use chars
            yield return new object[] { CharRange('\uFDD0', '\uFDEF') }; // Low BMP non-chars
            // TODO: Uncomment when dotnet/corefx#7166 is fixed.
            // yield return new object[] { "\uFFFE\uFFFF" }; // High BMP non-chars

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
            yield return new object[] { null, 0, 0, null };
        }

        [Theory]
        [MemberData(nameof(UrlDecodeToBytes_TestData))]
        public static void UrlDecodeToBytes(byte[] value, int offset, int count, byte[] expected)
        {
            byte[] actual = WebUtility.UrlDecodeToBytes(value, offset, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void UrlDecodeToBytes_Invalid()
        {
            Assert.Throws<ArgumentNullException>("bytes", () => WebUtility.UrlDecodeToBytes(null, 0, 1)); // Bytes is null

            Assert.Throws<ArgumentOutOfRangeException>("offset", () => WebUtility.UrlDecodeToBytes(new byte[1], -1, 1)); // Offset < 0
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => WebUtility.UrlDecodeToBytes(new byte[1], 2, 1)); // Offset > bytes.Length

            Assert.Throws<ArgumentOutOfRangeException>("count", () => WebUtility.UrlDecodeToBytes(new byte[1], 0, -1)); // Count < 0
            Assert.Throws<ArgumentOutOfRangeException>("count", () => WebUtility.UrlDecodeToBytes(new byte[1], 0, 3)); // Count > bytes.Length
        }

        [Theory]
        [InlineData("a", 0, 1)]
        [InlineData("a", 1, 0)]
        [InlineData("abc", 0, 3)]
        [InlineData("abc", 1, 2)]
        [InlineData("abc", 1, 1)]
        [InlineData("abcd", 1, 2)]
        [InlineData("abcd", 2, 2)]
        public static void UrlEncodeToBytes_NothingToExpand_OutputMatchesSubInput(string inputString, int offset, int count)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);
            byte[] subInputBytes = new byte[count];
            Buffer.BlockCopy(inputBytes, offset, subInputBytes, 0, count);
            Assert.Equal(inputString.Length, inputBytes.Length);

            byte[] outputBytes = WebUtility.UrlEncodeToBytes(inputBytes, offset, count);

            Assert.NotSame(inputBytes, outputBytes);
            Assert.Equal(count, outputBytes.Length);
            Assert.Equal(subInputBytes, outputBytes);
        }
        
        public static IEnumerable<object[]> UrlEncodeToBytes_TestData()
        {
            foreach (var tuple in UrlEncode_SharedTestData())
            {
                byte[] input = Encoding.UTF8.GetBytes(tuple.Item1);
                byte[] output = Encoding.UTF8.GetBytes(tuple.Item2);
                yield return new object[] { input, 0, input.Length, output };
            }
            // Mixture of ASCII and non-URL safe chars (full and in a range)
            yield return new object[] { new byte[] { 97, 225, 136, 180, 98 }, 0, 5, new byte[] { 97, 37, 69, 49, 37, 56, 56, 37, 66, 52, 98 } };
            yield return new object[] { new byte[] { 97, 225, 136, 180, 98 }, 1, 3, new byte[] { 37, 69, 49, 37, 56, 56, 37, 66, 52 } };

            yield return new object[] { null, 0, 0, null };
        }

        [Theory]
        [MemberData(nameof(UrlEncodeToBytes_TestData))]
        public static void UrlEncodeToBytes(byte[] value, int offset, int count, byte[] expected)
        {
            byte[] actual = WebUtility.UrlEncodeToBytes(value, offset, count);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void UrlEncodeToBytes_Invalid()
        {
            Assert.Throws<ArgumentNullException>("bytes", () => WebUtility.UrlEncodeToBytes(null, 0, 1)); // Bytes is null

            Assert.Throws<ArgumentOutOfRangeException>("offset", () => WebUtility.UrlEncodeToBytes(new byte[1], -1, 1)); // Offset < 0
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => WebUtility.UrlEncodeToBytes(new byte[1], 2, 1)); // Offset > bytes.Length

            Assert.Throws<ArgumentOutOfRangeException>("count", () => WebUtility.UrlEncodeToBytes(new byte[1], 0, -1)); // Count < 0
            Assert.Throws<ArgumentOutOfRangeException>("count", () => WebUtility.UrlEncodeToBytes(new byte[1], 0, 3)); // Count > bytes.Length
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
            // due to the CharRange calls resulting in giant (several megabyte) strings.  Since these
            // values become part of the test names, they're resulting in gigantic logs.  To avoid that,
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

        [Theory]
        [InlineData("FooBarQuux", 3, 7, "BarQuux")]
        public static void UrlDecodeToBytes_ExcludeIrrelevantData(string value, int offset, int count, string expected)
        {
            byte[] input = Encoding.UTF8.GetBytes(value);
            byte[] decoded = WebUtility.UrlDecodeToBytes(input, offset, count);
            string actual = Encoding.UTF8.GetString(decoded);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void UrlEncodeToBytes_NewArray()
        {
            // If no encoding is needed, the current implementation simply
            // returns the input array to a method which then clones it.

            // We have to make sure it always returns a new array, to
            // prevent problems where the input array is changed if
            // the output one is modified.

            byte[] input = Encoding.UTF8.GetBytes("Dont.Need.Encoding");
            byte[] output = WebUtility.UrlEncodeToBytes(input, 0, input.Length);
            Assert.NotSame(input, output);
        }

        [Fact]
        public static void UrlDecodeToBytes_NewArray()
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
