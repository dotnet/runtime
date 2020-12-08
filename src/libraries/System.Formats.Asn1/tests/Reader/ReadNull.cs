// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadNull
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, "0500")]
        [InlineData(AsnEncodingRules.CER, "0500")]
        [InlineData(AsnEncodingRules.DER, "0500")]
        [InlineData(AsnEncodingRules.BER, "0583000000")]
        public static void ReadNull_Success(AsnEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            reader.ReadNull();
            Assert.False(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData("Long length", AsnEncodingRules.CER, "0583000000")]
        [InlineData("Long length", AsnEncodingRules.DER, "0583000000")]
        [InlineData("Constructed definite length", AsnEncodingRules.BER, "2500")]
        [InlineData("Constructed definite length", AsnEncodingRules.DER, "2500")]
        [InlineData("Constructed indefinite length", AsnEncodingRules.BER, "25800000")]
        [InlineData("Constructed indefinite length", AsnEncodingRules.CER, "25800000")]
        [InlineData("No length", AsnEncodingRules.BER, "05")]
        [InlineData("No length", AsnEncodingRules.CER, "05")]
        [InlineData("No length", AsnEncodingRules.DER, "05")]
        [InlineData("No data", AsnEncodingRules.BER, "")]
        [InlineData("No data", AsnEncodingRules.CER, "")]
        [InlineData("No data", AsnEncodingRules.DER, "")]
        [InlineData("NonEmpty", AsnEncodingRules.BER, "050100")]
        [InlineData("NonEmpty", AsnEncodingRules.CER, "050100")]
        [InlineData("NonEmpty", AsnEncodingRules.DER, "050100")]
        [InlineData("Incomplete length", AsnEncodingRules.BER, "0581")]
        public static void ReadNull_Throws(string description, AsnEncodingRules ruleSet, string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadNull());
        }


        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 5, 0 };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNull(new Asn1Tag(UniversalTagNumber.Integer)));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadNull(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            reader.ReadNull();
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 0 };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNull(new Asn1Tag(UniversalTagNumber.Integer)));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadNull());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(() => reader.ReadNull(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(() => reader.ReadNull(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            reader.ReadNull(new Asn1Tag(TagClass.ContextSpecific, 7));
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0500", TagClass.Universal, 5)]
        [InlineData(AsnEncodingRules.CER, "0500", TagClass.Universal, 5)]
        [InlineData(AsnEncodingRules.DER, "0500", TagClass.Universal, 5)]
        [InlineData(AsnEncodingRules.BER, "8000", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C00", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A4600", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);
            reader.ReadNull(new Asn1Tag(tagClass, tagValue, true));
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);
            reader.ReadNull(new Asn1Tag(tagClass, tagValue, false));
            Assert.False(reader.HasData);
        }
    }
}
