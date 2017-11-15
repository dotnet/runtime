// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public class DerSequenceReaderTests
    {
        [Fact]
        public static void ReadIntegers()
        {
            byte[] derEncoded =
            {
                /* SEQUENCE */     0x30, 23,
                /* INTEGER(0) */   0x02, 0x01, 0x00,
                /* INTEGER(256) */ 0x02, 0x02, 0x01, 0x00,
                /* INTEGER(-1) */  0x02, 0x01, 0xFF,
                /* Big integer */  0x02, 0x0B, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D,
            };

            DerSequenceReader reader = new DerSequenceReader(derEncoded);
            Assert.True(reader.HasData);
            Assert.Equal(23, reader.ContentLength);

            int first = reader.ReadInteger();
            Assert.Equal(0, first);

            int second = reader.ReadInteger();
            Assert.Equal(256, second);

            int third = reader.ReadInteger();
            Assert.Equal(-1, third);

            // Reader reads Big-Endian, BigInteger reads Little-Endian
            byte[] fourthBytes = reader.ReadIntegerBytes();
            Array.Reverse(fourthBytes);
            BigInteger fourth = new BigInteger(fourthBytes);
            Assert.Equal(BigInteger.Parse("3645759592820458633497613"), fourth);

            // And... done.
            Assert.False(reader.HasData);
        }

        [Fact]
        public static void ReadOids()
        {
            byte[] derEncoded =
            {
                // Noise
                0x10, 0x20, 0x30, 0x04, 0x05,

                // Data
                0x30, 34,
                0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B,
                0x06, 0x03, 0x55, 0x04, 0x03,
                0x06, 0x09, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x15, 0x07,
                0x06, 0x05, 0x2B, 0x81, 0x04, 0x00, 0x22,

                // More noise.
                0x85, 0x71, 0x23, 0x74, 0x01,
            };

            DerSequenceReader reader = new DerSequenceReader(derEncoded, 5, derEncoded.Length - 10);
            Assert.True(reader.HasData);
            Assert.Equal(34, reader.ContentLength);

            Oid first = reader.ReadOid();
            Assert.Equal("1.2.840.113549.1.1.11", first.Value);

            Oid second = reader.ReadOid();
            Assert.Equal("2.5.4.3", second.Value);

            Oid third = reader.ReadOid();
            Assert.Equal("1.3.6.1.4.1.311.21.7", third.Value);

            Oid fourth = reader.ReadOid();
            Assert.Equal("1.3.132.0.34", fourth.Value);

            // And... done.
            Assert.False(reader.HasData);
        }

        [Theory]
        [InlineData("Universal 31", "1F1F" + "0C" + "50323031375930324D313844")]
        [InlineData("Universal 32", "1F20" + "0A" + "5030304830304D303053")]
        [InlineData("Universal 127", "1F7F" + "01" + "00")]
        [InlineData("Universal 128", "1F8100" + "01" + "00")]
        [InlineData("Application 31", "5F1F" + "01" + "00")]
        [InlineData("Application 32", "5F20" + "01" + "00")]
        [InlineData("Application 127", "5F7F" + "01" + "00")]
        [InlineData("Application 128", "5F8100" + "01" + "00")]
        [InlineData("Context 31", "9F1F" + "01" + "00")]
        [InlineData("Context 32", "9F20" + "01" + "00")]
        [InlineData("Context 127", "9F7F" + "01" + "00")]
        [InlineData("Context 128", "9F8100" + "01" + "00")]
        [InlineData("Private 31", "DF1F" + "01" + "00")]
        [InlineData("Private 32", "DF20" + "01" + "00")]
        [InlineData("Private 127", "DF7F" + "01" + "00")]
        [InlineData("Private 128", "DF8100" + "01" + "00")]
        public static void NoSupportForMultiByteTags(string caseName, string hexInput)
        {
            byte[] bytes = hexInput.HexToByteArray();
            DerSequenceReader reader = DerSequenceReader.CreateForPayload(bytes);

            Assert.Throws<CryptographicException>(() => reader.PeekTag());
            Assert.Throws<CryptographicException>(() => reader.SkipValue());
            Assert.Throws<CryptographicException>(() => reader.ReadNextEncodedValue());
        }

        [Theory]
        [InlineData("0401")]
        [InlineData("0485")]
        [InlineData("048500000000")]
        [InlineData("04850000000000")]
        [InlineData("048480000000")]
        [InlineData("0484FFFFFFFF")]
        [InlineData("0484FFFFFFFA")]
        [InlineData("0485FF00000000")]
        public static void InvalidLengthSpecified(string hexInput)
        {
            byte[] bytes = hexInput.HexToByteArray();
            DerSequenceReader reader = DerSequenceReader.CreateForPayload(bytes);

            // Doesn't throw.
            reader.PeekTag();

            // Since EatTag will have succeeded the reader needs to be reconstructed after each test.
            Assert.Throws<CryptographicException>(() => reader.SkipValue());
            reader = DerSequenceReader.CreateForPayload(bytes);

            Assert.Throws<CryptographicException>(() => reader.ReadOctetString());
            reader = DerSequenceReader.CreateForPayload(bytes);

            Assert.Throws<CryptographicException>(() => reader.ReadNextEncodedValue());
        }

        [Fact]
        public static void InteriorLengthTooLong()
        {
            byte[] bytes =
            {
                // CONSTRUCTED SEQUENCE (8 bytes)
                0x30, 0x08,

                // CONSTRUCTED SEQUENCE (2 bytes)
                0x30, 0x02,

                // OCTET STRING (0 bytes)
                0x04, 0x00,

                // OCTET STRING (after the inner sequence, 3 bytes, but that exceeds the sequence bounds)
                0x04, 0x03, 0x01, 0x02, 0x03
            };

            DerSequenceReader reader = new DerSequenceReader(bytes);
            DerSequenceReader nested = reader.ReadSequence();
            Assert.Equal(0, nested.ReadOctetString().Length);
            Assert.False(nested.HasData);
            Assert.Throws<CryptographicException>(() => reader.ReadOctetString());
        }

        [Fact]
        public static void InteriorLengthTooLong_Nested()
        {
            byte[] bytes =
            {
                // CONSTRUCTED SEQUENCE (9 bytes)
                0x30, 0x09,

                // CONSTRUCTED SEQUENCE (2 bytes)
                0x30, 0x02,

                // OCTET STRING (1 byte, but 0 remain for the inner sequence)
                0x04, 0x01,

                // OCTET STRING (in the outer sequence, after the inner sequence, 3 bytes)
                0x04, 0x03, 0x01, 0x02, 0x03
            };

            DerSequenceReader reader = new DerSequenceReader(bytes);
            DerSequenceReader nested = reader.ReadSequence();
            Assert.Throws<CryptographicException>(() => nested.ReadOctetString());
        }

        [Fact]
        public static void LengthTooLong_ForBounds()
        {
            byte[] bytes =
            {
                // CONSTRUCTED SEQUENCE (9 bytes)
                0x30, 0x09,

                // CONSTRUCTED SEQUENCE (2 bytes)
                0x30, 0x02,

                // OCTET STRING (0 bytes)
                0x04, 0x00,

                // OCTET STRING (after the inner sequence, 3 bytes)
                0x04, 0x03, 0x01, 0x02, 0x03
            };

            Assert.Throws<CryptographicException>(() => new DerSequenceReader(bytes, 0, bytes.Length - 1));
        }
    }
}
