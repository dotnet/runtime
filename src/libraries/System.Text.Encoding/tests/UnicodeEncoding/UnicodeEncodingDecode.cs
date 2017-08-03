// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                char c = (char)i;
                yield return new object[] { new byte[] { 97, 0, (byte)c, 0, 98, 0 }, 0, 6, "a" + c + "b" };
                yield return new object[] { new byte[] { 97, 0, (byte)c, 0, 98, 0 }, 2, 2, c.ToString() };
            }

            // Long ASCII strings
            yield return new object[] { new byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 0, 6, "\u0061\u0062\u0063" };
            yield return new object[] { new byte[] { 0x61, 0x00, 0x62, 0x00, 0x63, 0x00 }, 4, 2, "\u0063" };

            // Unicode
            byte[] unicodeBytes = new byte[] { 97, 0, 52, 18, 98, 0 };
            yield return new object[] { unicodeBytes, 0, 6, "a\u1234b" };
            yield return new object[] { unicodeBytes, 2, 2, "\u1234" };

            // Surrogate pairs
            byte[] surrogateBytes = new byte[] { 97, 0, 0, 216, 0, 220, 98, 0 };
            yield return new object[] { surrogateBytes, 0, 8, "a\uD800\uDC00b" };
            yield return new object[] { surrogateBytes, 2, 4, "\uD800\uDC00" };

            // Mixture of ASCII and Unicode
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0 }, 0, 16, "TestTest" };
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, 0, 20, "TestString" };

            yield return new object[] { new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 }, 0, 14, "FooBA\u0400R" };

            yield return new object[] { new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 }, 0, 14, "\u00C0nima\u0300l" };
            yield return new object[] { new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3 }, 0, 10, "za\u0306\u01fd\u03b2" };
            yield return new object[] { new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3 }, 0, 8, "za\u0306\u01fd" };

            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 }, 0, 20, "Test\uD803\uDD75Test" };
            yield return new object[] { new byte[] { 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, 0, 30, "\0Te\nst\0\t\0T\u000Fest\0" };

            yield return new object[] { new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 }, 0, 12, "\uD803\uDD75\uD803\uDD75\uD803\uDD75" };
            yield return new object[] { new byte[] { 3, 216, 117, 221, 3, 216, 117, 221 }, 0, 8, "\uD803\uDD75\uD803\uDD75" };

            yield return new object[] { new byte[] { 48, 1 }, 0, 2, "\u0130" };
            yield return new object[] { new byte[] { 92, 0, 97, 0, 98, 0, 99, 0, 32, 0, }, 0, 10, "\\abc\u0020" };

            // High BMP non-chars
            yield return new object[] { new byte[] { 253, 255 }, 0, 2, "\uFFFD" };
            yield return new object[] { new byte[] { 254, 255 }, 0, 2, "\uFFFE" };
            yield return new object[] { new byte[] { 255, 255 }, 0, 2, "\uFFFF" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFE, 0xFF }, 0, 4, "\uFFFF\uFFFE" };

            // U+FDD0 - U+FDEF
            yield return new object[] { new byte[] { 0xD0, 0xFD, 0xEF, 0xFD }, 0, 4, "\uFDD0\uFDEF" };

            yield return new object[] { new byte[] { 0, 216, 0, 220 }, 1, 2, "\u00D8" };

            // Empty string
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
            yield return new object[] { unicodeBytes, 6, 0, string.Empty };
            yield return new object[] { unicodeBytes, 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20525", TargetFrameworkMonikers.UapAot)]
        public void Decode(byte[] littleEndianBytes, int index, int count, string expected)
        {
            byte[] bigEndianBytes = GetBigEndianBytes(littleEndianBytes, index, count);

            EncodingHelpers.Decode(new UnicodeEncoding(false, true, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(false, false, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, false, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, true, false), bigEndianBytes, index, count, expected);

            // Decoding valid bytes should throw with a DecoderExceptionFallback
            EncodingHelpers.Decode(new UnicodeEncoding(false, true, true), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(false, false, true), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, false, true), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, true, true), bigEndianBytes, index, count, expected);
        }

        public static IEnumerable<object[]> Decode_InvalidBytes_TestData()
        {
            yield return new object[] { new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0, 70 }, 0, 15, "FooBA\u0400R\uFFFD" };

            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0, 45 }, 0, 21, "TestString\uFFFD" };
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 117, 221 }, 0, 18, "TestTest\uFFFD" };
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 3, 216 }, 0, 17, "TestTest\uFFFD" };

            yield return new object[] { new byte[] { 84, 0, 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0, 0 }, 0, 33, "T\0Te\nst\0\t\0T\u000Fest\0\uFFFD" };

            yield return new object[] { new byte[] { 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0, 0 }, 0, 31, "\0Te\nst\0\t\0T\u000Fest\0\uFFFD" };

            yield return new object[] { new byte[] { 3, 216, 84 }, 0, 3, "\uFFFD\uFFFD" };

            // Invalid surrogate bytes
            byte[] validSurrogateBytes1 = new byte[] { 0, 216, 0, 220 };
            yield return new object[] { validSurrogateBytes1, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { validSurrogateBytes1, 1, 3, "\u00D8\uFFFD" };
            yield return new object[] { validSurrogateBytes1, 0, 2, "\uFFFD" };
            yield return new object[] { validSurrogateBytes1, 2, 2, "\uFFFD" };
            yield return new object[] { validSurrogateBytes1, 2, 1, "\uFFFD" };

            yield return new object[] { new byte[] { 0xFF, 0xDB, 0x00, 0xDC }, 0, 2, "\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xDB, 0x00, 0xDC }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xDB, 0xFF, 0xDF }, 1, 3, "\uFFDB\uFFFD" };
            yield return new object[] { new byte[] { 0x00, 0xD8, 0xFF, 0xDF }, 2, 2, "\uFFFD" };

            yield return new object[] { new byte[] { 0xFF, 0xDF }, 0, 2, "\uFFFD" };

            // Odd number of bytes
            yield return new object[] { new byte[] { 97 }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 97, 0, 97 }, 0, 3, "a\uFFFD" };

            yield return new object[] { new byte[] { 3, 216, 48 }, 0, 3, "\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0x61, 0x00, 0x00 }, 0, 3, "\u0061\uFFFD" };
            yield return new object[] { new byte[] { 0x61 }, 0, 1, "\uFFFD" };

        }

        [Theory]
        [MemberData(nameof(Decode_InvalidBytes_TestData))]
        public void Decode_InvalidBytes(byte[] littleEndianBytes, int index, int count, string expected)
        {
            byte[] bigEndianBytes = GetBigEndianBytes(littleEndianBytes, index, count);

            EncodingHelpers.Decode(new UnicodeEncoding(false, true, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(false, false, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, false, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, true, false), bigEndianBytes, index, count, expected);

            // Decoding invalid bytes should throw with a DecoderExceptionFallback
            NegativeEncodingTests.Decode_Invalid(new UnicodeEncoding(false, true, true), littleEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UnicodeEncoding(false, false, true), littleEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UnicodeEncoding(true, false, true), bigEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UnicodeEncoding(true, true, true), bigEndianBytes, index, count);
        }

        public static byte[] GetBigEndianBytes(byte[] littleEndianBytes, int index, int count)
        {
            byte[] bytes = new byte[littleEndianBytes.Length];

            int i;
            for (i = index; i + 1 < index + count; i += 2)
            {
                bytes[i] = littleEndianBytes[i + 1];
                bytes[i + 1] = littleEndianBytes[i];
            }

            // Invalid byte arrays may not have a multiple of 2 length
            // Since they are invalid in both big and little endian orderings,
            // we don't need to convert the ordering.
            for (; i < index + count; i++)
            {
                bytes[i] = littleEndianBytes[i];
            }

            return bytes;
        }
    }
}
