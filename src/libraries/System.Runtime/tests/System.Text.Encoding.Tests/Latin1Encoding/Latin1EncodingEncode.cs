// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class Latin1EncodingEncode
    {
        public static IEnumerable<object[]> Encode_ValidLatin1Chars_TestData()
        {
            // All Latin1 chars
            for (int i = 0; i <= 0xFF; i++)
            {
                char c = (char)i;
                yield return new object[] { c, 0, 1 };
                yield return new object[] { "a" + c + "b", 1, 1 };
                yield return new object[] { "a" + c + "b", 2, 1 };
                yield return new object[] { "a" + c + "b", 0, 3 };
            }

            // Empty string
            yield return new object[] { string.Empty, 0, 0 };
            yield return new object[] { "abc", 3, 0 };
            yield return new object[] { "abc", 0, 0 };
        }

        [Theory]
        [MemberData(nameof(Encode_ValidLatin1Chars_TestData))]
        public void Encode_ValidLatin1Chars(string source, int index, int count)
        {
            Encode(source, index, count, GetBytes(source, index, count), valid: true);
        }

        public static IEnumerable<object[]> Encode_BestFit_TestData()
        {
            yield return new object[] { "\u0100", 0, 1, new byte[] { (byte)'A' } };
            yield return new object[] { "\u0100\u201E\uFF5E\u16DA", 0, 4, new byte[] { 0x41, 0x22, 0x7E, 0x3F } };

            yield return new object[] { "\uFF59\uFF60\u0262\u5FC3", 0, 4, new byte[] { 0x79, 0x3F, 0x3F, 0x3F } };
            yield return new object[] { "\u0001\u0060\u007E\u00E3\u0108\u2018\uFF59", 0, 7, new byte[] { 0x01, 0x60, 0x7E, 0xE3, 0x43, 0x27, 0x79 } };
        }

        [Theory]
        [MemberData(nameof(Encode_BestFit_TestData))]
        public void Encode_BestFit(string source, int index, int count, byte[] expected)
        {
            Encode(source, index, count, expected, valid: false);
        }

        public static IEnumerable<object[]> Encode_InvalidChars_TestData()
        {
            yield return new object[] { "\u1234\u2345", 0, 2 };
            yield return new object[] { "a\u1234\u2345b", 0, 4 };

            yield return new object[] { "\uD800\uDC00", 0, 2 };
            yield return new object[] { "a\uD800\uDC00b", 0, 2 };

            yield return new object[] { "\uD800\uDFFF", 0, 2 };

            // Invalid Unicode
            yield return new object[] { "\uD800", 0, 1 }; // Lone high surrogate
            yield return new object[] { "\uDC00", 0, 1 }; // Lone low surrogate
            yield return new object[] { "\uD800\uDC00", 0, 1 }; // Surrogate pair out of range
            yield return new object[] { "\uD800\uDC00", 1, 1 }; // Surrogate pair out of range

            yield return new object[] { "\uD800\uD800", 0, 2 }; // High, high
            yield return new object[] { "\uDC00\uD800", 0, 2 }; // Low, high
            yield return new object[] { "\uDC00\uDC00", 0, 2 }; // Low, low

            // High BMP non-chars
            yield return new object[] { "\uFFFD", 0, 1 };
            yield return new object[] { "\uFFFE", 0, 1 };
            yield return new object[] { "\uFFFF", 0, 1 };

        }

        [Theory]
        [MemberData(nameof(Encode_InvalidChars_TestData))]
        public void Encode_InvalidChars(string source, int index, int count)
        {
            Encode(source, index, count, GetBytes(source, index, count), valid: false);
        }

        private static void Encode(string source, int index, int count, byte[] expected, bool valid)
        {
            EncodingHelpers.Encode(Encoding.GetEncoding("latin1"), source, index, count, expected);

            Encoding exceptionEncoding = Encoding.GetEncoding("latin1", new EncoderExceptionFallback(), new DecoderReplacementFallback("?"));
            if (valid)
            {
                EncodingHelpers.Encode(exceptionEncoding, source, index, count, expected);
            }
            else
            {
                NegativeEncodingTests.Encode_Invalid(exceptionEncoding, source, index, count);
            }
        }

        private static byte[] GetBytes(string source, int index, int count)
        {
            return NarrowCharsToBytesWithReplacement(source.AsSpan(index, count));
        }

        private static byte[] NarrowCharsToBytesWithReplacement(ReadOnlySpan<char> source)
        {
            byte[] bytes = new byte[source.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                char c = source[i];
                bytes[i] = c <= 0xFF ? (byte)c : (byte)'?';
            }
            return bytes;
        }

        [Theory]
        [MemberData(nameof(Encode_ReplacesSurrogatesWithQuestionMark_TestData))]
        public void Encode_ReplacesSurrogatesWithQuestionMark(Encoding encoding)
        {
            char[] testInput = "\U0001F603abc\U0001F603".ToCharArray(); // U+1F603 = SMILING FACE WITH OPEN MOUTH
            byte[] expectedBytes = NarrowCharsToBytesWithReplacement(testInput);

            // First, test Encoding.GetBytes(char[]).

            Assert.Equal(expectedBytes, encoding.GetBytes(testInput));

            // Next, try using the encoder fallback mechanism.

            Encoder encoder = encoding.GetEncoder();
            Assert.NotNull(encoder);

            byte[] outputBuffer = new byte[7];
            encoder.Convert(testInput, outputBuffer.AsSpan(0, 5), false /* flush */, out int charsConsumed, out int bytesWritten, out bool completed);
            Assert.Equal(5, bytesWritten);
            Assert.False(completed);

            encoder.Convert(testInput.AsSpan(charsConsumed), outputBuffer.AsSpan(bytesWritten), true /* flush */, out charsConsumed, out bytesWritten, out completed);
            Assert.Equal(2, bytesWritten);
            Assert.True(completed);

            Assert.Equal(expectedBytes, outputBuffer);
        }

        public static IEnumerable<object[]> Encode_ReplacesSurrogatesWithQuestionMark_TestData()
        {
            yield return new object[] { Encoding.GetEncoding("latin1") }; // uses best fit encoding by default
            yield return new object[] { Encoding.GetEncoding("latin1", EncoderFallback.ReplacementFallback, DecoderFallback.ExceptionFallback) };
        }
    }
}
