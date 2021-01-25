// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteOctetString : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void WriteEmpty(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteOctetString(ReadOnlySpan<byte>.Empty);

            Verify(writer, "0400");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 1, "0401")]
        [InlineData(AsnEncodingRules.CER, 2, "0402")]
        [InlineData(AsnEncodingRules.DER, 3, "0403")]
        [InlineData(AsnEncodingRules.BER, 126, "047E")]
        [InlineData(AsnEncodingRules.CER, 127, "047F")]
        [InlineData(AsnEncodingRules.DER, 128, "048180")]
        [InlineData(AsnEncodingRules.BER, 1000, "048203E8")]
        [InlineData(AsnEncodingRules.CER, 1000, "048203E8")]
        [InlineData(AsnEncodingRules.DER, 1000, "048203E8")]
        [InlineData(AsnEncodingRules.BER, 1001, "048203E9")]
        [InlineData(AsnEncodingRules.DER, 1001, "048203E9")]
        [InlineData(AsnEncodingRules.BER, 2001, "048207D1")]
        [InlineData(AsnEncodingRules.DER, 2001, "048207D1")]
        public void WritePrimitive(AsnEncodingRules ruleSet, int length, string hexStart)
        {
            string payloadHex = new string('0', 2 * length);
            string expectedHex = hexStart + payloadHex;
            byte[] data = new byte[length];

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteOctetString(data);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(1001, "2480048203E8", "0401")]
        [InlineData(1999, "2480048203E8", "048203E7")]
        [InlineData(2000, "2480048203E8", "048203E8")]
        public void WriteSegmentedCER(int length, string hexStart, string hexStart2)
        {
            string payload1Hex = new string('8', 2000);
            string payload2Hex = new string('8', (length - 1000) * 2);
            string expectedHex = hexStart + payload1Hex + hexStart2 + payload2Hex + "0000";
            byte[] data = new byte[length];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0x88;
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            writer.WriteOctetString(data);

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 0, false)]
        [InlineData(AsnEncodingRules.CER, 0, false)]
        [InlineData(AsnEncodingRules.DER, 0, false)]
        [InlineData(AsnEncodingRules.BER, 999, false)]
        [InlineData(AsnEncodingRules.CER, 999, false)]
        [InlineData(AsnEncodingRules.DER, 999, false)]
        [InlineData(AsnEncodingRules.BER, 1000, false)]
        [InlineData(AsnEncodingRules.CER, 1000, false)]
        [InlineData(AsnEncodingRules.DER, 1000, false)]
        [InlineData(AsnEncodingRules.BER, 1001, false)]
        [InlineData(AsnEncodingRules.CER, 1001, true)]
        [InlineData(AsnEncodingRules.DER, 1001, false)]
        [InlineData(AsnEncodingRules.BER, 1998, false)]
        [InlineData(AsnEncodingRules.CER, 1998, true)]
        [InlineData(AsnEncodingRules.DER, 1998, false)]
        [InlineData(AsnEncodingRules.BER, 1999, false)]
        [InlineData(AsnEncodingRules.CER, 1999, true)]
        [InlineData(AsnEncodingRules.DER, 1999, false)]
        [InlineData(AsnEncodingRules.BER, 2000, false)]
        [InlineData(AsnEncodingRules.CER, 2000, true)]
        [InlineData(AsnEncodingRules.DER, 2000, false)]
        [InlineData(AsnEncodingRules.BER, 2001, false)]
        [InlineData(AsnEncodingRules.CER, 2001, true)]
        [InlineData(AsnEncodingRules.DER, 2001, false)]
        [InlineData(AsnEncodingRules.BER, 4096, false)]
        [InlineData(AsnEncodingRules.CER, 4096, true)]
        [InlineData(AsnEncodingRules.DER, 4096, false)]
        public void VerifyWriteOctetString_PrimitiveOrConstructed(
            AsnEncodingRules ruleSet,
            int payloadLength,
            bool expectConstructed)
        {
            byte[] data = new byte[payloadLength];

            Asn1Tag[] tagsToTry =
            {
                new Asn1Tag(UniversalTagNumber.OctetString),
                new Asn1Tag(UniversalTagNumber.OctetString, isConstructed: true),
                new Asn1Tag(TagClass.Private, 87),
                new Asn1Tag(TagClass.ContextSpecific, 13, isConstructed: true),
            };

            byte[] answerBuf = new byte[payloadLength + 100];

            foreach (Asn1Tag toTry in tagsToTry)
            {
                AsnWriter writer = new AsnWriter(ruleSet);
                writer.WriteOctetString(data, toTry);

                Assert.True(writer.TryEncode(answerBuf, out _));
                Assert.True(Asn1Tag.TryDecode(answerBuf, out Asn1Tag writtenTag, out _));

                if (expectConstructed)
                {
                    Assert.True(writtenTag.IsConstructed, $"writtenTag.IsConstructed ({toTry})");
                }
                else
                {
                    Assert.False(writtenTag.IsConstructed, $"writtenTag.IsConstructed ({toTry})");
                }

                Assert.Equal(toTry.TagClass, writtenTag.TagClass);
                Assert.Equal(toTry.TagValue, writtenTag.TagValue);
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteOctetString_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteOctetString(ReadOnlySpan<byte>.Empty, Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteOctetString(new byte[1], Asn1Tag.Null));
        }
    }
}
