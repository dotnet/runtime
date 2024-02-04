// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingEncode
    {
        public static IEnumerable<object[]> Encode_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= 0x7F; i++)
            {
                char b = (char)i;
                yield return new object[] { b, 0, 1 };
                yield return new object[] { "a" + b + "c", 1, 1 };
                yield return new object[] { "a" + b + "c", 2, 1 };
                yield return new object[] { "a" + b + "c", 0, 3 };
            }

            string testString = "Hello World123#?!";
            yield return new object[] { testString, 0, testString.Length };
            yield return new object[] { testString, 4, 5 };

            yield return new object[] { "ABCDEFGH", 0, 8 };

            // Empty strings
            yield return new object[] { string.Empty, 0, 0 };
            yield return new object[] { "abc", 3, 0 };
            yield return new object[] { "abc", 0, 0 };
        }

        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode(string source, int index, int count)
        {
            byte[] expected = GetBytes(source, index, count);
            EncodingHelpers.Encode(new ASCIIEncoding(), source, index, count, expected);

            // Encoding valid chars should not throw with an EncoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderExceptionFallback(), new DecoderReplacementFallback("?"));
            EncodingHelpers.Encode(exceptionEncoding, source, index, count, expected);

            byte[] actual = new byte[expected.Length];
            Assert.Equal(OperationStatus.Done , Ascii.FromUtf16(source.AsSpan(index, count), actual, out int bytesWritten));
            Assert.Equal(expected.Length, bytesWritten);
            Assert.Equal(expected, actual.Take(bytesWritten).ToArray());

            if (expected.Length > 1)
            {
                actual = new byte[expected.Length - 1];
                Assert.Equal(OperationStatus.DestinationTooSmall, Ascii.FromUtf16(source.AsSpan(index, count), actual, out bytesWritten));
                Assert.Equal(expected.Length - 1, bytesWritten);
                Assert.Equal(expected.Take(bytesWritten).ToArray(), actual.Take(bytesWritten).ToArray());
            }
        }

        public static IEnumerable<object[]> Encode_InvalidChars_TestData()
        {
            // All non-ASCII Latin1 chars
            for (int i = 0x80; i <= 0xFF; i++)
            {
                char b = (char)i;
                yield return new object[] { b, 0, 1, 0 };
            }

            // Unicode chars
            yield return new object[] { "\u1234\u2345", 0, 2, 0 };
            yield return new object[] { "a\u1234\u2345b", 0, 4, 1 };

            yield return new object[] { "\uD800\uDC00", 0, 2, 0 };
            yield return new object[] { "a\uD800\uDC00b", 0, 2, 1 };

            yield return new object[] { "\uD800\uDC00\u0061\u0CFF", 0, 4, 0 };

            // Invalid Unicode
            yield return new object[] { "\uD800", 0, 1, 0 }; // Lone high surrogate
            yield return new object[] { "\uDC00", 0, 1, 0 }; // Lone low surrogate
            yield return new object[] { "\uD800\uDC00", 0, 1, 0 }; // Surrogate pair out of range
            yield return new object[] { "\uD800\uDC00", 1, 1, 0 }; // Surrogate pair out of range

            yield return new object[] { "\uD800\uD800", 0, 2, 0 }; // High, high
            yield return new object[] { "\uDC00\uD800", 0, 2, 0 }; // Low, high
            yield return new object[] { "\uDC00\uDC00", 0, 2, 0 }; // Low, low

            yield return new object[] { "\u0080\u00FF\u0B71\uFFFF\uD800\uDFFF", 0, 6, 0 };

            // High BMP non-chars
            yield return new object[] { "\uFFFD", 0, 1, 0 };
            yield return new object[] { "\uFFFE", 0, 1, 0 };
            yield return new object[] { "\uFFFF", 0, 1, 0 };
        }

        [Theory]
        [MemberData(nameof(Encode_InvalidChars_TestData))]
        public void Encode_InvalidChars(string source, int index, int count, int expectedCharsConsumed)
        {
            byte[] expected = GetBytes(source, index, count);
            EncodingHelpers.Encode(new ASCIIEncoding(), source, index, count, expected);

            // Encoding invalid chars should throw with an EncoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderExceptionFallback(), new DecoderReplacementFallback("?"));
            NegativeEncodingTests.Encode_Invalid(exceptionEncoding, source, index, count);

            byte[] actual = new byte[expected.Length];
            Assert.Equal(OperationStatus.InvalidData, Ascii.FromUtf16(source.AsSpan(index, count), actual, out int bytesWritten));
            Assert.Equal(expectedCharsConsumed, bytesWritten);
            Assert.Equal(expected.Take(bytesWritten).ToArray(), actual.Take(bytesWritten).ToArray());
        }

        private static byte[] GetBytes(string source, int index, int count)
        {
            byte[] bytes = new byte[count];
            for (int i = 0; i < bytes.Length; i++)
            {
                if (source[i] <= 0x7f)
                {
                    bytes[i] = (byte)source[i + index];
                }
                else
                {
                    // Verify the fallback character for non-ASCII chars
                    bytes[i] = (byte)'?';
                }
            }
            return bytes;
        }

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
            Assert.Equal(expectedResult.ToCharArray(), encoding.GetChars(WideToNarrowStr(input)));
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
