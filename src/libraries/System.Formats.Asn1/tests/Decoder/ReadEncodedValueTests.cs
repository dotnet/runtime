// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Decoder
{
    public sealed class ReadEncodedValueTests
    {
        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEncodedValue_Primitive(AsnEncodingRules ruleSet)
        {
            // OCTET STRING (6 content bytes)
            // NULL
            ReadOnlySpan<byte> data =
            [
                0x04, 0x06, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
                0x05, 0x00,
            ];

            ExpectSuccess(data, ruleSet, Asn1Tag.PrimitiveOctetString, 2, 6);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEncodedValue_Indefinite(AsnEncodingRules ruleSet)
        {
            // CONSTRUCTED OCTET STRING (indefinite)
            //   OCTET STRING (1 byte)
            //   OCTET STRING (5 bytes)
            // END OF CONTENTS
            // NULL
            ReadOnlySpan<byte> data =
            [
                0x24, 0x80,
                0x04, 0x01, 0x01,
                0x04, 0x05, 0x02, 0x03, 0x04, 0x05, 0x06,
                0x00, 0x00,
                0x05, 0x00,
            ];

            // BER: Indefinite length encoding is OK, no requirements on the contents.
            // CER: Indefinite length encoding is required for CONSTRUCTED, the contents are invalid for OCTET STRING,
            //      but (Try)ReadEncodedValue doesn't pay attention to that.
            // DER: Indefinite length encoding is never permitted.

            if (ruleSet == AsnEncodingRules.DER)
            {
                ExpectFailure(data, ruleSet);
            }
            else
            {
                ExpectSuccess(data, ruleSet, Asn1Tag.ConstructedOctetString, 2, 10, indefiniteLength: true);
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEncodedValue_DefiniteConstructed(AsnEncodingRules ruleSet)
        {
            // CONSTRUCTED OCTET STRING (11 bytes)
            //   OCTET STRING (1 byte)
            //   OCTET STRING (5 bytes)
            // NULL
            ReadOnlySpan<byte> data =
            [
                0x24, 0x0A,
                0x04, 0x01, 0x01,
                0x04, 0x05, 0x02, 0x03, 0x04, 0x05, 0x06,
                0x05, 0x00,
            ];

            // BER: Indefinite length encoding is OK, no requirements on the contents.
            // CER: Indefinite length encoding is required for CONSTRUCTED, so fail.
            // DER: CONSTRUCTED OCTET STRING is not permitted, but ReadEncodedValue doesn't check for that,
            //      since the length is in minimal representation, the read is successful

            if (ruleSet == AsnEncodingRules.CER)
            {
                ExpectFailure(data, ruleSet);
            }
            else
            {
                ExpectSuccess(data, ruleSet, Asn1Tag.ConstructedOctetString, 2, 10);
            }
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEncodedValue_OutOfBoundsLength(AsnEncodingRules ruleSet)
        {
            // SEQUENCE (3 bytes), but only one byte remains.
            ReadOnlySpan<byte> data = [0x30, 0x03, 0x00];

            ExpectFailure(data, ruleSet);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEncodedValue_LargeOutOfBoundsLength(AsnEncodingRules ruleSet)
        {
            // SEQUENCE (int.MaxValue bytes), but no bytes remain.
            ReadOnlySpan<byte> data = [0x30, 0x84, 0x7F, 0xFF, 0xFF, 0xFF];

            ExpectFailure(data, ruleSet);
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void ReadEncodedValue_ExtremelyLargeLength(AsnEncodingRules ruleSet)
        {
            if (!Environment.Is64BitProcess)
            {
                return;
            }

            // OCTET STRING ((int.MaxValue - 6) bytes), span will be inflated to make it look valid.
            byte[] data = "04847FFFFFF9".HexToByteArray();

            unsafe
            {
                fixed (byte* ptr = data)
                {
                    // Verify that the length can be interpreted this large, but that it doesn't read that far.
                    ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(ptr, int.MaxValue);
                    ExpectSuccess(span, ruleSet, Asn1Tag.PrimitiveOctetString, 6, int.MaxValue - 6);
                }
            }
        }

        private static void ExpectSuccess(
            ReadOnlySpan<byte> data,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            int expectedContentOffset,
            int expectedContentLength,
            bool indefiniteLength = false)
        {
            Asn1Tag tag;
            int contentOffset;
            int contentLength;
            int bytesConsumed;

            bool read = AsnDecoder.TryReadEncodedValue(
                data,
                ruleSet,
                out tag,
                out contentOffset,
                out contentLength,
                out bytesConsumed);

            Assert.True(read, "AsnDecoder.TryReadEncodedValue unexpectedly returned false");
            Assert.Equal(expectedTag, tag);
            Assert.Equal(expectedContentOffset, contentOffset);
            Assert.Equal(expectedContentLength, contentLength);

            int expectedBytesConsumed = expectedContentOffset + expectedContentLength + (indefiniteLength ? 2 : 0);
            Assert.Equal(expectedBytesConsumed, bytesConsumed);

            contentOffset = contentLength = bytesConsumed = default;

            tag = AsnDecoder.ReadEncodedValue(
                data,
                ruleSet,
                out contentOffset,
                out contentLength,
                out bytesConsumed);

            Assert.Equal(expectedTag, tag);
            Assert.Equal(expectedContentOffset, contentOffset);
            Assert.Equal(expectedContentLength, contentLength);
            Assert.Equal(expectedBytesConsumed, bytesConsumed);
        }

        private static void ExpectFailure(ReadOnlySpan<byte> data, AsnEncodingRules ruleSet)
        {
            Asn1Tag tag;
            int contentOffset;
            int contentLength;
            int bytesConsumed;

            bool read = AsnDecoder.TryReadEncodedValue(
                data,
                ruleSet,
                out tag,
                out contentOffset,
                out contentLength,
                out bytesConsumed);

            Assert.False(read, "AsnDecoder.TryReadEncodedValue unexpectedly returned true");
            Assert.Equal(default, tag);
            Assert.Equal(default, contentOffset);
            Assert.Equal(default, contentLength);
            Assert.Equal(default, bytesConsumed);

            int seed = Environment.CurrentManagedThreadId;
            Asn1Tag seedTag = new Asn1Tag(TagClass.Private, seed, (seed & 1) == 0);
            tag = seedTag;
            contentOffset = contentLength = bytesConsumed = seed;

            try
            {
                tag = AsnDecoder.ReadEncodedValue(
                    data,
                    ruleSet,
                    out contentOffset,
                    out contentLength,
                    out bytesConsumed);

                Assert.Fail("ReadEncodedValue should have thrown AsnContentException");
            }
            catch (AsnContentException e)
            {
                Assert.IsType<AsnContentException>(e);
            }

            Assert.Equal(seedTag, tag);
            Assert.Equal(seed, contentOffset);
            Assert.Equal(seed, contentLength);
            Assert.Equal(seed, bytesConsumed);
        }
    }
}
