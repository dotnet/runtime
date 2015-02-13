// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using Xunit;

namespace HttpWebUtilityTests
{
    public class WebUtilityTests
    {
        [Fact]
        public static void HtmlDecodeWithoutTextWriter()
        {
            // Arrange
            string input = "Hello! &apos;&quot;&lt;&amp;&gt;\u2665&hearts;\u00E7&#xe7;&#231;";
            string expected = @"Hello! '""<&>\u2665\u2665\u00E7\u00E7\u00E7";

            // Act
            string returned = WebUtility.HtmlDecode(input);

            // Assert
            Assert.Equal(expected, returned);
        }

        [Fact]
        public static void HtmlDecodeWithoutTextWriterReturnsNullIfInputIsNull()
        {
            // Act
            string returned = WebUtility.HtmlDecode(null);

            // Assert
            Assert.Null(returned);
        }

        [Fact]
        public static void HtmlDecodeNoTextWriterOriginalStringNoSpecialCharacters()
        {
            // Arrange
            string input = @"Hello, world! ""<>\u2665\u00E7";

            // Act
            string returned = WebUtility.HtmlDecode(input);

            // Assert
            Assert.Equal(input, returned);
        }

        [Fact]
        public static void HtmlEncodeSingleQuote()
        {
            // Single quotes need to be encoded as &#39; rather than &apos; since &#39; is valid both for
            // HTML and XHTML, but &apos; is valid only for XHTML.
            // For more info: http://fishbowl.pastiche.org/2003/07/01/the_curse_of_apos/

            // Arrange
            string input = "'";
            string expected = "&#39;";

            // Act
            string returned = WebUtility.HtmlEncode(input);

            // Assert
            Assert.Equal(expected, returned);
        }

        [Fact]
        public static void HtmlEncodeWithoutTextWriter()
        {
            // Arrange
            string input = @"Hello! '""<&>\u2665\u00E7";
            string expected = "Hello! &#39;&quot;&lt;&amp;&gt;\u2665&#231;";

            // Act
            string returned = WebUtility.HtmlEncode(input);

            // Assert
            Assert.Equal(expected, returned);
        }

        [Fact]
        public static void HtmlEncodeWithoutTextWriterReturnsNullIfInputIsNull()
        {
            // Act
            string returned = WebUtility.HtmlEncode((string)null);

            // Assert
            Assert.Null(returned);
        }

        [Fact]
        public static void HtmlEncodeNoTextWriterOriginalStringNoSpecialCharacters()
        {
            // Arrange
            string input = "Hello, world!";

            // Act
            string returned = WebUtility.HtmlEncode(input);

            // Assert
            Assert.Equal(input, returned);
        }

        [Fact]
        public static void UrlDecodeFromStringNoEncodingReturnsNullIfInputIsNull()
        {
            // Act
            string returned = WebUtility.UrlDecode((string)null);

            // Assert
            Assert.Null(returned);
        }

        [Fact]
        public static void UrlEncodeFromStringNoEncoding()
        {
            // Recent change brings function inline with RFC 3986 to return hex-encoded chars in uppercase
            // Arrange
            string input = "/\\\"\tHello! \u2665?/\\\"\tWorld! \u2665?\u2665";
            string expected = "%2F%5C%22%09Hello!+%E2%99%A5%3F%2F%5C%22%09World!+%E2%99%A5%3F%E2%99%A5";

            // Act
            string returned = WebUtility.UrlEncode(input);

            // Assert
            Assert.Equal(expected, returned);
        }

        [Fact]
        public static void UrlEncodeFromStringNoEncodingReturnsNullIfInputIsNull()
        {
            // Act
            string returned = WebUtility.UrlEncode((string)null);

            // Assert
            Assert.Null(returned);
        }

        [Fact]
        public static void UrlEncodeSingleQuote()
        {
            Assert.Equal("%27", WebUtility.UrlEncode("'"));
        }

        [Fact]
        public static void HtmlDefaultStrictSettingEncode()
        {
            Assert.Equal(WebUtility.HtmlEncode(Char.ConvertFromUtf32(144308)), "&#144308;");
        }

        [Fact]
        public static void HtmlDefaultStrictSettingDecode()
        {
            Assert.Equal(Char.ConvertFromUtf32(144308), WebUtility.HtmlDecode("&#144308;"));
        }
    }
}