// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class EncoderReplacementFallbackTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            EncoderReplacementFallback exception = new EncoderReplacementFallback();
            Assert.Equal(1, exception.MaxCharCount);
            Assert.Equal("?", exception.DefaultString);
            Assert.Equal("?".GetHashCode(), exception.GetHashCode());
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("abc")]
        [InlineData("\uD800\uDC00")]
        public void Ctor_String(string replacement)
        {
            EncoderReplacementFallback fallback = new EncoderReplacementFallback(replacement);
            Assert.Equal(replacement.Length, fallback.MaxCharCount);
            Assert.Equal(replacement, fallback.DefaultString);
            Assert.Equal(replacement.GetHashCode(), fallback.GetHashCode());
        }

        [Fact]
        public void Ctor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => new EncoderReplacementFallback(null));

            // Invalid surrogate pair
            AssertExtensions.Throws<ArgumentException>("replacement", () => new EncoderReplacementFallback("\uD800"));
            AssertExtensions.Throws<ArgumentException>("replacement", () => new EncoderReplacementFallback("\uD800a"));
            AssertExtensions.Throws<ArgumentException>("replacement", () => new EncoderReplacementFallback("\uDC00"));
            AssertExtensions.Throws<ArgumentException>("replacement", () => new EncoderReplacementFallback("a\uDC00"));
            AssertExtensions.Throws<ArgumentException>("replacement", () => new EncoderReplacementFallback("\uDC00\uDC00"));
            AssertExtensions.Throws<ArgumentException>("replacement", () => new EncoderReplacementFallback("\uD800\uD800"));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new EncoderReplacementFallback(), new EncoderReplacementFallback(), true };
            yield return new object[] { new EncoderReplacementFallback(), new EncoderReplacementFallback("?"), true };

            yield return new object[] { new EncoderReplacementFallback("abc"), new EncoderReplacementFallback("abc"), true };
            yield return new object[] { new EncoderReplacementFallback("abc"), new EncoderReplacementFallback("def"), false };

            yield return new object[] { new EncoderReplacementFallback(), new object(), false };
            yield return new object[] { new EncoderReplacementFallback(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(EncoderReplacementFallback fallback, object value, bool expected)
        {
            Assert.Equal(expected, fallback.Equals(value));
        }

        [Fact]
        public void CreateFallbackBuffer()
        {
            EncoderFallbackBuffer buffer = new EncoderReplacementFallback().CreateFallbackBuffer();
            Assert.Equal((char)0, buffer.GetNextChar());
            Assert.False(buffer.MovePrevious());
            Assert.Equal(0, buffer.Remaining);
        }

        [Theory]
        [InlineData("", 'a', false)]
        [InlineData("?", 'a', true)]
        public void CreateFallbackBuffer_Fallback_Char(string replacement, char charUnknown, bool expected)
        {
            EncoderFallbackBuffer buffer = new EncoderReplacementFallback(replacement).CreateFallbackBuffer();
            Assert.Equal(expected, buffer.Fallback(charUnknown, 0));
        }

        [Theory]
        [InlineData("?")]
        [InlineData("\uD800\uDC00")]
        public void CreateFallbackBuffer_MultipleFallback_ThrowsArgumentException(string replacement)
        {
            EncoderFallbackBuffer buffer = new EncoderReplacementFallback(replacement).CreateFallbackBuffer();
            buffer.Fallback('a', 0);

            AssertExtensions.Throws<ArgumentException>("chars", () => buffer.Fallback('a', 0));
            AssertExtensions.Throws<ArgumentException>("chars", () => buffer.Fallback('\uD800', '\uDC00', 0));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("a", true)]
        public void CreateFallbackBuffer_Fallback_Char_Char(string replacement, bool expected)
        {
            EncoderFallbackBuffer buffer = new EncoderReplacementFallback(replacement).CreateFallbackBuffer();
            Assert.Equal(expected, buffer.Fallback('\uD800', '\uDC00', 0));
        }

        [Fact]
        public void CreateFallbackBuffer_Fallback_InvalidSurrogateChars_ThrowsArgumentOutOfRangeException()
        {
            EncoderFallbackBuffer buffer = new EncoderReplacementFallback().CreateFallbackBuffer();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("charUnknownHigh", () => buffer.Fallback('a', '\uDC00', 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("charUnknownLow", () => buffer.Fallback('\uD800', 'a', 0));
        }
    }
}
