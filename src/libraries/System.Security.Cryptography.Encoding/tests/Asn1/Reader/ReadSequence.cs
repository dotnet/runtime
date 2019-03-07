// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadSequence : Asn1ReaderTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER, "3000", false, -1)]
        [InlineData(PublicEncodingRules.BER, "30800000", false, -1)]
        [InlineData(PublicEncodingRules.BER, "3083000000", false, -1)]
        [InlineData(PublicEncodingRules.CER, "30800000", false, -1)]
        [InlineData(PublicEncodingRules.DER, "3000", false, -1)]
        [InlineData(PublicEncodingRules.BER, "3000" + "0500", true, -1)]
        [InlineData(PublicEncodingRules.BER, "3002" + "0500", false, 5)]
        [InlineData(PublicEncodingRules.CER, "3080" + "0500" + "0000", false, 5)]
        [InlineData(PublicEncodingRules.CER, "3080" + "010100" + "0000" + "0500", true, 1)]
        [InlineData(PublicEncodingRules.DER, "3005" + "0500" + "0101FF", false, 5)]
        public static void ReadSequence_Success(
            PublicEncodingRules ruleSet,
            string inputHex,
            bool expectDataRemaining,
            int expectedSequenceTagNumber)
        {
            byte[] inputData = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);
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
        [InlineData("Empty", PublicEncodingRules.BER, "")]
        [InlineData("Empty", PublicEncodingRules.CER, "")]
        [InlineData("Empty", PublicEncodingRules.DER, "")]
        [InlineData("Incomplete Tag", PublicEncodingRules.BER, "1F")]
        [InlineData("Incomplete Tag", PublicEncodingRules.CER, "1F")]
        [InlineData("Incomplete Tag", PublicEncodingRules.DER, "1F")]
        [InlineData("Missing Length", PublicEncodingRules.BER, "30")]
        [InlineData("Missing Length", PublicEncodingRules.CER, "30")]
        [InlineData("Missing Length", PublicEncodingRules.DER, "30")]
        [InlineData("Primitive Encoding", PublicEncodingRules.BER, "1000")]
        [InlineData("Primitive Encoding", PublicEncodingRules.CER, "1000")]
        [InlineData("Primitive Encoding", PublicEncodingRules.DER, "1000")]
        [InlineData("Definite Length Encoding", PublicEncodingRules.CER, "3000")]
        [InlineData("Indefinite Length Encoding", PublicEncodingRules.DER, "3080" + "0000")]
        [InlineData("Missing Content", PublicEncodingRules.BER, "3001")]
        [InlineData("Missing Content", PublicEncodingRules.DER, "3001")]
        [InlineData("Length Out Of Bounds", PublicEncodingRules.BER, "3005" + "010100")]
        [InlineData("Length Out Of Bounds", PublicEncodingRules.DER, "3005" + "010100")]
        [InlineData("Missing Content - Indefinite", PublicEncodingRules.BER, "3080")]
        [InlineData("Missing Content - Indefinite", PublicEncodingRules.CER, "3080")]
        [InlineData("Missing EoC", PublicEncodingRules.BER, "3080" + "010100")]
        [InlineData("Missing EoC", PublicEncodingRules.CER, "3080" + "010100")]
        [InlineData("Missing Outer EoC", PublicEncodingRules.BER, "3080" + "010100" + ("3080" + "0000"))]
        [InlineData("Missing Outer EoC", PublicEncodingRules.CER, "3080" + "010100" + ("3080" + "0000"))]
        [InlineData("Wrong Tag - Definite", PublicEncodingRules.BER, "3100")]
        [InlineData("Wrong Tag - Definite", PublicEncodingRules.DER, "3100")]
        [InlineData("Wrong Tag - Indefinite", PublicEncodingRules.BER, "3180" + "0000")]
        [InlineData("Wrong Tag - Indefinite", PublicEncodingRules.CER, "3180" + "0000")]
        public static void ReadSequence_Throws(
            string description,
            PublicEncodingRules ruleSet,
            string inputHex)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(() => reader.ReadSequence());
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

            if (!spkiReader.TryReadPrimitiveBitStringValue(out unusedBitCount, out publicKeyValue))
            {
                // The correct answer is 65 bytes.
                for (int i = 10; ; i *= 2)
                {
                    byte[] buf = new byte[i];

                    if (spkiReader.TryCopyBitStringBytes(buf, out unusedBitCount, out int bytesWritten))
                    {
                        publicKeyValue = new ReadOnlyMemory<byte>(buf, 0, bytesWritten);
                        break;
                    }
                }
            }

            Assert.False(spkiReader.HasData, "spkiReader.HasData after reading subjectPublicKey");
            Assert.True(algorithmReader.HasData, "algorithmReader.HasData before reading");

            Oid algorithmOid = algorithmReader.ReadObjectIdentifier();
            Assert.True(algorithmReader.HasData, "algorithmReader.HasData after reading first OID");

            Assert.Equal("1.2.840.10045.2.1", algorithmOid.Value);

            Oid curveOid = algorithmReader.ReadObjectIdentifier();
            Assert.False(algorithmReader.HasData, "algorithmReader.HasData after reading second OID");

            Assert.Equal("1.2.840.10045.3.1.7", curveOid.Value);

            const string PublicKeyValue =
                "04" +
                "2363DD131DA65E899A2E63E9E05E50C830D4994662FFE883DB2B9A767DCCABA2" +
                "F07081B5711BE1DEE90DFC8DE17970C2D937A16CD34581F52B8D59C9E9532D13";

            Assert.Equal(PublicKeyValue, publicKeyValue.ByteArrayToHex());
            Assert.Equal(0, unusedBitCount);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadEcPublicKey_DefiniteLength(PublicEncodingRules ruleSet)
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
            ReadEcPublicKey((AsnEncodingRules)ruleSet, inputData);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        public static void ReadEcPublicKey_IndefiniteLength(PublicEncodingRules ruleSet)
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
            ReadEcPublicKey((AsnEncodingRules)ruleSet, inputData);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal_Definite(PublicEncodingRules ruleSet)
        {
            byte[] inputData = "30020500".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSequence();
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        public static void TagMustBeCorrect_Universal_Indefinite(PublicEncodingRules ruleSet)
        {
            byte[] inputData = "308005000000".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            AsnReader seq = reader.ReadSequence();
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

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
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadSequence());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

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
                () => reader.ReadSequence(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(() => reader.ReadSequence());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.Application, 5)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            AsnReader seq = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.Equal("0500", seq.ReadEncodedValue().ByteArrayToHex());

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "30030101FF", PublicTagClass.Universal, 16)]
        [InlineData(PublicEncodingRules.BER, "30800101000000", PublicTagClass.Universal, 16)]
        [InlineData(PublicEncodingRules.CER, "30800101000000", PublicTagClass.Universal, 16)]
        [InlineData(PublicEncodingRules.DER, "30030101FF", PublicTagClass.Universal, 16)]
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

            AsnReader val1 = reader.ReadSequence(new Asn1Tag((TagClass)tagClass, tagValue, true));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AsnReader val2 = reader.ReadSequence(new Asn1Tag((TagClass)tagClass, tagValue, false));

            Assert.False(reader.HasData);

            Assert.Equal(val1.ReadEncodedValue().ByteArrayToHex(), val2.ReadEncodedValue().ByteArrayToHex());
        }
    }
}
