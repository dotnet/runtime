// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (char c = char.MinValue; c <= 0x7F; c++)
            {
                yield return new object[] { new byte[] { (byte)c }, 0, 1, c.ToString() };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 1, 1, c.ToString() };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 0, 3, "a" + c.ToString() + "b" };
            }

            yield return new object[] { new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, 0, 10, "TestString" };
            yield return new object[] { new byte[] { 84, 101, 115, 116, 84, 101, 115, 116 }, 0, 8, "TestTest" };

            // Mixture of ASCII and Unicode
            yield return new object[] { new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 }, 0, 8, "FooBA\u0400R" };
            yield return new object[] { new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 }, 0, 9, "\u00C0nima\u0300l" };
            yield return new object[] { new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 }, 0, 12, "Test\uD803\uDD75Test" };
            yield return new object[] { new byte[] { 0, 84, 101, 10, 115, 116, 0, 9, 0, 84, 15, 101, 115, 116, 0 }, 0, 15, "\0Te\nst\0\t\0T\u000Fest\0" };
            yield return new object[] { new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, 0, 12, "\uD803\uDD75\uD803\uDD75\uD803\uDD75" };
            yield return new object[] { new byte[] { 196, 176 }, 0, 2, "\u0130" };

            // Surrogate pairs
            yield return new object[] { new byte[] { 240, 144, 128, 128 }, 0, 4, "\uD800\uDC00" };
            yield return new object[] { new byte[] { 97, 240, 144, 128, 128, 98 }, 0, 6, "a\uD800\uDC00b" };

            yield return new object[] { new byte[0], 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF8Encoding(true, false), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF8Encoding(false, false), bytes, index, count, expected);

            EncodingHelpers.Decode(new UTF8Encoding(false, true), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF8Encoding(true, true), bytes, index, count, expected);
        }
        
        public static IEnumerable<object[]> Decode_InvalidBytes_TestData()
        {
            yield return new object[] { new byte[] { 196, 84, 101, 115, 116, 196, 196, 196, 176, 176, 84, 101, 115, 116, 176 }, 0, 15, "\uFFFDTest\uFFFD\uFFFD\u0130\uFFFDTest\uFFFD" };
            yield return new object[] { new byte[] { 240, 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 240 }, 0, 12, "\uFFFD\uD803\uDD75\uD803\uDD75\uFFFD\uFFFD" };
        }

        [Theory]
        [MemberData(nameof(Decode_InvalidBytes_TestData))]
        public static void Decode_InvalidBytes(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF8Encoding(true, false), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF8Encoding(false, false), bytes, index, count, expected);

            NegativeEncodingTests.Decode_Invalid(new UTF8Encoding(false, true), bytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF8Encoding(true, true), bytes, index, count);
        }

        [Fact]
        public void Decode_InvalidBytes()
        {
            // TODO: add into Decode_TestData or Decode_InvalidBytes_TestData once #7166 is fixed
            // High BMP non-chars
            Decode(new byte[] { 239, 191, 189 }, 0, 3, "\uFFFD");
            Decode(new byte[] { 239, 191, 190 }, 0, 3, "\uFFFE");
            Decode(new byte[] { 239, 191, 191 }, 0, 3, "\uFFFF");

            // Invalid bytes
            byte[] validSurrogateBytes = new byte[] { 240, 144, 128, 128 };
            Decode_InvalidBytes(validSurrogateBytes, 0, 3, "\uFFFD");
            Decode_InvalidBytes(validSurrogateBytes, 1, 3, "\uFFFD\uFFFD\uFFFD");
            Decode_InvalidBytes(validSurrogateBytes, 0, 2, "\uFFFD");
            Decode_InvalidBytes(validSurrogateBytes, 1, 2, "\uFFFD\uFFFD");
            Decode_InvalidBytes(validSurrogateBytes, 2, 2, "\uFFFD\uFFFD");
            Decode_InvalidBytes(validSurrogateBytes, 2, 1, "\uFFFD");

            // These are examples of overlong sequences. This can cause security
            // vulnerabilities (e.g. MS00-078) so it is important we parse these as invalid.
            Decode_InvalidBytes(new byte[] { 0xC0, 0xAF }, 0, 2, "\uFFFD\uFFFD");
            Decode_InvalidBytes(new byte[] { 0xE0, 0x80, 0xBF }, 0, 3, "\uFFFD\uFFFD");
            Decode_InvalidBytes(new byte[] { 0xF0, 0x80, 0x80, 0xBF }, 0, 4, "\uFFFD\uFFFD\uFFFD");
            Decode_InvalidBytes(new byte[] { 0xF8, 0x80, 0x80, 0x80, 0xBF }, 0, 5, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD");
            Decode_InvalidBytes(new byte[] { 0xFC, 0x80, 0x80, 0x80, 0x80, 0xBF }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD");

            Decode_InvalidBytes(new byte[] { 176 }, 0, 1, "\uFFFD");
            Decode_InvalidBytes(new byte[] { 196 }, 0, 1, "\uFFFD");
        }
    }
}
