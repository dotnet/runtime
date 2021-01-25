// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class PushPopOctetString : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopNewWriter(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Assert.Throws<InvalidOperationException>(
                () => writer.PopOctetString());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopNewWriter_CustomTag(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            Assert.Throws<InvalidOperationException>(
                () => writer.PopOctetString(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopBalancedWriter(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString();
            writer.PopOctetString();

            Assert.Throws<InvalidOperationException>(
                () => writer.PopOctetString());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PopBalancedWriter_CustomTag(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString();
            writer.PopOctetString();

            Assert.Throws<InvalidOperationException>(
                () => writer.PopOctetString(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushCustom_PopStandard(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true));

            Assert.Throws<InvalidOperationException>(
                () => writer.PopOctetString());
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushStandard_PopCustom(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString();

            Assert.Throws<InvalidOperationException>(
                () => writer.PopOctetString(new Asn1Tag(TagClass.ContextSpecific, (int)ruleSet, true)));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushPrimitive_PopStandard(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString(new Asn1Tag(UniversalTagNumber.OctetString));
            writer.PopOctetString();

            Verify(writer, "0400");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushCustomPrimitive_PopConstructed(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString(new Asn1Tag(TagClass.Private, 5));
            writer.PopOctetString(new Asn1Tag(TagClass.Private, 5, true));

            Verify(writer, "C500");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushStandard_PopPrimitive(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString();
            writer.PopOctetString(new Asn1Tag(UniversalTagNumber.OctetString));

            Verify(writer, "0400");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void PushCustomConstructed_PopPrimitive(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.PushOctetString(new Asn1Tag(TagClass.Private, (int)ruleSet, true));
            writer.PopOctetString(new Asn1Tag(TagClass.Private, (int)ruleSet));

            byte tag = (byte)((int)ruleSet | 0b1100_0000);
            string tagHex = tag.ToString("X2");
            string expectedHex = tagHex + "00";

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void LargePayload_1000(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            using (writer.PushOctetString(new Asn1Tag(TagClass.ContextSpecific, 9)))
            {
                byte[] tmp = new byte[496];
                writer.WriteOctetString(tmp);
                writer.WriteOctetString(tmp, new Asn1Tag(TagClass.ContextSpecific, 10));
            }

            string zeroHex496Bytes = new string('0', 496 * 2);

            string expectedHex =
                // Tag
                "89" +
                // Length
                "8203E8" +
                // First written content
                "048201F0" + zeroHex496Bytes +
                // Second written content
                "8A8201F0" + zeroHex496Bytes;

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void LargePayload_1001(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            using (writer.PushOctetString(new Asn1Tag(TagClass.Private, 9)))
            {
                byte[] tmp = new byte[497];
                writer.WriteOctetString(tmp.AsSpan(0, 496));
                writer.WriteOctetString(tmp, new Asn1Tag(TagClass.Application, 10));
            }

            string zeroHex496Bytes = new string('0', 496 * 2);

            string expectedHex;

            if (ruleSet == AsnEncodingRules.CER)
            {
                // This moved into the constructed encoding form.
                // Tag
                expectedHex =
                    // Tag
                    "E9" +
                    // Indefinite length
                    "80" +
                    // Definite octet string
                    "04" +
                    // 1000 bytes
                    "8203E8" +
                    // First written content
                    "048201F0" + zeroHex496Bytes +
                    // Second written content tag, length, and first 498 payload bytes
                    "4A8201F1" + zeroHex496Bytes +
                    // Second definite octet string, 1 byte, { 0x00 } payload
                    "040100" +
                    // End indefinite length
                    "0000";
            }
            else
            {
                expectedHex =
                    // Tag
                    "C9" +
                    // Length
                    "8203E9" +
                    // First written content
                    "048201F0" + zeroHex496Bytes +
                    // Second written content
                    "4A8201F1" + zeroHex496Bytes + "00";
            }

            Verify(writer, expectedHex);
        }
    }
}
