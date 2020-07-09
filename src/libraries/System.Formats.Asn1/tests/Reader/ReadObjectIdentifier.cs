// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadObjectIdentifier
    {
        [Theory]
        [InlineData("Wrong tag", AsnEncodingRules.BER, "010100")]
        [InlineData("Wrong tag", AsnEncodingRules.CER, "010100")]
        [InlineData("Wrong tag", AsnEncodingRules.DER, "010100")]
        [InlineData("Overreaching length", AsnEncodingRules.BER, "0608883703")]
        [InlineData("Overreaching length", AsnEncodingRules.CER, "0608883703")]
        [InlineData("Overreaching length", AsnEncodingRules.DER, "0608883703")]
        [InlineData("Zero length", AsnEncodingRules.BER, "0600")]
        [InlineData("Zero length", AsnEncodingRules.CER, "0600")]
        [InlineData("Zero length", AsnEncodingRules.DER, "0600")]
        [InlineData("Constructed Definite Form", AsnEncodingRules.BER, "2605" + "0603883703")]
        [InlineData("Constructed Indefinite Form", AsnEncodingRules.BER, "2680" + "0603883703" + "0000")]
        [InlineData("Constructed Indefinite Form", AsnEncodingRules.CER, "2680" + "0603883703" + "0000")]
        [InlineData("Unresolved carry-bit (first sub-identifier)", AsnEncodingRules.BER, "060188")]
        [InlineData("Unresolved carry-bit (first sub-identifier)", AsnEncodingRules.CER, "060188")]
        [InlineData("Unresolved carry-bit (first sub-identifier)", AsnEncodingRules.DER, "060188")]
        [InlineData("Unresolved carry-bit (later sub-identifier)", AsnEncodingRules.BER, "0603883781")]
        [InlineData("Unresolved carry-bit (later sub-identifier)", AsnEncodingRules.CER, "0603883781")]
        [InlineData("Unresolved carry-bit (later sub-identifier)", AsnEncodingRules.DER, "0603883781")]
        [InlineData("Sub-Identifier with leading 0x80", AsnEncodingRules.BER, "060488378001")]
        [InlineData("Sub-Identifier with leading 0x80", AsnEncodingRules.CER, "060488378001")]
        [InlineData("Sub-Identifier with leading 0x80", AsnEncodingRules.DER, "060488378001")]
        public static void ReadObjectIdentifier_Throws(
            string description,
            AsnEncodingRules ruleSet,
            string inputHex)
        {
            _ = description;
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Throws<AsnContentException>(() => reader.ReadObjectIdentifier());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0603883703", "2.999.3")]
        [InlineData(AsnEncodingRules.CER, "06028837", "2.999")]
        [InlineData(AsnEncodingRules.DER, "06068837C27B0302", "2.999.8571.3.2")]
        [InlineData(AsnEncodingRules.BER, "0603550406", "2.5.4.6")]
        [InlineData(AsnEncodingRules.CER, "06092A864886F70D010105", "1.2.840.113549.1.1.5")]
        [InlineData(AsnEncodingRules.DER, "060100", "0.0")]
        [InlineData(AsnEncodingRules.BER, "06080992268993F22C63", "0.9.2342.19200300.99")]
        [InlineData(
            AsnEncodingRules.DER,
            "0616824F83F09DA7EBCFDEE0C7A1A7B2C0948CC8F9D77603",
            // Using the rules of ITU-T-REC-X.667-201210 for 2.25.{UUID} unregistered arcs, and
            // their sample value of f81d4fae-7dec-11d0-a765-00a0c91e6bf6
            // this is
            // { joint-iso-itu-t(2) uuid(255) thatuuid(329800735698586629295641978511506172918) three(3) }
            "2.255.329800735698586629295641978511506172918.3")]
        public static void ReadObjectIdentifier_Success(
            AsnEncodingRules ruleSet,
            string inputHex,
            string expectedValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            string oidValue = reader.ReadObjectIdentifier();
            Assert.Equal(expectedValue, oidValue);
        }

        [Theory]
        // Start at a UUID as a big integer.  128 semantic bits takes 19
        // content bytes to write down. Walk it backwards to 1.
        // This uses the OID from the last case of ReadObjectIdentifierAsString_Success, but
        // without the "255" arc (therefore the initial second arc is the UUID decimal value - 80)
        [InlineData("061383F09DA7EBCFDEE0C7A1A7B2C0948CC8F9D776", "2.329800735698586629295641978511506172838")]
        // Drop the last byte, clear the high bit in the last remaining byte, secondArc = (secondArc + 80) >> 7 - 80.
        [InlineData("061283F09DA7EBCFDEE0C7A1A7B2C0948CC8F957", "2.2576568247645208041372202957121141895")]
        [InlineData("061183F09DA7EBCFDEE0C7A1A7B2C0948CC879", "2.20129439434728187823220335602508841")]
        [InlineData("061083F09DA7EBCFDEE0C7A1A7B2C0948C48", "2.157261245583813967368908871894520")]
        [InlineData("060F83F09DA7EBCFDEE0C7A1A7B2C0940C", "2.1228603481123546620069600561596")]
        [InlineData("060E83F09DA7EBCFDEE0C7A1A7B2C014", "2.9598464696277707969293754308")]
        [InlineData("060D83F09DA7EBCFDEE0C7A1A7B240", "2.74988005439669593510107376")]
        [InlineData("060C83F09DA7EBCFDEE0C7A1A732", "2.585843792497418699297634")]
        [InlineData("060B83F09DA7EBCFDEE0C7A127", "2.4576904628886083588183")]
        [InlineData("060A83F09DA7EBCFDEE0C721", "2.35757067413172527953")]
        [InlineData("060983F09DA7EBCFDEE047", "2.279352089165410295")]
        [InlineData("060883F09DA7EBCFDE60", "2.2182438196604688")]
        [InlineData("060783F09DA7EBCF5E", "2.17050298410894")]
        [InlineData("060683F09DA7EB4F", "2.133205456255")]
        [InlineData("060583F09DA76B", "2.1040667547")]
        [InlineData("060483F09D27", "2.8130135")]
        [InlineData("060383F01D", "2.63437")]
        [InlineData("06028370", "2.416")]
        [InlineData("060103", "0.3")]
        public static void VerifyMultiByteParsing(string inputHex, string expectedValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, AsnEncodingRules.DER);

            string oidValue = reader.ReadObjectIdentifier();
            Assert.Equal(expectedValue, oidValue);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "06028837".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadIntegerBytes(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.Equal("2.999", reader.ReadObjectIdentifier());
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "87028837".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadIntegerBytes(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(() => reader.ReadObjectIdentifier());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadObjectIdentifier(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.Equal(
                "2.999",
                reader.ReadObjectIdentifier(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "06028837", TagClass.Universal, 6)]
        [InlineData(AsnEncodingRules.CER, "06028837", TagClass.Universal, 6)]
        [InlineData(AsnEncodingRules.DER, "06028837", TagClass.Universal, 6)]
        [InlineData(AsnEncodingRules.BER, "80028837", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C028837", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A46028837", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            Asn1Tag constructedTag = new Asn1Tag(tagClass, tagValue, true);
            Asn1Tag primitiveTag = new Asn1Tag(tagClass, tagValue, false);
            AsnReader reader = new AsnReader(inputData, ruleSet);

            string val1 = reader.ReadObjectIdentifier(constructedTag);
            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            string val2 = reader.ReadObjectIdentifier(primitiveTag);
            Assert.False(reader.HasData);

            Assert.Equal(val1, val2);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadVeryLongOid(AsnEncodingRules ruleSet)
        {
            byte[] inputData = new byte[100000];
            // 06 83 02 00 00 (OBJECT IDENTIFIER, 65536 bytes).
            inputData[0] = 0x06;
            inputData[1] = 0x83;
            inputData[2] = 0x01;
            inputData[3] = 0x00;
            inputData[4] = 0x00;
            // and the rest are all zero.

            // The first byte produces "0.0". Each of the remaining 65535 bytes produce
            // another ".0".
            const int ExpectedLength = 65536 * 2 + 1;
            StringBuilder builder = new StringBuilder(ExpectedLength);
            builder.Append('0');

            for (int i = 0; i <= ushort.MaxValue; i++)
            {
                builder.Append('.');
                builder.Append(0);
            }

            AsnReader reader = new AsnReader(inputData, ruleSet);
            string oidString = reader.ReadObjectIdentifier();

            Assert.Equal(ExpectedLength, oidString.Length);
            Assert.Equal(builder.ToString(), oidString);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadVeryLongOidArc(AsnEncodingRules ruleSet)
        {
            byte[] inputData = new byte[255];
            // 06 81 93 (OBJECT IDENTIFIER, 147 bytes).
            inputData[0] = 0x06;
            inputData[1] = 0x81;
            inputData[2] = 0x93;

            // With 147 bytes we get 147*7 = 1029 value bits.
            // The smallest legal number to encode would have a top byte of 0x81,
            // leaving 1022 bits remaining.  If they're all zero then we have 2^1022.
            //
            // Since it's our first sub-identifier it's really encoding "2.(2^1022 - 80)".
            inputData[3] = 0x81;
            // Leave the last byte as 0.
            new Span<byte>(inputData, 4, 145).Fill(0x80);

            const string ExpectedOid =
                "2." +
                "449423283715578976932326297697256183404494244735576643183575" +
                "202894331689513752407831771193306018840052800284699678483394" +
                "146974422036041556232118576598685310944419733562163713190755" +
                "549003115235298632707380212514422095376705856157203684782776" +
                "352068092908376276711465745599868114846199290762088390824060" +
                "56034224";

            AsnReader reader = new AsnReader(inputData, ruleSet);

            string oidString = reader.ReadObjectIdentifier();
            Assert.Equal(ExpectedOid, oidString);
        }
    }
}
