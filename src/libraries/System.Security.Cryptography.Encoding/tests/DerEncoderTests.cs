// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class DerEncoderTests
    {
        [Theory]
        [InlineData("00", 0x02, "01", "00")]
        [InlineData("01", 0x02, "01", "01")]
        [InlineData("7F", 0x02, "01", "7F")]
        [InlineData("80", 0x02, "02", "0080")]
        [InlineData("FF", 0x02, "02", "00FF")]
        [InlineData("00FF", 0x02, "02", "00FF")]
        [InlineData("01FF", 0x02, "02", "01FF")]
        [InlineData("7FFF", 0x02, "02", "7FFF")]
        [InlineData("8000", 0x02, "03", "008000")]
        [InlineData("FFFF", 0x02, "03", "00FFFF")]
        [InlineData("00FFFF", 0x02, "03", "00FFFF")]
        [InlineData("7FFFFF", 0x02, "03", "7FFFFF")]
        [InlineData("800000", 0x02, "04", "00800000")]
        [InlineData("FFFFFF", 0x02, "04", "00FFFFFF")]
        [InlineData("00FFFFFF", 0x02, "04", "00FFFFFF")]
        [InlineData("7FFFFFFF", 0x02, "04", "7FFFFFFF")]
        [InlineData("80000000", 0x02, "05", "0080000000")]
        [InlineData("FFFFFFFF", 0x02, "05", "00FFFFFFFF")]
        [InlineData("0123456789ABCDEF", 0x02, "08", "0123456789ABCDEF")]
        [InlineData("FEDCBA9876543210", 0x02, "09", "00FEDCBA9876543210")]
        public static void ValidateUintEncodings(string hexRaw, byte tag, string hexLength, string hexValue)
        {
            byte[] raw = hexRaw.HexToByteArray();
            byte[] length = hexLength.HexToByteArray();
            byte[] value = hexValue.HexToByteArray();

            byte[][] segments = DerEncoder.SegmentedEncodeUnsignedInteger(raw);

            Assert.Equal(3, segments.Length);

            Assert.Equal(new[] { tag }, segments[0]);
            Assert.Equal(length, segments[1]);
            Assert.Equal(value, segments[2]);
        }

        [Theory]
        [InlineData("", 0, "01", "00")]
        [InlineData("00", 0, "02", "0000")]
        [InlineData("00", 7, "02", "0700")]
        [InlineData("0000", 0, "03", "000000")]
        [InlineData("007F", 7, "03", "070000")]
        [InlineData("007F", 6, "03", "060040")]
        [InlineData("007F", 5, "03", "050060")]
        [InlineData("007F", 4, "03", "040070")]
        [InlineData("007F", 3, "03", "030078")]
        [InlineData("007F", 2, "03", "02007C")]
        [InlineData("007F", 1, "03", "01007E")]
        [InlineData("007F", 0, "03", "00007F")]
        public static void ValidateBitStringEncodings(string hexRaw, int unusedBits, string hexLength, string encodedData)
        {
            byte[] input = hexRaw.HexToByteArray();
            const byte tag = 0x03;
            byte[] length = hexLength.HexToByteArray();
            byte[] expectedOutput = encodedData.HexToByteArray();

            byte[][] segments = DerEncoder.SegmentedEncodeBitString(unusedBits, input);

            Assert.Equal(3, segments.Length);

            Assert.Equal(new[] { tag }, segments[0]);
            Assert.Equal(length, segments[1]);
            Assert.Equal(expectedOutput, segments[2]);
        }

        [Theory]
        [InlineData("", 9, "01", "00")]
        [InlineData("00", 9, "01", "00")]
        [InlineData("0000", 9, "01", "00")]
        [InlineData("007F", 9, "01", "00")]
        [InlineData("8000", 3, "02", "0780")]
        [InlineData("8FF0", 3, "02", "0780")]
        [InlineData("8FF0", 7, "02", "018E")]
        [InlineData("8FF0", 8, "02", "008F")]
        [InlineData("8FF0", 9, "03", "078F80")]
        public static void ValidateNamedBitEncodings(string hexRaw, int namedBits, string hexLength, string encodedData)
        {
            byte[] input = hexRaw.HexToByteArray();
            const byte tag = 0x03;
            byte[] length = hexLength.HexToByteArray();
            byte[] expectedOutput = encodedData.HexToByteArray();

            byte[][] segments = DerEncoder.SegmentedEncodeNamedBitList(input, namedBits);

            Assert.Equal(3, segments.Length);
            
            Assert.Equal(new[] { tag }, segments[0]);
            Assert.Equal(length, segments[1]);
            Assert.Equal(expectedOutput, segments[2]);
        }

        [Theory]
        [InlineData("010203040506070809", "09")]
        [InlineData("", "00")]
        public static void ValidateOctetStringEncodings(string hexData, string hexLength)
        {
            byte[] input = hexData.HexToByteArray();
            const byte tag = 0x04;
            byte[] length = hexLength.HexToByteArray();

            byte[][] segments = DerEncoder.SegmentedEncodeOctetString(input);

            Assert.Equal(3, segments.Length);

            Assert.Equal(new[] { tag }, segments[0]);
            Assert.Equal(length, segments[1]);
            Assert.Equal(input, segments[2]);
        }

        [Theory]
        [InlineData("1.3.6.1.5.5.7.3.1", "08", "2B06010505070301")]
        [InlineData("1.3.6.1.5.5.7.3.2", "08", "2B06010505070302")]
        [InlineData("1.3.132.0.34", "05", "2B81040022")]
        [InlineData("2.999.3", "03", "883703")]
        [InlineData("2.999.19427512891.25", "08", "8837C8AFE1A43B19")]
        public static void ValidateOidEncodings(string oidValue, string hexLength, string encodedData)
        {
            Oid oid = new Oid(oidValue, oidValue);
            const byte tag = 0x06;
            byte[] length = hexLength.HexToByteArray();
            byte[] expectedOutput = encodedData.HexToByteArray();

            byte[][] segments = DerEncoder.SegmentedEncodeOid(oid);

            Assert.Equal(3, segments.Length);

            Assert.Equal(new[] { tag }, segments[0]);
            Assert.Equal(length, segments[1]);
            Assert.Equal(expectedOutput, segments[2]);
        }

        [Fact]
        public static void ConstructSequence()
        {
            byte[] expected =
            {
                /* SEQUENCE */     0x30, 0x07,
                /* INTEGER(0) */   0x02, 0x01, 0x00,
                /* INTEGER(256) */ 0x02, 0x02, 0x01, 0x00,
            };

            byte[] encoded = DerEncoder.ConstructSequence(
                DerEncoder.SegmentedEncodeUnsignedInteger(new byte[] { 0x00 }),
                DerEncoder.SegmentedEncodeUnsignedInteger(new byte[] { 0x01, 0x00 }));

            Assert.Equal(expected, encoded);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("This is a PrintableString.", true)]
        [InlineData("This is not @ PrintableString.", false)]
        [InlineData("\u65E5\u672C\u8A9E is not a PrintableString", false)]
        public static void CheckPrintableString(string candidate, bool expected)
        {
            Assert.Equal(expected, DerEncoder.IsValidPrintableString(candidate.ToCharArray()));
        }

        // No bounds check tests are done here, because the method is currently assert-based,
        // not exception-based.
        [Theory]
        [InlineData("", 0, 0, true)]
        [InlineData("This is a PrintableString.", 0, 26, true)]
        [InlineData("This is not @ PrintableString.", 0, 30, false)]
        [InlineData("This is not @ PrintableString.", 0, 12, true)]
        [InlineData("This is not @ PrintableString.", 12, 0, true)]
        [InlineData("This is not @ PrintableString.", 12, 1, false)]
        [InlineData("This is not @ PrintableString.", 13, 17, true)]
        [InlineData("\u65E5\u672C\u8A9E is not a PrintableString", 0, 27, false)]
        [InlineData("\u65E5\u672C\u8A9E is not a PrintableString", 0, 0, true)]
        [InlineData("\u65E5\u672C\u8A9E is not a PrintableString", 3, 24, true)]
        public static void CheckPrintableSubstring(string candidate, int offset, int length, bool expected)
        {
            Assert.Equal(expected, DerEncoder.IsValidPrintableString(candidate.ToCharArray(), offset, length));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Hello", "48656C6C6F")]
        [InlineData("banana", "62616E616E61")]
        public static void CheckIA5StringEncoding(string input, string expectedHex)
        {
            byte[][] encodedString = DerEncoder.SegmentedEncodeIA5String(input.ToCharArray());

            Assert.NotNull(encodedString);
            Assert.Equal(3, encodedString.Length);

            // Check the tag
            Assert.NotNull(encodedString[0]);
            Assert.Equal(1, encodedString[0].Length);
            Assert.Equal(0x16, encodedString[0][0]);

            // Check the length. Since the input cases are all less than 0x7F bytes
            // the length is only one byte.
            Assert.NotNull(encodedString[1]);
            Assert.Equal(1, encodedString[1].Length);
            Assert.Equal(expectedHex.Length / 2, encodedString[1][0]);

            // Check the value
            Assert.Equal(expectedHex.HexToByteArray(), encodedString[2]);

            // And, full roundtrip
            Assert.Equal(input, Text.Encoding.ASCII.GetString(encodedString[2]));
        }

        [Theory]
        [InlineData("Hello", 3, 2, "6C6F")]
        [InlineData("banana", 1, 4, "616E616E")]
        public static void CheckIA5SubstringEncoding(string input, int offset, int length, string expectedHex)
        {
            byte[][] encodedString = DerEncoder.SegmentedEncodeIA5String(input.ToCharArray(), offset, length);

            Assert.NotNull(encodedString);
            Assert.Equal(3, encodedString.Length);

            // Check the tag
            Assert.NotNull(encodedString[0]);
            Assert.Equal(1, encodedString[0].Length);
            Assert.Equal(0x16, encodedString[0][0]);

            // Check the length. Since the input cases are all less than 0x7F bytes
            // the length is only one byte.
            Assert.NotNull(encodedString[1]);
            Assert.Equal(1, encodedString[1].Length);
            Assert.Equal(expectedHex.Length / 2, encodedString[1][0]);

            // Check the value
            Assert.Equal(expectedHex.HexToByteArray(), encodedString[2]);

            // And, full roundtrip
            Assert.Equal(input.Substring(offset, length), Text.Encoding.ASCII.GetString(encodedString[2]));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Hello", "48656C6C6F")]
        [InlineData("banana", "62616E616E61")]
        public static void CheckPrintableStringEncoding(string input, string expectedHex)
        {
            byte[][] encodedString = DerEncoder.SegmentedEncodePrintableString(input.ToCharArray());

            Assert.NotNull(encodedString);
            Assert.Equal(3, encodedString.Length);

            // Check the tag
            Assert.NotNull(encodedString[0]);
            Assert.Equal(1, encodedString[0].Length);
            Assert.Equal(0x13, encodedString[0][0]);

            // Check the length. Since the input cases are all less than 0x7F bytes
            // the length is only one byte.
            Assert.NotNull(encodedString[1]);
            Assert.Equal(1, encodedString[1].Length);
            Assert.Equal(expectedHex.Length / 2, encodedString[1][0]);

            // Check the value
            Assert.Equal(expectedHex.HexToByteArray(), encodedString[2]);

            // And, full roundtrip
            Assert.Equal(input, Text.Encoding.ASCII.GetString(encodedString[2]));
        }

        [Theory]
        [InlineData("Hello", 3, 2, "6C6F")]
        [InlineData("banana", 1, 4, "616E616E")]
        public static void CheckPrintableSubstringEncoding(string input, int offset, int length, string expectedHex)
        {
            byte[][] encodedString = DerEncoder.SegmentedEncodePrintableString(input.ToCharArray(), offset, length);

            Assert.NotNull(encodedString);
            Assert.Equal(3, encodedString.Length);

            // Check the tag
            Assert.NotNull(encodedString[0]);
            Assert.Equal(1, encodedString[0].Length);
            Assert.Equal(0x13, encodedString[0][0]);

            // Check the length. Since the input cases are all less than 0x7F bytes
            // the length is only one byte.
            Assert.NotNull(encodedString[1]);
            Assert.Equal(1, encodedString[1].Length);
            Assert.Equal(expectedHex.Length / 2, encodedString[1][0]);

            // Check the value
            Assert.Equal(expectedHex.HexToByteArray(), encodedString[2]);

            // And, full roundtrip
            Assert.Equal(input.Substring(offset, length), Text.Encoding.ASCII.GetString(encodedString[2]));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Hello", "48656C6C6F")]
        [InlineData("banana", "62616E616E61")]
        [InlineData("\u65E5\u672C\u8A9E", "E697A5E69CACE8AA9E")]
        public static void CheckUTF8StringEncoding(string input, string expectedHex)
        {
            byte[][] encodedString = DerEncoder.SegmentedEncodeUtf8String(input.ToCharArray());

            Assert.NotNull(encodedString);
            Assert.Equal(3, encodedString.Length);

            // Check the tag
            Assert.NotNull(encodedString[0]);
            Assert.Equal(1, encodedString[0].Length);
            Assert.Equal(0x0C, encodedString[0][0]);

            // Check the length. Since the input cases are all less than 0x7F bytes
            // the length is only one byte.
            Assert.NotNull(encodedString[1]);
            Assert.Equal(1, encodedString[1].Length);
            Assert.Equal(expectedHex.Length / 2, encodedString[1][0]);

            // Check the value
            Assert.Equal(expectedHex.HexToByteArray(), encodedString[2]);

            // And, full roundtrip
            Assert.Equal(input, Text.Encoding.UTF8.GetString(encodedString[2]));
        }

        [Theory]
        [InlineData("Hello", 3, 2, "6C6F")]
        [InlineData("banana", 1, 4, "616E616E")]
        [InlineData("\u65E5\u672C\u8A9E", 1, 1, "E69CAC")]
        public static void CheckUTF8SubstringEncoding(string input, int offset, int length, string expectedHex)
        {
            byte[][] encodedString = DerEncoder.SegmentedEncodeUtf8String(input.ToCharArray(), offset, length);
            Assert.NotNull(encodedString);
            Assert.Equal(3, encodedString.Length);

            // Check the tag
            Assert.NotNull(encodedString[0]);
            Assert.Equal(1, encodedString[0].Length);
            Assert.Equal(0x0C, encodedString[0][0]);

            // Check the length. Since the input cases are all less than 0x7F bytes
            // the length is only one byte.
            Assert.NotNull(encodedString[1]);
            Assert.Equal(1, encodedString[1].Length);
            Assert.Equal(expectedHex.Length / 2, encodedString[1][0]);

            // Check the value
            Assert.Equal(expectedHex.HexToByteArray(), encodedString[2]);

            // And, full roundtrip
            Assert.Equal(input.Substring(offset, length), Text.Encoding.UTF8.GetString(encodedString[2]));
        }
    }
}
