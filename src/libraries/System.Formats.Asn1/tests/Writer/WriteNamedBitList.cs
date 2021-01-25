// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Formats.Asn1.Tests.Reader;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteNamedBitList : Asn1WriterTests
    {
        [Theory]
        [InlineData(
            AsnEncodingRules.BER,
            "030100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            AsnEncodingRules.CER,
            "030100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            AsnEncodingRules.DER,
            "030100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            AsnEncodingRules.BER,
            "0309000000000000000003",
            ReadNamedBitList.ULongFlags.Max | ReadNamedBitList.ULongFlags.AlmostMax)]
        [InlineData(
            AsnEncodingRules.CER,
            "0309010000000080000002",
            ReadNamedBitList.LongFlags.Max | ReadNamedBitList.LongFlags.Mid)]
        [InlineData(
            AsnEncodingRules.DER,
            "030204B0",
            ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment)]
        public static void VerifyWriteNamedBitList(
            AsnEncodingRules ruleSet,
            string expectedHex,
            Enum value)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNamedBitList(value);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(
            AsnEncodingRules.BER,
            "C00100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            AsnEncodingRules.CER,
            "410100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            AsnEncodingRules.DER,
            "820100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            AsnEncodingRules.BER,
            "C009000000000000000003",
            ReadNamedBitList.ULongFlags.Max | ReadNamedBitList.ULongFlags.AlmostMax)]
        [InlineData(
            AsnEncodingRules.CER,
            "4109010000000080000002",
            ReadNamedBitList.LongFlags.Max | ReadNamedBitList.LongFlags.Mid)]
        [InlineData(
            AsnEncodingRules.DER,
            "820204B0",
            ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment)]
        public static void VerifyWriteNamedBitList_WithTag(
            AsnEncodingRules ruleSet,
            string expectedHex,
            Enum value)
        {
            int ruleSetVal = (int)ruleSet;
            TagClass tagClass = (TagClass)(byte)(ruleSetVal << 6);

            if (tagClass == TagClass.Universal)
                tagClass = TagClass.Private;

            Asn1Tag tag = new Asn1Tag(tagClass, ruleSetVal);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNamedBitList(value, tag);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_Generic(AsnEncodingRules ruleSet)
        {
            AsnWriter objWriter = new AsnWriter(ruleSet);
            AsnWriter genWriter = new AsnWriter(ruleSet);

            var flagsValue =
                ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment;

            genWriter.WriteNamedBitList(flagsValue);
            objWriter.WriteNamedBitList((Enum)flagsValue);

            Verify(genWriter, objWriter.Encode().ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_Generic_WithTag(AsnEncodingRules ruleSet)
        {
            AsnWriter objWriter = new AsnWriter(ruleSet);
            AsnWriter genWriter = new AsnWriter(ruleSet);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 52);

            var flagsValue =
                ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment;

            genWriter.WriteNamedBitList(flagsValue, tag);
            objWriter.WriteNamedBitList((Enum)flagsValue, tag);

            Verify(genWriter, objWriter.Encode().ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_NonNull(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentNullException>(
                "value",
                () => writer.WriteNamedBitList((Enum)null));

            AssertExtensions.Throws<ArgumentNullException>(
                "value",
                () => writer.WriteNamedBitList((Enum)null, new Asn1Tag(TagClass.ContextSpecific, 1)));

            AssertExtensions.Throws<ArgumentNullException>(
                "value",
                () => writer.WriteNamedBitList((BitArray)null));

            AssertExtensions.Throws<ArgumentNullException>(
                "value",
                () => writer.WriteNamedBitList((BitArray)null, new Asn1Tag(TagClass.Private, 2)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_FlagsEnumRequired(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteNamedBitList(AsnEncodingRules.BER));

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteNamedBitList(
                    AsnEncodingRules.BER,
                    new Asn1Tag(TagClass.ContextSpecific, 1)));

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteNamedBitList((Enum)AsnEncodingRules.BER));

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteNamedBitList(
                    (Enum)AsnEncodingRules.BER,
                    new Asn1Tag(TagClass.ContextSpecific, 1)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteNamedBitList(
                    StringSplitOptions.RemoveEmptyEntries,
                    Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteNamedBitList(
                    (Enum)StringSplitOptions.RemoveEmptyEntries,
                    Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_BitArray(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            BitArray bits = new BitArray(18);
            bits.Set(1, true);
            bits.Set(15, true);

            writer.WriteNamedBitList(bits, new Asn1Tag(TagClass.Application, 4));

            Verify(writer, "440406400100");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_BitArray_Empty(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            BitArray bits = new BitArray(0);

            writer.WriteNamedBitList(bits);

            Verify(writer, "030100");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_BitArray_EveryPattern(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            byte[] allTheBytes = new byte[256];

            for (int i = 0; i < allTheBytes.Length; i++)
            {
                allTheBytes[i] = (byte)i;
            }

            BitArray bits = new BitArray(allTheBytes);
            writer.WriteNamedBitList(bits, new Asn1Tag(TagClass.Private, 491));

            const string ExpectedHex =
                // Tag
                "DF836B" +
                // Length
                "820101" +
                // Unused bit count
                "00" +
                // Reversed bits for byte patterns 0x00-0x1F
                "008040C020A060E0109050D030B070F0088848C828A868E8189858D838B878F8" +
                // Reversed bits for byte patterns 0x20-0x3F
                "048444C424A464E4149454D434B474F40C8C4CCC2CAC6CEC1C9C5CDC3CBC7CFC" +
                // Reversed bits for byte patterns 0x40-0x5F
                "028242C222A262E2129252D232B272F20A8A4ACA2AAA6AEA1A9A5ADA3ABA7AFA" +
                // Reversed bits for byte patterns 0x60-0x7F
                "068646C626A666E6169656D636B676F60E8E4ECE2EAE6EEE1E9E5EDE3EBE7EFE" +
                // Reversed bits for byte patterns 0x80-0x9F
                "018141C121A161E1119151D131B171F1098949C929A969E9199959D939B979F9" +
                // Reversed bits for byte patterns 0xA0-0xBF
                "058545C525A565E5159555D535B575F50D8D4DCD2DAD6DED1D9D5DDD3DBD7DFD" +
                // Reversed bits for byte patterns 0xC0-0xDF
                "038343C323A363E3139353D333B373F30B8B4BCB2BAB6BEB1B9B5BDB3BBB7BFB" +
                // Reversed bits for byte patterns 0xE0-0xFF
                "078747C727A767E7179757D737B777F70F8F4FCF2FAF6FEF1F9F5FDF3FBF7FFF";

            Verify(writer, ExpectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_BitArray_7992Bits(AsnEncodingRules ruleSet)
        {
            BitArray array = new BitArray(7992);
            array.Set(4, true);
            array.Set(7990, true);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNamedBitList(array, new Asn1Tag(TagClass.ContextSpecific, 4));

            string expectedHex = "848203E80008" + new string('0', 1994) + "02";
            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_BitArray_7993Bits(AsnEncodingRules ruleSet)
        {
            BitArray array = new BitArray(7993);
            array.Set(7992, true);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNamedBitList(array, new Asn1Tag(TagClass.ContextSpecific, 5));

            string expectedHex;

            if (ruleSet == AsnEncodingRules.CER)
            {
                expectedHex = "A580038203E8" + new string('0', 2000) + "03020780" + "0000";
            }
            else
            {
                expectedHex = "858203E907" + new string('0', 1998) + "80";
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_KeyUsage_OneByte(AsnEncodingRules ruleSet)
        {
            //     KeyUsage ::= BIT STRING {
            //       digitalSignature   (0),
            //       nonRepudiation     (1),
            //       keyEncipherment    (2),
            //       dataEncipherment   (3),
            //       keyAgreement       (4),
            //       keyCertSign        (5),
            //       cRLSign            (6),
            //       encipherOnly       (7),
            //       decipherOnly       (8) }

            X509KeyUsageExtension kuExt = new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                critical: false);

            BitArray array = new BitArray(7);
            array.Set(6, true);
            array.Set(5, true);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNamedBitList(array);

            Verify(writer, kuExt.RawData.ByteArrayToHex());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_KeyUsage_TwoByte(AsnEncodingRules ruleSet)
        {
            X509KeyUsageExtension kuExt = new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.DecipherOnly,
                critical: false);

            BitArray array = new BitArray(9);
            array.Set(4, true);
            array.Set(8, true);

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteNamedBitList(array);

            Verify(writer, kuExt.RawData.ByteArrayToHex());
        }
    }
}
