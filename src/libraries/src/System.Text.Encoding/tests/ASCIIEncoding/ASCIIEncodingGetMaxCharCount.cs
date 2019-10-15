// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void GetMaxCharCount(int byteCount)
        {
            Assert.Equal(byteCount, new ASCIIEncoding().GetMaxCharCount(byteCount));

            // Now test the input for an Encoding which has a zero or negative-length DecoderFallback.MaxCharCount.

            Assert.Equal(byteCount, Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetMaxCharCount(byteCount));
            Assert.Equal(byteCount, Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new CustomLengthDecoderFallback(-5)).GetMaxCharCount(byteCount));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 50)]
        [InlineData(10_000, 50_000)]
        public void GetMaxCharCount_WithLongDecoderFallback(int byteCount, int expectedMaxCharCount)
        {
            Encoding asciiEncoding = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new DecoderReplacementFallback("abcde"));
            Assert.Equal(expectedMaxCharCount, asciiEncoding.GetMaxCharCount(byteCount));
        }

        [Fact]
        public void GetMaxCharCount_WithDefaultDecoder_InvalidArg()
        {
            Assert.Throws<ArgumentOutOfRangeException>("byteCount", () => Encoding.ASCII.GetMaxCharCount(-1));
        }

        [Fact]
        public void GetMaxCharCount_Overflow_WithLongDecoderFallbackMaxCharCount()
        {
            Encoding asciiEncoding = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new CustomLengthDecoderFallback(1_000_000));
            Assert.Throws<ArgumentOutOfRangeException>("byteCount", () => asciiEncoding.GetMaxCharCount(5_000_000));
        }

        private class CustomLengthDecoderFallback : DecoderFallback
        {
            public CustomLengthDecoderFallback(int maxCharCount) { MaxCharCount = maxCharCount; }

            public override int MaxCharCount { get; }

            public override DecoderFallbackBuffer CreateFallbackBuffer()
            {
                throw new NotImplementedException();
            }
        }
    }
}
