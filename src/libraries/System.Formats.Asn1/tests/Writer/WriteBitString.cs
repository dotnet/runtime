// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public class WriteBitString : Asn1WriterTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void WriteEmpty(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteBitString(ReadOnlySpan<byte>.Empty);

            Verify(writer, "030100");
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, 1, 1, "030201")]
        [InlineData(AsnEncodingRules.CER, 2, 1, "030301")]
        [InlineData(AsnEncodingRules.DER, 3, 1, "030401")]
        [InlineData(AsnEncodingRules.BER, 126, 0, "037F00")]
        [InlineData(AsnEncodingRules.CER, 127, 3, "03818003")]
        [InlineData(AsnEncodingRules.BER, 999, 0, "038203E800")]
        [InlineData(AsnEncodingRules.CER, 999, 0, "038203E800")]
        [InlineData(AsnEncodingRules.DER, 999, 0, "038203E800")]
        [InlineData(AsnEncodingRules.BER, 1000, 0, "038203E900")]
        [InlineData(AsnEncodingRules.DER, 1000, 0, "038203E900")]
        [InlineData(AsnEncodingRules.BER, 2000, 0, "038207D100")]
        [InlineData(AsnEncodingRules.DER, 2000, 0, "038207D100")]
        public void WritePrimitive(AsnEncodingRules ruleSet, int length, int unusedBitCount, string hexStart)
        {
            string payloadHex = new string('0', 2 * length);
            string expectedHex = hexStart + payloadHex;
            byte[] data = new byte[length];

            AsnWriter writer = new AsnWriter(ruleSet);
            writer.WriteBitString(data, unusedBitCount);

            Verify(writer, expectedHex);
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

            AsnWriter writer = new AsnWriter(AsnEncodingRules.CER);
            writer.WriteBitString(data, unusedBitCount);

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
        [InlineData(AsnEncodingRules.CER, 1000, true)]
        [InlineData(AsnEncodingRules.DER, 1000, false)]
        [InlineData(AsnEncodingRules.BER, 1998, false)]
        [InlineData(AsnEncodingRules.CER, 1998, true)]
        [InlineData(AsnEncodingRules.BER, 4096, false)]
        [InlineData(AsnEncodingRules.CER, 4096, true)]
        [InlineData(AsnEncodingRules.DER, 4096, false)]
        public void VerifyWriteBitString_PrimitiveOrConstructed(
            AsnEncodingRules ruleSet,
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

                AsnWriter writer = new AsnWriter(ruleSet);
                writer.WriteBitString(data, tag: toTry);

                Assert.True(writer.TryEncode(answerBuf, out _));
                Assert.True(Asn1Tag.TryDecode(answerBuf, out writtenTag, out _));

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
        [InlineData(AsnEncodingRules.BER, 0, "FF", false)]
        [InlineData(AsnEncodingRules.BER, 1, "FE", false)]
        [InlineData(AsnEncodingRules.CER, 1, "FE", false)]
        [InlineData(AsnEncodingRules.DER, 1, "FE", false)]
        [InlineData(AsnEncodingRules.BER, 1, "FF", true)]
        [InlineData(AsnEncodingRules.CER, 1, "FF", true)]
        [InlineData(AsnEncodingRules.DER, 1, "FF", true)]
        [InlineData(AsnEncodingRules.BER, 7, "C0", true)]
        [InlineData(AsnEncodingRules.CER, 7, "C0", true)]
        [InlineData(AsnEncodingRules.DER, 7, "C0", true)]
        [InlineData(AsnEncodingRules.BER, 7, "80", false)]
        [InlineData(AsnEncodingRules.CER, 7, "80", false)]
        [InlineData(AsnEncodingRules.DER, 7, "80", false)]
        [InlineData(AsnEncodingRules.DER, 7, "40", true)]
        [InlineData(AsnEncodingRules.DER, 6, "40", false)]
        [InlineData(AsnEncodingRules.DER, 6, "C0", false)]
        [InlineData(AsnEncodingRules.DER, 6, "20", true)]
        [InlineData(AsnEncodingRules.DER, 5, "20", false)]
        [InlineData(AsnEncodingRules.DER, 5, "A0", false)]
        [InlineData(AsnEncodingRules.DER, 5, "10", true)]
        [InlineData(AsnEncodingRules.DER, 4, "10", false)]
        [InlineData(AsnEncodingRules.DER, 4, "90", false)]
        [InlineData(AsnEncodingRules.DER, 4, "30", false)]
        [InlineData(AsnEncodingRules.DER, 4, "08", true)]
        [InlineData(AsnEncodingRules.DER, 4, "88", true)]
        [InlineData(AsnEncodingRules.DER, 3, "08", false)]
        [InlineData(AsnEncodingRules.DER, 3, "A8", false)]
        [InlineData(AsnEncodingRules.DER, 3, "04", true)]
        [InlineData(AsnEncodingRules.DER, 3, "14", true)]
        [InlineData(AsnEncodingRules.DER, 2, "04", false)]
        [InlineData(AsnEncodingRules.DER, 2, "0C", false)]
        [InlineData(AsnEncodingRules.DER, 2, "FC", false)]
        [InlineData(AsnEncodingRules.DER, 2, "02", true)]
        [InlineData(AsnEncodingRules.DER, 2, "82", true)]
        [InlineData(AsnEncodingRules.DER, 2, "FE", true)]
        [InlineData(AsnEncodingRules.DER, 1, "02", false)]
        [InlineData(AsnEncodingRules.DER, 1, "82", false)]
        [InlineData(AsnEncodingRules.DER, 1, "80", false)]
        public static void WriteBitString_UnusedBitCount_MustBeValid(
            AsnEncodingRules ruleSet,
            int unusedBitCount,
            string inputHex,
            bool expectThrow)
        {
            byte[] inputBytes = inputHex.HexToByteArray();

            AsnWriter writer = new AsnWriter(ruleSet);

            if (expectThrow)
            {
                AssertExtensions.Throws<ArgumentException>(
                    "unusedBitCount",
                    () => writer.WriteBitString(inputBytes, unusedBitCount));

                AssertExtensions.Throws<ArgumentException>(
                    "unusedBitCount",
                    () => writer.WriteBitString(
                        inputBytes,
                        unusedBitCount,
                        new Asn1Tag(TagClass.ContextSpecific, 3)));

                return;
            }

            byte[] output = new byte[512];
            writer.WriteBitString(inputBytes, unusedBitCount);
            Assert.True(writer.TryEncode(output, out int bytesWritten));

            // This assumes that inputBytes is never more than 999 (and avoids CER constructed forms)
            Assert.Equal(unusedBitCount, output[bytesWritten - inputBytes.Length - 1]);

            writer.WriteBitString(inputBytes, unusedBitCount, new Asn1Tag(TagClass.ContextSpecific, 9));
            Assert.True(writer.TryEncode(output, out bytesWritten));

            Assert.Equal(unusedBitCount, output[bytesWritten - inputBytes.Length - 1]);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, -1)]
        [InlineData(AsnEncodingRules.CER, -1)]
        [InlineData(AsnEncodingRules.DER, -1)]
        [InlineData(AsnEncodingRules.BER, -2)]
        [InlineData(AsnEncodingRules.CER, -2)]
        [InlineData(AsnEncodingRules.DER, -2)]
        [InlineData(AsnEncodingRules.BER, 8)]
        [InlineData(AsnEncodingRules.CER, 8)]
        [InlineData(AsnEncodingRules.DER, 8)]
        [InlineData(AsnEncodingRules.BER, 9)]
        [InlineData(AsnEncodingRules.CER, 9)]
        [InlineData(AsnEncodingRules.DER, 9)]
        [InlineData(AsnEncodingRules.BER, 1048576)]
        [InlineData(AsnEncodingRules.CER, 1048576)]
        [InlineData(AsnEncodingRules.DER, 1048576)]
        public static void UnusedBitCounts_Bounds(AsnEncodingRules ruleSet, int unusedBitCount)
        {
            byte[] data = new byte[5];

            AsnWriter writer = new AsnWriter(ruleSet);

            ArgumentOutOfRangeException exception = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                nameof(unusedBitCount),
                () => writer.WriteBitString(data, unusedBitCount));

            Assert.Equal(unusedBitCount, exception.ActualValue);

            exception = AssertExtensions.Throws<ArgumentOutOfRangeException>(
                nameof(unusedBitCount),
                () => writer.WriteBitString(data, unusedBitCount, new Asn1Tag(TagClass.ContextSpecific, 5)));

            Assert.Equal(unusedBitCount, exception.ActualValue);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void EmptyData_Requires0UnusedBits(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            Assert.Throws<ArgumentException>(
                "unusedBitCount",
                () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, 1));

            Assert.Throws<ArgumentException>(
                "unusedBitCount",
                () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, 7));

            Asn1Tag contextTag = new Asn1Tag(TagClass.ContextSpecific, 19);

            Assert.Throws<ArgumentException>(
                "unusedBitCount",
                () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, 1, contextTag));

            Assert.Throws<ArgumentException>(
                "unusedBitCount",
                () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, 7, contextTag));

            writer.WriteBitString(ReadOnlySpan<byte>.Empty, 0);
            writer.WriteBitString(ReadOnlySpan<byte>.Empty, 0, contextTag);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER, TagClass.Universal, 3, "030100")]
        [InlineData(AsnEncodingRules.CER, TagClass.Universal, 3, "030100")]
        [InlineData(AsnEncodingRules.DER, TagClass.Universal, 3, "030100")]
        [InlineData(AsnEncodingRules.BER, TagClass.Private, 1, "C10100")]
        [InlineData(AsnEncodingRules.CER, TagClass.Application, 5, "450100")]
        [InlineData(AsnEncodingRules.DER, TagClass.ContextSpecific, 32, "9F200100")]
        public static void EmptyData_Allows0UnusedBits(
            AsnEncodingRules ruleSet,
            TagClass tagClass,
            int tagValue,
            string expectedHex)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            if (tagClass == TagClass.Universal)
            {
                Debug.Assert(tagValue == (int)UniversalTagNumber.BitString);
                writer.WriteBitString(ReadOnlySpan<byte>.Empty, 0);
            }
            else
            {
                writer.WriteBitString(ReadOnlySpan<byte>.Empty, 0, new Asn1Tag(tagClass, tagValue));
            }

            Verify(writer, expectedHex);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public void VerifyWriteBitString_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteBitString(ReadOnlySpan<byte>.Empty, tag: Asn1Tag.Null));

            AssertExtensions.Throws<ArgumentException>(
                "tag",
                () => writer.WriteBitString(new byte[1], tag: Asn1Tag.Null));
        }
    }
}
