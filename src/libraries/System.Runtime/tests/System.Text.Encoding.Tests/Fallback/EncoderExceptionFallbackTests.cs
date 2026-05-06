// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class EncoderExceptionFallbackTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            EncoderExceptionFallback fallback = new EncoderExceptionFallback();
            Assert.Equal(0, fallback.MaxCharCount);
            Assert.Equal(654, fallback.GetHashCode());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new EncoderExceptionFallback(), new EncoderExceptionFallback(), true };
            yield return new object[] { new EncoderExceptionFallback(), new object(), false };
            yield return new object[] { new EncoderExceptionFallback(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(EncoderExceptionFallback fallback, object value, bool expected)
        {
            Assert.Equal(expected, fallback.Equals(value));
        }

        [Fact]
        public void CreateFallbackBuffer()
        {
            EncoderFallbackBuffer buffer = new EncoderExceptionFallback().CreateFallbackBuffer();

            Assert.Equal((char)0, buffer.GetNextChar());
            Assert.False(buffer.MovePrevious());
            Assert.Equal(0, buffer.Remaining);

            EncoderFallbackException ex = Assert.Throws<EncoderFallbackException>(() => buffer.Fallback('a', 0));
            Assert.Equal('a', ex.CharUnknown);

            ex = Assert.Throws<EncoderFallbackException>(() => buffer.Fallback('\uD800', '\uDC00', 0));
            Assert.Equal('\uD800', ex.CharUnknownHigh);
            Assert.Equal('\uDC00', ex.CharUnknownLow);
        }

        [Fact]
        public void CreateFallbackBuffer_Fallback_InvalidSurrogateChars_ThrowsArgumentOutOfRangeException()
        {
            EncoderFallbackBuffer buffer = new EncoderExceptionFallback().CreateFallbackBuffer();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("charUnknownHigh", () => buffer.Fallback('a', '\uDC00', 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("charUnknownLow", () => buffer.Fallback('\uD800', 'a', 0));
        }
    }
}
