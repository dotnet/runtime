// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingEncode
    {
        public static IEnumerable<object[]> Encode_TestData()
        {
            yield return new object[] { "\u0023\u0025\u03a0\u03a3", 1, 2, new byte[] { 37, 206, 160 } };
            
            // Surrogate pairs
            yield return new object[] { "\uD800\uDC00", 0, 2, new byte[] { 240, 144, 128, 128 } };
            yield return new object[] { "a\uD800\uDC00b", 0, 4, new byte[] { 97, 240, 144, 128, 128, 98 } };

            // All ASCII chars
            for (char c = char.MinValue; c <= 0x7F; c++)
            {
                yield return new object[] { c.ToString(), 0, 1, new byte[] { (byte)c } };
                yield return new object[] { "a" + c.ToString() + "b", 1, 1, new byte[] { (byte)c } };
                yield return new object[] { "a" + c.ToString() + "b", 0, 3, new byte[] { 97, (byte)c, 98 } };
            }

            // Misc ASCII and Unicode strings
            yield return new object[] { "FooBA\u0400R", 0, 7, new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 } };
            yield return new object[] { "\u00C0nima\u0300l", 0, 7, new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 } };
            yield return new object[] { "Test\uD803\uDD75Test", 0, 10, new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 } };
            yield return new object[] { "\u0130", 0, 1, new byte[] { 196, 176 } };
            
            yield return new object[] { string.Empty, 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode(string chars, int index, int count, byte[] expected)
        {
            EncodingHelpers.Encode(new UTF8Encoding(), chars, index, count, expected);
            EncodingHelpers.Encode(new UTF8Encoding(true), chars, index, count, expected);
        }

        [Fact]
        public void Encode_InvalidUnicode()
        {
            // TODO: add into Encode_TestData once #7166 is fixed
            byte[] unicodeReplacementBytes1 = new byte[] { 239, 191, 189 };
            Encode("\uD800", 0, 1, unicodeReplacementBytes1); // Lone high surrogate
            Encode("\uDD75", 0, 1, unicodeReplacementBytes1); // Lone high surrogate
            Encode("\uDC00", 0, 1, unicodeReplacementBytes1); // Lone low surrogate
            Encode("\uD800\uDC00", 0, 1, unicodeReplacementBytes1); // Surrogate pair out of range
            Encode("\uD800\uDC00", 1, 1, unicodeReplacementBytes1); // Surrogate pair out of range

            // Mixture of ASCII, valid Unicode and invalid unicode
            Encode("\uD803\uDD75\uD803\uDD75\uD803\uDD75", 0, 6, new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 });
            Encode("\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", 0, 14, new byte[] { 239, 191, 189, 239, 191, 189, 240, 144, 181, 181, 239, 191, 189, 239, 191, 189, 239, 191, 189, 239, 191, 189, 239, 191, 189, 240, 144, 181, 181, 239, 191, 189, 239, 191, 189, 239, 191, 189 });
            Encode("Test\uD803Test", 0, 9, new byte[] { 84, 101, 115, 116, 239, 191, 189, 84, 101, 115, 116 });
            Encode("Test\uDD75Test", 0, 9, new byte[] { 84, 101, 115, 116, 239, 191, 189, 84, 101, 115, 116 });
            Encode("TestTest\uDD75", 0, 9, new byte[] { 84, 101, 115, 116, 84, 101, 115, 116, 239, 191, 189 });
            Encode("TestTest\uD803", 0, 9, new byte[] { 84, 101, 115, 116, 84, 101, 115, 116, 239, 191, 189 });

            Encode("za\u0306\u01FD\u03B2\uD8FF\uDCFF", 0, 7, new byte[] { 122, 97, 204, 134, 199, 189, 206, 178, 241, 143, 179, 191 });
            Encode("za\u0306\u01FD\u03B2\uD8FF\uDCFF", 4, 3, new byte[] { 206, 178, 241, 143, 179, 191 });

            // High BMP non-chars
            Encode("\uFFFD", 0, 1, unicodeReplacementBytes1);
            Encode("\uFFFE", 0, 1, new byte[] { 239, 191, 190 });
            Encode("\uFFFF", 0, 1, new byte[] { 239, 191, 191 });

            byte[] unicodeReplacementBytes2 = new byte[] { 239, 191, 189, 239, 191, 189 };
            Encode("\uD800\uD800", 0, 2, unicodeReplacementBytes2); // High, high
            Encode("\uDC00\uD800", 0, 2, unicodeReplacementBytes2); // Low, high
            Encode("\uDC00\uDC00", 0, 2, unicodeReplacementBytes2); // Low, low
        }
    }
}
