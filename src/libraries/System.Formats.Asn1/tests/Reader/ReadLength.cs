// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadLength
    {
        private static Asn1Tag ReadTagAndLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int? parsedLength,
            out int bytesRead)
        {
            Asn1Tag tag = Asn1Tag.Decode(source, out int tagLength);
            parsedLength = AsnDecoder.DecodeLength(source.Slice(tagLength), ruleSet, out int lengthLength);
            bytesRead = tagLength + lengthLength;
            return tag;
        }

        private static bool TryReadTagAndLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out Asn1Tag tag,
            out int? parsedLength,
            out int bytesRead)
        {
            Asn1Tag localTag = Asn1Tag.Decode(source, out int tagLength);

            bool read = AsnDecoder.TryDecodeLength(
                source.Slice(tagLength),
                ruleSet,
                out parsedLength,
                out int lengthLength);

            if (read)
            {
                tag = localTag;
                bytesRead = tagLength + lengthLength;
            }
            else
            {
                tag = default;
                bytesRead = default;
            }

            return read;
        }

        [Theory]
        [InlineData(4, 0, "0400")]
        [InlineData(1, 1, "0101")]
        [InlineData(4, 127, "047F")]
        [InlineData(4, 128, "048180")]
        [InlineData(4, 255, "0481FF")]
        [InlineData(2, 256, "02820100")]
        [InlineData(4, int.MaxValue, "04847FFFFFFF")]
        public static void MinimalPrimitiveLength(int tagValue, int length, string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            foreach (AsnEncodingRules rules in Enum.GetValues(typeof(AsnEncodingRules)))
            {
                Asn1Tag tag = ReadTagAndLength(inputBytes, rules, out int? parsedLength, out int bytesRead);

                Assert.Equal(inputBytes.Length, bytesRead);
                Assert.False(tag.IsConstructed, "tag.IsConstructed");
                Assert.Equal(tagValue, tag.TagValue);
                Assert.Equal(length, parsedLength.Value);

                Assert.True(TryReadTagAndLength(inputBytes, rules, out tag, out parsedLength, out bytesRead));

                Assert.Equal(inputBytes.Length, bytesRead);
                Assert.False(tag.IsConstructed, "tag.IsConstructed");
                Assert.Equal(tagValue, tag.TagValue);
                Assert.Equal(length, parsedLength.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public static void ReadWithUnknownRuleSet(int invalidRuleSetValue)
        {
            byte[] data = { 0x05, 0x00 };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AsnReader(data, (AsnEncodingRules)invalidRuleSetValue));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ReadTagAndLength(data, (AsnEncodingRules)invalidRuleSetValue, out _, out _));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => TryReadTagAndLength(data, (AsnEncodingRules)invalidRuleSetValue, out _, out _, out _));
        }

        private static void ReadValid(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            int? expectedLength,
            int expectedBytesRead = -1)
        {
            if (expectedBytesRead < 0)
            {
                expectedBytesRead = source.Length;
            }

            ReadTagAndLength(
                source,
                ruleSet,
                out int? length,
                out int bytesRead);

            Assert.Equal(expectedBytesRead, bytesRead);
            Assert.Equal(expectedLength, length);

            bool read = TryReadTagAndLength(
                source,
                ruleSet,
                out _,
                out length,
                out bytesRead);

            Assert.True(read);
            Assert.Equal(expectedBytesRead, bytesRead);
            Assert.Equal(expectedLength, length);
        }

        private static void ReadInvalid(byte[] source, AsnEncodingRules ruleSet)
        {
            Assert.Throws<AsnContentException>(
                () => ReadTagAndLength(source, ruleSet, out _, out _));

            Asn1Tag tag;
            int? decodedLength;
            int bytesConsumed;

            Assert.False(
                TryReadTagAndLength(source, ruleSet, out tag, out decodedLength, out bytesConsumed));

            Assert.True(tag == default);
            Assert.Null(decodedLength);
            Assert.Equal(0, bytesConsumed);
        }

        [Theory]
        [InlineData("05")]
        [InlineData("0481")]
        [InlineData("048201")]
        [InlineData("04830102")]
        [InlineData("0484010203")]
        public static void ReadWithInsufficientData(string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();

            ReadInvalid(inputData, AsnEncodingRules.BER);
            ReadInvalid(inputData, AsnEncodingRules.CER);
            ReadInvalid(inputData, AsnEncodingRules.DER);
        }

        [Theory]
        [InlineData("DER indefinite constructed", AsnEncodingRules.DER, "3080" + "0500" + "0000")]
        [InlineData("0xFF-BER", AsnEncodingRules.BER, "04FF")]
        [InlineData("0xFF-CER", AsnEncodingRules.CER, "04FF")]
        [InlineData("0xFF-DER", AsnEncodingRules.DER, "04FF")]
        [InlineData("DER indefinite primitive", AsnEncodingRules.DER, "0480" + "0000")]
        [InlineData("DER non-minimal 0", AsnEncodingRules.DER, "048100")]
        [InlineData("DER non-minimal 7F", AsnEncodingRules.DER, "04817F")]
        [InlineData("DER non-minimal 80", AsnEncodingRules.DER, "04820080")]
        [InlineData("CER non-minimal 0", AsnEncodingRules.CER, "048100")]
        [InlineData("CER non-minimal 7F", AsnEncodingRules.CER, "04817F")]
        [InlineData("CER non-minimal 80", AsnEncodingRules.CER, "04820080")]
        [InlineData("BER too large", AsnEncodingRules.BER, "048480000000")]
        [InlineData("CER too large", AsnEncodingRules.CER, "048480000000")]
        [InlineData("DER too large", AsnEncodingRules.DER, "048480000000")]
        [InlineData("BER padded too large", AsnEncodingRules.BER, "0486000080000000")]
        [InlineData("BER uint.MaxValue", AsnEncodingRules.BER, "0484FFFFFFFF")]
        [InlineData("CER uint.MaxValue", AsnEncodingRules.CER, "0484FFFFFFFF")]
        [InlineData("DER uint.MaxValue", AsnEncodingRules.DER, "0484FFFFFFFF")]
        [InlineData("BER padded uint.MaxValue", AsnEncodingRules.BER, "048800000000FFFFFFFF")]
        [InlineData("BER 5 byte spread", AsnEncodingRules.BER, "04850100000000")]
        [InlineData("CER 5 byte spread", AsnEncodingRules.CER, "04850100000000")]
        [InlineData("DER 5 byte spread", AsnEncodingRules.DER, "04850100000000")]
        [InlineData("BER padded 5 byte spread", AsnEncodingRules.BER, "0486000100000000")]
        public static void InvalidLengths(
            string description,
            AsnEncodingRules rules,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();

            ReadInvalid(inputData, rules);
        }

        [Theory]
        [InlineData("CER definite constructed", AsnEncodingRules.CER, 0x0500, 4, "30820500")]
        [InlineData("BER indefinite primitive", AsnEncodingRules.BER, null, 2, "0480" + "0000")]
        [InlineData("CER indefinite primitive", AsnEncodingRules.CER, null, 2, "0480" + "0000")]
        public static void ContextuallyInvalidLengths(
            string description,
            AsnEncodingRules rules,
            int? expectedLength,
            int expectedBytesRead,
            string inputHex)
        {
            // These inputs will all throw from AsnDecoder.ReadTagAndLength, but require
            // the tag as context.

            _ = description;
            byte[] inputData = inputHex.HexToByteArray();

            ReadValid(inputData, rules, expectedLength, expectedBytesRead);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        public static void IndefiniteLength(AsnEncodingRules ruleSet)
        {
            // SEQUENCE (indefinite)
            //   NULL
            //   End-of-Contents
            byte[] data = { 0x30, 0x80, 0x05, 0x00, 0x00, 0x00 };

            ReadValid(data, ruleSet, null, 2);
        }

        [Theory]
        [InlineData(0, "0483000000")]
        [InlineData(1, "048A00000000000000000001")]
        [InlineData(128, "049000000000000000000000000000000080")]
        public static void BerNonMinimalLength(int expectedLength, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();

            ReadValid(inputData, AsnEncodingRules.BER, expectedLength);
            ReadInvalid(inputData, AsnEncodingRules.CER);
            ReadInvalid(inputData, AsnEncodingRules.DER);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, 5, "0483000000" + "0500")]
        [InlineData(AsnEncodingRules.DER, 1, 2, "0101" + "FF")]
        [InlineData(AsnEncodingRules.CER, null, 2, "3080" + "0500" + "0000")]
        public static void ReadWithDataRemaining(
            AsnEncodingRules ruleSet,
            int? expectedLength,
            int expectedBytesRead,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();

            ReadValid(inputData, ruleSet, expectedLength, expectedBytesRead);
        }
    }
}
