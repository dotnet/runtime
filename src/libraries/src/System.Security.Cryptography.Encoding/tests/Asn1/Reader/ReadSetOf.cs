// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadSetOf : Asn1ReaderTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER, "3100", false, -1)]
        [InlineData(PublicEncodingRules.BER, "31800000", false, -1)]
        [InlineData(PublicEncodingRules.BER, "3183000000", false, -1)]
        [InlineData(PublicEncodingRules.CER, "31800000", false, -1)]
        [InlineData(PublicEncodingRules.DER, "3100", false, -1)]
        [InlineData(PublicEncodingRules.BER, "3100" + "0500", true, -1)]
        [InlineData(PublicEncodingRules.BER, "3102" + "0500", false, 5)]
        [InlineData(PublicEncodingRules.CER, "3180" + "0500" + "0000", false, 5)]
        [InlineData(PublicEncodingRules.CER, "3180" + "010100" + "0000" + "0500", true, 1)]
        [InlineData(PublicEncodingRules.CER, "3180" + "010100" + "0101FF" + "0500" + "0000", false, 1)]
        [InlineData(PublicEncodingRules.DER, "3105" + "0101FF" + "0500", false, 1)]
        public static void ReadSetOf_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            bool expectDataRemaining,
            int expectedSequenceTagNumber)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            AsnReader sequence = reader.ReadSetOf();

            if (expectDataRemaining)
            {
                Assert.True(reader.HasData, "reader.HasData");
            }
            else
            {
                Assert.False(reader.HasData, "reader.HasData");
            }

            if (expectedSequenceTagNumber < 0)
            {
                Assert.False(sequence.HasData, "sequence.HasData");
            }
            else
            {
                Assert.True(sequence.HasData, "sequence.HasData");

                Asn1Tag firstTag = sequence.PeekTag();
                Assert.Equal(expectedSequenceTagNumber, firstTag.TagValue);
            }
        }

        [Theory]
        [InlineData("Empty", PublicEncodingRules.BER, "")]
        [InlineData("Empty", PublicEncodingRules.CER, "")]
        [InlineData("Empty", PublicEncodingRules.DER, "")]
        [InlineData("Incomplete Tag", PublicEncodingRules.BER, "1F")]
        [InlineData("Incomplete Tag", PublicEncodingRules.CER, "1F")]
        [InlineData("Incomplete Tag", PublicEncodingRules.DER, "1F")]
        [InlineData("Missing Length", PublicEncodingRules.BER, "31")]
        [InlineData("Missing Length", PublicEncodingRules.CER, "31")]
        [InlineData("Missing Length", PublicEncodingRules.DER, "31")]
        [InlineData("Primitive Encoding", PublicEncodingRules.BER, "1100")]
        [InlineData("Primitive Encoding", PublicEncodingRules.CER, "1100")]
        [InlineData("Primitive Encoding", PublicEncodingRules.DER, "1100")]
        [InlineData("Definite Length Encoding", PublicEncodingRules.CER, "3100")]
        [InlineData("Indefinite Length Encoding", PublicEncodingRules.DER, "3180" + "0000")]
        [InlineData("Missing Content", PublicEncodingRules.BER, "3101")]
        [InlineData("Missing Content", PublicEncodingRules.DER, "3101")]
        [InlineData("Length Out Of Bounds", PublicEncodingRules.BER, "3105" + "010100")]
        [InlineData("Length Out Of Bounds", PublicEncodingRules.DER, "3105" + "010100")]
        [InlineData("Missing Content - Indefinite", PublicEncodingRules.BER, "3180")]
        [InlineData("Missing Content - Indefinite", PublicEncodingRules.CER, "3180")]
        [InlineData("Missing EoC", PublicEncodingRules.BER, "3180" + "010100")]
        [InlineData("Missing EoC", PublicEncodingRules.CER, "3180" + "010100")]
        [InlineData("Missing Outer EoC", PublicEncodingRules.BER, "3180" + "010100" + ("3180" + "0000"))]
        [InlineData("Missing Outer EoC", PublicEncodingRules.CER, "3180" + "010100" + ("3180" + "0000"))]
        [InlineData("Wrong Tag - Definite", PublicEncodingRules.BER, "3000")]
        [InlineData("Wrong Tag - Definite", PublicEncodingRules.DER, "3000")]
        [InlineData("Wrong Tag - Indefinite", PublicEncodingRules.BER, "3080" + "0000")]
        [InlineData("Wrong Tag - Indefinite", PublicEncodingRules.CER, "3080" + "0000")]
        public static void ReadSetOf_Throws(
            string description,
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadSetOf());
        }

        [Theory]
        // BER can read out of order (indefinite)
        [InlineData(PublicEncodingRules.BER, "3180" + "0101FF" + "010100" + "0000", true, 1)]
        // BER can read out of order (definite)
        [InlineData(PublicEncodingRules.BER, "3106" + "0101FF" + "010100", true, 1)]
        // CER will not read out of order
        [InlineData(PublicEncodingRules.CER, "3180" + "0500" + "010100" + "0000", false, 1)]
        [InlineData(PublicEncodingRules.CER, "3180" + "0101FF" + "010100" + "0000", false, 1)]
        // CER is happy in order:
        [InlineData(PublicEncodingRules.CER, "3180" + "010100" + "0500" + "0000", true, 5)]
        [InlineData(PublicEncodingRules.CER, "3180" + "010100" + "0101FF" + "0500" + "0000", true, 5)]
        [InlineData(PublicEncodingRules.CER, "3180" + "010100" + "010100" + "0500" + "0000", true, 5)]
        // DER will not read out of order
        [InlineData(PublicEncodingRules.DER, "3106" + "0101FF" + "010100", false, 1)]
        [InlineData(PublicEncodingRules.DER, "3105" + "0500" + "010100", false, 1)]
        // DER is happy in order:
        [InlineData(PublicEncodingRules.DER, "3105" + "010100" + "0500", true, 5)]
        [InlineData(PublicEncodingRules.DER, "3108" + "010100" + "0101FF" + "0500", true, 5)]
        [InlineData(PublicEncodingRules.DER, "3108" + "010100" + "010100" + "0500", true, 5)]
        public static void ReadSetOf_DataSorting(
            PublicEncodingRules ruleSet,
            string inputHex,
            bool expectSuccess,
            int lastTagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            AsnReader setOf;

            if (expectSuccess)
            {
                setOf = reader.ReadSetOf();
            }
            else
            {
                AsnReader alsoReader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

                Assert.Throws<CryptographicException>(() => alsoReader.ReadSetOf());

                setOf = reader.ReadSetOf(skipSortOrderValidation: true);
            }

            int lastTag = -1;

            while (setOf.HasData)
            {
                Asn1Tag tag = setOf.PeekTag();
                lastTag = tag.TagValue;

                // Ignore the return, just drain it.
                setOf.GetEncodedValue();
            }

            Assert.Equal(lastTagValue, lastTag);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal_Definite(PublicEncodingRules ruleSet)
        {
            byte[] inputData = "31020500".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSetOf();
            Assert.Equal("0500", seq.GetEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        public static void TagMustBeCorrect_Universal_Indefinite(PublicEncodingRules ruleSet)
        {
            byte[] inputData = "318005000000".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSetOf();
            Assert.Equal("0500", seq.GetEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom_Definite(PublicEncodingRules ruleSet)
        {
            byte[] inputData = "A5020500".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadSetOf());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSetOf(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.GetEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        public static void TagMustBeCorrect_Custom_Indefinite(PublicEncodingRules ruleSet)
        {
            byte[] inputData = "A58005000000".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadSetOf());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSetOf(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.GetEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "31030101FF", PublicTagClass.Universal, 17)]
        [InlineData(PublicEncodingRules.BER, "31800101000000", PublicTagClass.Universal, 17)]
        [InlineData(PublicEncodingRules.CER, "31800101000000", PublicTagClass.Universal, 17)]
        [InlineData(PublicEncodingRules.DER, "31030101FF", PublicTagClass.Universal, 17)]
        [InlineData(PublicEncodingRules.BER, "A0030101FF", PublicTagClass.ContextSpecific, 0)]
        [InlineData(PublicEncodingRules.BER, "A1800101000000", PublicTagClass.ContextSpecific, 1)]
        [InlineData(PublicEncodingRules.CER, "6C800101000000", PublicTagClass.Application, 12)]
        [InlineData(PublicEncodingRules.DER, "FF8A46030101FF", PublicTagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            PublicEncodingRules ruleSet,
            string inputHex,
            PublicTagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AsnReader val1 = reader.ReadSetOf(new Asn1Tag((TagClass)tagClass, tagValue, true));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AsnReader val2 = reader.ReadSetOf(new Asn1Tag((TagClass)tagClass, tagValue, false));

            Assert.False(reader.HasData);

            Assert.Equal(val1.GetEncodedValue().ByteArrayToHex(), val2.GetEncodedValue().ByteArrayToHex());
        }
    }
}
