// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (int i = 0; i <= 0x7F; i++)
            {
                byte b = (byte)i;
                yield return new object[] { new byte[] { b }, 0, 1 };
                yield return new object[] { new byte[] { 96, b, 97 }, 1, 1 };
                yield return new object[] { new byte[] { 96, b, 98 }, 2, 1 };
                yield return new object[] { new byte[] { 97, b, 97 }, 0, 3 };
            }
            
            // Empty strings
            yield return new object[] { new byte[0], 0, 0 };
            yield return new object[] { new byte[10], 5, 0 };
            yield return new object[] { new byte[10], 5, 5 };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count)
        {
            string expected = GetString(bytes, index, count);
            EncodingHelpers.Decode(new ASCIIEncoding(), bytes, index, count, expected);

            // Decoding valid bytes should not throw with a DecoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("?"), new DecoderExceptionFallback());
            EncodingHelpers.Decode(exceptionEncoding, bytes, index, count, expected);
        }

        public static IEnumerable<object[]> Decode_InvalidBytes_TestData()
        {
            // All Latin-1 Supplement bytes
            for (int i = 0x80; i <= byte.MaxValue; i++)
            {
                byte b = (byte)i;
                yield return new object[] { new byte[] { b }, 0, 1 };
                yield return new object[] { new byte[] { 96, b, 97 }, 1, 1 };
                yield return new object[] { new byte[] { 97, b, 97 }, 0, 3 };
            }

            yield return new object[] { new byte[] { 0xC1, 0x41, 0xF0, 0x42 }, 0, 4 };
        }

        [Theory]
        [MemberData(nameof(Decode_InvalidBytes_TestData))]
        public void Decode_InvalidBytes(byte[] bytes, int index, int count)
        {
            string expected = GetString(bytes, index, count);
            EncodingHelpers.Decode(new ASCIIEncoding(), bytes, index, count, expected);

            // Decoding invalid bytes should throw with a DecoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("ascii", new EncoderReplacementFallback("?"), new DecoderExceptionFallback());
            NegativeEncodingTests.Decode_Invalid(exceptionEncoding, bytes, index, count);
        }

        public static string GetString(byte[] bytes, int index, int count)
        {
            char[] chars = new char[count];
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i + index];
                chars[i] = b <= 0x7F ? (char)b : '?';
            }
            return new string(chars);
        }
    }
}
