// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class Latin1EncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All Latin1 chars
            for (int i = 0; i <= 0xFF; i++)
            {
                byte b = (byte)i;
                yield return new object[] { new byte[] { b }, 0, 1 };
                yield return new object[] { new byte[] { 96, b, 97 }, 1, 1 };
                yield return new object[] { new byte[] { 97, b, 97 }, 0, 3 };
            }

            yield return new object[] { new byte[] { 0x01, 0x09, 0x10, 0x3F, 0x5C, 0x9F, 0xCB, 0xE7, 0xFF }, 0, 9 };
            yield return new object[] { new byte[] { 0x60, 0x7E, 0xE3 }, 0, 3 };

            // Empty string
            yield return new object[] { new byte[0], 0, 0 };
            yield return new object[] { new byte[10], 5, 0 };
            yield return new object[] { new byte[10], 5, 5 };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] bytes, int index, int count)
        {
            string expected = GetString(bytes, index, count);
            EncodingHelpers.Decode(Encoding.GetEncoding("latin1"), bytes, index, count, expected);

            // Decoding valid bytes should not throw with a DecoderExceptionFallback
            Encoding exceptionEncoding = Encoding.GetEncoding("latin1", new EncoderReplacementFallback("?"), new DecoderExceptionFallback());
            EncodingHelpers.Decode(exceptionEncoding, bytes, index, count, expected);
        }

        public static string GetString(byte[] bytes, int index, int count)
        {
            char[] chars = new char[count];
            for (int i = 0; i < count; i++)
            {
                chars[i] = (char)bytes[i + index];
            }
            return new string(chars);
        }
    }
}
