// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadBoolean
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, false, 3, "010100")]
        [InlineData(AsnEncodingRules.BER, true, 3, "010101")]
        // Padded length
        [InlineData(AsnEncodingRules.BER, true, 4, "01810101")]
        [InlineData(AsnEncodingRules.BER, true, 3, "0101FF0500")]
        [InlineData(AsnEncodingRules.CER, false, 3, "0101000500")]
        [InlineData(AsnEncodingRules.CER, true, 3, "0101FF")]
        [InlineData(AsnEncodingRules.DER, false, 3, "010100")]
        [InlineData(AsnEncodingRules.DER, true, 3, "0101FF0500")]
        // Context Specific 0
        [InlineData(AsnEncodingRules.DER, true, 3, "8001FF0500")]
        // Application 31
        [InlineData(AsnEncodingRules.DER, true, 4, "5F1F01FF0500")]
        // Private 253
        [InlineData(AsnEncodingRules.CER, false, 5, "DF817D01000500")]
        public static void ReadBoolean_Success(
            AsnEncodingRules ruleSet,
            bool expectedValue,
            int expectedBytesRead,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Asn1Tag tag = reader.PeekTag();
            bool value;

            if (tag.TagClass == TagClass.Universal)
            {
                value = reader.ReadBoolean();
            }
            else
            {
                value = reader.ReadBoolean(tag);
            }

            if (inputData.Length == expectedBytesRead)
            {
                Assert.False(reader.HasData, "reader.HasData");
            }
            else
            {
                Assert.True(reader.HasData, "reader.HasData");
            }

            if (expectedValue)
            {
                Assert.True(value, "value");
            }
            else
            {
                Assert.False(value, "value");
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 1, 1, 0 };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadBoolean(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadBoolean(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            bool value = reader.ReadBoolean();
            Assert.False(value, "value");
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x80, 1, 0xFF };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadBoolean(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadBoolean());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadBoolean(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadBoolean(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            bool value = reader.ReadBoolean(new Asn1Tag(TagClass.ContextSpecific, 0));
            Assert.True(value, "value");
            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0101FF", TagClass.Universal, 1)]
        [InlineData(AsnEncodingRules.CER, "0101FF", TagClass.Universal, 1)]
        [InlineData(AsnEncodingRules.DER, "0101FF", TagClass.Universal, 1)]
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
            bool val1 = reader.ReadBoolean(new Asn1Tag(tagClass, tagValue, true));
            Assert.False(reader.HasData);
            reader = new AsnReader(inputData, ruleSet);
            bool val2 = reader.ReadBoolean(new Asn1Tag(tagClass, tagValue, false));
            Assert.False(reader.HasData);

            Assert.Equal(val1, val2);
        }

        [Theory]
        [InlineData("Empty", AsnEncodingRules.DER, "")]
        [InlineData("Empty", AsnEncodingRules.CER, "")]
        [InlineData("Empty", AsnEncodingRules.BER, "")]
        [InlineData("TagOnly", AsnEncodingRules.BER, "01")]
        [InlineData("TagOnly", AsnEncodingRules.CER, "01")]
        [InlineData("TagOnly", AsnEncodingRules.DER, "01")]
        [InlineData("MultiByte TagOnly", AsnEncodingRules.DER, "9F1F")]
        [InlineData("MultiByte TagOnly", AsnEncodingRules.CER, "9F1F")]
        [InlineData("MultiByte TagOnly", AsnEncodingRules.BER, "9F1F")]
        [InlineData("TagAndLength", AsnEncodingRules.BER, "0101")]
        [InlineData("Tag and MultiByteLength", AsnEncodingRules.BER, "01820001")]
        [InlineData("TagAndLength", AsnEncodingRules.CER, "8001")]
        [InlineData("TagAndLength", AsnEncodingRules.DER, "C001")]
        [InlineData("MultiByteTagAndLength", AsnEncodingRules.DER, "9F2001")]
        [InlineData("MultiByteTagAndLength", AsnEncodingRules.CER, "9F2001")]
        [InlineData("MultiByteTagAndLength", AsnEncodingRules.BER, "9F2001")]
        [InlineData("MultiByteTagAndMultiByteLength", AsnEncodingRules.BER, "9F28200001")]
        [InlineData("TooShort", AsnEncodingRules.BER, "0100")]
        [InlineData("TooShort", AsnEncodingRules.CER, "8000")]
        [InlineData("TooShort", AsnEncodingRules.DER, "0100")]
        [InlineData("TooLong", AsnEncodingRules.DER, "C0020000")]
        [InlineData("TooLong", AsnEncodingRules.CER, "01020000")]
        [InlineData("TooLong", AsnEncodingRules.BER, "C081020000")]
        [InlineData("MissingContents", AsnEncodingRules.BER, "C001")]
        [InlineData("MissingContents", AsnEncodingRules.CER, "0101")]
        [InlineData("MissingContents", AsnEncodingRules.DER, "8001")]
        [InlineData("NonCanonical", AsnEncodingRules.DER, "0101FE")]
        [InlineData("NonCanonical", AsnEncodingRules.CER, "800101")]
        [InlineData("Constructed", AsnEncodingRules.BER, "2103010101")]
        [InlineData("Constructed", AsnEncodingRules.CER, "2103010101")]
        [InlineData("Constructed", AsnEncodingRules.DER, "2103010101")]
        [InlineData("WrongTag", AsnEncodingRules.DER, "0400")]
        [InlineData("WrongTag", AsnEncodingRules.CER, "0400")]
        [InlineData("WrongTag", AsnEncodingRules.BER, "0400")]
        [InlineData("IndefiniteLength", AsnEncodingRules.BER, "01800101FF00")]
        [InlineData("IndefiniteLength", AsnEncodingRules.CER, "01800101FF00")]
        [InlineData("IndefiniteLength", AsnEncodingRules.DER, "01800101FF00")]
        public static void ReadBoolean_Failure(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            Asn1Tag tag = default(Asn1Tag);

            if (inputData.Length > 0)
            {
                tag = reader.PeekTag();
            }

            if (tag.TagClass == TagClass.Universal)
            {
                Assert.Throws<AsnContentException>(() => reader.ReadBoolean());
            }
            else
            {
                Assert.Throws<AsnContentException>(() => reader.ReadBoolean(tag));
            }

            if (inputData.Length == 0)
            {
                // If we started with nothing, where did the data come from?
                Assert.False(reader.HasData, "reader.HasData");
            }
            else
            {
                // Nothing should have moved
                Assert.True(reader.HasData, "reader.HasData");
            }
        }
    }
}
