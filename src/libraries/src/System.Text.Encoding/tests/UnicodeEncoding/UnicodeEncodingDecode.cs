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
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0, 45 }, 0, 21, "TestString\uFFFD" };
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 117, 221 }, 0, 18, "TestTest\uFFFD" };
            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 3, 216 }, 0, 17, "TestTest\uFFFD" };

            yield return new object[] { new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 }, 0, 14, "FooBA\u0400R" };
            yield return new object[] { new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 }, 0, 14, "\u00C0nima\u0300l" };
            yield return new object[] { new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3 }, 0, 10, "za\u0306\u01fd\u03b2" };
            yield return new object[] { new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3 }, 0, 8, "za\u0306\u01fd" };

            yield return new object[] { new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 }, 0, 20, "Test\uD803\uDD75Test" };
            yield return new object[] { new byte[] { 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, 0, 30, "\0Te\nst\0\t\0T\u000Fest\0" };

            yield return new object[] { new byte[] { 3, 216, 84 }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 }, 0, 12, "\uD803\uDD75\uD803\uDD75\uD803\uDD75" };
            yield return new object[] { new byte[] { 3, 216, 117, 221, 3, 216, 117, 221 }, 0, 8, "\uD803\uDD75\uD803\uDD75" };

            yield return new object[] { new byte[] { 48, 1 }, 0, 2, "\u0130" };
            yield return new object[] { new byte[] { 92, 0, 97, 0, 98, 0, 99, 0, 32, 0, }, 0, 10, "\\abc\u0020" };

            // Empty string
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
            yield return new object[] { unicodeBytes, 6, 0, string.Empty };
            yield return new object[] { unicodeBytes, 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] littleEndianBytes, int index, int count, string expected)
        {
            Decode_LittleEndian(littleEndianBytes, index, count, expected);

            byte[] bigEndianBytes = (byte[])littleEndianBytes.Clone();
            for (int i = 0; i < bigEndianBytes.Length; i += 2)
            {
                if (i + 1 >= bigEndianBytes.Length)
                {
                    continue;
                }
                byte b1 = bigEndianBytes[i];
                byte b2 = bigEndianBytes[i + 1];

                bigEndianBytes[i] = b2;
                bigEndianBytes[i + 1] = b1;
            }
            Decode_BigEndian(bigEndianBytes, index, count, expected);
        }

        public void Decode_LittleEndian(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UnicodeEncoding(false, true), bytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(false, false), bytes, index, count, expected);
        }

        public static IEnumerable<object[]> Decode_BigEndian_TestData()
        {
            yield return new object[] { new byte[] { 216, 3, 48 }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0, 0, 0, 84, 0, 101, 0, 10, 0, 115, 0, 116, 0, 0, 0, 9, 0, 0, 0, 84, 0, 15, 0, 101, 0, 115, 0, 116, 0, 0, 0 }, 0, 31, "\0Te\nst\0\t\0T\u000Fest\0\uFFFD" };

            yield return new object[] { new byte[] { 0, 70, 0, 111, 0, 111, 0, 66, 0, 65, 4, 0, 0, 82, 70 }, 0, 15, "FooBA\u0400R\uFFFD" };
        }
        
        [Theory]
        [MemberData(nameof(Decode_BigEndian_TestData))]
        public void Decode_BigEndian(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UnicodeEncoding(true, false), bytes, index, count, expected);
            EncodingHelpers.Decode(new UnicodeEncoding(true, true), bytes, index, count, expected);
        }

        [Fact]
        public void Decode_InvalidUnicode()
        {
            // TODO: add into Decode_TestData once #7166 is fixed
            // High BMP non-chars
            Decode(new byte[] { 253, 255 }, 0, 2, "\uFFFD");
            Decode(new byte[] { 254, 255 }, 0, 2, "\uFFFE");
            Decode(new byte[] { 255, 255 }, 0, 2, "\uFFFF");

            // Invalid bytes
            byte[] validSurrogateBytes = new byte[] { 0, 216, 0, 220 };
            Decode_LittleEndian(validSurrogateBytes, 0, 3, "\uFFFD\uFFFD");
            Decode_LittleEndian(validSurrogateBytes, 1, 3, "\u00D8\uFFFD");
            Decode(validSurrogateBytes, 0, 2, "\uFFFD");
            Decode_LittleEndian(validSurrogateBytes, 1, 2, "\u00D8");
            Decode(validSurrogateBytes, 2, 2, "\uFFFD");
            Decode(validSurrogateBytes, 2, 1, "\uFFFD");

            Decode(new byte[] { 97 }, 0, 1, "\uFFFD");
            Decode(new byte[] { 97, 0, 97 }, 0, 3, "a\uFFFD");
        }
    }
}
