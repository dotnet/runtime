// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingEncode
    {
        public static IEnumerable<object[]> Encode_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                char c = (char)i;
                yield return new object[] { "a" + c + "b", 0, 3, new byte[] { 97, 0, (byte)c, 0, 98, 0 } };
                yield return new object[] { "a" + c + "b", 1, 1, new byte[] { (byte)c, 0 } };
                yield return new object[] { "a" + c + "b", 2, 1, new byte[] { 98, 0 } };
            }

            // Unicode
            yield return new object[] { "a\u1234b", 0, 3, new byte[] { 97, 0, 52, 18, 98, 0 } };
            yield return new object[] { "a\u1234b", 1, 1, new byte[] { 52, 18 } };

            // Surrogate pairs
            yield return new object[] { "\uD800\uDC00", 0, 2, new byte[] { 0, 216, 0, 220 } };
            yield return new object[] { "a\uD800\uDC00b", 0, 4, new byte[] { 97, 0, 0, 216, 0, 220, 98, 0 } };
            
            yield return new object[] { "\uD800\uDC00\uFFFD\uFEB7", 0, 4, new byte[] { 0x00, 0xD8, 0x00, 0xDC, 0xFD, 0xFF, 0xB7, 0xFE } };

            // Mixture of ASCII and Unicode
            yield return new object[] { "FooBA\u0400R", 0, 7, new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 } };
            yield return new object[] { "\u00C0nima\u0300l", 0, 7, new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 } };
            yield return new object[] { "Test\uD803\uDD75Test", 0, 10, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 } };
            yield return new object[] { "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 0, 6, new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 } };
            yield return new object[] { "\u0130", 0, 1, new byte[] { 48, 1 } };

            yield return new object[] { "za\u0306\u01fd\u03b2", 0, 5, new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3 } };
            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", 0, 7, new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3, 255, 216, 255, 220 } };
            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", 4, 3, new byte[] { 178, 3, 255, 216, 255, 220 } };

            // High BMP non-chars
            yield return new object[] { "\uFFFD", 0, 1, new byte[] { 253, 255 } };
            yield return new object[] { "\uFFFE", 0, 1, new byte[] { 254, 255 } };
            yield return new object[] { "\uFFFF", 0, 1, new byte[] { 255, 255 } };
            yield return new object[] { "\uFFFF\uFFFE", 0, 2, new byte[] { 0xFF, 0xFF, 0xFE, 0xFF } };

            // Empty strings
            yield return new object[] { string.Empty, 0, 0, new byte[0] };
            yield return new object[] { "a\u1234b", 3, 0, new byte[0] };
            yield return new object[] { "a\u1234b", 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode(string source, int index, int count, byte[] expectedLittleEndian)
        {
            byte[] expectedBigEndian = GetBigEndianBytes(expectedLittleEndian);

            EncodingHelpers.Encode(new UnicodeEncoding(false, true, false), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(false, false, false), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, true, false), source, index, count, expectedBigEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, false, false), source, index, count, expectedBigEndian);

            EncodingHelpers.Encode(new UnicodeEncoding(false, true, true), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(false, false, true), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, true, true), source, index, count, expectedBigEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, false, true), source, index, count, expectedBigEndian);
        }

        public static IEnumerable<object[]> Encode_InvalidChars_TestData()
        {
            byte[] unicodeReplacementBytes1 = new byte[] { 253, 255 };
            yield return new object[] { "\uD800", 0, 1, unicodeReplacementBytes1 }; // Lone high surrogate
            yield return new object[] { "\uDC00", 0, 1, unicodeReplacementBytes1 }; // Lone low surrogate

            // Surrogate pair out of range
            yield return new object[] { "\uD800\uDC00", 0, 1, unicodeReplacementBytes1 };
            yield return new object[] { "\uD800\uDC00", 1, 1, unicodeReplacementBytes1 };
            yield return new object[] { "\uDBFF\uDFFF", 0, 1, unicodeReplacementBytes1 };
            yield return new object[] { "\uDBFF\uDFFF", 1, 1, unicodeReplacementBytes1 };

            byte[] unicodeReplacementBytes2 = new byte[] { 253, 255, 253, 255 };
            yield return new object[] { "\uD800\uD800", 0, 2, unicodeReplacementBytes2 }; // High, high
            yield return new object[] { "\uDC00\uD800", 0, 2, unicodeReplacementBytes2 }; // Low, high
            yield return new object[] { "\uDC00\uDC00", 0, 2, unicodeReplacementBytes2 }; // Low, low

            // Mixture of ASCII, valid Unicode and invalid Unicode
            yield return new object[] { "Test\uD803Test", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0 } };
            yield return new object[] { "Test\uDD75Test", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0 } };
            yield return new object[] { "TestTest\uDD75", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 } };
            yield return new object[] { "TestTest\uD803", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 } };
            yield return new object[] { "\uDD75", 0, 1, new byte[] { 253, 255 } };
            yield return new object[] { "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", 0, 14, new byte[] { 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255, 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255 } };
        }

        [Theory]
        [MemberData(nameof(Encode_InvalidChars_TestData))]        
        public void Encode_InvalidChars(string source, int index, int count, byte[] expectedLittleEndian)
        {
            byte[] expectedBigEndian = GetBigEndianBytes(expectedLittleEndian);

            EncodingHelpers.Encode(new UnicodeEncoding(false, true, false), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(false, false, false), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, true, false), source, index, count, expectedBigEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, false, false), source, index, count, expectedBigEndian);

            NegativeEncodingTests.Encode_Invalid(new UnicodeEncoding(false, true, true), source, index, count);
            NegativeEncodingTests.Encode_Invalid(new UnicodeEncoding(false, false, true), source, index, count);
            NegativeEncodingTests.Encode_Invalid(new UnicodeEncoding(true, true, true), source, index, count);
            NegativeEncodingTests.Encode_Invalid(new UnicodeEncoding(true, false, true), source, index, count);
        }

        [Fact]
        public unsafe void GetByteCount_OverlyLargeCount_ThrowsArgumentOutOfRangeException()
        {
            UnicodeEncoding encoding = new UnicodeEncoding();
            fixed (char* pChars = "abc")
            {
                char* pCharsLocal = pChars;
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => encoding.GetByteCount(pCharsLocal, int.MaxValue / 2 + 1));
            }
        }

        public static byte[] GetBigEndianBytes(byte[] littleEndianBytes)
        {
            byte[] bigEndianBytes = (byte[])littleEndianBytes.Clone();
            for (int i = 0; i < bigEndianBytes.Length; i += 2)
            {
                byte b1 = bigEndianBytes[i];
                byte b2 = bigEndianBytes[i + 1];

                bigEndianBytes[i] = b2;
                bigEndianBytes[i + 1] = b1;
            }
            return bigEndianBytes;
        }
    }
}
