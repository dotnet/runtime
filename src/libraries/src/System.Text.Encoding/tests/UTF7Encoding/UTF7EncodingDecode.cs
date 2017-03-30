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
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 2, 1, "b" };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 0, 3, "a" + c.ToString() + "b" };
            }

            // Plus
            yield return new object[] { new byte[] { (byte)'+' }, 0, 1, string.Empty };
            yield return new object[] { new byte[] { 43, 45 }, 0, 2, "+" };
            yield return new object[] { new byte[] { 43, 45, 65 }, 0, 3, "+A" };
            yield return new object[] { new byte[] { 0x2B, 0x2D, 0x2D }, 0, 3,"+-" };

            // UTF7 code points can be represented in different sequences of bytes
            yield return new object[] { new byte[] { 0x41, 0x09, 0x0D, 0x0A, 0x20, 0x2F, 0x7A }, 0, 7, "A\t\r\n /z" };

            yield return new object[] { new byte[] { 0x2B, 0x41, 0x45, 0x45, 0x41, 0x43, 0x51 }, 0, 7, "A\t" };
            yield return new object[] { new byte[] { 0x2B, 0x09 }, 0, 2, "\t" };
            yield return new object[] { new byte[] { 0x2B, 0x09, 0x2D }, 0, 3, "\t-" };

            yield return new object[] { new byte[] { 0x2B, 0x1E, 0x2D }, 0, 3, "\u001E-" };
            yield return new object[] { new byte[] { 0x2B, 0x7F, 0x1E, 0x2D }, 0, 4, "\u007F\u001E-" };
            yield return new object[] { new byte[] { 0x1E }, 0, 1, "\u001e" };

            yield return new object[] { new byte[] { 0x21 }, 0, 1, "!" };
            yield return new object[] { new byte[] { 0x2B, 0x21, 0x2D }, 0, 3, "!-" };
            yield return new object[] { new byte[] { 0x2B, 0x21, 0x41, 0x41, 0x2D }, 0, 5, "!AA-" };

            yield return new object[] { new byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 5, "\u0080\u0081\u0082-" };
            yield return new object[] { new byte[] { 0x2B, 0x80, 0x81, 0x82, 0x2D }, 0, 4, "\u0080\u0081\u0082" };
            yield return new object[] { new byte[] { 0x80, 0x81 }, 0, 2, "\u0080\u0081" };
            yield return new object[] { new byte[] { 0x2B, 0x80, 0x21, 0x80, 0x21, 0x1E, 0x2D }, 0, 7, "\u0080!\u0080!-" };
            
            // Exclamation mark
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51 }, 0, 7, "!}" };
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51,   0x2D }, 0, 8, "!}" };
            yield return new object[] { new byte[] { 0x21, 0x7D }, 0, 2, "!}" };
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D }, 1, 2, "AC" };

            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 5, "!" };
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 2, string.Empty };
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x45, 0x2D }, 0, 3, string.Empty };

            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x48, 0x2D }, 0, 5, "!" };

            // Unicode
            yield return new object[] { new byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 8, "\u0E59\u05D1" };
            yield return new object[] { new byte[] { 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 7, "\u0E59\u05D1" };

            yield return new object[] { new byte[] { 0x41, 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 17, "\u0041\u0021\u007D\u0009\u0E59\u05D1" };
            yield return new object[] { new byte[] { 0x41, 0x2B, 0x41, 0x43, 0x45, 0x41, 0x66, 0x51, 0x2D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 18, "\u0041\u0021\u007D\u0009\u0E59\u05D1" };

            yield return new object[] { new byte[] { 0x41, 0x21, 0x7D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51, 0x2D }, 0, 12, "\u0041\u0021\u007D\u0009\u0E59\u05D1" };
            yield return new object[] { new byte[] { 0x41, 0x21, 0x7D, 0x09, 0x2B, 0x44, 0x6C, 0x6B, 0x46, 0x30, 0x51 }, 0, 11, "\u0041\u0021\u007D\u0009\u0E59\u05D1" };

            yield return new object[] { new byte[] { 0x2B, 0x2B, 0x41, 0x41, 0x2D }, 0, 5, "\uF800" };
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x48, 0x35, 0x41, 0x41, 0x2D }, 0, 8, "\u0021\uF900" };
            yield return new object[] { new byte[] { 0x2B, 0x41, 0x43, 0x48, 0x35, 0x41, 0x41, 0x2D }, 0, 4, "!" };

            // Surrogate pairs
            yield return new object[] { new byte[] { 0x2B, 0x32, 0x41, 0x44, 0x66, 0x2F, 0x77, 0x2D }, 0, 8, "\uD800\uDFFF" };

            // Invalid Unicode
            yield return new object[] { new byte[] { 43, 50, 65, 65, 45 }, 0, 5, "\uD800" }; // Lone high surrogate
            yield return new object[] { new byte[] { 43, 51, 65, 65, 45 }, 0, 5, "\uDC00" }; // Lone low surrogate
            yield return new object[] { new byte[] { 0x2B, 0x33, 0x2F, 0x38, 0x2D }, 0, 5, "\uDFFF" }; // Lone low surrogate

            yield return new object[] { new byte[] { 43, 50, 65, 68, 89, 65, 65, 45 }, 0, 8, "\uD800\uD800" }; // High, high
            yield return new object[] { new byte[] { 43, 51, 65, 68, 89, 65, 65, 45 }, 0, 8, "\uDC00\uD800" }; // Low, high
            yield return new object[] { new byte[] { 43, 51, 65, 68, 99, 65, 65, 45 }, 0, 8, "\uDC00\uDC00" }; // Low, low

            // High BMP non-chars
            yield return new object[] { new byte[] { 43, 47, 47, 48, 45 }, 0, 5, "\uFFFD" };
            yield return new object[] { new byte[] { 43, 47, 47, 52, 45 }, 0, 5, "\uFFFE" };
            yield return new object[] { new byte[] { 43, 47, 47, 56, 45 }, 0, 5, "\uFFFF" };

            // Empty strings
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF7Encoding(true), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF7Encoding(false), bytes, index, count, expected);
        }
    }
}
