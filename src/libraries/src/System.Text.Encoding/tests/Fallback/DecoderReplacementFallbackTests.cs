// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class DecoderReplacementFallbackTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            DecoderReplacementFallback fallback = new DecoderReplacementFallback();
            Assert.Equal(1, fallback.MaxCharCount);
            Assert.Equal("?", fallback.DefaultString);
            Assert.Equal("?".GetHashCode(), fallback.GetHashCode());
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("abc")]
        [InlineData("\uD800\uDC00")]
        public void Ctor_String(string replacement)
        {
            DecoderReplacementFallback exception = new DecoderReplacementFallback(replacement);
            Assert.Equal(replacement.Length, exception.MaxCharCount);
            Assert.Equal(replacement, exception.DefaultString);
            Assert.Equal(replacement.GetHashCode(), exception.GetHashCode());
        }

        [Fact]
        public void Ctor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => new DecoderReplacementFallback(null));

            // Invalid surrogate pair
            AssertExtensions.Throws<ArgumentException>(null, () => new DecoderReplacementFallback("\uD800"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DecoderReplacementFallback("\uD800a"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DecoderReplacementFallback("\uDC00"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DecoderReplacementFallback("a\uDC00"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DecoderReplacementFallback("\uDC00\uDC00"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DecoderReplacementFallback("\uD800\uD800"));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new DecoderReplacementFallback(), new DecoderReplacementFallback(), true };
            yield return new object[] { new DecoderReplacementFallback(), new DecoderReplacementFallback("?"), true };

            yield return new object[] { new DecoderReplacementFallback("abc"), new DecoderReplacementFallback("abc"), true };
            yield return new object[] { new DecoderReplacementFallback("abc"), new DecoderReplacementFallback("def"), false };

            yield return new object[] { new DecoderReplacementFallback(), new object(), false };
            yield return new object[] { new DecoderReplacementFallback(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(DecoderReplacementFallback fallback, object value, bool expected)
        {
            Assert.Equal(expected, fallback.Equals(value));
        }

        [Fact]
        public void CreateFallbackBuffer()
        {
            DecoderFallbackBuffer buffer = new DecoderReplacementFallback().CreateFallbackBuffer();
            Assert.Equal((char)0, buffer.GetNextChar());
            Assert.False(buffer.MovePrevious());
            Assert.Equal(0, buffer.Remaining);
        }

        [Theory]
        [InlineData("", new byte[0], false)]
        [InlineData("", new byte[] { 1 }, false)]
        [InlineData("?", new byte[0], true)]
        [InlineData("?", new byte[] { 1 }, true)]
        public void CreateFallbackBuffer_Fallback_Char(string replacement, byte[] bytesUnknown, bool expected)
        {
            DecoderFallbackBuffer buffer = new DecoderReplacementFallback(replacement).CreateFallbackBuffer();
            Assert.Equal(expected, buffer.Fallback(bytesUnknown, 0));
        }

        [Theory]
        [InlineData("?")]
        [InlineData("\uD800\uDC00")]
        public void CreateFallbackBuffer_MultipleFallback_ThrowsArgumentException(string replacement)
        {
            DecoderFallbackBuffer buffer = new DecoderReplacementFallback(replacement).CreateFallbackBuffer();
            buffer.Fallback(new byte[] { 1 }, 0);

            AssertExtensions.Throws<ArgumentException>("bytesUnknown", () => buffer.Fallback(new byte[] { 1 }, 0));
        }
    }
}
