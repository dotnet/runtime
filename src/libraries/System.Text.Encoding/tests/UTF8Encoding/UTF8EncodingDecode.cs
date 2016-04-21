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

            // Surrogate pairs
            yield return new object[] { new byte[] { 240, 144, 128, 128 }, 0, 4, "\uD800\uDC00" };
            yield return new object[] { new byte[] { 97, 240, 144, 128, 128, 98 }, 0, 6, "a\uD800\uDC00b" };

            yield return new object[] { new byte[0], 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF8Encoding(), bytes, index, count, expected);
        }

        [Fact]
        public void Decode_InvalidUnicode()
        {
            // TODO: add into Decode_TestData once #7166 is fixed
            Decode(new byte[] { 239, 191, 189 }, 0, 3, "\uFFFD");
            Decode(new byte[] { 239, 191, 190 }, 0, 3, "\uFFFE");
            Decode(new byte[] { 239, 191, 191 }, 0, 3, "\uFFFF");

            // Invalid bytes
            byte[] validSurrogateBytes = new byte[] { 240, 144, 128, 128 };
            Decode(validSurrogateBytes, 0, 3, "\uFFFD");
            Decode(validSurrogateBytes, 1, 3, "\uFFFD\uFFFD\uFFFD");
            Decode(validSurrogateBytes, 0, 2, "\uFFFD");
            Decode(validSurrogateBytes, 1, 2, "\uFFFD\uFFFD");
            Decode(validSurrogateBytes, 2, 2, "\uFFFD\uFFFD");
            Decode(validSurrogateBytes, 2, 1, "\uFFFD");

            // These are examples of overlong sequences. This can cause security
            // vulnerabilities (e.g. MS00-078) so it is important we parse these as invalid.
            Decode(new byte[] { 0xC0, 0xAF }, 0, 2, "\uFFFD\uFFFD");
            Decode(new byte[] { 0xE0, 0x80, 0xBF }, 0, 3, "\uFFFD\uFFFD");
            Decode(new byte[] { 0xF0, 0x80, 0x80, 0xBF }, 0, 4, "\uFFFD\uFFFD\uFFFD");
            Decode(new byte[] { 0xF8, 0x80, 0x80, 0x80, 0xBF }, 0, 5, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD");
            Decode(new byte[] { 0xFC, 0x80, 0x80, 0x80, 0x80, 0xBF }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD");
        }
    }
}
