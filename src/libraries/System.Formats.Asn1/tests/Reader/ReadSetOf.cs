// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadSetOf
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, "3100", false, -1)]
        [InlineData(AsnEncodingRules.BER, "31800000", false, -1)]
        [InlineData(AsnEncodingRules.BER, "3183000000", false, -1)]
        [InlineData(AsnEncodingRules.CER, "31800000", false, -1)]
        [InlineData(AsnEncodingRules.DER, "3100", false, -1)]
        [InlineData(AsnEncodingRules.BER, "3100" + "0500", true, -1)]
        [InlineData(AsnEncodingRules.BER, "3102" + "0500", false, 5)]
        [InlineData(AsnEncodingRules.CER, "3180" + "0500" + "0000", false, 5)]
        [InlineData(AsnEncodingRules.CER, "3180" + "010100" + "0000" + "0500", true, 1)]
        [InlineData(AsnEncodingRules.CER, "3180" + "010100" + "0101FF" + "0500" + "0000", false, 1)]
        [InlineData(AsnEncodingRules.DER, "3105" + "0101FF" + "0500", false, 1)]
        public static void ReadSetOf_Success(
            AsnEncodingRules ruleSet,
            string inputHex,
            bool expectDataRemaining,
            int expectedSequenceTagNumber)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
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
        [InlineData("Empty", AsnEncodingRules.BER, "")]
        [InlineData("Empty", AsnEncodingRules.CER, "")]
        [InlineData("Empty", AsnEncodingRules.DER, "")]
        [InlineData("Incomplete Tag", AsnEncodingRules.BER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.CER, "1F")]
        [InlineData("Incomplete Tag", AsnEncodingRules.DER, "1F")]
        [InlineData("Missing Length", AsnEncodingRules.BER, "31")]
        [InlineData("Missing Length", AsnEncodingRules.CER, "31")]
        [InlineData("Missing Length", AsnEncodingRules.DER, "31")]
        [InlineData("Primitive Encoding", AsnEncodingRules.BER, "1100")]
        [InlineData("Primitive Encoding", AsnEncodingRules.CER, "1100")]
        [InlineData("Primitive Encoding", AsnEncodingRules.DER, "1100")]
        [InlineData("Definite Length Encoding", AsnEncodingRules.CER, "3100")]
        [InlineData("Indefinite Length Encoding", AsnEncodingRules.DER, "3180" + "0000")]
        [InlineData("Missing Content", AsnEncodingRules.BER, "3101")]
        [InlineData("Missing Content", AsnEncodingRules.DER, "3101")]
        [InlineData("Length Out Of Bounds", AsnEncodingRules.BER, "3105" + "010100")]
        [InlineData("Length Out Of Bounds", AsnEncodingRules.DER, "3105" + "010100")]
        [InlineData("Missing Content - Indefinite", AsnEncodingRules.BER, "3180")]
        [InlineData("Missing Content - Indefinite", AsnEncodingRules.CER, "3180")]
        [InlineData("Missing EoC", AsnEncodingRules.BER, "3180" + "010100")]
        [InlineData("Missing EoC", AsnEncodingRules.CER, "3180" + "010100")]
        [InlineData("Missing Outer EoC", AsnEncodingRules.BER, "3180" + "010100" + ("3180" + "0000"))]
        [InlineData("Missing Outer EoC", AsnEncodingRules.CER, "3180" + "010100" + ("3180" + "0000"))]
        [InlineData("Wrong Tag - Definite", AsnEncodingRules.BER, "3000")]
        [InlineData("Wrong Tag - Definite", AsnEncodingRules.DER, "3000")]
        [InlineData("Wrong Tag - Indefinite", AsnEncodingRules.BER, "3080" + "0000")]
        [InlineData("Wrong Tag - Indefinite", AsnEncodingRules.CER, "3080" + "0000")]
        public static void ReadSetOf_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadSetOf());
        }

        [Theory]
        // BER can read out of order (indefinite)
        [InlineData(AsnEncodingRules.BER, "3180" + "0101FF" + "010100" + "0000", true, 1)]
        // BER can read out of order (definite)
        [InlineData(AsnEncodingRules.BER, "3106" + "0101FF" + "010100", true, 1)]
        // CER will not read out of order
        [InlineData(AsnEncodingRules.CER, "3180" + "0500" + "010100" + "0000", false, 1)]
        [InlineData(AsnEncodingRules.CER, "3180" + "0101FF" + "010100" + "0000", false, 1)]
        // CER is happy in order:
        [InlineData(AsnEncodingRules.CER, "3180" + "010100" + "0500" + "0000", true, 5)]
        [InlineData(AsnEncodingRules.CER, "3180" + "010100" + "0101FF" + "0500" + "0000", true, 5)]
        [InlineData(AsnEncodingRules.CER, "3180" + "010100" + "010100" + "0500" + "0000", true, 5)]
        // DER will not read out of order
        [InlineData(AsnEncodingRules.DER, "3106" + "0101FF" + "010100", false, 1)]
        [InlineData(AsnEncodingRules.DER, "3105" + "0500" + "010100", false, 1)]
        // DER is happy in order:
        [InlineData(AsnEncodingRules.DER, "3105" + "010100" + "0500", true, 5)]
        [InlineData(AsnEncodingRules.DER, "3108" + "010100" + "0101FF" + "0500", true, 5)]
        [InlineData(AsnEncodingRules.DER, "3108" + "010100" + "010100" + "0500", true, 5)]
        public static void ReadSetOf_DataSorting(
            AsnEncodingRules ruleSet,
            string inputHex,
            bool expectSuccess,
            int lastTagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            AsnReader setOf;

            AsnReader laxReader = new AsnReader(
                inputData,
                ruleSet,
                new AsnReaderOptions { SkipSetSortOrderVerification = true });

            if (expectSuccess)
            {
                setOf = reader.ReadSetOf();
            }
            else
            {
                AsnReader alsoReader = new AsnReader(inputData, ruleSet);
                Assert.Throws<AsnContentException>(() => alsoReader.ReadSetOf());
                Assert.Throws<AsnContentException>(() => laxReader.ReadSetOf(false));

                setOf = reader.ReadSetOf(skipSortOrderValidation: true);
            }

            int lastTag = -1;

            while (setOf.HasData)
            {
                Asn1Tag tag = setOf.PeekTag();
                lastTag = tag.TagValue;

                // Ignore the return, just drain it.
                setOf.ReadEncodedValue();
            }

            Assert.Equal(lastTagValue, lastTag);

            setOf = laxReader.ReadSetOf();
            lastTag = -1;

            while (setOf.HasData)
            {
                Asn1Tag tag = setOf.PeekTag();
                lastTag = tag.TagValue;

                // Ignore the return, just drain it.
                setOf.ReadEncodedValue();
            }

            Assert.Equal(lastTagValue, lastTag);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal_Definite(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "31020500".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(expectedTag: Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSetOf();
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        public static void TagMustBeCorrect_Universal_Indefinite(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "318005000000".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(expectedTag: Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSetOf();
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom_Definite(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "A5020500".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(expectedTag: Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadSetOf());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        public static void TagMustBeCorrect_Custom_Indefinite(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "A58005000000".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSetOf(expectedTag: Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadSetOf());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSetOf(expectedTag: new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "31030101FF", TagClass.Universal, 17)]
        [InlineData(AsnEncodingRules.BER, "31800101000000", TagClass.Universal, 17)]
        [InlineData(AsnEncodingRules.CER, "31800101000000", TagClass.Universal, 17)]
        [InlineData(AsnEncodingRules.DER, "31030101FF", TagClass.Universal, 17)]
        [InlineData(AsnEncodingRules.BER, "A0030101FF", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.BER, "A1800101000000", TagClass.ContextSpecific, 1)]
        [InlineData(AsnEncodingRules.CER, "6C800101000000", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "FF8A46030101FF", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AsnReader val1 = reader.ReadSetOf(expectedTag: new Asn1Tag(tagClass, tagValue, true));
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            AsnReader val2 = reader.ReadSetOf(expectedTag: new Asn1Tag(tagClass, tagValue, false));
            Assert.False(reader.HasData);

            Assert.Equal(val1.ReadEncodedValue().ByteArrayToHex(), val2.ReadEncodedValue().ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadSetOf_PreservesOptions(AsnEncodingRules ruleSet)
        {
            // [5] (UtcTime) 500102123456Z
            // UtcTime 120102235959Z
            //
            // They're sorted backwards, though.
            const string PayloadHex =
                "850D3530303130323132333435365A" +
                "170D3132303130323233353935395A";

            byte[] inputData;

            // Build the rule-specific form of SET-OF { SET-OF { dates }, NULL }
            // The outer Set-Of is also invalid, because the NULL should be first.
            if (ruleSet == AsnEncodingRules.DER)
            {
                inputData = ("3122" + "A21E" + PayloadHex + "0500").HexToByteArray();
            }
            else
            {
                inputData = ("3180" + "A280" + PayloadHex + "0000" + "0500" + "0000").HexToByteArray();
            }

            AsnReaderOptions options = new AsnReaderOptions
            {
                SkipSetSortOrderVerification = true,
                UtcTimeTwoDigitYearMax = 2011,
            };

            AsnReader initial = new AsnReader(inputData, ruleSet, options);

            if (ruleSet != AsnEncodingRules.BER)
            {
                Assert.Throws<AsnContentException>(() => initial.ReadSetOf(false));
            }

            AsnReader outer = initial.ReadSetOf();
            Assert.False(initial.HasData);

            Asn1Tag innerTag = new Asn1Tag(TagClass.ContextSpecific, 2);

            if (ruleSet != AsnEncodingRules.BER)
            {
                Assert.Throws<AsnContentException>(() => outer.ReadSetOf(false, innerTag));
            }

            // This confirms that we've passed SkipSetOrderVerification this far.
            AsnReader inner = outer.ReadSetOf(innerTag);
            Assert.True(outer.HasData);

            Assert.Equal(
                new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero),
                inner.ReadUtcTime(new Asn1Tag(TagClass.ContextSpecific, 5)));

            // This confirms that we've passed UtcTimeTwoDigitYearMax,
            // the default would call this 2012.
            Assert.Equal(
                new DateTimeOffset(1912, 1, 2, 23, 59, 59, TimeSpan.Zero),
                inner.ReadUtcTime());

            Assert.False(inner.HasData);

            outer.ReadNull();
            Assert.False(outer.HasData);

            inner.ThrowIfNotEmpty();
            outer.ThrowIfNotEmpty();
            initial.ThrowIfNotEmpty();
        }
    }
}
