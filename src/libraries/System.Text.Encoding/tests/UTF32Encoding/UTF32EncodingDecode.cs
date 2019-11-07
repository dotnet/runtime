// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (char c = char.MinValue; c <= 0xFF; c++)
            {
                yield return new object[] { new byte[] { (byte)c, 0, 0, 0 }, 0, 4, c.ToString() };
                yield return new object[] { new byte[] { 97, 0, 0, 0, (byte)c, 0, 0, 0, 98, 0, 0, 0 }, 4, 4, c.ToString() };
                yield return new object[] { new byte[] { 97, 0, 0, 0, (byte)c, 0, 0, 0, 98, 0, 0, 0 }, 0, 12, "a" + c.ToString() + "b" };
            }

            // Surrogate pairs
            yield return new object[] { new byte[] { 0, 0, 1, 0 }, 0, 4, "\uD800\uDC00" };
            yield return new object[] { new byte[] { 97, 0, 0, 0, 0, 0, 1, 0, 98, 0, 0, 0 }, 0, 12, "a\uD800\uDC00b"  };

            yield return new object[] { new byte[] { 0x00, 0x00, 0x01, 0x00, 0xFF, 0xFF, 0x10, 0x00 }, 0, 8, "\uD800\uDC00\uDBFF\uDFFF" };

            // Mixture of ASCII and Unciode
            yield return new object[] { new byte[] { 70, 0, 0, 0, 111, 0, 0, 0, 111, 0, 0, 0, 66, 0, 0, 0, 65, 0, 0, 0, 0, 4, 0, 0, 82, 0, 0, 0 }, 0, 28, "FooBA\u0400R" };

            // U+FDD0 - U+FDEF
            yield return new object[] { new byte[] { 0xD0, 0xFD, 0x00, 0x00, 0xEF, 0xFD, 0x00, 0x00 }, 0, 8, "\uFDD0\uFDEF" };
            yield return new object[] { new byte[] { 0xD0, 0xFD, 0x00, 0x00, 0xEF, 0xFD, 0x00, 0x00 }, 0, 8, "\uFDD0\uFDEF" };

            // High BMP non-chars: U+FFFF, U+FFFE, U+FFFD
            yield return new object[] { new byte[] { 253, 255, 0, 0 }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 254, 255, 0, 0 }, 0, 4, "\uFFFE" };
            yield return new object[] { new byte[] { 255, 255, 0, 0 }, 0, 4, "\uFFFF" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 0, 12, "\uFFFF\uFFFE\uFFFD" };

            // Empty strings
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] littleEndianBytes, int index, int count, string expected)
        {
            byte[] bigEndianBytes = GetBigEndianBytes(littleEndianBytes, index, count);

            EncodingHelpers.Decode(new UTF32Encoding(true, true, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(true, false, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, true, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, false, false), littleEndianBytes, index, count, expected);

            EncodingHelpers.Decode(new UTF32Encoding(true, true, true), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(true, false, true), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, true, true), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, false, true), littleEndianBytes, index, count, expected);
        }

        public static IEnumerable<object[]> Decode_InvalidBytes_TestData()
        {
            yield return new object[] { new byte[] { 123 }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 123, 123 }, 0, 2, "\uFFFD" };
            yield return new object[] { new byte[] { 123, 123, 123 }, 0, 3, "\uFFFD" };
            yield return new object[] { new byte[] { 123, 123, 123, 123 }, 1, 3, "\uFFFD" };
            yield return new object[] { new byte[] { 97, 0, 0, 0, 0 }, 0, 5, "a\uFFFD" };

            yield return new object[] { new byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 8, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 4, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0x00, 0xD8, 0x00, 0x00, 0x00, 0xDC, 0x00, 0x00 }, 0, 8, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xDB, 0x00, 0x00, 0xFD, 0xFF, 0x00, 0x00 }, 0, 8, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0x00, 0x80, 0x00, 0x00, 0xFF, 0xDF, 0x00, 0x00 }, 0, 8, "\u8000\uFFFD" };

            // Too high scalar values
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0x11, 0x00 }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x11, 0x00 }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0x10, 0x01 }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0x00, 0x00, 0x00, 0xFF }, 0, 4, "\uFFFD" };
            yield return new object[] { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 4, "\uFFFD" };
        }

        [Theory]
        [MemberData(nameof(Decode_InvalidBytes_TestData))]
        public void Decode_InvalidBytes(byte[] littleEndianBytes, int index, int count, string expected)
        {
            byte[] bigEndianBytes = GetBigEndianBytes(littleEndianBytes, index, count);

            EncodingHelpers.Decode(new UTF32Encoding(true, true, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(true, false, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, true, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, false, false), littleEndianBytes, index, count, expected);

            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(true, true, true), bigEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(true, false, true), bigEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(false, true, true), littleEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(false, false, true), littleEndianBytes, index, count);
        }

        public static byte[] GetBigEndianBytes(byte[] littleEndianBytes, int index, int count)
        {
            byte[] bytes = new byte[littleEndianBytes.Length];

            int i;
            for (i = index; i + 3 < index + count; i += 4)
            {
                bytes[i] = littleEndianBytes[i + 3];
                bytes[i + 1] = littleEndianBytes[i + 2];
                bytes[i + 2] = littleEndianBytes[i + 1];
                bytes[i + 3] = littleEndianBytes[i];
            }

            // Invalid byte arrays may not have a multiple of 4 length
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
