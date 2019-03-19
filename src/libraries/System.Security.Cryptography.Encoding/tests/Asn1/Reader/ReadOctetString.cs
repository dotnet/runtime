// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadOctetString : Asn1ReaderTests
    {
        [Theory]
        [InlineData("Constructed Payload", PublicEncodingRules.BER, "2402040100")]
        [InlineData("Constructed Payload-Indefinite", PublicEncodingRules.BER, "248004010000")]
        // This value is actually invalid CER, but it returns false since it's not primitive and
        // it isn't worth preempting the descent to find out it was invalid.
        [InlineData("Constructed Payload-Indefinite", PublicEncodingRules.CER, "248004010000")]
        public static void TryGetOctetStringBytes_Fails(
            string description,
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents);

            Assert.False(didRead, "reader.TryReadOctetStringBytes");
            Assert.Equal(0, contents.Length);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, "0400")]
        [InlineData(PublicEncodingRules.BER, 1, "040100")]
        [InlineData(PublicEncodingRules.BER, 2, "040201FE")]
        [InlineData(PublicEncodingRules.CER, 5, "040502FEEFF00C")]
        [InlineData(PublicEncodingRules.DER, 2, "04020780")]
        [InlineData(PublicEncodingRules.DER, 5, "040500FEEFF00D" + "0500")]
        public static void TryGetOctetStringBytes_Success(
            PublicEncodingRules ruleSet,
            int expectedLength,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents);

            Assert.True(didRead, "reader.TryReadOctetStringBytes");
            Assert.Equal(expectedLength, contents.Length);
        }

        [Theory]
        [InlineData("Wrong Tag", PublicEncodingRules.BER, "0500")]
        [InlineData("Wrong Tag", PublicEncodingRules.CER, "0500")]
        [InlineData("Wrong Tag", PublicEncodingRules.DER, "0500")]
        [InlineData("Bad Length", PublicEncodingRules.BER, "040200")]
        [InlineData("Bad Length", PublicEncodingRules.CER, "040200")]
        [InlineData("Bad Length", PublicEncodingRules.DER, "040200")]
        [InlineData("Constructed Form", PublicEncodingRules.DER, "2403040100")]
        public static void TryGetOctetStringBytes_Throws(
            string description,
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(
                () => reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents));
        }

        [Fact]
        public static void TryGetOctetStringBytes_Throws_CER_TooLong()
        {
            // CER says that the maximum encoding length for an OctetString primitive
            // is 1000.
            //
            // So we need 04 [1001] { 1001 0x00s }
            // 1001 => 0x3E9, so the length encoding is 82 03 E9.
            // 1001 + 3 + 1 == 1005
            byte[] input = new byte[1005];
            input[0] = 0x04;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE9;

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            Assert.Throws<CryptographicException>(
                () => reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents));
        }

        [Fact]
        public static void TryGetOctetStringBytes_Success_CER_MaxLength()
        {
            // CER says that the maximum encoding length for an OctetString primitive
            // is 1000.
            //
            // So we need 04 [1000] { 1000 anythings }
            // 1000 => 0x3E8, so the length encoding is 82 03 E8.
            // 1000 + 3 + 1 == 1004
            byte[] input = new byte[1004];
            input[0] = 0x04;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE8;

            // Contents
            input[4] = 0x02;
            input[5] = 0xA0;
            input[1002] = 0xA5;
            input[1003] = 0xFC;

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents);

            Assert.True(success, "reader.TryReadOctetStringBytes");
            Assert.Equal(1000, contents.Length);

            // Check that it is, in fact, the same memory. No copies with this API.
            Assert.True(
                Unsafe.AreSame(
                    ref MemoryMarshal.GetReference(contents.Span),
                    ref input[4]));
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "04020780")]
        [InlineData(PublicEncodingRules.BER, "040207FF")]
        [InlineData(PublicEncodingRules.CER, "04020780")]
        [InlineData(PublicEncodingRules.DER, "04020780")]
        [InlineData(
            PublicEncodingRules.BER,
            "2480" +
              "2480" +
                "0000" +
              "04020000" +
              "0000")]
        public static void TryCopyOctetStringBytes_Fails(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryCopyOctetStringBytes(
                Span<byte>.Empty,
                out int bytesWritten);

            Assert.False(didRead, "reader.TryCopyOctetStringBytes");
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "04020780", "0780")]
        [InlineData(PublicEncodingRules.BER, "040207FF", "07FF")]
        [InlineData(PublicEncodingRules.CER, "04020780", "0780")]
        [InlineData(PublicEncodingRules.DER, "04020680", "0680")]
        [InlineData(PublicEncodingRules.BER, "24800000", "")]
        [InlineData(PublicEncodingRules.BER, "2400", "")]
        [InlineData(PublicEncodingRules.BER, "2400" + "0500", "")]
        [InlineData(
            PublicEncodingRules.BER,
            "2480" +
              "2480" +
                "0000" +
              "04020005" +
              "0000",
            "0005")]
        [InlineData(
            PublicEncodingRules.BER,
            "2480" +
              "2406" +
                "0401FA" +
                "0401CE" +
              "2480" +
                "2480" +
                  "2480" +
                    "0402F00D" +
                    "0000" +
                  "0000" +
                "04020001" +
                "0000" +
              "0403000203" +
              "040203FF" +
              "2480" +
                "0000" +
              "0000",
            "FACEF00D000100020303FF")]
        public static void TryCopyOctetStringBytes_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            string expectedHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            byte[] output = new byte[expectedHex.Length / 2];
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            bool didRead = reader.TryCopyOctetStringBytes(
                output,
                out int bytesWritten);

            Assert.True(didRead, "reader.TryCopyOctetStringBytes");
            Assert.Equal(expectedHex, output.AsSpan(0, bytesWritten).ByteArrayToHex());
        }

        private static void TryCopyOctetStringBytes_Throws(
            PublicEncodingRules ruleSet,
            byte[] input)
        {
            Assert.Throws<CryptographicException>(
                () =>
                {
                    AsnReader reader = new AsnReader(input, (AsnEncodingRules)ruleSet);
                    reader.TryCopyOctetStringBytes(
                        Span<byte>.Empty,
                        out int bytesWritten);
                });
        }

        [Theory]
        [InlineData("Wrong Tag", PublicEncodingRules.BER, "0500")]
        [InlineData("Wrong Tag", PublicEncodingRules.CER, "0500")]
        [InlineData("Wrong Tag", PublicEncodingRules.DER, "0500")]
        [InlineData("Bad Length", PublicEncodingRules.BER, "040200")]
        [InlineData("Bad Length", PublicEncodingRules.CER, "040200")]
        [InlineData("Bad Length", PublicEncodingRules.DER, "040200")]
        [InlineData("Constructed Form", PublicEncodingRules.DER, "2403040100")]
        [InlineData("Nested context-specific", PublicEncodingRules.BER, "2404800400FACE")]
        [InlineData("Nested context-specific (indef)", PublicEncodingRules.BER, "2480800400FACE0000")]
        [InlineData("Nested context-specific (indef)", PublicEncodingRules.CER, "2480800400FACE0000")]
        [InlineData("Nested boolean", PublicEncodingRules.BER, "2403010100")]
        [InlineData("Nested boolean (indef)", PublicEncodingRules.BER, "24800101000000")]
        [InlineData("Nested boolean (indef)", PublicEncodingRules.CER, "24800101000000")]
        [InlineData("Nested constructed form", PublicEncodingRules.CER, "2480" + "2480" + "04010" + "000000000")]
        [InlineData("No terminator", PublicEncodingRules.BER, "2480" + "04020000" + "")]
        [InlineData("No terminator", PublicEncodingRules.CER, "2480" + "04020000" + "")]
        [InlineData("No content", PublicEncodingRules.BER, "2480")]
        [InlineData("No content", PublicEncodingRules.CER, "2480")]
        [InlineData("No nested content", PublicEncodingRules.CER, "24800000")]
        [InlineData("Nested value too long", PublicEncodingRules.BER, "2480040A00")]
        [InlineData("Nested value too long - constructed", PublicEncodingRules.BER, "2480240A00")]
        [InlineData("Nested value too long - simple", PublicEncodingRules.BER, "2403" + "04050000000000")]
        [InlineData("Constructed EndOfContents", PublicEncodingRules.BER, "248020000000")]
        [InlineData("Constructed EndOfContents", PublicEncodingRules.CER, "248020000000")]
        [InlineData("NonEmpty EndOfContents", PublicEncodingRules.BER, "2480000100")]
        [InlineData("NonEmpty EndOfContents", PublicEncodingRules.CER, "2480000100")]
        [InlineData("LongLength EndOfContents", PublicEncodingRules.BER, "2480008100")]
        [InlineData("Constructed Payload-TooShort", PublicEncodingRules.CER, "24800401000000")]
        public static void TryCopyOctetStringBytes_Throws(
            string description,
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            TryCopyOctetStringBytes_Throws(ruleSet, inputData);
        }

        [Fact]
        public static void TryCopyOctetStringBytes_Throws_CER_NestedTooLong()
        {
            // CER says that the maximum encoding length for an OctetString primitive
            // is 1000.
            //
            // This test checks it for a primitive contained within a constructed.
            //
            // So we need 04 [1001] { 1001 0x00s }
            // 1001 => 0x3E9, so the length encoding is 82 03 E9.
            // 1001 + 3 + 1 == 1005
            //
            // Plus a leading 24 80 (indefinite length constructed)
            // and a trailing 00 00 (End of contents)
            // == 1009
            byte[] input = new byte[1009];
            // CONSTRUCTED OCTET STRING (indefinite)
            input[0] = 0x24;
            input[1] = 0x80;
            // OCTET STRING (1001)
            input[2] = 0x04;
            input[3] = 0x82;
            input[4] = 0x03;
            input[5] = 0xE9;
            // EOC implicit since the byte[] initializes to zeros

            TryCopyOctetStringBytes_Throws(PublicEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyOctetStringBytes_Throws_CER_NestedTooShortIntermediate()
        {
            // CER says that the maximum encoding length for an OctetString primitive
            // is 1000, and in the constructed form the lengths must be
            // [ 1000, 1000, 1000, ..., len%1000 ]
            //
            // So 1000, 2, 2 is illegal.
            //
            // 24 80 (indefinite constructed octet string)
            //    04 82 03 08 (octet string, 1000 bytes)
            //       [1000 content bytes]
            //    04 02 (octet string, 2 bytes)
            //       [2 content bytes]
            //    04 02 (octet string, 2 bytes)
            //       [2 content bytes]
            //    00 00 (end of contents)
            // Looks like 1,016 bytes.
            byte[] input = new byte[1016];
            // CONSTRUCTED OCTET STRING (indefinite)
            input[0] = 0x23;
            input[1] = 0x80;
            // OCTET STRING (1000)
            input[2] = 0x04;
            input[3] = 0x82;
            input[4] = 0x03;
            input[5] = 0xE8;
            // OCTET STRING (2)
            input[1006] = 0x04;
            input[1007] = 0x02;
            // OCTET STRING (2)
            input[1010] = 0x04;
            input[1011] = 0x02;
            // EOC implicit since the byte[] initializes to zeros

            TryCopyOctetStringBytes_Throws(PublicEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyOctetStringBytes_Success_CER_MaxPrimitiveLength()
        {
            // CER says that the maximum encoding length for an OctetString primitive
            // is 1000.
            //
            // So we need 04 [1000] { 1000 anythings }
            // 1000 => 0x3E8, so the length encoding is 82 03 E8.
            // 1000 + 3 + 1 == 1004
            byte[] input = new byte[1004];
            input[0] = 0x04;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE8;

            // Content
            input[4] = 0x02;
            input[5] = 0xA0;
            input[1002] = 0xA5;
            input[1003] = 0xFC;

            byte[] output = new byte[1000];

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryCopyOctetStringBytes(
                output,
                out int bytesWritten);

            Assert.True(success, "reader.TryCopyOctetStringBytes");
            Assert.Equal(1000, bytesWritten);

            Assert.Equal(
                input.AsSpan(4).ByteArrayToHex(),
                output.ByteArrayToHex());
        }

        [Fact]
        public static void TryCopyOctetStringBytes_Success_CER_MinConstructedLength()
        {
            // CER says that the maximum encoding length for an OctetString primitive
            // is 1000, and that a constructed form must be used for values greater
            // than 1000 bytes, with segments dividing up for each thousand
            // [1000, 1000, ..., len%1000].
            //
            // So our smallest constructed form is 1001 bytes, [1000, 1]
            //
            // 24 80 (indefinite constructed octet string)
            //    04 82 03 E9 (primitive octet string, 1000 bytes)
            //       [1000 content bytes]
            //    04 01 (primitive octet string, 1 byte)
            //       pp
            //    00 00 (end of contents, 0 bytes)
            // 1011 total.
            byte[] input = new byte[1011];
            int offset = 0;
            // CONSTRUCTED OCTET STRING (Indefinite)
            input[offset++] = 0x24;
            input[offset++] = 0x80;
            // OCTET STRING (1000)
            input[offset++] = 0x04;
            input[offset++] = 0x82;
            input[offset++] = 0x03;
            input[offset++] = 0xE8;

            // Primitive 1: (55 A0 :: A5 FC) (1000)
            input[offset++] = 0x55;
            input[offset] = 0xA0;
            offset += 997;
            input[offset++] = 0xA5;
            input[offset++] = 0xFC;

            // OCTET STRING (1)
            input[offset++] = 0x04;
            input[offset++] = 0x01;

            // Primitive 2: One more byte
            input[offset] = 0xF7;

            byte[] expected = new byte[1001];
            offset = 0;
            expected[offset++] = 0x55;
            expected[offset] = 0xA0;
            offset += 997;
            expected[offset++] = 0xA5;
            expected[offset++] = 0xFC;
            expected[offset] = 0xF7;

            byte[] output = new byte[1001];

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryCopyOctetStringBytes(
                output,
                out int bytesWritten);

            Assert.True(success, "reader.TryCopyOctetStringBytes");
            Assert.Equal(1001, bytesWritten);

            Assert.Equal(
                expected.ByteArrayToHex(),
                output.ByteArrayToHex());
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 4, 1, 0x7E };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadPrimitiveOctetStringBytes(Asn1Tag.Null, out _));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.TryReadPrimitiveOctetStringBytes(new Asn1Tag(TagClass.ContextSpecific, 0), out _));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.True(reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> value));
            Assert.Equal("7E", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, 0, 0x80 };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadPrimitiveOctetStringBytes(Asn1Tag.Null, out _));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.TryReadPrimitiveOctetStringBytes(out _));

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.TryReadPrimitiveOctetStringBytes(new Asn1Tag(TagClass.Application, 0), out _));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.TryReadPrimitiveOctetStringBytes(new Asn1Tag(TagClass.ContextSpecific, 1), out _));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.True(
                reader.TryReadPrimitiveOctetStringBytes(
                    new Asn1Tag(TagClass.ContextSpecific, 7),
                    out ReadOnlyMemory<byte> value));

            Assert.Equal("0080", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0401FF", PublicTagClass.Universal, 4)]
        [InlineData(PublicEncodingRules.CER, "0401FF", PublicTagClass.Universal, 4)]
        [InlineData(PublicEncodingRules.DER, "0401FF", PublicTagClass.Universal, 4)]
        [InlineData(PublicEncodingRules.BER, "8001FF", PublicTagClass.ContextSpecific, 0)]
        [InlineData(PublicEncodingRules.CER, "4C01FF", PublicTagClass.Application, 12)]
        [InlineData(PublicEncodingRules.DER, "DF8A4601FF", PublicTagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            PublicEncodingRules ruleSet,
            string inputHex,
            PublicTagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.True(
                reader.TryReadPrimitiveOctetStringBytes(
                    new Asn1Tag((TagClass)tagClass, tagValue, true),
                    out ReadOnlyMemory<byte> val1));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.True(
                reader.TryReadPrimitiveOctetStringBytes(
                    new Asn1Tag((TagClass)tagClass, tagValue, false),
                    out ReadOnlyMemory<byte> val2));

            Assert.False(reader.HasData);

            Assert.Equal(val1.ByteArrayToHex(), val2.ByteArrayToHex());
        }

        [Fact]
        public static void TryCopyOctetStringBytes_ExtremelyNested()
        {
            byte[] dataBytes = new byte[4 * 16384];

            // This will build 2^14 nested indefinite length values.
            // In the end, none of them contain any content.
            //
            // For what it's worth, the initial algorithm succeeded at 1061, and StackOverflowed with 1062.
            int end = dataBytes.Length / 2;

            // UNIVERSAL OCTET STRING [Constructed]
            const byte Tag = 0x20 | (byte)UniversalTagNumber.OctetString;

            for (int i = 0; i < end; i += 2)
            {
                dataBytes[i] = Tag;
                // Indefinite length
                dataBytes[i + 1] = 0x80;
            }

            AsnReader reader = new AsnReader(dataBytes, AsnEncodingRules.BER);

            int bytesWritten;

            Assert.True(reader.TryCopyOctetStringBytes(Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }
    }
}
