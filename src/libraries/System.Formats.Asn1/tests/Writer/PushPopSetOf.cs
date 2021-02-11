// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class PushPopSetOf : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopNewWriter(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopNewWriter_CustomTag(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopBalancedWriter(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf();
            writer.PopSetOf();

            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopBalancedWriter_CustomTag(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf();
            writer.PopSetOf();

            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushCustom_PopStandard(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true));

            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushStandard_PopCustom(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf();

            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushPrimitive_PopStandard(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf(new Asn1Tag(UniversalTagNumber.SetOf));
            writer.PopSetOf();

            if (ruleSet == AsnEncodingRules.CER)
            {
                Verify(writer, "31800000");
            }
            else
            {
                Verify(writer, "3100");
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushCustomPrimitive_PopConstructed(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf(new Asn1Tag(TagClass.Private, 5));
            writer.PopSetOf(new Asn1Tag(TagClass.Private, 5, true));

            if (ruleSet == AsnEncodingRules.CER)
            {
                Verify(writer, "E5800000");
            }
            else
            {
                Verify(writer, "E500");
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushStandard_PopPrimitive(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf();
            writer.PopSetOf(new Asn1Tag(UniversalTagNumber.SetOf));

            if (ruleSet == AsnEncodingRules.CER)
            {
                Verify(writer, "31800000");
            }
            else
            {
                Verify(writer, "3100");
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushCustomConstructed_PopPrimitive(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf(new Asn1Tag(TagClass.Private, (int)ruleSet, true));
            writer.PopSetOf(new Asn1Tag(TagClass.Private, (int)ruleSet));

            byte tag = (byte)((int)ruleSet | 0b1110_0000);
            string tagHex = tag.ToString("X2");
            string rest = ruleSet == AsnEncodingRules.CER ? "800000" : "00";

            Verify(writer, tagHex + rest);
        }

        [Fact]
        public static void BER_WritesDefinite_Empty()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.PushSetOf();
            writer.PopSetOf();

            Verify(writer, "3100");
        }

        [Fact]
        public static void CER_WritesIndefinite_Empty()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            writer.PushSetOf();
            writer.PopSetOf();

            Verify(writer, "31800000");
        }

        [Fact]
        public static void DER_WritesDefinite_CustomTag_Empty()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSetOf();
            writer.PopSetOf();

            Verify(writer, "3100");
        }

        [Fact]
        public static void BER_WritesDefinite_CustomTag__Empty()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            Asn1Tag tag = new Asn1Tag(TagClass.Private, 15, true);
            writer.PushSetOf(tag);
            writer.PopSetOf(tag);

            Verify(writer, "EF00");
        }

        [Fact]
        public static void CER_WritesIndefinite_CustomTag__Empty()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            Asn1Tag tag = new Asn1Tag(TagClass.Application, 91, true);
            writer.PushSetOf(tag);
            writer.PopSetOf(tag);

            Verify(writer, "7F5B800000");
        }

        [Fact]
        public static void DER_WritesDefinite_CustomTag__Empty()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 30, true);
            writer.PushSetOf(tag);
            writer.PopSetOf(tag);

            Verify(writer, "BE00");
        }

        private static void TestNested(AsnWriter writer, Asn1Tag alt, string expectedHex)
        {
            // Written in pre-sorted order, since sorting is a different test.
            writer.PushSetOf();
            {
                writer.PushSetOf();
                {
                    writer.PushSetOf(alt);
                    {
                        writer.PushSetOf();
                        writer.PopSetOf();
                    }

                    writer.PopSetOf(alt);
                }

                writer.PopSetOf();

                writer.PushSetOf(alt);
                writer.PopSetOf(alt);
            }

            writer.PopSetOf();

            Verify(writer, expectedHex);
        }

        [Fact]
        public static void BER_Nested()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            Asn1Tag alt = new Asn1Tag(TagClass.Private, 127, true);

            TestNested(writer, alt, "310A3105FF7F023100FF7F00");
        }

        [Fact]
        public static void CER_Nested()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            Asn1Tag alt = new Asn1Tag(TagClass.ContextSpecific, 12, true);

            TestNested(writer, alt, "31803180AC803180000000000000AC8000000000");
        }

        [Fact]
        public static void DER_Nested()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            Asn1Tag alt = new Asn1Tag(TagClass.Application, 5, true);

            TestNested(writer, alt, "31083104650231006500");
        }

        private static void SimpleContentShiftCore(AsnWriter writer, string expectedHex)
        {
            writer.PushSetOf();

            // F00DF00D...F00DF00D
            byte[] contentBytes = new byte[126];

            for (int i = 0; i < contentBytes.Length; i += 2)
            {
                contentBytes[i] = 0xF0;
                contentBytes[i + 1] = 0x0D;
            }

            writer.WriteOctetString(contentBytes);
            writer.PopSetOf();

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void SimpleContentShift(AsnEncodingRules ruleSet)
        {
            const string ExpectedHex =
                "318180" +
                    "047E" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D";

            AsnWriter writer = new AsnWriter(ruleSet);
            SimpleContentShiftCore(writer, ExpectedHex);
        }

        [Fact]
        public static void SimpleContentShift_CER()
        {
            const string ExpectedHex =
                "3180" +
                    "047E" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                        "F00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00DF00D" +
                    "0000";

            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            SimpleContentShiftCore(writer, ExpectedHex);
        }

        private static void ValidateDataSorting(AsnEncodingRules ruleSet, string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushSetOf();

            // 02 01 FF
            writer.WriteInteger(-1);
            // 02 01 00
            writer.WriteInteger(0);
            // 02 02 00 FF
            writer.WriteInteger(255);
            // 01 01 FF
            writer.WriteBoolean(true);
            // 45 01 00
            writer.WriteBoolean(false, new Asn1Tag(TagClass.Application, 5));
            // 02 01 7F
            writer.WriteInteger(127);
            // 02 01 80
            writer.WriteInteger(sbyte.MinValue);
            // 02 02 00 FE
            writer.WriteInteger(254);
            // 02 01 00
            writer.WriteInteger(0);

            writer.PopSetOf();

            // The correct sort order (CER, DER) is
            // Universal Boolean: true
            // Universal Integer: 0
            // Universal Integer: 0
            // Universal Integer: 127
            // Universal Integer: -128
            // Universal Integer: -1
            // Universal Integer: 254
            // Universal Integer: 255
            // Application 5 (Boolean): false

            // This test would be
            //
            // GrabBag ::= SET OF GrabBagItem
            //
            // GrabBagItem ::= CHOICE (
            //    value INTEGER
            //    bool BOOLEAN
            //    grr [APPLICATION 5] IMPLICIT BOOLEAN
            // )

            Verify(writer, expectedHex);
        }

        [Fact]
        public static void BER_DoesNotSort()
        {
            const string ExpectedHex =
                "311D" +
                    "0201FF" +
                    "020100" +
                    "020200FF" +
                    "0101FF" +
                    "450100" +
                    "02017F" +
                    "020180" +
                    "020200FE" +
                    "020100";

            ValidateDataSorting(AsnEncodingRules.BER, ExpectedHex);
        }

        [Fact]
        public static void CER_SortsData()
        {
            const string ExpectedHex =
                "3180" +
                    "0101FF" +
                    "020100" +
                    "020100" +
                    "02017F" +
                    "020180" +
                    "0201FF" +
                    "020200FE" +
                    "020200FF" +
                    "450100" +
                    "0000";

            ValidateDataSorting(AsnEncodingRules.CER, ExpectedHex);
        }

        [Fact]
        public static void DER_SortsData()
        {
            const string ExpectedHex =
                "311D" +
                    "0101FF" +
                    "020100" +
                    "020100" +
                    "02017F" +
                    "020180" +
                    "0201FF" +
                    "020200FE" +
                    "020200FF" +
                    "450100";

            ValidateDataSorting(AsnEncodingRules.DER, ExpectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, false)]
        [InlineData(AsnEncodingRules.CER, false)]
        [InlineData(AsnEncodingRules.DER, false)]
        [InlineData(AsnEncodingRules.BER, true)]
        [InlineData(AsnEncodingRules.CER, true)]
        [InlineData(AsnEncodingRules.DER, true)]
        public static void CannotEncodeWhileUnbalanced(AsnEncodingRules ruleSet, bool customTag)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (customTag)
            {
                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true));
            }
            else
            {
                writer.PushSetOf();
            }

            int written = -5;

            Assert.Throws<InvalidOperationException>(() => writer.GetEncodedLength());
            Assert.Throws<InvalidOperationException>(() => writer.Encode());
            Assert.Throws<InvalidOperationException>(() => writer.TryEncode(Span<byte>.Empty, out written));
            Assert.Equal(-5, written);

            byte[] buf = new byte[10];
            Assert.Throws<InvalidOperationException>(() => writer.TryEncode(buf, out written));
            Assert.Throws<InvalidOperationException>(() => writer.EncodedValueEquals(buf));
            Assert.Equal(-5, written);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushSetOf_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.PushSetOf(Asn1Tag.Null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushSequence_PopSetOf(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 3);

            writer.PushSequence(tag);

            Assert.Throws<InvalidOperationException>(
                () => writer.PopSetOf(tag));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushSetOf_IdempotentDispose(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 3);

            string expectedHex = ruleSet == AsnEncodingRules.CER ? "A3800000" : "A300";

            using (var scope = writer.PushSetOf(tag))
            {
                writer.PopSetOf(tag);

                // The state is wrong now, so Dispose does nothing
                scope.Dispose();
                scope.Dispose();
                scope.Dispose();
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushSetOf_IdempotentDispose_Complex(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Asn1Tag tag = new Asn1Tag(TagClass.ContextSpecific, 3);

            string expectedHex = ruleSet == AsnEncodingRules.CER ? "A3800000" : "A300";
            expectedHex += expectedHex;

            using (var scope = writer.PushSetOf(tag))
            {
                writer.PopSetOf(tag);

                writer.PushSetOf(tag);
                // The state is wrong now, so Dispose does nothing
                scope.Dispose();
                scope.Dispose();
                scope.Dispose();
                writer.PopSetOf(tag);
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushSetOf_DisposeThrowsIfDeepContained(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            var scope = writer.PushSetOf();
            writer.PushSequence();

            Assert.Throws<InvalidOperationException>(() => scope.Dispose());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushSetOf_DisposeSilentNotContained(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            using (var scope = writer.PushSetOf())
            {
                writer.PopSetOf();

                // Since this has a different write offset, the equals is false, so no exception.
                writer.PushSetOf();
                writer.PushSequence();

                scope.Dispose();
                scope.Dispose();
                scope.Dispose();
            }
        }
    }
}
