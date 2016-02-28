// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Tests
{
    public class WebUtilityTests
    {
        public static IEnumerable<object[]> HtmlDecode_TestData()
        {
            yield return new object[] { "Hello! &apos;&quot;&lt;&amp;&gt;\u2665&hearts;\u00E7&#xe7;&#231;", "Hello! '\"<&>\u2665\u2665\u00E7\u00E7\u00E7" };
            yield return new object[] { "Hello, world! \"<>\u2665\u00E7", "Hello, world! \"<>\u2665\u00E7" }; // No special chars
            yield return new object[] { null, null };

            yield return new object[] { "&#144308;", char.ConvertFromUtf32(144308) };
        }

        [Theory]
        [MemberData("HtmlDecode_TestData")]
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
            yield return new object[] { null, null };
            yield return new object[] { "Hello, world!", "Hello, world!" }; // No special chars

            yield return new object[] { char.ConvertFromUtf32(144308), "&#144308;" }; // Default strict settings
        }

        [Theory]
        [MemberData("HtmlEncode_TestData")]
        public static void HtmlEncode(string value, string expected)
        {
            Assert.Equal(expected, WebUtility.HtmlEncode(value));
        }

        public static IEnumerable<object[]> UrlDecode_TestData()
        {
            // Recent change brings function inline with RFC 3986 to return hex-encoded chars in uppercase
            yield return new object[] { "%2F%5C%22%09Hello!+%E2%99%A5%3F%2F%5C%22%09World!+%E2%99%A5%3F%E2%99%A5", "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665" };
            yield return new object[] { "Hello, world", "Hello, world" }; // No special chars
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData("UrlDecode_TestData")]
        public static void UrlDecode(string encodedValue, string expected)
        {
            Assert.Equal(expected, WebUtility.UrlDecode(encodedValue));
        }

        public static IEnumerable<object[]> UrlEncode_TestData()
        {
            // Recent change brings function inline with RFC 3986 to return hex-encoded chars in uppercase
            yield return new object[] { "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665", "%2F%5C%22%09Hello!+%E2%99%A5%3F%2F%5C%22%09World!+%E2%99%A5%3F%E2%99%A5" };
            yield return new object[] { "'", "%27" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData("UrlEncode_TestData")]
        public static void UrlEncode(string value, string expected)
        {
            Assert.Equal(expected, WebUtility.UrlEncode(value));
        }

        [Theory]
        [InlineData("'")]
        [InlineData("http://www.microsoft.com")]
        [InlineData("/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665")]
        public static void UrlEncodeDecode_Roundtrip(string value)
        {
            string encoded = WebUtility.UrlEncode(value);
            Assert.Equal(value, WebUtility.UrlDecode(encoded));
        }
        
        [Fact]
        public static void UrlDecodeToBytes_NullEncodedValue_ReturnsNull()
        {
            Assert.Null(WebUtility.UrlDecodeToBytes(null, 0, 0));
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

        public static IEnumerable<object[]> Url_EncodeToBytes_TestData()
        {
            yield return new object[] { null, 0, 0, null };
        }

        [Theory]
        public static void UrlEncodeToBytes_NullValue_ReturnsNull()
        {
            Assert.Null(WebUtility.UrlEncodeToBytes(null, 0, 0));
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
        [InlineData("'")]
        [InlineData("http://www.microsoft.com")]
        [InlineData("/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665")]
        public static void UrlEncodeDecodeToBytes_Roundtrip(string url)
        {
            byte[] input = System.Text.Encoding.UTF8.GetBytes(url);
            byte[] encoded = WebUtility.UrlEncodeToBytes(input, 0, input.Length);
            Assert.Equal(input, WebUtility.UrlDecodeToBytes(encoded, 0, encoded.Length));
        }
    }
}
