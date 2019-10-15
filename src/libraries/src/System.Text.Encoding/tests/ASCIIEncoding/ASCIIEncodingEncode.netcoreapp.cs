// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public partial class ASCIIEncodingEncode
    {
        [Theory]
        [InlineData("hello!", 6)]
        [InlineData("hello\u0080there!", 16)]
        [InlineData("\u00ff\u00ff", 10)]
        public void GetCharCount_WithReplacementFallback(string input, int expectedCharCount)
        {
            Encoding encoding = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new DecoderReplacementFallback("abcde"));
            Assert.Equal(expectedCharCount, encoding.GetCharCount(WideToNarrowStr(input)));
        }

        [Fact]
        public void GetCharCount_WithInvalidFallbackBuffer_ValidatesAscii()
        {
            // Internal fallback logic should notice that we're about to write out a standalone
            // surrogate character and should abort the operation.

            Encoding encoding = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new StandaloneLowSurrogateDecoderFallback());
            Assert.Throws<ArgumentException>(() => encoding.GetCharCount(new byte[] { 0x80 }));
        }

        [Theory]
        [InlineData("hello!", "hello!")]
        [InlineData("hello\u0080there!", "helloabcdethere!")]
        [InlineData("\u00ff\u00ff", "abcdeabcde")]
        public void GetChars_WithReplacementFallback(string input, string expectedResult)
        {
            Encoding encoding = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new DecoderReplacementFallback("abcde"));
            Assert.Equal(expectedResult, encoding.GetChars(WideToNarrowStr(input)));
        }

        [Fact]
        public void GetChars_WithNonAsciiInput_AndSingleCharNonAsciiReplacementFallback_Throws()
        {
            // Internal fallback logic should notice that we're about to write out a standalone
            // surrogate character and should abort the operation.

            Encoding encoding = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, new StandaloneLowSurrogateDecoderFallback());
            Assert.Throws<ArgumentException>(() => encoding.GetChars(new byte[] { 0x80 }));
        }

        private static byte[] WideToNarrowStr(string input)
        {
            return input.Select(ch => checked((byte)ch)).ToArray(); // makes sure each char is 00..FF
        }

        private class StandaloneLowSurrogateDecoderFallback : DecoderFallback
        {
            public override int MaxCharCount => 1;

            public override DecoderFallbackBuffer CreateFallbackBuffer()
            {
                return new InnerFallbackBuffer();
            }

            private class InnerFallbackBuffer : DecoderFallbackBuffer
            {
                private int _remaining;

                public override int Remaining => _remaining;

                public override bool Fallback(byte[] bytesUnknown, int index)
                {
                    _remaining = 1;
                    return true;
                }

                public override char GetNextChar()
                {
                    // Return a standalone low surrogate
                    return (--_remaining >= 0) ? '\udc00' : default;
                }

                public override bool MovePrevious()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
