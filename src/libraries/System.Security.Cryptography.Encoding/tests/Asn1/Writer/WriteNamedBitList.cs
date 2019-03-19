// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteNamedBitList : Asn1WriterTests
    {
        [Theory]
        [InlineData(
            PublicEncodingRules.BER,
            "030100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            PublicEncodingRules.CER,
            "030100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            PublicEncodingRules.DER,
            "030100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            PublicEncodingRules.BER,
            "0309000000000000000003",
            ReadNamedBitList.ULongFlags.Max | ReadNamedBitList.ULongFlags.AlmostMax)]
        [InlineData(
            PublicEncodingRules.CER,
            "0309010000000080000002",
            ReadNamedBitList.LongFlags.Max | ReadNamedBitList.LongFlags.Mid)]
        [InlineData(
            PublicEncodingRules.DER,
            "030204B0",
            ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment)]
        public static void VerifyWriteNamedBitList(
            PublicEncodingRules ruleSet,
            string expectedHex,
            object value)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteNamedBitList(value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(
            PublicEncodingRules.BER,
            "C00100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            PublicEncodingRules.CER,
            "410100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            PublicEncodingRules.DER,
            "820100",
            ReadNamedBitList.ULongFlags.None)]
        [InlineData(
            PublicEncodingRules.BER,
            "C009000000000000000003",
            ReadNamedBitList.ULongFlags.Max | ReadNamedBitList.ULongFlags.AlmostMax)]
        [InlineData(
            PublicEncodingRules.CER,
            "4109010000000080000002",
            ReadNamedBitList.LongFlags.Max | ReadNamedBitList.LongFlags.Mid)]
        [InlineData(
            PublicEncodingRules.DER,
            "820204B0",
            ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment)]
        public static void VerifyWriteNamedBitList_WithTag(
            PublicEncodingRules ruleSet,
            string expectedHex,
            object value)
        {
            int ruleSetVal = (int)ruleSet;
            TagClass tagClass = (TagClass)(byte)(ruleSetVal << 6);

            if (tagClass == TagClass.Universal)
                tagClass = TagClass.Private;

            Asn1Tag tag = new Asn1Tag(tagClass, ruleSetVal);

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteNamedBitList(tag, value);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_Generic(PublicEncodingRules ruleSet)
        {
            using (AsnWriter objWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            using (AsnWriter genWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                var flagsValue =
                    ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                    ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                    ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment;

                genWriter.WriteNamedBitList(flagsValue);
                objWriter.WriteNamedBitList((object)flagsValue);

                Verify(genWriter, objWriter.Encode().ByteArrayToHex());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_Generic_WithTag(PublicEncodingRules ruleSet)
        {
            using (AsnWriter objWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            using (AsnWriter genWriter = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 52);

                var flagsValue =
                    ReadNamedBitList.X509KeyUsageCSharpStyle.DigitalSignature |
                    ReadNamedBitList.X509KeyUsageCSharpStyle.KeyEncipherment |
                    ReadNamedBitList.X509KeyUsageCSharpStyle.DataEncipherment;

                genWriter.WriteNamedBitList(tag, flagsValue);
                objWriter.WriteNamedBitList(tag, (object)flagsValue);

                Verify(genWriter, objWriter.Encode().ByteArrayToHex());
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_NonNull(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteNamedBitList(null));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteNamedBitList(new Asn1Tag(TagClass.ContextSpecific, 1), null));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_EnumRequired(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Assert.Throws<ArgumentException>(
                    () => writer.WriteNamedBitList(3));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteNamedBitList(new Asn1Tag(TagClass.ContextSpecific, 1), 3));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteNamedBitList((object)3));

                Assert.Throws<ArgumentException>(
                    () => writer.WriteNamedBitList(new Asn1Tag(TagClass.ContextSpecific, 1), (object)3));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_FlagsEnumRequired(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList(AsnEncodingRules.BER));

                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList(
                        new Asn1Tag(TagClass.ContextSpecific, 1),
                        AsnEncodingRules.BER));

                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList((object)AsnEncodingRules.BER));

                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList(
                        new Asn1Tag(TagClass.ContextSpecific, 1),
                        (object)AsnEncodingRules.BER));
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void VerifyWriteNamedBitList_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(
                        Asn1Tag.EndOfContents,
                        StringSplitOptions.RemoveEmptyEntries));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(
                        Asn1Tag.EndOfContents,
                        (object)StringSplitOptions.RemoveEmptyEntries));
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
                    () => writer.WriteNamedBitList(false));

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteNamedBitList((object)"hi"));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteNamedBitList((object)null));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteNamedBitList(ReadNamedBitList.LongFlags.Mid));

                // Type is not [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList(ReadEnumerated.UIntBacked.Fluff));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteNamedBitList((object)ReadNamedBitList.ULongFlags.Mid));

                // Unboxed type is [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList((object)ReadEnumerated.SByteBacked.Fluff));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, false));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, (object)"hi"));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, (object)null));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, ReadNamedBitList.LongFlags.Mid));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, ReadEnumerated.UIntBacked.Fluff));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, (object)ReadNamedBitList.ULongFlags.Mid));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteNamedBitList(Asn1Tag.Integer, (object)ReadEnumerated.SByteBacked.Fluff));

                Asn1Tag tag = new Asn1Tag(TagClass.Private, 6);

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteNamedBitList(tag, false));

                // Type not enum
                AssertExtensions.Throws<ArgumentException>(
                    "enumType",
                    () => writer.WriteNamedBitList(tag, (object)"hi"));

                AssertExtensions.Throws<ArgumentNullException>(
                    "enumValue",
                    () => writer.WriteNamedBitList(tag, (object)null));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteNamedBitList(tag, ReadNamedBitList.LongFlags.Mid));

                // Type is [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList(tag, ReadEnumerated.UIntBacked.Fluff));

                // valid input
                Assert.Throws<ObjectDisposedException>(
                    () => writer.WriteNamedBitList(tag, (object)ReadNamedBitList.ULongFlags.Mid));

                // Unboxed type is not [Flags]
                AssertExtensions.Throws<ArgumentException>(
                    "tEnum",
                    () => writer.WriteNamedBitList(tag, (object)ReadEnumerated.SByteBacked.Fluff));
            }
        }
    }
}
