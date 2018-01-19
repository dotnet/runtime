// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteOctetString : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void WriteEmpty(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteOctetString(ReadOnlySpan<byte>.Empty);

                Verify(writer, "0400");
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 1, "0401")]
        [InlineData(PublicEncodingRules.CER, 2, "0402")]
        [InlineData(PublicEncodingRules.DER, 3, "0403")]
        [InlineData(PublicEncodingRules.BER, 126, "047E")]
        [InlineData(PublicEncodingRules.CER, 127, "047F")]
        [InlineData(PublicEncodingRules.DER, 128, "048180")]
        [InlineData(PublicEncodingRules.BER, 1000, "048203E8")]
        [InlineData(PublicEncodingRules.CER, 1000, "048203E8")]
        [InlineData(PublicEncodingRules.DER, 1000, "048203E8")]
        [InlineData(PublicEncodingRules.BER, 1001, "048203E9")]
        [InlineData(PublicEncodingRules.DER, 1001, "048203E9")]
        [InlineData(PublicEncodingRules.BER, 2001, "048207D1")]
        [InlineData(PublicEncodingRules.DER, 2001, "048207D1")]
        public void WritePrimitive(PublicEncodingRules ruleSet, int length, string hexStart)
        {
            string payloadHex = new string('0', 2 * length);
            string expectedHex = hexStart + payloadHex;
            byte[] data = new byte[length];

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteOctetString(data);

                Verify(writer, expectedHex);
            }
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

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                writer.WriteOctetString(data);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 0, false)]
        [InlineData(PublicEncodingRules.CER, 0, false)]
        [InlineData(PublicEncodingRules.DER, 0, false)]
        [InlineData(PublicEncodingRules.BER, 999, false)]
        [InlineData(PublicEncodingRules.CER, 999, false)]
        [InlineData(PublicEncodingRules.DER, 999, false)]
        [InlineData(PublicEncodingRules.BER, 1000, false)]
        [InlineData(PublicEncodingRules.CER, 1000, false)]
        [InlineData(PublicEncodingRules.DER, 1000, false)]
        [InlineData(PublicEncodingRules.BER, 1001, false)]
        [InlineData(PublicEncodingRules.CER, 1001, true)]
        [InlineData(PublicEncodingRules.DER, 1001, false)]
        [InlineData(PublicEncodingRules.BER, 1998, false)]
        [InlineData(PublicEncodingRules.CER, 1998, true)]
        [InlineData(PublicEncodingRules.DER, 1998, false)]
        [InlineData(PublicEncodingRules.BER, 1999, false)]
        [InlineData(PublicEncodingRules.CER, 1999, true)]
        [InlineData(PublicEncodingRules.DER, 1999, false)]
        [InlineData(PublicEncodingRules.BER, 2000, false)]
        [InlineData(PublicEncodingRules.CER, 2000, true)]
        [InlineData(PublicEncodingRules.DER, 2000, false)]
        [InlineData(PublicEncodingRules.BER, 2001, false)]
        [InlineData(PublicEncodingRules.CER, 2001, true)]
        [InlineData(PublicEncodingRules.DER, 2001, false)]
        [InlineData(PublicEncodingRules.BER, 4096, false)]
        [InlineData(PublicEncodingRules.CER, 4096, true)]
        [InlineData(PublicEncodingRules.DER, 4096, false)]
        public void VerifyWriteOctetString_PrimitiveOrConstructed(
            PublicEncodingRules ruleSet,
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
                using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
                {
                    writer.WriteOctetString(toTry, data);

                    Assert.True(writer.TryEncode(answerBuf, out _));
                }
                Assert.True(Asn1Tag.TryParse(answerBuf, out Asn1Tag writtenTag, out _));

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
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteOctetString_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteOctetString(Asn1Tag.EndOfContents, ReadOnlySpan<byte>.Empty));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteOctetString(Asn1Tag.EndOfContents, new byte[1]));
            }
        }
    }
}
