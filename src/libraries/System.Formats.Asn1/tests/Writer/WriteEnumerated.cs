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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.SByteBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumerated.SByteBacked.Pillow, true, "9E01EF")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.SByteBacked.Fluff, false, "0A0153")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.SByteBacked.Fluff, true, "9E0153")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.SByteBacked)(-127), true, "9E0181")]
        public void VerifyWriteEnumerated_SByte(
            AsnEncodingRules ruleSet,
            ReadEnumerated.SByteBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ByteBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumerated.ByteBacked.NotFluffy, true, "9A010B")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.ByteBacked.Fluff, false, "0A010C")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ByteBacked.Fluff, true, "9A010C")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.ByteBacked)253, false, "0A0200FD")]
        public void VerifyWriteEnumerated_Byte(
            AsnEncodingRules ruleSet,
            ReadEnumerated.ByteBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ShortBacked.Zero, true, "DF81540100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumerated.ShortBacked.Pillow, true, "DF815402FC00")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.ShortBacked.Fluff, false, "0A020209")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ShortBacked.Fluff, true, "DF8154020209")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.ShortBacked)25321, false, "0A0262E9")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.ShortBacked)(-12345), false, "0A02CFC7")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.ShortBacked)(-1), true, "DF815401FF")]
        public void VerifyWriteEnumerated_Short(
            AsnEncodingRules ruleSet,
            ReadEnumerated.ShortBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.UShortBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.UShortBacked.Zero, true, "4D0100")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.UShortBacked.Fluff, false, "0A03008000")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.UShortBacked.Fluff, true, "4D03008000")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.UShortBacked)11, false, "0A010B")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.UShortBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumerated.UShortBacked)ushort.MaxValue, true, "4D0300FFFF")]
        public void VerifyWriteEnumerated_UShort(
            AsnEncodingRules ruleSet,
            ReadEnumerated.UShortBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.IntBacked.Zero, true, "5F81FF7F0100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumerated.IntBacked.Pillow, true, "5F81FF7F03FEFFFF")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.IntBacked.Fluff, false, "0A03010001")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.IntBacked.Fluff, true, "5F81FF7F03010001")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.IntBacked)25321, false, "0A0262E9")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.IntBacked)(-12345), false, "0A02CFC7")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumerated.IntBacked)(-1), true, "5F81FF7F01FF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.IntBacked)int.MinValue, true, "5F81FF7F0480000000")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.IntBacked)int.MaxValue, false, "0A047FFFFFFF")]
        public void VerifyWriteEnumerated_Int(
            AsnEncodingRules ruleSet,
            ReadEnumerated.IntBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.UIntBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.UIntBacked.Zero, true, "9F610100")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.UIntBacked.Fluff, false, "0A050080000005")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.UIntBacked.Fluff, true, "9F61050080000005")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.UIntBacked)11, false, "0A010B")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.UIntBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumerated.UIntBacked)ushort.MaxValue, true, "9F610300FFFF")]
        public void VerifyWriteEnumerated_UInt(
            AsnEncodingRules ruleSet,
            ReadEnumerated.UIntBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.LongBacked.Zero, true, "800100")]
        [InlineData(AsnEncodingRules.CER, ReadEnumerated.LongBacked.Pillow, true, "8005FF00000000")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.LongBacked.Fluff, false, "0A050200000441")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.LongBacked.Fluff, true, "80050200000441")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.LongBacked)25321, false, "0A0262E9")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.LongBacked)(-12345), false, "0A02CFC7")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumerated.LongBacked)(-1), true, "8001FF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.LongBacked)int.MinValue, true, "800480000000")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.LongBacked)int.MaxValue, false, "0A047FFFFFFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumerated.LongBacked)long.MinValue, false, "0A088000000000000000")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.LongBacked)long.MaxValue, true, "80087FFFFFFFFFFFFFFF")]
        public void VerifyWriteEnumerated_Long(
            AsnEncodingRules ruleSet,
            ReadEnumerated.LongBacked value,
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
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ULongBacked.Zero, false, "0A0100")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ULongBacked.Zero, true, "C10100")]
        [InlineData(AsnEncodingRules.DER, ReadEnumerated.ULongBacked.Fluff, false, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.BER, ReadEnumerated.ULongBacked.Fluff, true, "C10900FACEF00DCAFEBEEF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.ULongBacked)11, false, "0A010B")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.ULongBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(AsnEncodingRules.BER, (ReadEnumerated.ULongBacked)ushort.MaxValue, true, "C10300FFFF")]
        [InlineData(AsnEncodingRules.CER, (ReadEnumerated.ULongBacked)long.MaxValue, true, "C1087FFFFFFFFFFFFFFF")]
        [InlineData(AsnEncodingRules.DER, (ReadEnumerated.ULongBacked)ulong.MaxValue, false, "0A0900FFFFFFFFFFFFFFFF")]
        public void VerifyWriteEnumerated_ULong(
            AsnEncodingRules ruleSet,
            ReadEnumerated.ULongBacked value,
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
                () => writer.WriteEnumeratedValue(ReadEnumerated.IntBacked.Pillow, Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteEnumeratedValue((Enum)ReadEnumerated.IntBacked.Pillow, Asn1Tag.Null));
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

            genWriter.WriteEnumeratedValue(ReadEnumerated.UIntBacked.Fluff);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumerated.UIntBacked.Fluff);

            genWriter.WriteEnumeratedValue(ReadEnumerated.SByteBacked.Fluff);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumerated.SByteBacked.Fluff);

            genWriter.WriteEnumeratedValue(ReadEnumerated.ULongBacked.Fluff);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumerated.ULongBacked.Fluff);

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

            genWriter.WriteEnumeratedValue(ReadEnumerated.UIntBacked.Fluff, tag);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumerated.UIntBacked.Fluff, tag);

            tag = new Asn1Tag(TagClass.Private, 4);

            genWriter.WriteEnumeratedValue(ReadEnumerated.SByteBacked.Fluff, tag);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumerated.SByteBacked.Fluff, tag);

            tag = new Asn1Tag(TagClass.Application, 75);

            genWriter.WriteEnumeratedValue(ReadEnumerated.ULongBacked.Fluff, tag);
            objWriter.WriteEnumeratedValue((Enum)ReadEnumerated.ULongBacked.Fluff, tag);

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
                ReadEnumerated.ULongBacked.Fluff,
                new Asn1Tag(UniversalTagNumber.Enumerated, isConstructed: true));

            writer.WriteEnumeratedValue(
                (Enum)ReadEnumerated.SByteBacked.Fluff,
                new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));

            Verify(writer, "0A0900FACEF00DCAFEBEEF" + "800153");
        }
    }
}
