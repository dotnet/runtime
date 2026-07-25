// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1.Tests.Reader;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteEnumerated : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.SByteBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumeratedBase.SByteBacked.Pillow, true, "9E01EF")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.SByteBacked.Fluff, false, "0A0153")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.SByteBacked.Fluff, true, "9E0153")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.SByteBacked)(-127), true, "9E0181")]
        public void VerifyWriteEnumerated_SByte(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.SByteBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.ContextSpecific, 30));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ByteBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumeratedBase.ByteBacked.NotFluffy, true, "9A010B")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.ByteBacked.Fluff, false, "0A010C")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ByteBacked.Fluff, true, "9A010C")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.ByteBacked)253, false, "0A0200FD")]
        public void VerifyWriteEnumerated_Byte(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.ByteBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.ContextSpecific, 26));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ShortBacked.Zero, true, "DF81540100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumeratedBase.ShortBacked.Pillow, true, "DF815402FC00")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.ShortBacked.Fluff, false, "0A020209")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ShortBacked.Fluff, true, "DF8154020209")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.ShortBacked)25321, false, "0A0262E9")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.ShortBacked)(-12345), false, "0A02CFC7")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.ShortBacked)(-1), true, "DF815401FF")]
        public void VerifyWriteEnumerated_Short(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.ShortBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.Private, 212));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.UShortBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.UShortBacked.Zero, true, "4D0100")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.UShortBacked.Fluff, false, "0A03008000")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.UShortBacked.Fluff, true, "4D03008000")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.UShortBacked)11, false, "0A010B")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.UShortBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumeratedBase.UShortBacked)ushort.MaxValue, true, "4D0300FFFF")]
        public void VerifyWriteEnumerated_UShort(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.UShortBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.Application, 13));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.IntBacked.Zero, true, "5F81FF7F0100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumeratedBase.IntBacked.Pillow, true, "5F81FF7F03FEFFFF")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.IntBacked.Fluff, false, "0A03010001")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.IntBacked.Fluff, true, "5F81FF7F03010001")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.IntBacked)25321, false, "0A0262E9")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.IntBacked)(-12345), false, "0A02CFC7")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumeratedBase.IntBacked)(-1), true, "5F81FF7F01FF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.IntBacked)int.MinValue, true, "5F81FF7F0480000000")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.IntBacked)int.MaxValue, false, "0A047FFFFFFF")]
        public void VerifyWriteEnumerated_Int(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.IntBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.Application, short.MaxValue));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.UIntBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.UIntBacked.Zero, true, "9F610100")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.UIntBacked.Fluff, false, "0A050080000005")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.UIntBacked.Fluff, true, "9F61050080000005")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.UIntBacked)11, false, "0A010B")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.UIntBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumeratedBase.UIntBacked)ushort.MaxValue, true, "9F610300FFFF")]
        public void VerifyWriteEnumerated_UInt(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.UIntBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.ContextSpecific, 97));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.LongBacked.Zero, true, "800100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumeratedBase.LongBacked.Pillow, true, "8005FF00000000")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.LongBacked.Fluff, false, "0A050200000441")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.LongBacked.Fluff, true, "80050200000441")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.LongBacked)25321, false, "0A0262E9")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.LongBacked)(-12345), false, "0A02CFC7")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumeratedBase.LongBacked)(-1), true, "8001FF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.LongBacked)int.MinValue, true, "800480000000")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.LongBacked)int.MaxValue, false, "0A047FFFFFFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumeratedBase.LongBacked)long.MinValue, false, "0A088000000000000000")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.LongBacked)long.MaxValue, true, "80087FFFFFFFFFFFFFFF")]
        public void VerifyWriteEnumerated_Long(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.LongBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.ContextSpecific, 0));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ULongBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ULongBacked.Zero, true, "C10100")]
        [InlineData(AsnEncodingRules.DER, ReadEnumeratedBase.ULongBacked.Fluff, false, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.BER, ReadEnumeratedBase.ULongBacked.Fluff, true, "C10900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.ULongBacked)11, false, "0A010B")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.ULongBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumeratedBase.ULongBacked)ushort.MaxValue, true, "C10300FFFF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumeratedBase.ULongBacked)long.MaxValue, true, "C1087FFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumeratedBase.ULongBacked)ulong.MaxValue, false, "0A0900FFFFFFFFFFFFFFFF")]
        public void VerifyWriteEnumerated_ULong(
            AsnEncodingRules ruleSet,
            ReadEnumeratedBase.ULongBacked value,
            bool customTag,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.WriteEnumeratedValue(value, new Asn1Tag(TagClass.Private, 1));
            }
            else
            {
                writer.WriteEnumeratedValue(value);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyFlagsBased(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteEnumeratedValue(OpenFlags.IncludeArchived));

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteEnumeratedValue(
                    OpenFlags.IncludeArchived,
                    new Asn1Tag(TagClass.ContextSpecific, 13)));

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteEnumeratedValue((Enum)OpenFlags.IncludeArchived));

            AssertExtensions.Throws<ArgumentException>(
                "tEnum",
                () => writer.WriteEnumeratedValue(
                    (Enum)OpenFlags.IncludeArchived,
                    new Asn1Tag(TagClass.ContextSpecific, 13)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyNull(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteEnumeratedValue(ReadEnumeratedBase.IntBacked.Pillow, Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteEnumeratedValue((Enum)ReadEnumeratedBase.IntBacked.Pillow, Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteEnumeratedValue_NonNull(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentNullException>(
                "value",
                () => writer.WriteEnumeratedValue(null));

            AssertExtensions.Throws<ArgumentNullException>(
                "value",
                () => writer.WriteEnumeratedValue(
                    null,
                    new Asn1Tag(TagClass.ContextSpecific, 1)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteEnumeratedValue_Object(AsnEncodingRules ruleSet)
        {
            AsnWriter objWriter = new AsnWriter(ruleSet);
            AsnWriter genWriter = new AsnWriter(ruleSet);

            genWriter.WriteEnumeratedValue(ReadEnumeratedBase.UIntBacked.Fluff);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumeratedBase.UIntBacked.Fluff);

            genWriter.WriteEnumeratedValue(ReadEnumeratedBase.SByteBacked.Fluff);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumeratedBase.SByteBacked.Fluff);

            genWriter.WriteEnumeratedValue(ReadEnumeratedBase.ULongBacked.Fluff);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumeratedBase.ULongBacked.Fluff);

            Verify(objWriter, genWriter.Encode().ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void VerifyWriteEnumeratedValue_Object_WithTag(AsnEncodingRules ruleSet)
        {
            AsnWriter objWriter = new AsnWriter(ruleSet);
            AsnWriter genWriter = new AsnWriter(ruleSet);

            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 52);

            genWriter.WriteEnumeratedValue(ReadEnumeratedBase.UIntBacked.Fluff, tag);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumeratedBase.UIntBacked.Fluff, tag);

            tag = new Asn1Tag(TagClass.Private, 4);

            genWriter.WriteEnumeratedValue(ReadEnumeratedBase.SByteBacked.Fluff, tag);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumeratedBase.SByteBacked.Fluff, tag);

            tag = new Asn1Tag(TagClass.Application, 75);

            genWriter.WriteEnumeratedValue(ReadEnumeratedBase.ULongBacked.Fluff, tag);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumeratedBase.ULongBacked.Fluff, tag);

            Verify(objWriter, genWriter.Encode().ByteArrayToHex());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteEnumeratedValue_ConstructedIgnored(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteEnumeratedValue(
                ReadEnumeratedBase.ULongBacked.Fluff,
                new Asn1Tag(UniversalTagNumber.Enumerated, isConstructed: true));

            writer.WriteEnumeratedValue(
                (Enum)ReadEnumeratedBase.SByteBacked.Fluff,
                new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));

            Verify(writer, "0A0900FACEF00DCAFEBEEF" + "800153");
        }
    }
}
