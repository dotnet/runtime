// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadSequence
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, "3000", false, -1)]
        [InlineData(AsnEncodingRules.BER, "30800000", false, -1)]
        [InlineData(AsnEncodingRules.BER, "3083000000", false, -1)]
        [InlineData(AsnEncodingRules.CER, "30800000", false, -1)]
        [InlineData(AsnEncodingRules.DER, "3000", false, -1)]
        [InlineData(AsnEncodingRules.BER, "3000" + "0500", true, -1)]
        [InlineData(AsnEncodingRules.BER, "3002" + "0500", false, 5)]
        [InlineData(AsnEncodingRules.CER, "3080" + "0500" + "0000", false, 5)]
        [InlineData(AsnEncodingRules.CER, "3080" + "010100" + "0000" + "0500", true, 1)]
        [InlineData(AsnEncodingRules.DER, "3005" + "0500" + "0101FF", false, 5)]
        public static void ReadSequence_Success(
            AsnEncodingRules ruleSet,
            string inputHex,
            bool expectDataRemaining,
            int expectedSequenceTagNumber)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, ruleSet);
            AsnReader sequence = reader.ReadSequence();

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
        [InlineData("Missing Length", AsnEncodingRules.BER, "30")]
        [InlineData("Missing Length", AsnEncodingRules.CER, "30")]
        [InlineData("Missing Length", AsnEncodingRules.DER, "30")]
        [InlineData("Primitive Encoding", AsnEncodingRules.BER, "1000")]
        [InlineData("Primitive Encoding", AsnEncodingRules.CER, "1000")]
        [InlineData("Primitive Encoding", AsnEncodingRules.DER, "1000")]
        [InlineData("Definite Length Encoding", AsnEncodingRules.CER, "3000")]
        [InlineData("Indefinite Length Encoding", AsnEncodingRules.DER, "3080" + "0000")]
        [InlineData("Missing Content", AsnEncodingRules.BER, "3001")]
        [InlineData("Missing Content", AsnEncodingRules.DER, "3001")]
        [InlineData("Length Out Of Bounds", AsnEncodingRules.BER, "3005" + "010100")]
        [InlineData("Length Out Of Bounds", AsnEncodingRules.DER, "3005" + "010100")]
        [InlineData("Missing Content - Indefinite", AsnEncodingRules.BER, "3080")]
        [InlineData("Missing Content - Indefinite", AsnEncodingRules.CER, "3080")]
        [InlineData("Missing EoC", AsnEncodingRules.BER, "3080" + "010100")]
        [InlineData("Missing EoC", AsnEncodingRules.CER, "3080" + "010100")]
        [InlineData("Missing Outer EoC", AsnEncodingRules.BER, "3080" + "010100" + ("3080" + "0000"))]
        [InlineData("Missing Outer EoC", AsnEncodingRules.CER, "3080" + "010100" + ("3080" + "0000"))]
        [InlineData("Wrong Tag - Definite", AsnEncodingRules.BER, "3100")]
        [InlineData("Wrong Tag - Definite", AsnEncodingRules.DER, "3100")]
        [InlineData("Wrong Tag - Indefinite", AsnEncodingRules.BER, "3180" + "0000")]
        [InlineData("Wrong Tag - Indefinite", AsnEncodingRules.CER, "3180" + "0000")]
        public static void ReadSequence_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadSequence());
        }

        private static void ReadEcPublicKey(AsnEncodingRules ruleSet, byte[] inputData)
        {
            AsnReader mainReader = new AsnReader(inputData, ruleSet);

            AsnReader spkiReader = mainReader.ReadSequence();
            Assert.False(mainReader.HasData, "mainReader.HasData after reading SPKI");

            AsnReader algorithmReader = spkiReader.ReadSequence();
            Assert.True(spkiReader.HasData, "spkiReader.HasData after reading algorithm");

            ReadOnlyMemory<byte> publicKeyValue;
            int unusedBitCount;

            if (!spkiReader.TryReadPrimitiveBitString(out unusedBitCount, out publicKeyValue))
            {
                // The correct answer is 65 bytes.
                for (int i = 10; ; i *= 2)
                {
                    byte[] buf = new byte[i];

                    if (spkiReader.TryReadBitString(buf, out unusedBitCount, out int bytesWritten))
                    {
                        publicKeyValue = new ReadOnlyMemory<byte>(buf, 0, bytesWritten);
                        break;
                    }
                }
            }

            Assert.False(spkiReader.HasData, "spkiReader.HasData after reading subjectPublicKey");
            Assert.True(algorithmReader.HasData, "algorithmReader.HasData before reading");

            string algorithmOid = algorithmReader.ReadObjectIdentifier();
            Assert.True(algorithmReader.HasData, "algorithmReader.HasData after reading first OID");

            Assert.Equal("1.2.840.10045.2.1", algorithmOid);

            string curveOid = algorithmReader.ReadObjectIdentifier();
            Assert.False(algorithmReader.HasData, "algorithmReader.HasData after reading second OID");

            Assert.Equal("1.2.840.10045.3.1.7", curveOid);

            const string PublicKeyValue =
                "04" +
                "2363DD131DA65E899A2E63E9E05E50C830D4994662FFE883DB2B9A767DCCABA2" +
                "F07081B5711BE1DEE90DFC8DE17970C2D937A16CD34581F52B8D59C9E9532D13";

            Assert.Equal(PublicKeyValue, publicKeyValue.ByteArrayToHex());
            Assert.Equal(0, unusedBitCount);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEcPublicKey_DefiniteLength(AsnEncodingRules ruleSet)
        {
            const string InputHex =
                "3059" +
                  "3013" +
                    "06072A8648CE3D0201" +
                    "06082A8648CE3D030107" +
                  "0342" +
                    "00" +
                    "04" +
                    "2363DD131DA65E899A2E63E9E05E50C830D4994662FFE883DB2B9A767DCCABA2" +
                    "F07081B5711BE1DEE90DFC8DE17970C2D937A16CD34581F52B8D59C9E9532D13";

            byte[] inputData = InputHex.HexToByteArray();
            ReadEcPublicKey(ruleSet, inputData);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        public static void ReadEcPublicKey_IndefiniteLength(AsnEncodingRules ruleSet)
        {
            const string InputHex =
                "3080" +
                  "3080" +
                    "06072A8648CE3D0201" +
                    "06082A8648CE3D030107" +
                    "0000" +
                  "0342" +
                    "00" +
                    "04" +
                    "2363DD131DA65E899A2E63E9E05E50C830D4994662FFE883DB2B9A767DCCABA2" +
                    "F07081B5711BE1DEE90DFC8DE17970C2D937A16CD34581F52B8D59C9E9532D13" +
                  "0000";

            byte[] inputData = InputHex.HexToByteArray();
            ReadEcPublicKey(ruleSet, inputData);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal_Definite(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "30020500".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSequence();
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        public static void TagMustBeCorrect_Universal_Indefinite(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "308005000000".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSequence();
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
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadSequence());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 5));
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
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadSequence());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "30030101FF", TagClass.Universal, 16)]
        [InlineData(AsnEncodingRules.BER, "30800101000000", TagClass.Universal, 16)]
        [InlineData(AsnEncodingRules.CER, "30800101000000", TagClass.Universal, 16)]
        [InlineData(AsnEncodingRules.DER, "30030101FF", TagClass.Universal, 16)]
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

            AsnReader val1 = reader.ReadSequence(new Asn1Tag(tagClass, tagValue, true));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            AsnReader val2 = reader.ReadSequence(new Asn1Tag(tagClass, tagValue, false));

            Assert.False(reader.HasData);

            Assert.Equal(val1.ReadEncodedValue().ByteArrayToHex(), val2.ReadEncodedValue().ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadSequenceOf_PreservesOptions(AsnEncodingRules ruleSet)
        {
            // [5] (UtcTime) 500102123456Z
            // UtcTime 120102235959Z
            //
            // They're sorted backwards, though.
            const string PayloadHex =
                "850D3530303130323132333435365A" +
                "170D3132303130323233353935395A";

            byte[] inputData;

            // Build the rule-specific form of SEQUENCE { [PRIVATE 9] SEQUENCE { SET-OF { dates }, NULL } }
            // The outer Set-Of is also invalid, because the NULL should be first.
            if (ruleSet == AsnEncodingRules.DER)
            {
                inputData = ("3024" + "E922" + "A21E" + PayloadHex + "0500").HexToByteArray();
            }
            else
            {
                string inputHex = "3080" + "E980" + "A280" + PayloadHex + "0000" + "0500" + "0000" + "0000";
                inputData = inputHex.HexToByteArray();
            }

            AsnReaderOptions options = new AsnReaderOptions
            {
                SkipSetSortOrderVerification = true,
                UtcTimeTwoDigitYearMax = 2011,
            };

            AsnReader initial = new AsnReader(inputData, ruleSet, options);
            AsnReader outer = initial.ReadSequence();
            Assert.False(initial.HasData);
            AsnReader inner = outer.ReadSequence(new Asn1Tag(TagClass.Private, 9));
            Assert.False(outer.HasData);

            Asn1Tag setTag = new Asn1Tag(TagClass.ContextSpecific, 2);

            if (ruleSet != AsnEncodingRules.BER)
            {
                Assert.Throws<AsnContentException>(() => inner.ReadSetOf(false, setTag));
            }

            // This confirms that we've passed SkipSetOrderVerification this far.
            AsnReader setOf = inner.ReadSetOf(setTag);
            Assert.True(inner.HasData);

            Assert.Equal(
                new DateTimeOffset(1950, 1, 2, 12, 34, 56, TimeSpan.Zero),
                setOf.ReadUtcTime(new Asn1Tag(TagClass.ContextSpecific, 5)));

            // This confirms that we've passed UtcTimeTwoDigitYearMax,
            // the default would call this 2012.
            Assert.Equal(
                new DateTimeOffset(1912, 1, 2, 23, 59, 59, TimeSpan.Zero),
                setOf.ReadUtcTime());

            Assert.False(setOf.HasData);

            inner.ReadNull();
            Assert.False(inner.HasData);

            setOf.ThrowIfNotEmpty();
            inner.ThrowIfNotEmpty();
            outer.ThrowIfNotEmpty();
            initial.ThrowIfNotEmpty();
        }
    }
}
