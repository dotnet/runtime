// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Xunit;

namespace Tests.System
{
    public class CharArrayHelpersTests
    {
        [Theory]
        [InlineData("Foo", "Foo", true)]
        [InlineData("FOO", "FOO", true)]
        [InlineData("fOO", "fOO", true)]
        [InlineData("HTTP/", "HTTP/", true)]
        [InlineData("http/", "http/", true)]
        [InlineData("GZIP", "GZIP", true)]
        [InlineData("gzip", "gzip", true)]
        [InlineData("DEFLATE", "DEFLATE", true)]
        [InlineData("deflate", "deflate", true)]
        [InlineData("Content-Length", "Content-Length", true)]
        [InlineData("content-length", "content-length", true)]
        [InlineData("DEFLATE", "deflate", false)]
        [InlineData("foo", "bar", false)]
        [InlineData("GZIP", "DEFLATE", false)]
        [InlineData("Content-Length", "content-length", false)]
        public void EqualsOrdinal_ComparingVariousInputsBothWays_ReturnsExpected(string leftString, string rightString, bool expected)
        {
            char[] left = leftString.ToCharArray();
            char[] right = rightString.ToCharArray();

            Assert.Equal(expected, CharArrayHelpers.EqualsOrdinal(leftString, right, 0, right.Length));
            Assert.Equal(expected, CharArrayHelpers.EqualsOrdinal(rightString, left, 0, left.Length));
        }

        [Theory]
        [InlineData("Foo", "Foo", true)]
        [InlineData("Foo", "FOO", true)]
        [InlineData("Foo", "fOO", true)]
        [InlineData("HTTP/", "http/", true)]
        [InlineData("GZIP", "gzip", true)]
        [InlineData("DEFLATE", "deflate", true)]
        [InlineData("Content-Length", "content-length", true)]
        [InlineData("foo", "bar", false)]
        [InlineData("GZIP", "DEFLATE", false)]
        public void EqualsOrdinalAsciiIgnoreCase_ComparingVariousInputsBothWays_ReturnsExpected(string leftString, string rightString, bool expected)
        {
            char[] left = leftString.ToCharArray();
            char[] right = rightString.ToCharArray();

            Assert.Equal(expected, CharArrayHelpers.EqualsOrdinalAsciiIgnoreCase(leftString, right, 0, right.Length));
            Assert.Equal(expected, CharArrayHelpers.EqualsOrdinalAsciiIgnoreCase(rightString, left, 0, left.Length));
        }

        [Fact]
        public void EqualsOrdinalAsciiIgnoreCase_ComparingLowerAndUpperCaseAsciiChars_ReturnsTrue()
        {
            for (char c = '\0'; c <= 127; c++)
            {
                string lowerString = c.ToString();
                string upperString = char.ToUpperInvariant(c).ToString();
                char[] lowerCharArray = new char[] { c };
                char[] upperCharArray = new char[] { char.ToUpperInvariant(c) };

                Assert.True(CharArrayHelpers.EqualsOrdinalAsciiIgnoreCase(lowerString, lowerCharArray, 0, 1));
                Assert.True(CharArrayHelpers.EqualsOrdinalAsciiIgnoreCase(upperString, lowerCharArray, 0, 1));
                Assert.True(CharArrayHelpers.EqualsOrdinalAsciiIgnoreCase(lowerString, upperCharArray, 0, 1));
                Assert.True(CharArrayHelpers.EqualsOrdinalAsciiIgnoreCase(upperString, upperCharArray, 0, 1));
            }
        }

        [Theory]
        [InlineData("     Foo     ", "Foo")]
        [InlineData("Foo     ", "Foo")]
        [InlineData("     Foo", "Foo")]
        [InlineData("\tBar\t", "Bar")]
        [InlineData("Bar\t", "Bar")]
        [InlineData("\tBar", "Bar")]
        public void Trim_VariousInputs_ReturnsExpectedSubstring(string valueString, string expected)
        {
            char[] value = valueString.ToCharArray();
            int startIndex = 0;
            int length = value.Length;

            CharArrayHelpers.Trim(value, ref startIndex, ref length);

            Assert.Equal(expected, new string(value, startIndex, length));
        }
    }
}
