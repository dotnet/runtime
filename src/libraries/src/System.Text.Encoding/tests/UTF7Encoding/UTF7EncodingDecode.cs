// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                char c = (char)i;
                if (c == 43)
                {
                    continue;
                }
                yield return new object[] { new byte[] { (byte)c }, 0, 1, c.ToString() };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 1, 1, c.ToString() };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 0, 3, "a" + c.ToString() + "b" };
            }

            yield return new object[] { new byte[] { (byte)'+' }, 0, 1, string.Empty };
            yield return new object[] { new byte[] { 43, 45 }, 0, 2, "+" };
            yield return new object[] { new byte[0], 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF7Encoding(true), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF7Encoding(false), bytes, index, count, expected);
        }

        [Fact]
        public void Decode_InvalidUnicode()
        {
            // TODO: add into Decode_TestData once #7166 is fixed
            Decode(new byte[] { 43, 50, 65, 65, 45 }, 0, 5, "\uD800"); // Lone high surrogate
            Decode(new byte[] { 43, 51, 65, 65, 45 }, 0, 5, "\uDC00"); // Lone low surrogate

            Decode(new byte[] { 43, 50, 65, 68, 89, 65, 65, 45 }, 0, 8, "\uD800\uD800"); // High, high
            Decode(new byte[] { 43, 51, 65, 68, 89, 65, 65, 45 }, 0, 8, "\uDC00\uD800"); // Low, high
            Decode(new byte[] { 43, 51, 65, 68, 99, 65, 65, 45 }, 0, 8, "\uDC00\uDC00"); // Low, low

            // High BMP non-chars
            Decode(new byte[] { 43, 47, 47, 48, 45 }, 0, 5, "\uFFFD");
            Decode(new byte[] { 43, 47, 47, 52, 45 }, 0, 5, "\uFFFE");
            Decode(new byte[] { 43, 47, 47, 56, 45 }, 0, 5, "\uFFFF");
        }
    }
}
