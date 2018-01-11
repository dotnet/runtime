// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadNull : Asn1ReaderTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER, "0500")]
        [InlineData(PublicEncodingRules.CER, "0500")]
        [InlineData(PublicEncodingRules.DER, "0500")]
        [InlineData(PublicEncodingRules.BER, "0583000000")]
        public static void ReadNull_Success(PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            reader.ReadNull();
            Assert.False(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData("Long length", PublicEncodingRules.CER, "0583000000")]
        [InlineData("Long length", PublicEncodingRules.DER, "0583000000")]
        [InlineData("Constructed definite length", PublicEncodingRules.BER, "2500")]
        [InlineData("Constructed definite length", PublicEncodingRules.DER, "2500")]
        [InlineData("Constructed indefinite length", PublicEncodingRules.BER, "25800000")]
        [InlineData("Constructed indefinite length", PublicEncodingRules.CER, "25800000")]
        [InlineData("No length", PublicEncodingRules.BER, "05")]
        [InlineData("No length", PublicEncodingRules.CER, "05")]
        [InlineData("No length", PublicEncodingRules.DER, "05")]
        [InlineData("No data", PublicEncodingRules.BER, "")]
        [InlineData("No data", PublicEncodingRules.CER, "")]
        [InlineData("No data", PublicEncodingRules.DER, "")]
        [InlineData("NonEmpty", PublicEncodingRules.BER, "050100")]
        [InlineData("NonEmpty", PublicEncodingRules.CER, "050100")]
        [InlineData("NonEmpty", PublicEncodingRules.DER, "050100")]
        [InlineData("Incomplete length", PublicEncodingRules.BER, "0581")]
        public static void ReadNull_Throws(string description, PublicEncodingRules ruleSet, string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadNull());
        }


        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 5, 0 };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNull(new Asn1Tag(UniversalTagNumber.Integer)));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadNull(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            reader.ReadNull();
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 0 };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNull(new Asn1Tag(UniversalTagNumber.Integer)));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadNull());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(() => reader.ReadNull(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(() => reader.ReadNull(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            reader.ReadNull(new Asn1Tag(TagClass.ContextSpecific, 7));
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0500", PublicTagClass.Universal, 5)]
        [InlineData(PublicEncodingRules.CER, "0500", PublicTagClass.Universal, 5)]
        [InlineData(PublicEncodingRules.DER, "0500", PublicTagClass.Universal, 5)]
        [InlineData(PublicEncodingRules.BER, "8000", PublicTagClass.ContextSpecific, 0)]
        [InlineData(PublicEncodingRules.CER, "4C00", PublicTagClass.Application, 12)]
        [InlineData(PublicEncodingRules.DER, "DF8A4600", PublicTagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            PublicEncodingRules ruleSet,
            string inputHex,
            PublicTagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            reader.ReadNull(new Asn1Tag((TagClass)tagClass, tagValue, true));
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
            reader.ReadNull(new Asn1Tag((TagClass)tagClass, tagValue, false));
            Assert.False(reader.HasData);
        }
    }
}
