// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadOctetString
    {
        [Theory]
        [InlineData("Constructed Payload", AsnEncodingRules.BER, "2402040100")]
        [InlineData("Constructed Payload-Indefinite", AsnEncodingRules.BER, "248004010000")]
        // This value is actually invalid CER, but it returns false since it's not primitive and
        // it isn't worth preempting the descent to find out it was invalid.
        [InlineData("Constructed Payload-Indefinite", AsnEncodingRules.CER, "248004010000")]
        public static void TryReadPrimitiveOctetStringBytes_Fails(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents);

            Assert.False(didRead, "reader.TryReadOctetStringBytes");
            Assert.Equal(0, contents.Length);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, "0400")]
        [InlineData(AsnEncodingRules.BER, 1, "040100")]
        [InlineData(AsnEncodingRules.BER, 2, "040201FE")]
        [InlineData(AsnEncodingRules.CER, 5, "040502FEEFF00C")]
        [InlineData(AsnEncodingRules.DER, 2, "04020780")]
        [InlineData(AsnEncodingRules.DER, 5, "040500FEEFF00D" + "0500")]
        public static void TryReadPrimitiveOctetStringBytes_Success(
            AsnEncodingRules ruleSet,
            int expectedLength,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents);

            Assert.True(didRead, "reader.TryReadOctetStringBytes");
            Assert.Equal(expectedLength, contents.Length);
        }

        [Theory]
        [InlineData("Wrong Tag", AsnEncodingRules.BER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.CER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.DER, "0500")]
        [InlineData("Bad Length", AsnEncodingRules.BER, "040200")]
        [InlineData("Bad Length", AsnEncodingRules.CER, "040200")]
        [InlineData("Bad Length", AsnEncodingRules.DER, "040200")]
        [InlineData("Constructed Form", AsnEncodingRules.DER, "2403040100")]
        public static void TryReadPrimitiveOctetStringBytes_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents));
        }

        [Fact]
        public static void TryReadPrimitiveOctetStringBytes_Throws_CER_TooLong()
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

            Assert.Throws<AsnContentException>(
                () => reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents));

            Assert.Throws<AsnContentException>(
                () => reader.TryReadOctetString(new byte[input.Length], out _));

            Assert.Throws<AsnContentException>(() => reader.ReadOctetString());
        }

        [Fact]
        public static void TryReadPrimitiveOctetStringBytes_Success_CER_MaxLength()
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

            bool success = reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents);

            Assert.True(success, "reader.TryReadOctetStringBytes");
            Assert.Equal(1000, contents.Length);

            // Check that it is, in fact, the same memory. No copies with this API.
            Assert.True(
                Unsafe.AreSame(
                    ref MemoryMarshal.GetReference(contents.Span),
                    ref input[4]));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "04020780")]
        [InlineData(AsnEncodingRules.BER, "040207FF")]
        [InlineData(AsnEncodingRules.CER, "04020780")]
        [InlineData(AsnEncodingRules.DER, "04020780")]
        [InlineData(
            AsnEncodingRules.BER,
            "2480" +
              "2480" +
                "0000" +
              "04020000" +
              "0000")]
        public static void TryReadOctetStringBytes_Fails(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadOctetString(
                Span<byte>.Empty,
                out int bytesWritten);

            Assert.False(didRead, "reader.TryReadOctetString");
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "04020780", "0780")]
        [InlineData(AsnEncodingRules.BER, "040207FF", "07FF")]
        [InlineData(AsnEncodingRules.CER, "04020780", "0780")]
        [InlineData(AsnEncodingRules.DER, "04020680", "0680")]
        [InlineData(AsnEncodingRules.BER, "24800000", "")]
        [InlineData(AsnEncodingRules.BER, "2400", "")]
        [InlineData(AsnEncodingRules.BER, "2400" + "0500", "")]
        [InlineData(
            AsnEncodingRules.BER,
            "2480" +
              "2480" +
                "0000" +
              "04020005" +
              "0000",
            "0005")]
        [InlineData(
            AsnEncodingRules.BER,
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
        public static void TryReadOctetStringBytes_Success(
            AsnEncodingRules ruleSet,
            string inputHex,
            string expectedHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            byte[] output = new byte[expectedHex.Length / 2];
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool didRead = reader.TryReadOctetString(
                output,
                out int bytesWritten);

            Assert.True(didRead, "reader.TryReadOctetString");
            Assert.Equal(expectedHex, output.AsSpan(0, bytesWritten).ByteArrayToHex());

            reader = new AsnReader(inputData, ruleSet);
            byte[] output2 = reader.ReadOctetString();
            Assert.Equal(output, output2);
        }

        private static void TryReadOctetStringBytes_Throws_Helper(
            AsnEncodingRules ruleSet,
            byte[] input)
        {
            AsnReader reader = new AsnReader(input, ruleSet);

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.TryReadOctetString(
                        Span<byte>.Empty,
                        out int bytesWritten);
                });
        }

        private static void ReadOctetStringBytes_Throws_Helper(
            AsnEncodingRules ruleSet,
            byte[] input)
        {
            AsnReader reader = new AsnReader(input, ruleSet);

            Assert.Throws<AsnContentException>(
                () =>
                {
                    reader.ReadOctetString();
                });
        }

        [Theory]
        [InlineData("Wrong Tag", AsnEncodingRules.BER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.CER, "0500")]
        [InlineData("Wrong Tag", AsnEncodingRules.DER, "0500")]
        [InlineData("Bad Length", AsnEncodingRules.BER, "040200")]
        [InlineData("Bad Length", AsnEncodingRules.CER, "040200")]
        [InlineData("Bad Length", AsnEncodingRules.DER, "040200")]
        [InlineData("Constructed Form", AsnEncodingRules.DER, "2403040100")]
        [InlineData("Nested context-specific", AsnEncodingRules.BER, "2404800400FACE")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.BER, "2480800400FACE0000")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.CER, "2480800400FACE0000")]
        [InlineData("Nested boolean", AsnEncodingRules.BER, "2403010100")]
        [InlineData("Nested boolean (indef)", AsnEncodingRules.BER, "24800101000000")]
        [InlineData("Nested boolean (indef)", AsnEncodingRules.CER, "24800101000000")]
        [InlineData("Nested constructed form", AsnEncodingRules.CER, "2480" + "2480" + "04010" + "000000000")]
        [InlineData("No terminator", AsnEncodingRules.BER, "2480" + "04020000" + "")]
        [InlineData("No terminator", AsnEncodingRules.CER, "2480" + "04020000" + "")]
        [InlineData("No content", AsnEncodingRules.BER, "2480")]
        [InlineData("No content", AsnEncodingRules.CER, "2480")]
        [InlineData("No nested content", AsnEncodingRules.CER, "24800000")]
        [InlineData("Nested value too long", AsnEncodingRules.BER, "2480040A00")]
        [InlineData("Nested value too long - constructed", AsnEncodingRules.BER, "2480240A00")]
        [InlineData("Nested value too long - simple", AsnEncodingRules.BER, "2403" + "04050000000000")]
        [InlineData("Constructed Null", AsnEncodingRules.BER, "248020000000")]
        [InlineData("Constructed Null", AsnEncodingRules.CER, "248020000000")]
        [InlineData("NonEmpty Null", AsnEncodingRules.BER, "2480000100")]
        [InlineData("NonEmpty Null", AsnEncodingRules.CER, "2480000100")]
        [InlineData("LongLength Null", AsnEncodingRules.BER, "2480008100")]
        [InlineData("Constructed Payload-TooShort", AsnEncodingRules.CER, "24800401000000")]
        public static void TryReadOctetStringBytes_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            TryReadOctetStringBytes_Throws_Helper(ruleSet, inputData);
            ReadOctetStringBytes_Throws_Helper(ruleSet, inputData);
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

            TryReadOctetStringBytes_Throws_Helper(AsnEncodingRules.CER, input);
            ReadOctetStringBytes_Throws_Helper(AsnEncodingRules.CER, input);
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

            TryReadOctetStringBytes_Throws_Helper(AsnEncodingRules.CER, input);
            ReadOctetStringBytes_Throws_Helper(AsnEncodingRules.CER, input);
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

            bool success = reader.TryReadOctetString(
                output,
                out int bytesWritten);

            Assert.True(success, "reader.TryReadOctetString");
            Assert.Equal(1000, bytesWritten);

            Assert.Equal(
                input.AsSpan(4).ByteArrayToHex(),
                output.ByteArrayToHex());

            reader = new AsnReader(input, AsnEncodingRules.CER);
            byte[] output2 = reader.ReadOctetString();
            Assert.Equal(output, output2);
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

            bool success = reader.TryReadOctetString(
                output,
                out int bytesWritten);

            Assert.True(success, "reader.TryReadOctetString");
            Assert.Equal(1001, bytesWritten);

            Assert.Equal(
                expected.ByteArrayToHex(),
                output.ByteArrayToHex());

            reader = new AsnReader(input, AsnEncodingRules.CER);
            byte[] output2 = reader.ReadOctetString();
            Assert.Equal(output, output2);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 4, 1, 0x7E };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadPrimitiveOctetString(out _, Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.TryReadPrimitiveOctetString(out _, new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.True(reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> value));
            Assert.Equal("7E", value.ByteArrayToHex());
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
                () => reader.TryReadPrimitiveOctetString(out _, Asn1Tag.Null));
            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadOctetString(output, out _, Asn1Tag.Null));
            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadOctetString(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.TryReadPrimitiveOctetString(out _));
            Assert.Throws<AsnContentException>(() => reader.TryReadOctetString(output, out _));
            Assert.Throws<AsnContentException>(() => reader.ReadOctetString());
            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(() => reader.TryReadPrimitiveOctetString(out _, wrongTag1));
            Assert.Throws<AsnContentException>(() => reader.TryReadOctetString(output, out _, wrongTag1));
            Assert.Throws<AsnContentException>(() => reader.ReadOctetString(wrongTag1));
            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(() => reader.TryReadPrimitiveOctetString(out _, wrongTag2));
            Assert.Throws<AsnContentException>(() => reader.TryReadOctetString(output, out _, wrongTag2));
            Assert.Throws<AsnContentException>(() => reader.ReadOctetString(wrongTag2));
            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.True(reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> value, correctTag));
            Assert.Equal("0080", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after reading value");

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(reader.TryReadOctetString(output.AsSpan(1), out int written, correctTag));
            Assert.Equal("0080", output.AsSpan(1, written).ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after reading value");

            reader = new AsnReader(inputData, ruleSet);

            byte[] output2 = reader.ReadOctetString(correctTag);
            Assert.Equal("0080", output2.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0401FF", TagClass.Universal, 4)]
        [InlineData(AsnEncodingRules.CER, "0401FF", TagClass.Universal, 4)]
        [InlineData(AsnEncodingRules.DER, "0401FF", TagClass.Universal, 4)]
        [InlineData(AsnEncodingRules.BER, "8001FF", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C01FF", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A4601FF", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryReadPrimitiveOctetString(
                    out ReadOnlyMemory<byte> val1,
                    new Asn1Tag(tagClass, tagValue, true)));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryReadPrimitiveOctetString(
                    out ReadOnlyMemory<byte> val2,
                    new Asn1Tag(tagClass, tagValue, false)));

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

            Assert.True(reader.TryReadOctetString(Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            reader = new AsnReader(dataBytes, AsnEncodingRules.BER);
            byte[] output2 = reader.ReadOctetString();

            // It's Same (ReferenceEqual) on .NET Core, but just Equal on .NET Framework
            Assert.Equal(Array.Empty<byte>(), output2);
        }
    }
}
