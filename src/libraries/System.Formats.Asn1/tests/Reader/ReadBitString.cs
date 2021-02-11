// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public static class ReadBitString
    {
        [Theory]
        [InlineData("Uncleared unused bit", AsnEncodingRules.BER, "030201FF")]
        [InlineData("Constructed Payload", AsnEncodingRules.BER, "2302030100")]
        [InlineData("Constructed Payload-Indefinite", AsnEncodingRules.BER, "238003010000")]
        // This value is actually invalid CER, but it returns false since it's not primitive and
        // it isn't worth preempting the descent to find out it was invalid.
        [InlineData("Constructed Payload-Indefinite", AsnEncodingRules.CER, "238003010000")]
        public static void TryReadPrimitiveBitStringValue_Fails(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadPrimitiveBitString(
                out int unusedBitCount,
                out ReadOnlyMemory<byte> contents);

            Assert.False(didRead, "reader.TryReadBitStringBytes");
            Assert.Equal(0, unusedBitCount);
            Assert.Equal(0, contents.Length);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, 0, "030100")]
        [InlineData(AsnEncodingRules.BER, 1, 1, "030201FE")]
        [InlineData(AsnEncodingRules.CER, 2, 4, "030502FEEFF00C")]
        [InlineData(AsnEncodingRules.DER, 7, 1, "03020780")]
        [InlineData(AsnEncodingRules.DER, 0, 4, "030500FEEFF00D" + "0500")]
        public static void TryReadPrimitiveBitStringValue_Success(
            AsnEncodingRules ruleSet,
            int expectedUnusedBitCount,
            int expectedLength,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadPrimitiveBitString(
                out int unusedBitCount,
                out ReadOnlyMemory<byte> contents);

            Assert.True(didRead, "reader.TryReadBitStringBytes");
            Assert.Equal(expectedUnusedBitCount, unusedBitCount);
            Assert.Equal(expectedLength, contents.Length);
        }

        [Theory]
        [InlineData("Wrong Tag", AsnEncodingRules.BER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.CER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.DER, "0500")]
        [InlineData("Zero Length", AsnEncodingRules.BER, "0300")]
        [InlineData("Zero Length", AsnEncodingRules.CER, "0300")]
        [InlineData("Zero Length", AsnEncodingRules.DER, "0300")]
        [InlineData("Bad Length", AsnEncodingRules.BER, "030200")]
        [InlineData("Bad Length", AsnEncodingRules.CER, "030200")]
        [InlineData("Bad Length", AsnEncodingRules.DER, "030200")]
        [InlineData("Constructed Form", AsnEncodingRules.DER, "2303030100")]
        public static void TryReadPrimitiveBitStringValue_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.TryReadPrimitiveBitString(
                        out int unusedBitCount,
                        out ReadOnlyMemory<byte> contents);
                });

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.TryReadBitString(
                        new byte[inputData.Length],
                        out int unusedBitCount,
                        out int written);
                });

            Assert.Throws<AsnContentException>(
                () => reader.ReadBitString(out int unusedBitCount));
        }

        [Fact]
        public static void TryReadPrimitiveBitStringValue_Throws_CER_TooLong()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte).
            //
            // So we need 03 [1001] { 1001 0x00s }
            // 1001 => 0x3E9, so the length encoding is 82 03 E9.
            // 1001 + 3 + 1 == 1005
            byte[] input = new byte[1005];
            input[0] = 0x03;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE9;

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.TryReadPrimitiveBitString(
                        out int unusedBitCount,
                        out ReadOnlyMemory<byte> contents);
                });

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.TryReadBitString(
                        new byte[input.Length],
                        out int unusedBitCount,
                        out int written);
                });

            Assert.Throws<AsnContentException>(
                () => reader.ReadBitString(out int unusedBitCount));
        }

        [Fact]
        public static void TryReadPrimitiveBitStringValue_Success_CER_MaxLength()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte).
            //
            // So we need 03 [1000] [0x00-0x07] { 998 anythings } [a byte that's legal for the bitmask]
            // 1000 => 0x3E8, so the length encoding is 82 03 E8.
            // 1000 + 3 + 1 == 1004
            byte[] input = new byte[1004];
            input[0] = 0x03;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE8;

            // Unused bits
            input[4] = 0x02;

            // Payload
            input[5] = 0xA0;
            input[1002] = 0xA5;
            input[1003] = 0xFC;

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryReadPrimitiveBitString(
                out int unusedBitCount,
                out ReadOnlyMemory<byte> contents);

            Assert.True(success, "reader.TryReadBitStringBytes");
            Assert.Equal(input[4], unusedBitCount);
            Assert.Equal(999, contents.Length);

            // Check that it is, in fact, the same memory. No copies with this API.
            Assert.True(
                Unsafe.AreSame(
                    ref MemoryMarshal.GetReference(contents.Span),
                    ref input[5]));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "03020780")]
        [InlineData(AsnEncodingRules.BER, "030207FF")]
        [InlineData(AsnEncodingRules.CER, "03020780")]
        [InlineData(AsnEncodingRules.DER, "03020780")]
        [InlineData(
            AsnEncodingRules.BER,
            "2380" +
              "2380" +
                "0000" +
              "03020000" +
              "0000")]
        public static void TryCopyBitStringBytes_Fails(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadBitString(
                Span<byte>.Empty,
                out int unusedBitCount,
                out int bytesWritten);

            Assert.False(didRead, "reader.TryReadBitString");
            Assert.Equal(0, unusedBitCount);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "03020780", "80", 7)]
        [InlineData(AsnEncodingRules.BER, "030207FF", "80", 7)]
        [InlineData(AsnEncodingRules.CER, "03020780", "80", 7)]
        [InlineData(AsnEncodingRules.DER, "03020680", "80", 6)]
        [InlineData(AsnEncodingRules.BER, "23800000", "", 0)]
        [InlineData(AsnEncodingRules.BER, "2300", "", 0)]
        [InlineData(AsnEncodingRules.BER, "2300" + "0500", "", 0)]
        [InlineData(AsnEncodingRules.BER, "0303010203" + "0500", "0202", 1)]
        [InlineData(
            AsnEncodingRules.BER,
            "2380" +
              "2380" +
                "0000" +
              "03020000" +
              "0000",
            "00",
            0)]
        [InlineData(
            AsnEncodingRules.BER,
            "230C" +
              "2380" +
                "2380" +
                  "0000" +
                "03020000" +
                "0000",
            "00",
            0)]
        [InlineData(
            AsnEncodingRules.BER,
            "2380" +
              "2308" +
                "030200FA" +
                "030200CE" +
              "2380" +
                "2380" +
                  "2380" +
                    "030300F00D" +
                    "0000" +
                  "0000" +
                "03020001" +
                "0000" +
              "0303000203" +
              "030203FF" +
              "2380" +
                "0000" +
              "0000",
            "FACEF00D010203F8",
            3)]
        public static void TryCopyBitStringBytes_Success(
            AsnEncodingRules ruleSet,
            string inputHex,
            string expectedHex,
            int expectedUnusedBitCount)
        {
            byte[] inputData = inputHex.HexToByteArray();
            byte[] output = new byte[expectedHex.Length / 2];
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadBitString(
                output,
                out int unusedBitCount,
                out int bytesWritten);

            Assert.True(didRead, "reader.TryReadBitString");
            Assert.Equal(expectedUnusedBitCount, unusedBitCount);
            Assert.Equal(expectedHex, output.AsSpan(0, bytesWritten).ByteArrayToHex());
        }

        private static void TryReadBitString_Throws_Helper(
            AsnEncodingRules ruleSet,
            byte[] input)
        {
            AsnReader reader = new AsnReader(input, ruleSet);

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.TryReadBitString(
                        Span<byte>.Empty,
                        out int unusedBitCount,
                        out int bytesWritten);
                });
        }

        private static void ReadBitString_Throws(
            AsnEncodingRules ruleSet,
            byte[] input)
        {
            AsnReader reader = new AsnReader(input, ruleSet);
            Assert.Throws<AsnContentException>(() => reader.ReadBitString(out int unusedBitCount));
        }

        [Theory]
        [InlineData("Wrong Tag", AsnEncodingRules.BER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.CER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.DER, "0500")]
        [InlineData("Zero Length", AsnEncodingRules.BER, "0300")]
        [InlineData("Zero Length", AsnEncodingRules.CER, "0300")]
        [InlineData("Zero Length", AsnEncodingRules.DER, "0300")]
        [InlineData("Bad Length", AsnEncodingRules.BER, "030200")]
        [InlineData("Bad Length", AsnEncodingRules.CER, "030200")]
        [InlineData("Bad Length", AsnEncodingRules.DER, "030200")]
        [InlineData("Constructed Form", AsnEncodingRules.DER, "2303030100")]
        [InlineData("Bad unused bits", AsnEncodingRules.BER, "03020800")]
        [InlineData("Bad unused bits", AsnEncodingRules.CER, "03020800")]
        [InlineData("Bad unused bits", AsnEncodingRules.DER, "03020800")]
        [InlineData("Bad unused bits-nodata", AsnEncodingRules.BER, "030101")]
        [InlineData("Bad unused bits-nodata", AsnEncodingRules.CER, "030101")]
        [InlineData("Bad unused bits-nodata", AsnEncodingRules.DER, "030101")]
        [InlineData("Bad nested unused bits", AsnEncodingRules.BER, "230403020800")]
        [InlineData("Bad nested unused bits-indef", AsnEncodingRules.BER, "2380030208000000")]
        [InlineData("Bad nested unused bits-indef", AsnEncodingRules.CER, "2380030208000000")]
        [InlineData("Bad nested unused bits-nodata", AsnEncodingRules.BER, "2303030101")]
        [InlineData("Bad nested unused bits-nodata-indef", AsnEncodingRules.BER, "23800301010000")]
        [InlineData("Bad nested unused bits-nodata-indef", AsnEncodingRules.CER, "23800301010000")]
        [InlineData("Bad mask", AsnEncodingRules.CER, "030201FF")]
        [InlineData("Bad mask", AsnEncodingRules.DER, "030201FF")]
        [InlineData("Bad nested mask", AsnEncodingRules.CER, "2380030201FF0000")]
        [InlineData("Nested context-specific", AsnEncodingRules.BER, "2304800300FACE")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.BER, "2380800300FACE0000")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.CER, "2380800300FACE0000")]
        [InlineData("Nested boolean", AsnEncodingRules.BER, "2303010100")]
        [InlineData("Nested boolean (indef)", AsnEncodingRules.BER, "23800101000000")]
        [InlineData("Nested boolean (indef)", AsnEncodingRules.CER, "23800101000000")]
        [InlineData("Nested constructed form", AsnEncodingRules.CER, "2380" + "2380" + "03010" + "000000000")]
        [InlineData("No terminator", AsnEncodingRules.BER, "2380" + "03020000" + "")]
        [InlineData("No terminator", AsnEncodingRules.CER, "2380" + "03020000" + "")]
        [InlineData("No content", AsnEncodingRules.BER, "2380")]
        [InlineData("No content", AsnEncodingRules.CER, "2380")]
        [InlineData("No nested content", AsnEncodingRules.CER, "23800000")]
        [InlineData("Nested value too long", AsnEncodingRules.BER, "2380030A00")]
        [InlineData("Nested value too long - constructed", AsnEncodingRules.BER, "2380230A00")]
        [InlineData("Nested value too long - simple", AsnEncodingRules.BER, "2303" + "03050000000000")]
        [InlineData(
            "Unused bits in intermediate segment",
            AsnEncodingRules.BER,
            "2380" +
              "0303000102" +
              "0303020304" +
              "0303010506" +
              "0000")]
        [InlineData("Constructed Null", AsnEncodingRules.BER, "238020000000")]
        [InlineData("Constructed Null", AsnEncodingRules.CER, "238020000000")]
        [InlineData("NonEmpty Null", AsnEncodingRules.BER, "2380000100")]
        [InlineData("NonEmpty Null", AsnEncodingRules.CER, "2380000100")]
        [InlineData("LongLength Null", AsnEncodingRules.BER, "2380008100")]
        [InlineData("Constructed Payload-TooShort", AsnEncodingRules.CER, "23800301000000")]
        public static void TryCopyBitStringBytes_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            TryReadBitString_Throws_Helper(ruleSet, inputData);
            ReadBitString_Throws(ruleSet, inputData);
        }

        [Fact]
        public static void TryCopyBitStringBytes_Throws_CER_TooLong()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte).
            //
            // So we need 03 [1001] { 1001 0x00s }
            // 1001 => 0x3E9, so the length encoding is 82 03 E9.
            // 1001 + 3 + 1 == 1004
            byte[] input = new byte[1004];
            input[0] = 0x03;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE9;

            TryReadBitString_Throws_Helper(AsnEncodingRules.CER, input);
            ReadBitString_Throws(AsnEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyBitStringBytes_Throws_CER_NestedTooLong()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte).
            //
            // This test checks it for a primitive contained within a constructed.
            //
            // So we need 03 [1001] { 1001 0x00s }
            // 1001 => 0x3E9, so the length encoding is 82 03 E9.
            // 1001 + 3 + 1 == 1005
            //
            // Plus a leading 23 80 (indefinite length constructed)
            // and a trailing 00 00 (End of contents)
            // == 1009
            byte[] input = new byte[1009];
            // CONSTRUCTED BIT STRING (indefinite)
            input[0] = 0x23;
            input[1] = 0x80;
            // BIT STRING (1001)
            input[2] = 0x03;
            input[3] = 0x82;
            input[4] = 0x03;
            input[5] = 0xE9;
            // EOC implicit since the byte[] initializes to zeros

            TryReadBitString_Throws_Helper(AsnEncodingRules.CER, input);
            ReadBitString_Throws(AsnEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyBitStringBytes_Throws_CER_NestedTooShortIntermediate()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte), and in the constructed
            // form the lengths must be
            // [ 1000, 1000, 1000, ..., len%1000 ]
            //
            // So 1000, 2, 2 is illegal.
            //
            // 23 80 (indefinite constructed bit string)
            //    03 82 03 08 (bit string, 1000 bytes)
            //       [1000 content bytes]
            //    03 02 (bit string, 2 bytes)
            //       [2 content bytes]
            //    03 02 (bit string, 2 bytes)
            //       [2 content bytes]
            //    00 00 (end of contents)
            // Looks like 1,016 bytes.
            byte[] input = new byte[1016];
            // CONSTRUCTED BIT STRING (indefinite)
            input[0] = 0x23;
            input[1] = 0x80;
            // BIT STRING (1000)
            input[2] = 0x03;
            input[3] = 0x82;
            input[4] = 0x03;
            input[5] = 0xE8;
            // BIT STRING (2)
            input[1006] = 0x03;
            input[1007] = 0x02;
            // BIT STRING (2)
            input[1010] = 0x03;
            input[1011] = 0x02;
            // EOC implicit since the byte[] initializes to zeros

            TryReadBitString_Throws_Helper(AsnEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyBitStringBytes_Success_CER_MaxPrimitiveLength()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte).
            //
            // So we need 03 [1000] [0x00-0x07] { 998 anythings } [a byte that's legal for the bitmask]
            // 1000 => 0x3E8, so the length encoding is 82 03 E8.
            // 1000 + 3 + 1 == 1003
            byte[] input = new byte[1004];
            input[0] = 0x03;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE8;

            // Unused bits
            input[4] = 0x02;

            // Payload
            input[5] = 0xA0;
            input[1002] = 0xA5;
            input[1003] = 0xFC;

            byte[] output = new byte[999];

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryReadBitString(
                output,
                out int unusedBitCount,
                out int bytesWritten);

            Assert.True(success, "reader.TryReadBitString");
            Assert.Equal(input[4], unusedBitCount);
            Assert.Equal(999, bytesWritten);

            Assert.Equal(
                input.AsSpan(5).ByteArrayToHex(),
                output.ByteArrayToHex());

            reader = new AsnReader(input, AsnEncodingRules.CER);
            byte[] output2 = reader.ReadBitString(out int ubc2);
            Assert.Equal(unusedBitCount, ubc2);
            Assert.Equal(output, output2);
        }

        [Fact]
        public static void TryCopyBitStringBytes_Success_CER_MinConstructedLength()
        {
            // CER says that the maximum encoding length for a BitString primitive is
            // 1000 (999 value bytes and 1 unused bit count byte), and that a constructed
            // form must be used for values greater than 1000 bytes, with segments dividing
            // up for each thousand [1000, 1000, ..., len%1000].
            //
            // Bit string primitives are one byte of "unused bits" and the rest are payload,
            // so the minimum constructed payload has total content length 1002:
            // [1000 (1+999), 2 (1+1)]
            //
            // 23 80 (indefinite constructed bit string)
            //    03 82 03 E9 (primitive bit string, 1000 bytes)
            //       00 [999 more payload bytes]
            //    03 02 (primitive bit string, 2 bytes)
            //       uu pp
            //    00 00 (end of contents, 0 bytes)
            // 1010 total.
            byte[] input = new byte[1012];
            int offset = 0;
            // CONSTRUCTED BIT STRING (Indefinite)
            input[offset++] = 0x23;
            input[offset++] = 0x80;
            // BIT STRING (1000)
            input[offset++] = 0x03;
            input[offset++] = 0x82;
            input[offset++] = 0x03;
            input[offset++] = 0xE8;

            // Primitive 1: Unused bits MUST be 0.
            input[offset++] = 0x00;

            // Payload (A0 :: A5 FC) (999)
            input[offset] = 0xA0;
            offset += 997;
            input[offset++] = 0xA5;
            input[offset++] = 0xFC;

            // BIT STRING (2)
            input[offset++] = 0x03;
            input[offset++] = 0x02;

            // Primitive 2: Unused bits 0-7
            input[offset++] = 0x3;

            // Payload (must have the three least significant bits unset)
            input[offset] = 0b0000_1000;

            byte[] expected = new byte[1000];
            offset = 0;
            expected[offset] = 0xA0;
            offset += 997;
            expected[offset++] = 0xA5;
            expected[offset++] = 0xFC;
            expected[offset] = 0b0000_1000;

            byte[] output = new byte[1000];

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryReadBitString(
                output,
                out int unusedBitCount,
                out int bytesWritten);

            Assert.True(success, "reader.TryReadBitString");
            Assert.Equal(input[1006], unusedBitCount);
            Assert.Equal(1000, bytesWritten);

            Assert.Equal(
                expected.ByteArrayToHex(),
                output.ByteArrayToHex());

            reader = new AsnReader(input, AsnEncodingRules.CER);
            byte[] output2 = reader.ReadBitString(out int ubc2);
            Assert.Equal(unusedBitCount, ubc2);
            Assert.Equal(output, output2);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 3, 2, 1, 0x7E };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadPrimitiveBitString(out _, out _, Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.TryReadPrimitiveBitString(out _, out _, new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.True(reader.TryReadPrimitiveBitString(out int unusedBitCount, out ReadOnlyMemory<byte> contents));
            Assert.Equal("7E", contents.ByteArrayToHex());
            Assert.Equal(1, unusedBitCount);
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, 0, 0x80 };
            byte[] output = new byte[inputData.Length];
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Asn1Tag wrongTag1 = new Asn1Tag(TagClass.Application, 0);
            Asn1Tag wrongTag2 = new Asn1Tag(TagClass.ContextSpecific, 1);
            Asn1Tag correctTag = new Asn1Tag(TagClass.ContextSpecific, 7);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadPrimitiveBitString(out _, out _, Asn1Tag.Null));
            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadBitString(output, out _, out _, Asn1Tag.Null));
            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadBitString(out _, Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.TryReadPrimitiveBitString(out _, out _));
            Assert.Throws<AsnContentException>(() => reader.TryReadBitString(output, out _, out _));
            Assert.Throws<AsnContentException>(() => reader.ReadBitString(out _));
            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(() => reader.TryReadPrimitiveBitString(out _, out _, wrongTag1));
            Assert.Throws<AsnContentException>(() => reader.TryReadBitString(output, out _, out _, wrongTag1));
            Assert.Throws<AsnContentException>(() => reader.ReadBitString(out _, wrongTag1));
            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(() => reader.TryReadPrimitiveBitString(out _, out _, wrongTag2));
            Assert.Throws<AsnContentException>(() => reader.TryReadBitString(output, out _, out _, wrongTag2));
            Assert.Throws<AsnContentException>(() => reader.ReadBitString(out _, wrongTag2));
            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.True(
                reader.TryReadPrimitiveBitString(
                    out int unusedBitCount,
                    out ReadOnlyMemory<byte> contents,
                    correctTag));

            Assert.Equal("80", contents.ByteArrayToHex());
            Assert.Equal(0, unusedBitCount);
            Assert.False(reader.HasData, "HasData after reading value");

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryReadBitString(
                    output.AsSpan(1),
                    out unusedBitCount,
                    out int written,
                    correctTag));

            Assert.Equal("80", output.AsSpan(1, written).ByteArrayToHex());
            Assert.Equal(0, unusedBitCount);
            Assert.False(reader.HasData, "HasData after reading value");

            reader = new AsnReader(inputData, ruleSet);
            byte[] output2 = reader.ReadBitString(out unusedBitCount, correctTag);

            Assert.Equal("80", output2.ByteArrayToHex());
            Assert.Equal(0, unusedBitCount);
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "030400010203", TagClass.Universal, 3)]
        [InlineData(AsnEncodingRules.CER, "030400010203", TagClass.Universal, 3)]
        [InlineData(AsnEncodingRules.DER, "030400010203", TagClass.Universal, 3)]
        [InlineData(AsnEncodingRules.BER, "800200FF", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C0200FF", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A460200FF", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);
            Asn1Tag correctConstructed = new Asn1Tag(tagClass, tagValue, true);
            Asn1Tag correctPrimitive = new Asn1Tag(tagClass, tagValue, false);

            Assert.True(
                reader.TryReadPrimitiveBitString(
                    out int ubc1,
                    out ReadOnlyMemory<byte> val1,
                    correctConstructed));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryReadPrimitiveBitString(
                    out int ubc2,
                    out ReadOnlyMemory<byte> val2,
                    correctPrimitive));

            Assert.False(reader.HasData);

            string val1Hex = val1.ByteArrayToHex();
            Assert.Equal(val1Hex, val2.ByteArrayToHex());
            Assert.Equal(ubc1, ubc2);

            reader = new AsnReader(inputData, ruleSet);
            byte[] output1 = new byte[inputData.Length];

            Assert.True(reader.TryReadBitString(output1.AsSpan(1), out ubc1, out int written, correctConstructed));
            Assert.Equal(ubc2, ubc1);
            Assert.Equal(val1Hex, output1.AsSpan(1, written).ByteArrayToHex());
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(reader.TryReadBitString(output1.AsSpan(2), out ubc1, out written, correctPrimitive));
            Assert.Equal(ubc2, ubc1);
            Assert.Equal(val1Hex, output1.AsSpan(2, written).ByteArrayToHex());
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);
            byte[] output2 = reader.ReadBitString(out ubc1, correctConstructed);

            Assert.Equal(ubc2, ubc1);
            Assert.Equal(val1Hex, output2.ByteArrayToHex());
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);
            byte[] output3 = reader.ReadBitString(out ubc1, correctPrimitive);

            Assert.Equal(ubc2, ubc1);
            Assert.Equal(val1Hex, output3.ByteArrayToHex());
            Assert.False(reader.HasData);
        }

        [Fact]
        public static void TryCopyBitStringBytes_ExtremelyNested()
        {
            byte[] dataBytes = new byte[4 * 16384];

            // This will build 2^14 nested indefinite length values.
            // In the end, none of them contain any content.
            //
            // For what it's worth, the initial algorithm succeeded at 1017, and StackOverflowed with 1018.
            int end = dataBytes.Length / 2;

            // UNIVERSAL BIT STRING [Constructed]
            const byte Tag = 0x20 | (byte)UniversalTagNumber.BitString;

            for (int i = 0; i < end; i += 2)
            {
                dataBytes[i] = Tag;
                // Indefinite length
                dataBytes[i + 1] = 0x80;
            }

            AsnReader reader = new AsnReader(dataBytes, AsnEncodingRules.BER);

            int bytesWritten;
            int unusedBitCount;

            Assert.True(
                reader.TryReadBitString(Span<byte>.Empty, out unusedBitCount, out bytesWritten));

            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, unusedBitCount);

            reader = new AsnReader(dataBytes, AsnEncodingRules.BER);
            byte[] output = reader.ReadBitString(out unusedBitCount);
            Assert.Equal(0, unusedBitCount);

            // It's Same (ReferenceEqual) on .NET Core, but just Equal on .NET Framework
            Assert.Equal(Array.Empty<byte>(), output);
        }
    }
}
