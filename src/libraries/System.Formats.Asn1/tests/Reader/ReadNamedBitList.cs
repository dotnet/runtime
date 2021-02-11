// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReadNamedBitList
    {
        [Flags]
        public enum X509KeyUsageCSharpStyle
        {
            None = 0,
            DigitalSignature = 1,
            NonRepudiation = 1 << 1,
            KeyEncipherment = 1 << 2,
            DataEncipherment = 1 << 3,
            KeyAgreement = 1 << 4,
            KeyCertSign = 1 << 5,
            CrlSign = 1 << 6,
            EncipherOnly = 1 << 7,
            DecipherOnly = 1 << 8,
        }

        [Flags]
        public enum ULongFlags : ulong
        {
            None = 0,
            Min = 1,
            Mid = 1L << 32,
            AlmostMax = 1L << 62,
            Max = 1UL << 63,
        }

        [Flags]
        public enum LongFlags : long
        {
            None = 0,
            Mid = 1L << 32,
            Max = 1L << 62,
            Min = long.MinValue,
        }

        [Theory]
        [InlineData(
            AsnEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            (long)(X509KeyUsageCSharpStyle.None),
            "030100")]
        [InlineData(
            AsnEncodingRules.CER,
            typeof(X509KeyUsageCSharpStyle),
            (long)(X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign),
            "0303070480")]
        [InlineData(
            AsnEncodingRules.DER,
            typeof(X509KeyUsageCSharpStyle),
            (long)(X509KeyUsageCSharpStyle.KeyAgreement),
            "03020308")]
        [InlineData(
            AsnEncodingRules.BER,
            typeof(LongFlags),
            (long)(LongFlags.Mid | LongFlags.Max),
            "0309010000000080000002")]
        [InlineData(
            AsnEncodingRules.CER,
            typeof(LongFlags),
            (long)(LongFlags.Mid | LongFlags.Min),
            "0309000000000080000001")]
        [InlineData(
            AsnEncodingRules.DER,
            typeof(LongFlags),
            (long)(LongFlags.Min | LongFlags.Max),
            "0309000000000000000003")]
        // BER: Unused bits are unmapped, regardless of value.
        [InlineData(
            AsnEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            (long)(X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign),
            "030307048F")]
        // BER: Trailing zeros are permitted.
        [InlineData(
            AsnEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            (long)(X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign | X509KeyUsageCSharpStyle.DataEncipherment),
            "03050014800000")]
        // BER: Trailing 0-bits don't have to be declared "unused"
        [InlineData(
            AsnEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            (long)(X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign | X509KeyUsageCSharpStyle.DataEncipherment),
            "0303001480")]
        public static void VerifyReadNamedBitListEncodings(
            AsnEncodingRules ruleSet,
            Type enumType,
            long enumValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputBytes, ruleSet);
            Enum readValue = reader.ReadNamedBitListValue(enumType);

            Assert.Equal(Enum.ToObject(enumType, enumValue), readValue);
        }

        [Theory]
        [InlineData(
            AsnEncodingRules.BER,
            typeof(ULongFlags),
            (ulong)(ULongFlags.Mid | ULongFlags.Max),
            "0309000000000080000001")]
        [InlineData(
            AsnEncodingRules.CER,
            typeof(ULongFlags),
            (ulong)(ULongFlags.Min | ULongFlags.Mid),
            "0306078000000080")]
        [InlineData(
            AsnEncodingRules.DER,
            typeof(ULongFlags),
            (ulong)(ULongFlags.Min | ULongFlags.Max),
            "0309008000000000000001")]
        public static void VerifyReadNamedBitListEncodings_ULong(
            AsnEncodingRules ruleSet,
            Type enumType,
            ulong enumValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputBytes, ruleSet);
            Enum readValue = reader.ReadNamedBitListValue(enumType);

            Assert.Equal(Enum.ToObject(enumType, enumValue), readValue);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyGenericReadNamedBitList(AsnEncodingRules ruleSet)
        {
            string inputHex = "0306078000000080" + "0309010000000080000002";
            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), ruleSet);

            ULongFlags uLongFlags = reader.ReadNamedBitListValue<ULongFlags>();
            LongFlags longFlags = reader.ReadNamedBitListValue<LongFlags>();

            Assert.False(reader.HasData);
            Assert.Equal(ULongFlags.Mid | ULongFlags.Min, uLongFlags);
            Assert.Equal(LongFlags.Mid | LongFlags.Max, longFlags);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_RequiresTypeArg(AsnEncodingRules ruleSet)
        {
            string inputHex = "030100";
            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), ruleSet);

            AssertExtensions.Throws<ArgumentNullException>(
                "flagsEnumType",
                () => reader.ReadNamedBitListValue(null!));

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_RequiresFlags(AsnEncodingRules ruleSet)
        {
            string inputHex = "030100";
            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "flagsEnumType",
                () => reader.ReadNamedBitListValue<AsnEncodingRules>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_DataOutOfRange(AsnEncodingRules ruleSet)
        {
            string inputHex = "0309000000000100000001";

            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_ExcessiveBytes(AsnEncodingRules ruleSet)
        {
            string inputHex = "03050014800000";

            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_ExcessiveBits(AsnEncodingRules ruleSet)
        {
            string inputHex = "0303061480";

            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), ruleSet);

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 3, 2, 1, 2 };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.Equal(
                X509KeyUsageCSharpStyle.CrlSign,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(AsnEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, 2, 4 };
            AsnReader reader = new AsnReader(inputData, ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<AsnContentException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.Equal(
                X509KeyUsageCSharpStyle.KeyCertSign,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, "0303070080", TagClass.Universal, 3)]
        [InlineData(AsnEncodingRules.CER, "0303070080", TagClass.Universal, 3)]
        [InlineData(AsnEncodingRules.DER, "0303070080", TagClass.Universal, 3)]
        [InlineData(AsnEncodingRules.BER, "8003070080", TagClass.ContextSpecific, 0)]
        [InlineData(AsnEncodingRules.CER, "4C03070080", TagClass.Application, 12)]
        [InlineData(AsnEncodingRules.DER, "DF8A4603070080", TagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            AsnEncodingRules ruleSet,
            string inputHex,
            TagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            Assert.Equal(
                X509KeyUsageCSharpStyle.DecipherOnly,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(
                    new Asn1Tag(tagClass, tagValue, true)));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, ruleSet);

            Assert.Equal(
                X509KeyUsageCSharpStyle.DecipherOnly,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(
                    new Asn1Tag(tagClass, tagValue, false)));

            Assert.False(reader.HasData);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_BitArray(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "440406400100".HexToByteArray();
            bool[] expected = new bool[18];
            expected[1] = expected[15] = true;

            AsnReader reader = new AsnReader(inputData, ruleSet);

            BitArray bits = reader.ReadNamedBitList(new Asn1Tag(TagClass.Application, 4));
            Assert.Equal(expected.Length, bits.Length);
            Assert.False(reader.HasData);

            bool[] actual = new bool[expected.Length];
            bits.CopyTo(actual, 0);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_BitArray_Empty(AsnEncodingRules ruleSet)
        {
            byte[] inputData = "030100".HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            BitArray bits = reader.ReadNamedBitList();
            Assert.Equal(0, bits.Length);
            Assert.False(reader.HasData);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_BitArray_EveryPattern(AsnEncodingRules ruleSet)
        {
            const string InputHex =
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

            byte[] inputData = InputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);

            byte[] allTheBytes = new byte[256];

            for (int i = 0; i < allTheBytes.Length; i++)
            {
                allTheBytes[i] = (byte)i;
            }

            BitArray bits = reader.ReadNamedBitList(new Asn1Tag(TagClass.Private, 491));
            Assert.Equal(allTheBytes.Length * 8, bits.Length);
            Assert.False(reader.HasData);

            byte[] actual = new byte[allTheBytes.Length];
            bits.CopyTo(actual, 0);

            Assert.Equal(actual, actual);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_BitArray_7992Bits(AsnEncodingRules ruleSet)
        {
            string inputHex = "848203E80008" + new string('0', 1994) + "02";
            byte[] inputData = inputHex.HexToByteArray();

            BitArray expected = new BitArray(7992);
            expected.Set(4, true);
            expected.Set(7990, true);

            AsnReader reader = new AsnReader(inputData, ruleSet);
            BitArray actual = reader.ReadNamedBitList(new Asn1Tag(TagClass.ContextSpecific, 4));
            Assert.False(reader.HasData);

            Assert.Equal(expected.Cast<bool>(), actual.Cast<bool>());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadNamedBitList_BitArray_7993Bits(AsnEncodingRules ruleSet)
        {
            string inputHex;

            if (ruleSet == AsnEncodingRules.CER)
            {
                inputHex = "A580038203E8" + new string('0', 2000) + "03020780" + "0000";
            }
            else
            {
                inputHex = "858203E907" + new string('0', 1998) + "80";
            }

            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, ruleSet);
            BitArray actual = reader.ReadNamedBitList(new Asn1Tag(TagClass.ContextSpecific, 5));
            Assert.False(reader.HasData);

            BitArray expected = new BitArray(7993);
            expected.Set(7992, true);

            Assert.Equal(expected.Cast<bool>(), actual.Cast<bool>());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyReadNamedBitList_KeyUsage_OneByte(AsnEncodingRules ruleSet)
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

            BitArray expected = new BitArray(7);
            expected.Set(6, true);
            expected.Set(5, true);

            AsnReader reader = new AsnReader(kuExt.RawData, ruleSet);
            BitArray actual = reader.ReadNamedBitList();

            Assert.Equal(expected.Cast<bool>(), actual.Cast<bool>());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyReadNamedBitList_KeyUsage_TwoByte(AsnEncodingRules ruleSet)
        {
            X509KeyUsageExtension kuExt = new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.DecipherOnly,
                critical: false);

            BitArray expected = new BitArray(9);
            expected.Set(4, true);
            expected.Set(8, true);

            AsnReader reader = new AsnReader(kuExt.RawData, ruleSet);
            BitArray actual = reader.ReadNamedBitList();

            Assert.Equal(expected.Cast<bool>(), actual.Cast<bool>());
        }
    }
}
