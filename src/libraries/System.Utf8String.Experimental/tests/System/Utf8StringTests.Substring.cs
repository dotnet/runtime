// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        [Theory]
        [InlineData("Hello", 0, false, "Hello")]
        [InlineData("Hello", 0, true, "")]
        [InlineData("Hello", 2, false, "llo")]
        [InlineData("Hello", 2, true, "lo")]
        [InlineData("Hello", 5, false, "")]
        [InlineData("Hello", 5, true, "Hello")]
        [InlineData("", 0, true, "")]
        [InlineData("", 0, false, "")]
        public static void Substring_Index(string sAsString, int indexValue, bool fromEnd, string expectedAsString)
        {
            Index index = new Index(indexValue, fromEnd);

            void Substring_IndexCore(Utf8String s, Utf8String expected)
            {
                Range r = new Range(index, ^0);
                Assert.Equal(expected, s[r]);

                if (index.Value == 0)
                {
                    Assert.Same(index.IsFromEnd ? Utf8String.Empty : s, s[r]);
                }

                if (index.Value == s.Length)
                {
                    Assert.Same(index.IsFromEnd ? s : Utf8String.Empty, s[r]);
                }
            };

            Substring_IndexCore(new Utf8String(sAsString), new Utf8String(expectedAsString));
        }

        [Theory]
        [InlineData("Hello", 0, 5, "Hello")]
        [InlineData("Hello", 0, 3, "Hel")]
        [InlineData("Hello", 2, 3, "llo")]
        [InlineData("Hello", 5, 0, "")]
        [InlineData("", 0, 0, "")]
        public static void Substring_Int(string sAsString, int startIndex, int length, string expectedAsString)
        {
            void Substring_IntCore(Utf8String s, Utf8String expected)
            {
                if (startIndex + length == s.Length)
                {
                    Assert.Equal(expected, s.Substring(startIndex));
                    Assert.Equal(expected, new Utf8String(s.AsBytes(startIndex)));

                    if (length == 0)
                    {
                        Assert.Same(Utf8String.Empty, s.Substring(startIndex));
                    }
                }
                Assert.Equal(expected, s.Substring(startIndex, length));

                Assert.Equal(expected, new Utf8String(s.AsBytes(startIndex, length)));

                if (length == s.Length)
                {
                    Assert.Same(s, s.Substring(startIndex));
                    Assert.Same(s, s.Substring(startIndex, length));
                }
                else if (length == 0)
                {
                    Assert.Same(Utf8String.Empty, s.Substring(startIndex, length));
                }
            };

            Substring_IntCore(new Utf8String(sAsString), new Utf8String(expectedAsString));
        }

        [Fact]
        public static void Substring_Range()
        {
            void Substring_RangeCore(Utf8String s, Range range, Utf8String expected)
            {
                Assert.Equal(expected, s[range]);

                if (expected.Length == s.Length)
                {
                    Assert.Same(s, s[range]);
                }

                if (expected.Length == 0)
                {
                    Assert.Same(Utf8String.Empty, s[range]);
                }
            };

            Substring_RangeCore(u8("Hello"), .., u8("Hello"));
            Substring_RangeCore(u8("Hello"), 0..3, u8("Hel"));
            Substring_RangeCore(u8("Hello"), ..^4, u8("H"));
            Substring_RangeCore(u8("Hello"), 1.., u8("ello"));
            Substring_RangeCore(u8("Hello"), ..^5, Utf8String.Empty);
        }

        [Fact]
        public static void Substring_Invalid()
        {
            // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => u8("foo").Substring(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => u8("foo").Substring(-1, 0));

            // Start index > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => u8("foo").Substring(4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => u8("foo").Substring(4, 0));

            // Length < 0 or length > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => u8("foo").Substring(0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => u8("foo").Substring(0, 4));

            // Start index + length > string.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => u8("foo").Substring(3, 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => u8("foo").Substring(2, 2));
        }
    }
}
