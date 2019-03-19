// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public sealed class ReadNamedBitList : Asn1ReaderTests
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
            PublicEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            X509KeyUsageCSharpStyle.None,
            "030100")]
        [InlineData(
            PublicEncodingRules.CER,
            typeof(X509KeyUsageCSharpStyle),
            X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign,
            "0303070480")]
        [InlineData(
            PublicEncodingRules.DER,
            typeof(X509KeyUsageCSharpStyle),
            X509KeyUsageCSharpStyle.KeyAgreement,
            "03020308")]
        [InlineData(
            PublicEncodingRules.BER,
            typeof(LongFlags),
            LongFlags.Mid | LongFlags.Max,
            "0309010000000080000002")]
        [InlineData(
            PublicEncodingRules.CER,
            typeof(LongFlags),
            LongFlags.Mid | LongFlags.Min,
            "0309000000000080000001")]
        [InlineData(
            PublicEncodingRules.DER,
            typeof(LongFlags),
            LongFlags.Min | LongFlags.Max,
            "0309000000000000000003")]
        // BER: Unused bits are unmapped, regardless of value.
        [InlineData(
            PublicEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign,
            "030307048F")]
        // BER: Trailing zeros are permitted.
        [InlineData(
            PublicEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign | X509KeyUsageCSharpStyle.DataEncipherment,
            "03050014800000")]
        // BER: Trailing 0-bits don't have to be declared "unused"
        [InlineData(
            PublicEncodingRules.BER,
            typeof(X509KeyUsageCSharpStyle),
            X509KeyUsageCSharpStyle.DecipherOnly | X509KeyUsageCSharpStyle.KeyCertSign | X509KeyUsageCSharpStyle.DataEncipherment,
            "0303001480")]
        public static void VerifyReadNamedBitListEncodings(
            PublicEncodingRules ruleSet,
            Type enumType,
            long enumValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputBytes, (AsnEncodingRules)ruleSet);
            Enum readValue = reader.ReadNamedBitListValue(enumType);

            Assert.Equal(Enum.ToObject(enumType, enumValue), readValue);
        }

        [Theory]
        [InlineData(
            PublicEncodingRules.BER,
            typeof(ULongFlags),
            ULongFlags.Mid | ULongFlags.Max,
            "0309000000000080000001")]
        [InlineData(
            PublicEncodingRules.CER,
            typeof(ULongFlags),
            ULongFlags.Min | ULongFlags.Mid,
            "0306078000000080")]
        [InlineData(
            PublicEncodingRules.DER,
            typeof(ULongFlags),
            ULongFlags.Min | ULongFlags.Max,
            "0309008000000000000001")]
        public static void VerifyReadNamedBitListEncodings_ULong(
            PublicEncodingRules ruleSet,
            Type enumType,
            ulong enumValue,
            string inputHex)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            AsnReader reader = new AsnReader(inputBytes, (AsnEncodingRules)ruleSet);
            Enum readValue = reader.ReadNamedBitListValue(enumType);

            Assert.Equal(Enum.ToObject(enumType, enumValue), readValue);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyGenericReadNamedBitList(PublicEncodingRules ruleSet)
        {
            string inputHex = "0306078000000080" + "0309010000000080000002";
            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), (AsnEncodingRules)ruleSet);

            ULongFlags uLongFlags = reader.ReadNamedBitListValue<ULongFlags>();
            LongFlags longFlags = reader.ReadNamedBitListValue<LongFlags>();

            Assert.False(reader.HasData);
            Assert.Equal(ULongFlags.Mid | ULongFlags.Min, uLongFlags);
            Assert.Equal(LongFlags.Mid | LongFlags.Max, longFlags);
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadNamedBitList_RequiresFlags(PublicEncodingRules ruleSet)
        {
            string inputHex = "030100";
            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tFlagsEnum",
                () => reader.ReadNamedBitListValue<AsnEncodingRules>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadNamedBitList_DataOutOfRange(PublicEncodingRules ruleSet)
        {
            string inputHex = "0309000000000100000001";

            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadNamedBitList_ExcessiveBytes(PublicEncodingRules ruleSet)
        {
            string inputHex = "03050014800000";

            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void ReadNamedBitList_ExcessiveBits(PublicEncodingRules ruleSet)
        {
            string inputHex = "0303061480";

            AsnReader reader = new AsnReader(inputHex.HexToByteArray(), (AsnEncodingRules)ruleSet);

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "reader.HasData");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Universal(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 3, 2, 1, 2 };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.ContextSpecific, 0)));

            Assert.True(reader.HasData, "HasData after wrong tag");

            Assert.Equal(
                X509KeyUsageCSharpStyle.CrlSign,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());
            Assert.False(reader.HasData, "HasData after read");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void TagMustBeCorrect_Custom(PublicEncodingRules ruleSet)
        {
            byte[] inputData = { 0x87, 2, 2, 4 };
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "expectedTag",
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(Asn1Tag.Null));

            Assert.True(reader.HasData, "HasData after bad universal tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>());

            Assert.True(reader.HasData, "HasData after default tag");

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.Application, 0)));

            Assert.True(reader.HasData, "HasData after wrong custom class");

            Assert.Throws<CryptographicException>(
                () => reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.ContextSpecific, 1)));

            Assert.True(reader.HasData, "HasData after wrong custom tag value");

            Assert.Equal(
                X509KeyUsageCSharpStyle.KeyCertSign,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(new Asn1Tag(TagClass.ContextSpecific, 7)));

            Assert.False(reader.HasData, "HasData after reading value");
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, "0303070080", PublicTagClass.Universal, 3)]
        [InlineData(PublicEncodingRules.CER, "0303070080", PublicTagClass.Universal, 3)]
        [InlineData(PublicEncodingRules.DER, "0303070080", PublicTagClass.Universal, 3)]
        [InlineData(PublicEncodingRules.BER, "8003070080", PublicTagClass.ContextSpecific, 0)]
        [InlineData(PublicEncodingRules.CER, "4C03070080", PublicTagClass.Application, 12)]
        [InlineData(PublicEncodingRules.DER, "DF8A4603070080", PublicTagClass.Private, 1350)]
        public static void ExpectedTag_IgnoresConstructed(
            PublicEncodingRules ruleSet,
            string inputHex,
            PublicTagClass tagClass,
            int tagValue)
        {
            byte[] inputData = inputHex.HexToByteArray();
            AsnReader reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Equal(
                X509KeyUsageCSharpStyle.DecipherOnly,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(
                    new Asn1Tag((TagClass)tagClass, tagValue, true)));

            Assert.False(reader.HasData);

            reader = new AsnReader(inputData, (AsnEncodingRules)ruleSet);

            Assert.Equal(
                X509KeyUsageCSharpStyle.DecipherOnly,
                reader.ReadNamedBitListValue<X509KeyUsageCSharpStyle>(
                    new Asn1Tag((TagClass)tagClass, tagValue, false)));

            Assert.False(reader.HasData);
        }
    }
}
