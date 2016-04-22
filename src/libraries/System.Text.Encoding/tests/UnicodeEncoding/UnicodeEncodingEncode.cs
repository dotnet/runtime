// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingEncode
    {
        public static IEnumerable<object[]> GetBytes_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                char c = (char)i;
                yield return new object[] { "a" + c + "b", 0, 3, new byte[] { 97, 0, (byte)c, 0, 98, 0 } };
                yield return new object[] { "a" + c + "b", 1, 1, new byte[] { (byte)c, 0 } };
            }

            // Unicode
            yield return new object[] { "a\u1234b", 0, 3, new byte[] { 97, 0, 52, 18, 98, 0 } };
            yield return new object[] { "a\u1234b", 1, 1, new byte[] { 52, 18 } };

            // Surrogate pairs
            yield return new object[] { "\uD800\uDC00", 0, 2, new byte[] { 0, 216, 0, 220 } };
            yield return new object[] { "a\uD800\uDC00b", 0, 4, new byte[] { 97, 0, 0, 216, 0, 220, 98, 0 } };

            // Mixture of ASCII and Unicode
            yield return new object[] { "FooBA\u0400R", 0, 7, new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 } };
            yield return new object[] { "\u00C0nima\u0300l", 0, 7, new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 } };
            yield return new object[] { "Test\uD803\uDD75Test", 0, 10, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 } };
            yield return new object[] { "\uD803\uDD75\uD803\uDD75\uD803\uDD75", 0, 6, new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 } };
            yield return new object[] { "\u0130", 0, 1, new byte[] { 48, 1 } };

            yield return new object[] { "za\u0306\u01fd\u03b2", 0, 5, new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3 } };
            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", 0, 7, new byte[] { 122, 0, 97, 0, 6, 3, 253, 1, 178, 3, 255, 216, 255, 220 } };
            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", 4, 3, new byte[] { 178, 3, 255, 216, 255, 220 } };

            // Empty bytes
            yield return new object[] { string.Empty, 0, 0, new byte[0] };
            yield return new object[] { "a\u1234b", 3, 0, new byte[0] };
            yield return new object[] { "a\u1234b", 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void Encode(string source, int index, int count, byte[] expectedLittleEndian)
        {
            EncodingHelpers.Encode(new UnicodeEncoding(false, true), source, index, count, expectedLittleEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(false, false), source, index, count, expectedLittleEndian);

            byte[] expectedBigEndian = (byte[])expectedLittleEndian.Clone();
            for (int i = 0; i < expectedBigEndian.Length; i += 2)
            {
                byte b1 = expectedBigEndian[i];
                byte b2 = expectedBigEndian[i + 1];

                expectedBigEndian[i] = b2;
                expectedBigEndian[i + 1] = b1;
            }

            EncodingHelpers.Encode(new UnicodeEncoding(true, true), source, index, count, expectedBigEndian);
            EncodingHelpers.Encode(new UnicodeEncoding(true, false), source, index, count, expectedBigEndian);
        }

        [Fact]
        public void Encode_InvalidUnicode()
        {
            // TODO: add into Encode_TestData once #7166 is fixed
            byte[] unicodeReplacementBytes1 = new byte[] { 253, 255 };
            Encode("\uD800", 0, 1, unicodeReplacementBytes1); // Lone high surrogate
            Encode("\uDC00", 0, 1, unicodeReplacementBytes1); // Lone low surrogate
            Encode("\uD800\uDC00", 0, 1, unicodeReplacementBytes1); // Surrogate pair out of range
            Encode("\uD800\uDC00", 1, 1, unicodeReplacementBytes1); // Surrogate pair out of range

            byte[] unicodeReplacementBytes2 = new byte[] { 253, 255, 253, 255 };
            Encode("\uD800\uD800", 0, 2, unicodeReplacementBytes2); // High, high
            Encode("\uDC00\uD800", 0, 2, unicodeReplacementBytes2); // Low, high
            Encode("\uDC00\uDC00", 0, 2, unicodeReplacementBytes2); // Low, low

            // Mixture of ASCII, valid Unicode and invalid Unicode
            Encode("Test\uD803Test", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0 });
            Encode("Test\uDD75Test", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0 });
            Encode("TestTest\uDD75", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 });
            Encode("TestTest\uD803", 0, 9, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 });
            Encode("\uDD75", 0, 1, new byte[] { 253, 255 });
            Encode("\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", 0, 14, new byte[] { 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255, 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255 });

            // High BMP non-chars
            Encode("\uFFFD", 0, 1, unicodeReplacementBytes1);
            Encode("\uFFFE", 0, 1, new byte[] { 254, 255 });
            Encode("\uFFFF", 0, 1, new byte[] { 255, 255 });
        }
    }
}
