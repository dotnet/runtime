// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= 0x7F; i++)
            {
                byte b = (byte)i;
                yield return new object[] { new byte[] { b }, 0, 1 };
                yield return new object[] { new byte[] { 96, b, 97 }, 1, 1 };
                yield return new object[] { new byte[] { 96, b, 98 }, 2, 1 };
                yield return new object[] { new byte[] { 97, b, 97 }, 0, 3 };
            }

            // Empty strings
            yield return new object[] { new byte[0], 0, 0 };
            yield return new object[] { new byte[10], 5, 0 };
            yield return new object[] { new byte[10], 5, 5 };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count)
        {
            string expected = GetString(bytes, index, count);
            EncodingHelpers.Decode(new ASCIIEncoding(), bytes, index, count, expected);

            // Decoding valid bytes should not throw with a DecoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("?"), new DecoderExceptionFallback());
            EncodingHelpers.Decode(exceptionEncoding, bytes, index, count, expected);

            char[] actual = new char[expected.Length];
            Assert.Equal(OperationStatus.Done, Ascii.ToUtf16(bytes.AsSpan(index, count), actual, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(actual.AsSpan(0, charsWritten)));

            if (expected.Length > 1)
            {
                actual = new char[expected.Length - 1];
                Assert.Equal(OperationStatus.DestinationTooSmall, Ascii.ToUtf16(bytes.AsSpan(index, count), actual, out charsWritten));
                Assert.Equal(expected.Length - 1, charsWritten);
                Assert.Equal(expected.Substring(0, expected.Length - 1), new string(actual.AsSpan(0, charsWritten)));
            }
        }

        public static IEnumerable<object[]> Decode_InvalidBytes_TestData()
        {
            // All Latin-1 Supplement bytes
            for (int i = 0x80; i <= byte.MaxValue; i++)
            {
                byte b = (byte)i;
                yield return new object[] { new byte[] { b }, 0, 1, 0 };
                yield return new object[] { new byte[] { 96, b, 97 }, 1, 1, 0 };
                yield return new object[] { new byte[] { 97, b, 97 }, 0, 3, 1 };
            }

            yield return new object[] { new byte[] { 0xC1, 0x41, 0xF0, 0x42 }, 0, 4, 0 };
        }

        [Theory]
        [MemberData(nameof(Decode_InvalidBytes_TestData))]
        public void Decode_InvalidBytes(byte[] bytes, int index, int count, int expectedBytesConsumed)
        {
            string expected = GetString(bytes, index, count);
            EncodingHelpers.Decode(new ASCIIEncoding(), bytes, index, count, expected);

            // Decoding invalid bytes should throw with a DecoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("?"), new DecoderExceptionFallback());
            NegativeEncodingTests.Decode_Invalid(exceptionEncoding, bytes, index, count);

            char[] actual = new char[expected.Length];
            Assert.Equal(OperationStatus.InvalidData, Ascii.ToUtf16(bytes.AsSpan(index, count), actual, out int charsWritten));
            Assert.Equal(expectedBytesConsumed, charsWritten);
            Assert.Equal(expected.Take(charsWritten).ToArray(), actual.Take(charsWritten).ToArray());
        }

        public static string GetString(byte[] bytes, int index, int count)
        {
            char[] chars = new char[count];
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i + index];
                chars[i] = b <= 0x7F ? (char)b : '?';
            }
            return new string(chars);
        }

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
