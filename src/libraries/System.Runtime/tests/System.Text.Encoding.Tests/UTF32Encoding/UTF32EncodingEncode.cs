// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingEncode
    {
        public static IEnumerable<object[]> Encode_TestData()
        {
            // All ASCII chars
            for (char c = char.MinValue; c <= 0xFF; c++)
            {
                yield return new object[] { c.ToString(), 0, 1, new byte[] { (byte)c, 0, 0, 0 } };
                yield return new object[] { "a" + c.ToString() + "b", 1, 1, new byte[] { (byte)c, 0, 0, 0 } };
                yield return new object[] { "a" + c.ToString() + "b", 2, 1, new byte[] { 98, 0, 0, 0 } };
                yield return new object[] { "a" + c.ToString() + "b", 0, 3, new byte[] { 97, 0, 0, 0, (byte)c, 0, 0, 0, 98, 0, 0, 0 } };
            }

            // Surrogate pairs
            yield return new object[] { "\uD800\uDC00", 0, 2, new byte[] { 0, 0, 1, 0 } };
            yield return new object[] { "a\uD800\uDC00b", 0, 4, new byte[] { 97, 0, 0, 0, 0, 0, 1, 0, 98, 0, 0, 0 } };

            yield return new object[] { "\uD800\uDFFF", 0, 2, new byte[] { 0xFF, 0x03, 0x01, 0x00 } };
            yield return new object[] { "\uDBFF\uDC00", 0, 2, new byte[] { 0x00, 0xFC, 0x10, 0x00 } };
            yield return new object[] { "\uDBFF\uDFFF", 0, 2, new byte[] { 0xFF, 0xFF, 0x10, 0x00 } };

            // Mixture of ASCII and Unciode
            yield return new object[] { "FooBA\u0400R", 0, 7, new byte[] { 70, 0, 0, 0, 111, 0, 0, 0, 111, 0, 0, 0, 66, 0, 0, 0, 65, 0, 0, 0, 0, 4, 0, 0, 82, 0, 0, 0 } };

            // High BMP non-chars: U+FFFF, U+FFFE, U+FFFD
            yield return new object[] { "\uFFFD", 0, 1, new byte[] { 253, 255, 0, 0 } };
            yield return new object[] { "\uFFFE", 0, 1, new byte[] { 254, 255, 0, 0 } };
            yield return new object[] { "\uFFFF", 0, 1, new byte[] { 255, 255, 0, 0 } };
            yield return new object[] { "\uFFFF\uFFFE\uFFFD", 0, 3, new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 } };

            // Empty strings
            yield return new object[] { "abc", 3, 0, new byte[0] };
            yield return new object[] { "abc", 0, 0, new byte[0] };
            yield return new object[] { string.Empty, 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode(string chars, int index, int count, byte[] littleEndianExpected)
        {
            byte[] bigEndianExpected = GetBigEndianBytes(littleEndianExpected);

            EncodingHelpers.Encode(new UTF32Encoding(true, true, false), chars, index, count, bigEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(true, false, false), chars, index, count, bigEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(false, true, false), chars, index, count, littleEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(false, false, false), chars, index, count, littleEndianExpected);

            EncodingHelpers.Encode(new UTF32Encoding(true, true, true), chars, index, count, bigEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(true, false, true), chars, index, count, bigEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(false, true, true), chars, index, count, littleEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(false, false, true), chars, index, count, littleEndianExpected);
        }

        public static IEnumerable<object[]> Encode_InvalidChars_TestData()
        {
            byte[] unicodeReplacementBytes1 = new byte[] { 253, 255, 0, 0 };
            yield return new object[] { "\uD800", 0, 1, unicodeReplacementBytes1 }; // Lone high surrogate
            yield return new object[] { "\uDD75", 0, 1, unicodeReplacementBytes1 }; // Lone high surrogate
            yield return new object[] { "\uDC00", 0, 1, unicodeReplacementBytes1 }; // Lone low surrogate
            yield return new object[] { "\uD800\uDC00", 0, 1, unicodeReplacementBytes1 }; // Surrogate pair out of range
            yield return new object[] { "\uD800\uDC00", 1, 1, unicodeReplacementBytes1 }; // Surrogate pair out of range

            byte[] unicodeReplacementBytes2 = new byte[] { 253, 255, 0, 0, 253, 255, 0, 0 };
            yield return new object[] { "\uD800\uD800", 0, 2, unicodeReplacementBytes2 }; // High, high
            yield return new object[] { "\uDC00\uD800", 0, 2, unicodeReplacementBytes2 }; // Low, high
            yield return new object[] { "\uDC00\uDC00", 0, 2, unicodeReplacementBytes2 }; // Low, low

            // Invalid first/second in surrogate pair
            yield return new object[] { "\uD800\u0041", 0, 2, new byte[] { 0xFD, 0xFF, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 } };
            yield return new object[] { "\u0065\uDC00", 0, 2, new byte[] { 0x65, 0x00, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 } };
        }

        [Theory]
        [MemberData(nameof(Encode_InvalidChars_TestData))]
        public void Encode_InvalidChars(string chars, int index, int count, byte[] littleEndianExpected)
        {
            byte[] bigEndianExpected = GetBigEndianBytes(littleEndianExpected);

            EncodingHelpers.Encode(new UTF32Encoding(true, true, false), chars, index, count, bigEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(true, false, false), chars, index, count, bigEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(false, true, false), chars, index, count, littleEndianExpected);
            EncodingHelpers.Encode(new UTF32Encoding(false, false, false), chars, index, count, littleEndianExpected);

            NegativeEncodingTests.Encode_Invalid(new UTF32Encoding(true, true, true), chars, index, count);
            NegativeEncodingTests.Encode_Invalid(new UTF32Encoding(true, false, true), chars, index, count);
            NegativeEncodingTests.Encode_Invalid(new UTF32Encoding(false, true, true), chars, index, count);
            NegativeEncodingTests.Encode_Invalid(new UTF32Encoding(false, false, true), chars, index, count);
        }

        public static byte[] GetBigEndianBytes(byte[] littleEndianBytes)
        {
            byte[] bigEndianBytes = (byte[])littleEndianBytes.Clone();
            for (int i = 0; i < littleEndianBytes.Length; i += 4)
            {
                byte b1 = bigEndianBytes[i];
                byte b2 = bigEndianBytes[i + 1];
                byte b3 = bigEndianBytes[i + 2];
                byte b4 = bigEndianBytes[i + 3];

                bigEndianBytes[i] = b4;
                bigEndianBytes[i + 1] = b3;
                bigEndianBytes[i + 2] = b2;
                bigEndianBytes[i + 3] = b1;
            }
            return bigEndianBytes;
        }
    }
}
