// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingDecode
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

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

            // Empty string
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
            yield return new object[] { unicodeBytes, 6, 0, string.Empty };
            yield return new object[] { unicodeBytes, 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UnicodeEncoding(), bytes, index, count, expected);
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
            Decode(validSurrogateBytes, 0, 3, "\uFFFD\uFFFD");
            Decode(validSurrogateBytes, 1, 3, "\u00D8\uFFFD");
            Decode(validSurrogateBytes, 0, 2, "\uFFFD");
            Decode(validSurrogateBytes, 1, 2, "\u00D8");
            Decode(validSurrogateBytes, 2, 2, "\uFFFD");
            Decode(validSurrogateBytes, 2, 1, "\uFFFD");

            Decode(new byte[] { 97 }, 0, 1, "\uFFFD");
            Decode(new byte[] { 97, 0, 97 }, 0, 3, "a\uFFFD");
        }
    }
}
