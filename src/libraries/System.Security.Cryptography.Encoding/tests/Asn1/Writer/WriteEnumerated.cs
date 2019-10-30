// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteEnumerated : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.SByteBacked.Zero, false, "0A0100")]
        [InlineData(PublicEncodingRules.CER, ReadEnumerated.SByteBacked.Pillow, true, "9E01EF")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.SByteBacked.Fluff, false, "0A0153")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.SByteBacked.Fluff, true, "9E0153")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.SByteBacked)(-127), true, "9E0181")]
        public void VerifyWriteEnumerated_SByte(
            PublicEncodingRules ruleSet,
            ReadEnumerated.SByteBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 30), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ByteBacked.Zero, false, "0A0100")]
        [InlineData(PublicEncodingRules.CER, ReadEnumerated.ByteBacked.NotFluffy, true, "9A010B")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.ByteBacked.Fluff, false, "0A010C")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ByteBacked.Fluff, true, "9A010C")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.ByteBacked)253, false, "0A0200FD")]
        public void VerifyWriteEnumerated_Byte(
            PublicEncodingRules ruleSet,
            ReadEnumerated.ByteBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 26), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ShortBacked.Zero, true, "DF81540100")]
        [InlineData(PublicEncodingRules.CER, ReadEnumerated.ShortBacked.Pillow, true, "DF815402FC00")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.ShortBacked.Fluff, false, "0A020209")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ShortBacked.Fluff, true, "DF8154020209")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.ShortBacked)25321, false, "0A0262E9")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.ShortBacked)(-12345), false, "0A02CFC7")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.ShortBacked)(-1), true, "DF815401FF")]
        public void VerifyWriteEnumerated_Short(
            PublicEncodingRules ruleSet,
            ReadEnumerated.ShortBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.Private, 212), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.UShortBacked.Zero, false, "0A0100")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.UShortBacked.Zero, true, "4D0100")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.UShortBacked.Fluff, false, "0A03008000")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.UShortBacked.Fluff, true, "4D03008000")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.UShortBacked)11, false, "0A010B")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.UShortBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(PublicEncodingRules.BER, (ReadEnumerated.UShortBacked)ushort.MaxValue, true, "4D0300FFFF")]
        public void VerifyWriteEnumerated_UShort(
            PublicEncodingRules ruleSet,
            ReadEnumerated.UShortBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.Application, 13), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.IntBacked.Zero, true, "5F81FF7F0100")]
        [InlineData(PublicEncodingRules.CER, ReadEnumerated.IntBacked.Pillow, true, "5F81FF7F03FEFFFF")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.IntBacked.Fluff, false, "0A03010001")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.IntBacked.Fluff, true, "5F81FF7F03010001")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.IntBacked)25321, false, "0A0262E9")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.IntBacked)(-12345), false, "0A02CFC7")]
        [InlineData(PublicEncodingRules.BER, (ReadEnumerated.IntBacked)(-1), true, "5F81FF7F01FF")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.IntBacked)int.MinValue, true, "5F81FF7F0480000000")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.IntBacked)int.MaxValue, false, "0A047FFFFFFF")]
        public void VerifyWriteEnumerated_Int(
            PublicEncodingRules ruleSet,
            ReadEnumerated.IntBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.Application, short.MaxValue), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.UIntBacked.Zero, false, "0A0100")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.UIntBacked.Zero, true, "9F610100")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.UIntBacked.Fluff, false, "0A050080000005")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.UIntBacked.Fluff, true, "9F61050080000005")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.UIntBacked)11, false, "0A010B")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.UIntBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(PublicEncodingRules.BER, (ReadEnumerated.UIntBacked)ushort.MaxValue, true, "9F610300FFFF")]
        public void VerifyWriteEnumerated_UInt(
            PublicEncodingRules ruleSet,
            ReadEnumerated.UIntBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 97), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.LongBacked.Zero, true, "800100")]
        [InlineData(PublicEncodingRules.CER, ReadEnumerated.LongBacked.Pillow, true, "8005FF00000000")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.LongBacked.Fluff, false, "0A050200000441")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.LongBacked.Fluff, true, "80050200000441")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.LongBacked)25321, false, "0A0262E9")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.LongBacked)(-12345), false, "0A02CFC7")]
        [InlineData(PublicEncodingRules.BER, (ReadEnumerated.LongBacked)(-1), true, "8001FF")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.LongBacked)int.MinValue, true, "800480000000")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.LongBacked)int.MaxValue, false, "0A047FFFFFFF")]
        [InlineData(PublicEncodingRules.BER, (ReadEnumerated.LongBacked)long.MinValue, false, "0A088000000000000000")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.LongBacked)long.MaxValue, true, "80087FFFFFFFFFFFFFFF")]
        public void VerifyWriteEnumerated_Long(
            PublicEncodingRules ruleSet,
            ReadEnumerated.LongBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 0), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ULongBacked.Zero, false, "0A0100")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ULongBacked.Zero, true, "C10100")]
        [InlineData(PublicEncodingRules.DER, ReadEnumerated.ULongBacked.Fluff, false, "0A0900FACEF00DCAFEBEEF")]
        [InlineData(PublicEncodingRules.BER, ReadEnumerated.ULongBacked.Fluff, true, "C10900FACEF00DCAFEBEEF")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.ULongBacked)11, false, "0A010B")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.ULongBacked)short.MaxValue, false, "0A027FFF")]
        [InlineData(PublicEncodingRules.BER, (ReadEnumerated.ULongBacked)ushort.MaxValue, true, "C10300FFFF")]
        [InlineData(PublicEncodingRules.CER, (ReadEnumerated.ULongBacked)long.MaxValue, true, "C1087FFFFFFFFFFFFFFF")]
        [InlineData(PublicEncodingRules.DER, (ReadEnumerated.ULongBacked)ulong.MaxValue, false, "0A0900FFFFFFFFFFFFFFFF")]
        public void VerifyWriteEnumerated_ULong(
            PublicEncodingRules ruleSet,
            ReadEnumerated.ULongBacked value,
            bool customTag,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (customTag)
                {
                    writer.WriteEnumeratedValue(new Asn1Tag(TagClass.Private, 1), value);
                }
                else
                {
                    writer.WriteEnumeratedValue(value);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyFlagsBased(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue(OpenFlags.IncludeArchived));

                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue(
                        new Asn1Tag(TagClass.ContextSpecific, 13),
                        OpenFlags.IncludeArchived));

                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue((object)OpenFlags.IncludeArchived));

                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue(
                        new Asn1Tag(TagClass.ContextSpecific, 13),
                        (object)OpenFlags.IncludeArchived));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyNonEnum(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Assert.Throws<ArgumentException>(
                    () => writer.WriteEnumeratedValue(5));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteEnumeratedValue((object)"hi"));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteEnumeratedValue((object)5));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 3), 5));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 3), (object)"hi"));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteEnumeratedValue(new Asn1Tag(TagClass.ContextSpecific, 3), (object)5));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyEndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.EndOfContents, ReadEnumerated.IntBacked.Pillow));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.EndOfContents, (object)ReadEnumerated.IntBacked.Pillow));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteEnumeratedValue_NonNull(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteEnumeratedValue(null));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteEnumeratedValue(
                        new Asn1Tag(TagClass.ContextSpecific, 1),
                        null));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteEnumeratedValue_Object(PublicEncodingRules ruleSet)
        {
            using (AsnWriter objWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            using (AsnWriter genWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                genWriter.WriteEnumeratedValue(ReadEnumerated.UIntBacked.Fluff);
                objWriter.WriteEnumeratedValue((object)ReadEnumerated.UIntBacked.Fluff);

                genWriter.WriteEnumeratedValue(ReadEnumerated.SByteBacked.Fluff);
                objWriter.WriteEnumeratedValue((object)ReadEnumerated.SByteBacked.Fluff);

                genWriter.WriteEnumeratedValue(ReadEnumerated.ULongBacked.Fluff);
                objWriter.WriteEnumeratedValue((object)ReadEnumerated.ULongBacked.Fluff);

                Verify(objWriter, genWriter.Encode().ByteArrayToHex());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteEnumeratedValue_Object_WithTag(PublicEncodingRules ruleSet)
        {
            using (AsnWriter objWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            using (AsnWriter genWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 52);

                genWriter.WriteEnumeratedValue(tag, ReadEnumerated.UIntBacked.Fluff);
                objWriter.WriteEnumeratedValue(tag, (object)ReadEnumerated.UIntBacked.Fluff);

                tag = new Asn1Tag(TagClass.Private, 4);

                genWriter.WriteEnumeratedValue(tag, ReadEnumerated.SByteBacked.Fluff);
                objWriter.WriteEnumeratedValue(tag, (object)ReadEnumerated.SByteBacked.Fluff);

                tag = new Asn1Tag(TagClass.Application, 75);

                genWriter.WriteEnumeratedValue(tag, ReadEnumerated.ULongBacked.Fluff);
                objWriter.WriteEnumeratedValue(tag, (object)ReadEnumerated.ULongBacked.Fluff);

                Verify(objWriter, genWriter.Encode().ByteArrayToHex());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteEnumeratedValue_ConstructedIgnored(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteEnumeratedValue(
                    new Asn1Tag(UniversalTagNumber.Enumerated, isConstructed: true),
                    ReadEnumerated.ULongBacked.Fluff);

                writer.WriteEnumeratedValue(
                    new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true),
                    (object)ReadEnumerated.SByteBacked.Fluff);

                Verify(writer, "0A0900FACEF00DCAFEBEEF" + "800153");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void WriteAfterDispose(bool empty)
        {
            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                if (!empty)
                {
                    writer.WriteNull();
                }

                writer.Dispose();

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteEnumeratedValue(false));

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteEnumeratedValue((object)"hi"));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteEnumeratedValue((object)null));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteEnumeratedValue(ReadEnumerated.UIntBacked.Fluff));

                // Type is [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue(ReadNamedBitList.LongFlags.Mid));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteEnumeratedValue((object)ReadEnumerated.SByteBacked.Fluff));

                // Unboxed type is [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue((object)ReadNamedBitList.ULongFlags.Mid));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, false));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, (object)"hi"));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, (object)null));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, ReadEnumerated.UIntBacked.Fluff));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, ReadNamedBitList.LongFlags.Mid));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, (object)ReadEnumerated.SByteBacked.Fluff));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteEnumeratedValue(Asn1Tag.Integer, (object)ReadNamedBitList.ULongFlags.Mid));

                Asn1Tag tag = new Asn1Tag(TagClass.Private, 6);

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteEnumeratedValue(tag, false));

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteEnumeratedValue(tag, (object)"hi"));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteEnumeratedValue(tag, (object)null));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteEnumeratedValue(tag, ReadEnumerated.UIntBacked.Fluff));

                // Type is [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue(tag, ReadNamedBitList.LongFlags.Mid));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteEnumeratedValue(tag, (object)ReadEnumerated.SByteBacked.Fluff));

                // Unboxed type is [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteEnumeratedValue(tag, (object)ReadNamedBitList.ULongFlags.Mid));
            }
        }
    }
}
