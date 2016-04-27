// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (char c = char.MinValue; c <= 0xFF; c++)
            {
                yield return new object[] { new byte[] { (byte)c, 0, 0, 0 }, 0, 4, c.ToString() };
                yield return new object[] { new byte[] { 97, 0, 0, 0, (byte)c, 0, 0, 0, 98, 0, 0, 0 }, 4, 4, c.ToString() };
                yield return new object[] { new byte[] { 97, 0, 0, 0, (byte)c, 0, 0, 0, 98, 0, 0, 0 }, 0, 12, "a" + c.ToString() + "b" };
            }

            // Surrogate pairs
            yield return new object[] { new byte[] { 0, 0, 1, 0 }, 0, 4, "\uD800\uDC00" };
            yield return new object[] { new byte[] { 97, 0, 0, 0, 0, 0, 1, 0, 98, 0, 0, 0 }, 0, 12, "a\uD800\uDC00b"  };

            // Mixture of ASCII and Unciode
            yield return new object[] { new byte[] { 70, 0, 0, 0, 111, 0, 0, 0, 111, 0, 0, 0, 66, 0, 0, 0, 65, 0, 0, 0, 0, 4, 0, 0, 82, 0, 0, 0 }, 0, 28, "FooBA\u0400R" };

            // Empty strings
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode(byte[] littleEndianBytes, int index, int count, string expected)
        {
            byte[] bigEndianBytes = GetBigEndianBytes(littleEndianBytes);

            EncodingHelpers.Decode(new UTF32Encoding(true, true, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(true, false, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, true, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, false, false), littleEndianBytes, index, count, expected);

            EncodingHelpers.Decode(new UTF32Encoding(true, true, true), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(true, false, true), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, true, true), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, false, true), littleEndianBytes, index, count, expected);
        }

        public void Decode_InvalidChars(byte[] littleEndianBytes, int index, int count, string expected)
        {
            byte[] bigEndianBytes = GetBigEndianBytes(littleEndianBytes);

            EncodingHelpers.Decode(new UTF32Encoding(true, true, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(true, false, false), bigEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, true, false), littleEndianBytes, index, count, expected);
            EncodingHelpers.Decode(new UTF32Encoding(false, false, false), littleEndianBytes, index, count, expected);

            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(true, true, true), bigEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(true, false, true), bigEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(false, true, true), littleEndianBytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF32Encoding(false, false, true), littleEndianBytes, index, count);
        }

        [Fact]
        public void Decode_InvalidChars()
        {
            // TODO: add into Decode_TestData or Decode_InvalidChars_TestData once #7166 is fixed
            // High BMP non-chars
            Decode(new byte[] { 253, 255, 0, 0 }, 0, 4, "\uFFFD");
            Decode(new byte[] { 254, 255, 0, 0 }, 0, 4, "\uFFFE");
            Decode(new byte[] { 255, 255, 0, 0 }, 0, 4, "\uFFFF");

            // Invalid bytes
            Decode_InvalidChars(new byte[] { 123 }, 0, 1, "\uFFFD");
            Decode_InvalidChars(new byte[] { 123, 123 }, 0, 2, "\uFFFD");
            Decode_InvalidChars(new byte[] { 123, 123, 123 }, 0, 3, "\uFFFD");
            Decode_InvalidChars(new byte[] { 123, 123, 123, 123 }, 1, 3, "\uFFFD");
            Decode_InvalidChars(new byte[] { 97, 0, 0, 0, 0 }, 0, 5, "a\uFFFD");
        }

        public static byte[] GetBigEndianBytes(byte[] littleEndianBytes)
        {
            byte[] bigEndianBytes = (byte[])littleEndianBytes.Clone();
            for (int i = 0; i < littleEndianBytes.Length; i += 4)
            {
                if (i + 3 >= littleEndianBytes.Length)
                {
                    continue;
                }

                byte b1 = bigEndianBytes[i];
                byte b2 = bigEndianBytes[i + 1];
                byte b3 = bigEndianBytes[i + 2];
                byte b4 = bigEndianBytes[i + 3];

                bigEndianBytes[i] = b4;
                bigEndianBytes[i + 1] = b3;
                bigEndianBytes[i + 2] = b2;
                bigEndianBytes[i + 3] = b1;
            }
            return bigEndianBytes;
        }
    }
}
