// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public partial class ASCIIEncodingDecode
    {
        [Theory]
        [InlineData("hello!", 6)]
        [InlineData("hello\u1234there!", 16)]
        [InlineData("\ud800\udc00", 10)]
        public void GetByteCount_WithReplacementFallback(string input, int expectedByteCount)
        {
            Encoding encoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("abcde"), DecoderFallback.ExceptionFallback);
            Assert.Equal(expectedByteCount, encoding.GetByteCount(input));
        }

        [Fact]
        public void GetByteCount_WithSingleCharNonAsciiReplacementFallback_ValidatesAscii()
        {
            // Tests trying to replace one non-ASCII character with another, which should cause
            // fallback logic to identify the invalid data and abort the operation.

            Encoding encoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("\u1234"), DecoderFallback.ExceptionFallback);
            Assert.Throws<ArgumentException>("chars", () => encoding.GetByteCount("\u0080"));
        }

        [Theory]
        [InlineData("hello!", "hello!")]
        [InlineData("hello\u1234there!", "helloabcdethere!")]
        [InlineData("\ud800\udc00", "abcdeabcde")]
        public void GetBytes_WithReplacementFallback(string input, string expectedResult)
        {
            Encoding encoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("abcde"), DecoderFallback.ExceptionFallback);
            Assert.Equal(WideToAsciiStr(expectedResult), encoding.GetBytes(input));
        }

        [Fact]
        public void GetBytes_WithNonAsciiInput_AndSingleCharNonAsciiReplacementFallback_Throws()
        {
            // Tests trying to replace one non-ASCII character with another, which should cause
            // fallback logic to identify the invalid data and abort the operation.

            Encoding encoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("\u1234"), DecoderFallback.ExceptionFallback);
            Assert.Throws<ArgumentException>("chars", () => encoding.GetBytes("\u0080"));
        }

        private static byte[] WideToAsciiStr(string input)
        {
            return input.Select(ch => (byte)checked((sbyte)ch)).ToArray(); // makes sure each char is 00..7F
        }
    }
}
