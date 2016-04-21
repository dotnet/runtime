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

            // Empty bytes
            yield return new object[] { string.Empty, 0, 0, new byte[0] };
            yield return new object[] { "a\u1234b", 3, 0, new byte[0] };
            yield return new object[] { "a\u1234b", 0, 0, new byte[0] };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void Encode(string source, int index, int count, byte[] expected)
        {
            EncodingHelpers.Encode(new UnicodeEncoding(), source, index, count, expected);
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

            // High BMP non-chars
            Encode("\uFFFD", 0, 1, unicodeReplacementBytes1);
            Encode("\uFFFE", 0, 1, new byte[] { 254, 255 });
            Encode("\uFFFF", 0, 1, new byte[] { 255, 255 });
        }
    }
}
