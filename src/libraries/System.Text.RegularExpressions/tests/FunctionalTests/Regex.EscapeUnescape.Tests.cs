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
    }
}
