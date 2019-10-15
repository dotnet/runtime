// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetMaxByteCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue - 1)]
        public void GetMaxByteCount(int charCount)
        {
            Assert.Equal(charCount + 1, new ASCIIEncoding().GetMaxByteCount(charCount));

            // Now test the input for an Encoding which has a zero or negative-length EncoderFallback.MaxCharCount.

            Assert.Equal(charCount + 1, Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetMaxByteCount(charCount));
            Assert.Equal(charCount + 1, Encoding.GetEncoding("ascii", new CustomLengthEncoderFallback(-5), DecoderFallback.ExceptionFallback).GetMaxByteCount(charCount));
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(10, 55)]
        [InlineData(10_000, 50_005)]
        public void GetMaxByteCount_WithLongEncoderFallback(int charCount, int expectedMaxByteCount)
        {
            Encoding asciiEncoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("abcde"), DecoderFallback.ExceptionFallback);
            Assert.Equal(expectedMaxByteCount, asciiEncoding.GetMaxByteCount(charCount));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void GetMaxByteCount_WithDefaultEncoder_InvalidArg(int charCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => Encoding.ASCII.GetMaxByteCount(charCount));
        }

        [Fact]
        public void GetMaxByteCount_Overflow_WithLongEncoderFallbackMaxCharCount()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("ascii", new CustomLengthEncoderFallback(1_000_000), DecoderFallback.ExceptionFallback);
            Assert.Throws<ArgumentOutOfRangeException>("charCount", () => asciiEncoding.GetMaxByteCount(5_000_000));
        }

        private class CustomLengthEncoderFallback : EncoderFallback
        {
            public CustomLengthEncoderFallback(int maxCharCount) { MaxCharCount = maxCharCount; }

            public override int MaxCharCount { get; }

            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                throw new NotImplementedException();
            }
        }
    }
}
