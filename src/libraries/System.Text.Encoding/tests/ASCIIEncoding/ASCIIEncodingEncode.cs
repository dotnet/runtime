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
            // All ASCII chars
            for (int i = 0; i <= 0x7F; i++)
            {
                char b = (char)i;
                yield return new object[] { b, 0, 1 };
                yield return new object[] { "a" + b + "c", 1, 1 };
                yield return new object[] { "a" + b + "c", 2, 1 };
                yield return new object[] { "a" + b + "c", 0, 3 };
            }

            string testString = "Hello World123#?!";
            yield return new object[] { testString, 0, testString.Length };
            yield return new object[] { testString, 4, 5 };

            yield return new object[] { "ABCDEFGH", 0, 8 };

            // Empty strings
            yield return new object[] { string.Empty, 0, 0 };
            yield return new object[] { "abc", 3, 0 };
            yield return new object[] { "abc", 0, 0 };
        }
        
        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode(string source, int index, int count)
        {
            byte[] expected = GetBytes(source, index, count);
            EncodingHelpers.Encode(new ASCIIEncoding(), source, index, count, expected);

            // Encoding valid chars should not throw with an EncoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderExceptionFallback(), new DecoderReplacementFallback("?"));
            EncodingHelpers.Encode(exceptionEncoding, source, index, count, expected);
        }

        public static IEnumerable<object[]> Encode_InvalidChars_TestData()
        {
            // All non-ASCII Latin1 chars
            for (int i = 0x80; i <= 0xFF; i++)
            {
                char b = (char)i;
                yield return new object[] { b, 0, 1 };
            }
            
            // Unicode chars
            yield return new object[] { "\u1234\u2345", 0, 2 };
            yield return new object[] { "a\u1234\u2345b", 0, 4 };

            yield return new object[] { "\uD800\uDC00", 0, 2 };
            yield return new object[] { "a\uD800\uDC00b", 0, 2 };

            yield return new object[] { "\uD800\uDC00\u0061\u0CFF", 0, 4 };

            // Invalid Unicode
            yield return new object[] { "\uD800", 0, 1 }; // Lone high surrogate
            yield return new object[] { "\uDC00", 0, 1 }; // Lone low surrogate
            yield return new object[] { "\uD800\uDC00", 0, 1 }; // Surrogate pair out of range
            yield return new object[] { "\uD800\uDC00", 1, 1 }; // Surrogate pair out of range

            yield return new object[] { "\uD800\uD800", 0, 2 }; // High, high
            yield return new object[] { "\uDC00\uD800", 0, 2 }; // Low, high
            yield return new object[] { "\uDC00\uDC00", 0, 2 }; // Low, low

            yield return new object[] { "\u0080\u00FF\u0B71\uFFFF\uD800\uDFFF", 0, 6 };

            // High BMP non-chars
            yield return new object[] { "\uFFFD", 0, 1 };
            yield return new object[] { "\uFFFE", 0, 1 };
            yield return new object[] { "\uFFFF", 0, 1 };
        }

        [Theory]
        [MemberData(nameof(Encode_InvalidChars_TestData))]
        public void Encode_InvalidChars(string source, int index, int count)
        {
            byte[] expected = GetBytes(source, index, count);
            EncodingHelpers.Encode(new ASCIIEncoding(), source, index, count, expected);

            // Encoding invalid chars should throw with an EncoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderExceptionFallback(), new DecoderReplacementFallback("?"));
            NegativeEncodingTests.Encode_Invalid(exceptionEncoding, source, index, count);
        }

        private static byte[] GetBytes(string source, int index, int count)
        {
            byte[] bytes = new byte[count];
            for (int i = 0; i < bytes.Length; i++)
            {
                if (source[i] <= 0x7f)
                {
                    bytes[i] = (byte)source[i + index];
                }
                else
                {
                    // Verify the fallback character for non-ASCII chars
                    bytes[i] = (byte)'?';
                }
            }
            return bytes;
        }
    }
}
