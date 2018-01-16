// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public class WriteBitString : Asn1WriterTests
    {
        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void WriteEmpty(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteBitString(ReadOnlySpan<byte>.Empty);

                Verify(writer, "030100");
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, 1, 1, "030201")]
        [InlineData(PublicEncodingRules.CER, 2, 1, "030301")]
        [InlineData(PublicEncodingRules.DER, 3, 1, "030401")]
        [InlineData(PublicEncodingRules.BER, 126, 0, "037F00")]
        [InlineData(PublicEncodingRules.CER, 127, 3, "03818003")]
        [InlineData(PublicEncodingRules.BER, 999, 0, "038203E800")]
        [InlineData(PublicEncodingRules.CER, 999, 0, "038203E800")]
        [InlineData(PublicEncodingRules.DER, 999, 0, "038203E800")]
        [InlineData(PublicEncodingRules.BER, 1000, 0, "038203E900")]
        [InlineData(PublicEncodingRules.DER, 1000, 0, "038203E900")]
        [InlineData(PublicEncodingRules.BER, 2000, 0, "038207D100")]
        [InlineData(PublicEncodingRules.DER, 2000, 0, "038207D100")]
        public void WritePrimitive(PublicEncodingRules ruleSet, int length, int unusedBitCount, string hexStart)
        {
            string payloadHex = new string('0', 2 * length);
            string expectedHex = hexStart + payloadHex;
            byte[] data = new byte[length];

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                writer.WriteBitString(data, unusedBitCount);

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(1000, 1, "2380038203E800", "030201")]
        [InlineData(999*2, 3, "2380038203E800", "038203E803")]
        public void WriteSegmentedCER(int length, int unusedBitCount, string hexStart, string hexStart2)
        {
            string payload1Hex = new string('8', 999 * 2);
            string payload2Hex = new string('8', (length - 999) * 2);
            string expectedHex = hexStart + payload1Hex + hexStart2 + payload2Hex + "0000";
            byte[] data = new byte[length];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0x88;
            }

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.CER))
            {
                writer.WriteBitString(data, unusedBitCount);

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
        [InlineData(PublicEncodingRules.CER, 1000, true)]
        [InlineData(PublicEncodingRules.DER, 1000, false)]
        [InlineData(PublicEncodingRules.BER, 1998, false)]
        [InlineData(PublicEncodingRules.CER, 1998, true)]
        [InlineData(PublicEncodingRules.BER, 4096, false)]
        [InlineData(PublicEncodingRules.CER, 4096, true)]
        [InlineData(PublicEncodingRules.DER, 4096, false)]
        public void VerifyWriteBitString_PrimitiveOrConstructed(
            PublicEncodingRules ruleSet,
            int payloadLength,
            bool expectConstructed)
        {
            byte[] data = new byte[payloadLength];

            Asn1Tag[] tagsToTry =
            {
                new Asn1Tag(UniversalTagNumber.BitString),
                new Asn1Tag(UniversalTagNumber.BitString, isConstructed: true),
                new Asn1Tag(TagClass.Private, 87),
                new Asn1Tag(TagClass.ContextSpecific, 13, isConstructed: true),
            };

            byte[] answerBuf = new byte[payloadLength + 100];

            foreach (Asn1Tag toTry in tagsToTry)
            {
                Asn1Tag writtenTag;

                using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
                {
                    writer.WriteBitString(toTry, data);

                    Assert.True(writer.TryEncode(answerBuf, out _));
                    Assert.True(Asn1Tag.TryParse(answerBuf, out writtenTag, out _));
                }

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
        [InlineData(PublicEncodingRules.BER, 0, "FF", false)]
        [InlineData(PublicEncodingRules.BER, 1, "FE", false)]
        [InlineData(PublicEncodingRules.CER, 1, "FE", false)]
        [InlineData(PublicEncodingRules.DER, 1, "FE", false)]
        [InlineData(PublicEncodingRules.BER, 1, "FF", true)]
        [InlineData(PublicEncodingRules.CER, 1, "FF", true)]
        [InlineData(PublicEncodingRules.DER, 1, "FF", true)]
        [InlineData(PublicEncodingRules.BER, 7, "C0", true)]
        [InlineData(PublicEncodingRules.CER, 7, "C0", true)]
        [InlineData(PublicEncodingRules.DER, 7, "C0", true)]
        [InlineData(PublicEncodingRules.BER, 7, "80", false)]
        [InlineData(PublicEncodingRules.CER, 7, "80", false)]
        [InlineData(PublicEncodingRules.DER, 7, "80", false)]
        [InlineData(PublicEncodingRules.DER, 7, "40", true)]
        [InlineData(PublicEncodingRules.DER, 6, "40", false)]
        [InlineData(PublicEncodingRules.DER, 6, "C0", false)]
        [InlineData(PublicEncodingRules.DER, 6, "20", true)]
        [InlineData(PublicEncodingRules.DER, 5, "20", false)]
        [InlineData(PublicEncodingRules.DER, 5, "A0", false)]
        [InlineData(PublicEncodingRules.DER, 5, "10", true)]
        [InlineData(PublicEncodingRules.DER, 4, "10", false)]
        [InlineData(PublicEncodingRules.DER, 4, "90", false)]
        [InlineData(PublicEncodingRules.DER, 4, "30", false)]
        [InlineData(PublicEncodingRules.DER, 4, "08", true)]
        [InlineData(PublicEncodingRules.DER, 4, "88", true)]
        [InlineData(PublicEncodingRules.DER, 3, "08", false)]
        [InlineData(PublicEncodingRules.DER, 3, "A8", false)]
        [InlineData(PublicEncodingRules.DER, 3, "04", true)]
        [InlineData(PublicEncodingRules.DER, 3, "14", true)]
        [InlineData(PublicEncodingRules.DER, 2, "04", false)]
        [InlineData(PublicEncodingRules.DER, 2, "0C", false)]
        [InlineData(PublicEncodingRules.DER, 2, "FC", false)]
        [InlineData(PublicEncodingRules.DER, 2, "02", true)]
        [InlineData(PublicEncodingRules.DER, 2, "82", true)]
        [InlineData(PublicEncodingRules.DER, 2, "FE", true)]
        [InlineData(PublicEncodingRules.DER, 1, "02", false)]
        [InlineData(PublicEncodingRules.DER, 1, "82", false)]
        [InlineData(PublicEncodingRules.DER, 1, "FE", false)]
        [InlineData(PublicEncodingRules.DER, 1, "80", false)]
        public static void WriteBitString_UnusedBitCount_MustBeValid(
            PublicEncodingRules ruleSet,
            int unusedBitCount,
            string inputHex,
            bool expectThrow)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                if (expectThrow)
                {
                    Assert.Throws<CryptographicException>(
                        () => writer.WriteBitString(inputBytes, unusedBitCount));

                    Assert.Throws<CryptographicException>(
                        () => writer.WriteBitString(
                            new Asn1Tag(TagClass.ContextSpecific, 3),
                            inputBytes,
                            unusedBitCount));

                    return;
                }

                byte[] output = new byte[512];
                writer.WriteBitString(inputBytes, unusedBitCount);
                Assert.True(writer.TryEncode(output, out int bytesWritten));

                // This assumes that inputBytes is never more than 999 (and avoids CER constructed forms)
                Assert.Equal(unusedBitCount, output[bytesWritten - inputBytes.Length - 1]);

                writer.WriteBitString(new Asn1Tag(TagClass.ContextSpecific, 9), inputBytes, unusedBitCount);
                Assert.True(writer.TryEncode(output, out bytesWritten));

                Assert.Equal(unusedBitCount, output[bytesWritten - inputBytes.Length - 1]);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, -1)]
        [InlineData(PublicEncodingRules.CER, -1)]
        [InlineData(PublicEncodingRules.DER, -1)]
        [InlineData(PublicEncodingRules.BER, -2)]
        [InlineData(PublicEncodingRules.CER, -2)]
        [InlineData(PublicEncodingRules.DER, -2)]
        [InlineData(PublicEncodingRules.BER, 8)]
        [InlineData(PublicEncodingRules.CER, 8)]
        [InlineData(PublicEncodingRules.DER, 8)]
        [InlineData(PublicEncodingRules.BER, 9)]
        [InlineData(PublicEncodingRules.CER, 9)]
        [InlineData(PublicEncodingRules.DER, 9)]
        [InlineData(PublicEncodingRules.BER, 1048576)]
        [InlineData(PublicEncodingRules.CER, 1048576)]
        [InlineData(PublicEncodingRules.DER, 1048576)]
        public static void UnusedBitCounts_Bounds(PublicEncodingRules ruleSet, int unusedBitCount)
        {
            byte[] data = new byte[5];

            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                ArgumentOutOfRangeException exception = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    nameof(unusedBitCount),
                    () => writer.WriteBitString(data, unusedBitCount));

                Assert.Equal(unusedBitCount, exception.ActualValue);

                exception = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    nameof(unusedBitCount),
                    () => writer.WriteBitString(new Asn1Tag(TagClass.ContextSpecific, 5), data, unusedBitCount));

                Assert.Equal(unusedBitCount, exception.ActualValue);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public static void EmptyData_Requires0UnusedBits(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                Assert.Throws<CryptographicException>(
                    () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, 1));

                Assert.Throws<CryptographicException>(
                    () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, 7));

                Asn1Tag contextTag = new Asn1Tag(TagClass.ContextSpecific, 19);

                Assert.Throws<CryptographicException>(
                    () => writer.WriteBitString(contextTag, ReadOnlySpan<byte>.Empty, 1));

                Assert.Throws<CryptographicException>(
                    () => writer.WriteBitString(contextTag, ReadOnlySpan<byte>.Empty, 7));

                writer.WriteBitString(ReadOnlySpan<byte>.Empty, 0);
                writer.WriteBitString(contextTag, ReadOnlySpan<byte>.Empty, 0);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER, PublicTagClass.Universal, 3, "030100")]
        [InlineData(PublicEncodingRules.CER, PublicTagClass.Universal, 3, "030100")]
        [InlineData(PublicEncodingRules.DER, PublicTagClass.Universal, 3, "030100")]
        [InlineData(PublicEncodingRules.BER, PublicTagClass.Private, 1, "C10100")]
        [InlineData(PublicEncodingRules.CER, PublicTagClass.Application, 5, "450100")]
        [InlineData(PublicEncodingRules.DER, PublicTagClass.ContextSpecific, 32, "9F200100")]
        public static void EmptyData_Allows0UnusedBits(
            PublicEncodingRules ruleSet,
            PublicTagClass tagClass,
            int tagValue,
            string expectedHex)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {

                if (tagClass == PublicTagClass.Universal)
                {
                    Debug.Assert(tagValue == (int)UniversalTagNumber.BitString);
                    writer.WriteBitString(ReadOnlySpan<byte>.Empty, 0);
                }
                else
                {
                    writer.WriteBitString(new Asn1Tag((TagClass)tagClass, tagValue), ReadOnlySpan<byte>.Empty, 0);
                }

                Verify(writer, expectedHex);
            }
        }

        [Theory]
        [InlineData(PublicEncodingRules.BER)]
        [InlineData(PublicEncodingRules.CER)]
        [InlineData(PublicEncodingRules.DER)]
        public void VerifyWriteBitString_EndOfContents(PublicEncodingRules ruleSet)
        {
            using (AsnWriter writer = new AsnWriter((AsnEncodingRules)ruleSet))
            {
                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteBitString(Asn1Tag.EndOfContents, ReadOnlySpan<byte>.Empty));

                AssertExtensions.Throws<ArgumentException>(
                    "tag",
                    () => writer.WriteBitString(Asn1Tag.EndOfContents, new byte[1]));
            }
        }
    }
}
