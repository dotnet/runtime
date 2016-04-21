// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingEncode
    {
        public static IEnumerable<object[]> Encode_TestData()
        {
            string testString = "Hello World123#?!";
            yield return new object[] { testString, 0, testString.Length };
            yield return new object[] { testString, 4, 5 };
                        
            yield return new object[] { "\u1234\u2345", 0, 2 };
            yield return new object[] { "a\u1234\u2345b", 0, 4 };

            yield return new object[] { "\uD800\uDC00", 0, 2 };
            yield return new object[] { "a\uD800\uDC00b", 0, 2 };

            yield return new object[] { string.Empty, 0, 0 };
        }
        
        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode(string source, int index, int count)
        {
            byte[] expectedBytes = new byte[count];
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (source[i] <= 0x7f)
                {
                    expectedBytes[i] = (byte)source[i + index];
                }
                else
                {
                    // Verify the fallback character for non-ASCII chars
                    expectedBytes[i] = (byte)'?';
                }
            }
            EncodingHelpers.Encode(new ASCIIEncoding(), source, index, count, expectedBytes);
        }

        [Fact]
        public void Encode_InvalidUnicode()
        {
            // TODO: move to Encode_TestData when #7166 is fixedEncode("\uD800", 0, 1, unicodeReplacementBytes1); // Lone high surrogate
            Encode("\uDC00", 0, 1); // Lone low surrogate
            Encode("\uD800\uDC00", 0, 1); // Surrogate pair out of range
            Encode("\uD800\uDC00", 1, 1); // Surrogate pair out of range

            Encode("\uD800\uD800", 0, 2); // High, high
            Encode("\uDC00\uD800", 0, 2); // Low, high
            Encode("\uDC00\uDC00", 0, 2); // Low, low

            // High BMP non-chars
            Encode("\uFFFD", 0, 1);
            Encode("\uFFFE", 0, 1);
            Encode("\uFFFF", 0, 1);
        }
    }
}
