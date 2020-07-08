// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadIA5String
    {
        public static IEnumerable<object[]> ValidEncodingData { get; } =
            new object[][]
            {
                new object[]
                {
                    AsnEncodingRules.BER,
                    "160D4A6F686E20512E20536D697468",
                    "John Q. Smith",
                },
                new object[]
                {
                    AsnEncodingRules.CER,
                    "160D4A6F686E20512E20536D697468",
                    "John Q. Smith",
                },
                new object[]
                {
                    AsnEncodingRules.DER,
                    "160D4A6F686E20512E20536D697468",
                    "John Q. Smith",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "3680" + "040D4A6F686E20512E20536D697468" + "0000",
                    "John Q. Smith",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "360F" + "040D4A6F686E20512E20536D697468",
                    "John Q. Smith",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "1600",
                    "",
                },
                new object[]
                {
                    AsnEncodingRules.CER,
                    "1600",
                    "",
                },
                new object[]
                {
                    AsnEncodingRules.DER,
                    "1600",
                    "",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "3600",
                    "",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "3680" + "0000",
                    "",
                },
                new object[]
                {
                    AsnEncodingRules.BER,
                    "3680" +
                      "2480" +
                        // "Dr."
                        "040344722E" +
                        // " & "
                        "0403202620" +
                        // "Mrs."
                        "04044D72732E" +
                        "0000" +
                      // " "
                      "040120" +
                      "2480" +
                        "240A" +
                          // "Smith"
                          "0405536D697468" +
                          // hyphen (U+2010) is not valid, so use hyphen-minus
                          "04012D" +
                        "0000" +
                      // "Jones"
                      "04054A6F6E6573" +
                      "2480" +
                        // " "
                        "040120" +
                        "2480" +
                          // small ampersand (U+FE60) is not valid, so use ampersand.
                          "040126" +
                          "0000" +
                        // " "
                        "040120" +
                        // "children"
                        "04086368696C6472656E" +
                        "0000" +
                      "0000",
                    "Dr. & Mrs. Smith-Jones & children",
                },
            };

        [Theory]
        [MemberData(nameof(ValidEncodingData))]
        public static void GetIA5String_Success(
            AsnEncodingRules ruleSet,
            string inputHex,
            string expectedValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);
            string value = reader.ReadCharacterString(UniversalTagNumber.IA5String);

            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [MemberData(nameof(ValidEncodingData))]
        public static void TryCopyIA5String(
            AsnEncodingRules ruleSet,
            string inputHex,
            string expectedValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            char[] output = new char[expectedValue.Length];

            AsnReader reader = new AsnReader(inputData, ruleSet);
            bool copied;
            int charsWritten;

            if (output.Length > 0)
            {
                output[0] = 'a';

                copied = reader.TryCopyIA5String(output.AsSpan(0, expectedValue.Length - 1),
                    out charsWritten);

                Assert.False(copied, "reader.TryCopyIA5String - too short");
                Assert.Equal(0, charsWritten);
                Assert.Equal('a', output[0]);
            }

            copied = reader.TryCopyIA5String(output,
                out charsWritten);

            Assert.True(copied, "reader.TryCopyIA5String");

            string actualValue = new string(output, 0, charsWritten);
            Assert.Equal(expectedValue, actualValue);
        }

        [Theory]
        [MemberData(nameof(ValidEncodingData))]
        public static void TryCopyIA5StringBytes(
            AsnEncodingRules ruleSet,
            string inputHex,
            string expectedString)
        {
            byte[] inputData = inputHex.HexToByteArray();
            string expectedHex = Text.Encoding.ASCII.GetBytes(expectedString).ByteArrayToHex();
            byte[] output = new byte[expectedHex.Length / 2];

            AsnReader reader = new AsnReader(inputData, ruleSet);
            bool copied;
            int bytesWritten;

            if (output.Length > 0)
            {
                output[0] = 32;

                copied = reader.TryCopyIA5StringBytes(output.AsSpan(0, output.Length - 1),
                    out bytesWritten);

                Assert.False(copied, "reader.TryCopyIA5StringBytes - too short");
                Assert.Equal(0, bytesWritten);
                Assert.Equal(32, output[0]);
            }

            copied = reader.TryCopyIA5StringBytes(output,
                out bytesWritten);

            Assert.True(copied, "reader.TryCopyIA5StringBytes");

            Assert.Equal(
                expectedHex,
                new ReadOnlySpan<byte>(output, 0, bytesWritten).ByteArrayToHex());

            Assert.Equal(output.Length, bytesWritten);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "160120", true)]
        [InlineData(AsnEncodingRules.BER, "3680" + "040120" + "0000", false)]
        [InlineData(AsnEncodingRules.BER, "3603" + "040120", false)]
        public static void TryGetIA5StringBytes(
            AsnEncodingRules ruleSet,
            string inputHex,
            bool expectSuccess)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            bool got = reader.TryGetIA5StringBytes(out ReadOnlyMemory<byte> contents);

            if (expectSuccess)
            {
                Assert.True(got, "reader.TryGetIA5StringBytes");

                Assert.True(
                    Unsafe.AreSame(
                        ref MemoryMarshal.GetReference(contents.Span),
                        ref inputData[2]));
            }
            else
            {
                Assert.False(got, "reader.TryGetIA5StringBytes");
                Assert.True(contents.IsEmpty, "contents.IsEmpty");
            }
        }

        [Theory]
        [InlineData("Incomplete Tag", AsnEncodingRules.BER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.CER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.DER, "1F")]
        [InlineData("Missing Length", AsnEncodingRules.BER, "16")]
        [InlineData("Missing Length", AsnEncodingRules.CER, "16")]
        [InlineData("Missing Length", AsnEncodingRules.DER, "16")]
        [InlineData("Missing Contents", AsnEncodingRules.BER, "1601")]
        [InlineData("Missing Contents", AsnEncodingRules.CER, "1601")]
        [InlineData("Missing Contents", AsnEncodingRules.DER, "1601")]
        [InlineData("Length Too Long", AsnEncodingRules.BER, "16034869")]
        [InlineData("Length Too Long", AsnEncodingRules.CER, "16034869")]
        [InlineData("Length Too Long", AsnEncodingRules.DER, "16034869")]
        [InlineData("Constructed Form", AsnEncodingRules.DER, "3603040149")]
        public static void TryGetIA5StringBytes_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.TryGetIA5StringBytes(out ReadOnlyMemory<byte> contents));
        }

        [Theory]
        [InlineData("Empty", AsnEncodingRules.BER, "")]
        [InlineData("Empty", AsnEncodingRules.CER, "")]
        [InlineData("Empty", AsnEncodingRules.DER, "")]
        [InlineData("Incomplete Tag", AsnEncodingRules.BER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.CER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.DER, "1F")]
        [InlineData("Missing Length", AsnEncodingRules.BER, "16")]
        [InlineData("Missing Length", AsnEncodingRules.CER, "16")]
        [InlineData("Missing Length", AsnEncodingRules.DER, "16")]
        [InlineData("Missing Contents", AsnEncodingRules.BER, "1601")]
        [InlineData("Missing Contents", AsnEncodingRules.CER, "1601")]
        [InlineData("Missing Contents", AsnEncodingRules.DER, "1601")]
        [InlineData("Missing Contents - Constructed", AsnEncodingRules.BER, "3601")]
        [InlineData("Missing Contents - Constructed Indef", AsnEncodingRules.BER, "3680")]
        [InlineData("Missing Contents - Constructed Indef", AsnEncodingRules.CER, "3680")]
        [InlineData("Length Too Long", AsnEncodingRules.BER, "16034869")]
        [InlineData("Length Too Long", AsnEncodingRules.CER, "16034869")]
        [InlineData("Length Too Long", AsnEncodingRules.DER, "16034869")]
        [InlineData("Definite Constructed Form", AsnEncodingRules.CER, "3603040149")]
        [InlineData("Definite Constructed Form", AsnEncodingRules.DER, "3603040149")]
        [InlineData("Indefinite Constructed Form - Short Payload", AsnEncodingRules.CER, "36800401490000")]
        [InlineData("Indefinite Constructed Form", AsnEncodingRules.DER, "36800401490000")]
        [InlineData("No nested content", AsnEncodingRules.CER, "36800000")]
        [InlineData("No EoC", AsnEncodingRules.BER, "3680" + "04024869")]
        [InlineData("Wrong Tag - Primitive", AsnEncodingRules.BER, "04024869")]
        [InlineData("Wrong Tag - Primitive", AsnEncodingRules.CER, "04024869")]
        [InlineData("Wrong Tag - Primitive", AsnEncodingRules.DER, "04024869")]
        [InlineData("Wrong Tag - Constructed", AsnEncodingRules.BER, "240404024869")]
        [InlineData("Wrong Tag - Constructed Indef", AsnEncodingRules.BER, "2480" + "04024869" + "0000")]
        [InlineData("Wrong Tag - Constructed Indef", AsnEncodingRules.CER, "2480" + "04024869" + "0000")]
        [InlineData("Wrong Tag - Constructed", AsnEncodingRules.DER, "240404024869")]
        [InlineData("Nested Bad Tag", AsnEncodingRules.BER, "3604" + "16024869")]
        [InlineData("Nested context-specific", AsnEncodingRules.BER, "3604800400FACE")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.BER, "3680800400FACE0000")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.CER, "3680800400FACE0000")]
        [InlineData("Nested Length Too Long", AsnEncodingRules.BER, "3607" + ("2402" + "0403") + "040149")]
        [InlineData("Nested Simple Length Too Long", AsnEncodingRules.BER, "3603" + "040548656C6C6F")]
        [InlineData("Constructed Null", AsnEncodingRules.BER, "368020000000")]
        [InlineData("Constructed Null", AsnEncodingRules.CER, "368020000000")]
        [InlineData("NonEmpty Null", AsnEncodingRules.BER, "3680000100")]
        [InlineData("NonEmpty Null", AsnEncodingRules.CER, "3680000100")]
        [InlineData("LongLength Null", AsnEncodingRules.BER, "3680008100")]
        public static void TryCopyIA5StringBytes_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            byte[] outputData = new byte[inputData.Length + 1];
            outputData[0] = 252;

            int bytesWritten = -1;
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.TryCopyIA5StringBytes(outputData, out bytesWritten));

            Assert.Equal(-1, bytesWritten);
            Assert.Equal(252, outputData[0]);
        }

        private static void TryCopyIA5String_Throws_Helper(AsnEncodingRules ruleSet, byte[] inputData)
        {
            char[] outputData = new char[inputData.Length + 1];
            outputData[0] = 'a';

            int bytesWritten = -1;
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.TryCopyIA5String(outputData, out bytesWritten));

            Assert.Equal(-1, bytesWritten);
            Assert.Equal('a', outputData[0]);
        }

        [Theory]
        [InlineData("Bad IA5 value", AsnEncodingRules.BER, "1602E280")]
        [InlineData("Bad IA5 value", AsnEncodingRules.CER, "1602E280")]
        [InlineData("Bad IA5 value", AsnEncodingRules.DER, "1602E280")]
        [InlineData("Wrong Tag", AsnEncodingRules.BER, "04024869")]
        public static void GetIA5String_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.ReadCharacterString(UniversalTagNumber.IA5String));
        }

        [Theory]
        [InlineData("Empty", AsnEncodingRules.BER, "")]
        [InlineData("Empty", AsnEncodingRules.CER, "")]
        [InlineData("Empty", AsnEncodingRules.DER, "")]
        [InlineData("Incomplete Tag", AsnEncodingRules.BER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.CER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.DER, "1F")]
        [InlineData("Missing Length", AsnEncodingRules.BER, "16")]
        [InlineData("Missing Length", AsnEncodingRules.CER, "16")]
        [InlineData("Missing Length", AsnEncodingRules.DER, "16")]
        [InlineData("Missing Contents", AsnEncodingRules.BER, "1601")]
        [InlineData("Missing Contents", AsnEncodingRules.CER, "1601")]
        [InlineData("Missing Contents", AsnEncodingRules.DER, "1601")]
        [InlineData("Missing Contents - Constructed", AsnEncodingRules.BER, "3601")]
        [InlineData("Missing Contents - Constructed Indef", AsnEncodingRules.BER, "3680")]
        [InlineData("Missing Contents - Constructed Indef", AsnEncodingRules.CER, "3680")]
        [InlineData("Length Too Long", AsnEncodingRules.BER, "16034869")]
        [InlineData("Length Too Long", AsnEncodingRules.CER, "16034869")]
        [InlineData("Length Too Long", AsnEncodingRules.DER, "16034869")]
        [InlineData("Definite Constructed Form", AsnEncodingRules.CER, "3603040149")]
        [InlineData("Definite Constructed Form", AsnEncodingRules.DER, "3603040149")]
        [InlineData("Indefinite Constructed Form - Short Payload", AsnEncodingRules.CER, "36800401490000")]
        [InlineData("Indefinite Constructed Form", AsnEncodingRules.DER, "36800401490000")]
        [InlineData("No nested content", AsnEncodingRules.CER, "36800000")]
        [InlineData("No EoC", AsnEncodingRules.BER, "3680" + "04024869")]
        [InlineData("Wrong Tag - Primitive", AsnEncodingRules.BER, "04024869")]
        [InlineData("Wrong Tag - Primitive", AsnEncodingRules.CER, "04024869")]
        [InlineData("Wrong Tag - Primitive", AsnEncodingRules.DER, "04024869")]
        [InlineData("Wrong Tag - Constructed", AsnEncodingRules.BER, "240404024869")]
        [InlineData("Wrong Tag - Constructed Indef", AsnEncodingRules.BER, "2480" + "04024869" + "0000")]
        [InlineData("Wrong Tag - Constructed Indef", AsnEncodingRules.CER, "2480" + "04024869" + "0000")]
        [InlineData("Wrong Tag - Constructed", AsnEncodingRules.DER, "240404024869")]
        [InlineData("Nested Bad Tag", AsnEncodingRules.BER, "3604" + "16024869")]
        [InlineData("Nested context-specific", AsnEncodingRules.BER, "3604800400FACE")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.BER, "3680800400FACE0000")]
        [InlineData("Nested context-specific (indef)", AsnEncodingRules.CER, "3680800400FACE0000")]
        [InlineData("Nested Length Too Long", AsnEncodingRules.BER, "3607" + ("2402" + "0403") + "040149")]
        [InlineData("Nested Simple Length Too Long", AsnEncodingRules.BER, "3603" + "040548656C6C6F")]
        [InlineData("Constructed Null", AsnEncodingRules.BER, "368020000000")]
        [InlineData("Constructed Null", AsnEncodingRules.CER, "368020000000")]
        [InlineData("NonEmpty Null", AsnEncodingRules.BER, "3680000100")]
        [InlineData("NonEmpty Null", AsnEncodingRules.CER, "3680000100")]
        [InlineData("LongLength Null", AsnEncodingRules.BER, "3680008100")]
        [InlineData("Bad IA5 value", AsnEncodingRules.BER, "1602E280")]
        public static void TryCopyIA5String_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            TryCopyIA5String_Throws_Helper(ruleSet, inputData);
        }

        [Fact]
        public static void TryCopyIA5String_Throws_CER_NestedTooLong()
        {
            // CER says that the maximum encoding length for a IA5String primitive
            // is 1000.
            //
            // This test checks it for a primitive contained within a constructed.
            //
            // So we need 04 [1001] { 1001 0x00s }
            // 1001 => 0x3E9, so the length encoding is 82 03 E9.
            // 1001 + 3 + 1 == 1005
            //
            // Plus a leading 36 80 (indefinite length constructed)
            // and a trailing 00 00 (End of contents)
            // == 1009
            byte[] input = new byte[1009];
            // CONSTRUCTED IA5STRING (indefinite)
            input[0] = 0x36;
            input[1] = 0x80;
            // OCTET STRING (1001)
            input[2] = 0x04;
            input[3] = 0x82;
            input[4] = 0x03;
            input[5] = 0xE9;
            // EOC implicit since the byte[] initializes to zeros

            TryCopyIA5String_Throws_Helper(AsnEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyIA5String_Throws_CER_NestedTooShortIntermediate()
        {
            // CER says that the maximum encoding length for a IA5String primitive
            // is 1000, and in the constructed form the lengths must be
            // [ 1000, 1000, 1000, ..., len%1000 ]
            //
            // So 1000, 2, 2 is illegal.
            //
            // 36 80 (indefinite constructed IA5 string)
            //    04 82 03 08 (octet string, 1000 bytes)
            //       [1000 content bytes]
            //    04 02 (octet string, 2 bytes)
            //       [2 content bytes]
            //    04 02 (octet string, 2 bytes)
            //       [2 content bytes]
            //    00 00 (end of contents)
            // Looks like 1,016 bytes.
            byte[] input = new byte[1016];
            // CONSTRUCTED IA5STRING (indefinite)
            input[0] = 0x36;
            input[1] = 0x80;
            // OCTET STRING (1000)
            input[2] = 0x03;
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

            TryCopyIA5String_Throws_Helper(AsnEncodingRules.CER, input);
        }

        [Fact]
        public static void TryCopyIA5StringBytes_Success_CER_MaxPrimitiveLength()
        {
            // CER says that the maximum encoding length for a IA5String primitive
            // is 1000.
            //
            // So we need 16 [1000] { 1000 anythings }
            // 1000 => 0x3E8, so the length encoding is 82 03 E8.
            // 1000 + 3 + 1 == 1004
            byte[] input = new byte[1004];
            input[0] = 0x16;
            input[1] = 0x82;
            input[2] = 0x03;
            input[3] = 0xE8;

            // Content
            input[4] = 0x65;
            input[5] = 0x65;
            input[1002] = 0x61;
            input[1003] = 0x61;

            byte[] output = new byte[1000];

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryCopyIA5StringBytes(output,
                out int bytesWritten);

            Assert.True(success, "reader.TryCopyIA5StringBytes");
            Assert.Equal(1000, bytesWritten);

            Assert.Equal(
                input.AsSpan(4).ByteArrayToHex(),
                output.ByteArrayToHex());
        }

        [Fact]
        public static void TryCopyIA5StringBytes_Success_CER_MinConstructedLength()
        {
            // CER says that the maximum encoding length for a IA5String primitive
            // is 1000, and that a constructed form must be used for values greater
            // than 1000 bytes, with segments dividing up for each thousand
            // [1000, 1000, ..., len%1000].
            //
            // So our smallest constructed form is 1001 bytes, [1000, 1]
            //
            // 36 80 (indefinite constructed IA5 string)
            //    04 82 03 E9 (primitive octet string, 1000 bytes)
            //       [1000 content bytes]
            //    04 01 (primitive octet string, 1 byte)
            //       pp
            //    00 00 (end of contents, 0 bytes)
            // 1011 total.
            byte[] input = new byte[1011];
            int offset = 0;
            // CONSTRUCTED IA5STRING (Indefinite)
            input[offset++] = 0x36;
            input[offset++] = 0x80;
            // OCTET STRING (1000)
            input[offset++] = 0x04;
            input[offset++] = 0x82;
            input[offset++] = 0x03;
            input[offset++] = 0xE8;

            // Primitive 1: (65 65 :: 61 61) (1000)
            input[offset++] = 0x65;
            input[offset] = 0x65;
            offset += 997;
            input[offset++] = 0x61;
            input[offset++] = 0x61;

            // OCTET STRING (1)
            input[offset++] = 0x04;
            input[offset++] = 0x01;

            // Primitive 2: One more byte
            input[offset] = 0x2E;

            byte[] expected = new byte[1001];
            offset = 0;
            expected[offset++] = 0x65;
            expected[offset] = 0x65;
            offset += 997;
            expected[offset++] = 0x61;
            expected[offset++] = 0x61;
            expected[offset] = 0x2E;

            byte[] output = new byte[1001];

            AsnReader reader = new AsnReader(input, AsnEncodingRules.CER);

            bool success = reader.TryCopyIA5StringBytes(output,
                out int bytesWritten);

            Assert.True(success, "reader.TryCopyIA5StringBytes");
            Assert.Equal(1001, bytesWritten);

            Assert.Equal(
                expected.ByteArrayToHex(),
                output.ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x16, 2, (byte)'e', (byte)'l' };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryGetIA5StringBytes(Asn1Tag.Null, out _));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.TryGetIA5StringBytes(new Asn1Tag(TagClass.ContextSpecific, 0), out _));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.True(reader.TryGetIA5StringBytes(out ReadOnlyMemory<byte> value));
            Assert.Equal("656C", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, (byte)'h', (byte)'i' };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryGetIA5StringBytes(Asn1Tag.Null, out _));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.TryGetIA5StringBytes(out _));

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.TryGetIA5StringBytes(new Asn1Tag(TagClass.Application, 0), out _));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.TryGetIA5StringBytes(new Asn1Tag(TagClass.ContextSpecific, 1), out _));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.True(
                reader.TryGetIA5StringBytes(
                    new Asn1Tag(TagClass.ContextSpecific, 7),
                    out ReadOnlyMemory<byte> value));

            Assert.Equal("6869", value.ByteArrayToHex());
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "16026869", TagClass.Universal, 22)]
        [InlineData(AsnEncodingRules.CER, "16026869", TagClass.Universal, 22)]
        [InlineData(AsnEncodingRules.DER, "16026869", TagClass.Universal, 22)]
        [InlineData(AsnEncodingRules.BER, "80023132", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C023132", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A46023132", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            Asn1Tag correctCons = new Asn1Tag(tagClass, tagValue, true);
            Asn1Tag correctPrim = new Asn1Tag(tagClass, tagValue, false);
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryGetIA5StringBytes(
                    correctCons,
                    out ReadOnlyMemory<byte> val1));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryGetIA5StringBytes(
                    correctPrim,
                    out ReadOnlyMemory<byte> val2));

            Assert.False(reader.HasData);

            Assert.Equal(val1.ByteArrayToHex(), val2.ByteArrayToHex());

#if NETCOREAPP
            string expected = Encoding.ASCII.GetString(val1.Span);
#else
            string expected = Encoding.ASCII.GetString(val1.ToArray());
#endif

            reader = new AsnReader(inputData, ruleSet);
            Assert.Equal(expected, reader.ReadCharacterString(UniversalTagNumber.IA5String, correctPrim));

            reader = new AsnReader(inputData, ruleSet);
            Assert.Equal(expected, reader.ReadCharacterString(UniversalTagNumber.IA5String, correctCons));

            char[] output = new char[28];

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryReadCharacterString(
                    output.AsSpan(1),
                    UniversalTagNumber.IA5String,
                    out int charsWritten,
                    correctPrim));

            Assert.Equal(expected, output.AsSpan(1, charsWritten).ToString());

            reader = new AsnReader(inputData, ruleSet);

            Assert.True(
                reader.TryReadCharacterString(
                    output.AsSpan(2),
                    UniversalTagNumber.IA5String,
                    out charsWritten,
                    correctCons));

            Assert.Equal(expected, output.AsSpan(2, charsWritten).ToString());
        }

        [Fact]
        public static void TryReadCharacterStringBytes_WrongKind()
        {
            byte[] inputData = "16026869".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.BER);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.TryReadCharacterStringBytes(Span<byte>.Empty, Asn1Tag.Boolean, out _));
        }
    }

    internal static class ReaderIA5Extensions
    {
        public static bool TryGetIA5StringBytes(
            this AsnReader reader,
            out ReadOnlyMemory<byte> contents)
        {
            return reader.TryReadPrimitiveCharacterStringBytes(
                new Asn1Tag(UniversalTagNumber.IA5String),
                out contents);
        }

        public static bool TryGetIA5StringBytes(
            this AsnReader reader,
            Asn1Tag expectedTag,
            out ReadOnlyMemory<byte> contents)
        {
            return reader.TryReadPrimitiveCharacterStringBytes(
                expectedTag,
                out contents);
        }

        public static bool TryCopyIA5StringBytes(
            this AsnReader reader,
            Span<byte> destination,
            out int bytesWritten)
        {
            return reader.TryReadCharacterStringBytes(
                destination,
                new Asn1Tag(UniversalTagNumber.IA5String),
                out bytesWritten);
        }

        public static bool TryCopyIA5String(
            this AsnReader reader,
            Span<char> destination,
            out int charsWritten)
        {
            return reader.TryReadCharacterString(
                destination,
                UniversalTagNumber.IA5String,
                out charsWritten);
        }
    }
}
