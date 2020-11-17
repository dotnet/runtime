// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public static class OverlappedReads
    {
        private delegate bool TryWriteMethod(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            out int bytesWritten);

        [Fact]
        public static void NoOverlappedBitStrings()
        {
            static bool Method(
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                AsnEncodingRules ruleSet,
                out int consumed,
                out int written)
            {
                bool ret = AsnDecoder.TryReadBitString(
                    source,
                    destination,
                    ruleSet,
                    out int unusedBitCount,
                    out consumed,
                    out written);

                if (ret)
                {
                    Assert.Equal(2, unusedBitCount);
                }

                return ret;
            }

            byte[] input = { 0x00, 0x00, 0x03, 0x02, 0x02, 0x3C, 0x00 };

            NoOverlappedReads(
                input,
                encodedValueOffset: 2,
                encodedValueLength: 4,
                copyLength: 1,
                Method);
        }

        [Fact]
        public static void NoOverlappedOctetStrings()
        {
            static bool Method(
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                AsnEncodingRules ruleSet,
                out int consumed,
                out int written)
            {
                return AsnDecoder.TryReadOctetString(
                    source,
                    destination,
                    ruleSet,
                    out consumed,
                    out written);
            }

            byte[] input = { 0x00, 0x00, 0x04, 0x01, 0x21, 0x00 };

            NoOverlappedReads(
                input,
                encodedValueOffset: 2,
                encodedValueLength: 3,
                copyLength: 1,
                Method);
        }

        [Fact]
        public static void NoOverlappedTextStrings()
        {
            static bool Method(
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                AsnEncodingRules ruleSet,
                out int consumed,
                out int written)
            {
                return AsnDecoder.TryReadCharacterStringBytes(
                    source,
                    destination,
                    ruleSet,
                    new Asn1Tag(UniversalTagNumber.UTF8String), 
                    out consumed,
                    out written);
            }

            byte[] input = { 0x00, 0x00, 0x0C, 0x01, 0x30, 0x00 };

            NoOverlappedReads(
                input,
                encodedValueOffset: 2,
                encodedValueLength: 3,
                copyLength: 1,
                Method);
        }

        private static void NoOverlappedReads(
            byte[] input,
            int encodedValueOffset,
            int encodedValueLength,
            int copyLength,
            TryWriteMethod tryWriteMethod)
        {
            // The write starts beyond the portion of source that will be read,
            // but that hasn't yet been determined.
            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => tryWriteMethod(
                    input.AsSpan(encodedValueOffset),
                    input.AsSpan(encodedValueOffset + encodedValueLength),
                    AsnEncodingRules.BER,
                    out _,
                    out _));

            // The CopyTo would actually end up with source == dest for this one
            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => tryWriteMethod(
                    input.AsSpan(encodedValueOffset),
                    input.AsSpan(encodedValueOffset + encodedValueLength - copyLength, copyLength),
                    AsnEncodingRules.BER,
                    out _,
                    out _));

            // destination[1] is source[0], but there isn't actually an overwrite because
            // the value length isn't long enough.
            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => tryWriteMethod(
                    input.AsSpan(encodedValueOffset),
                    input.AsSpan(encodedValueOffset - copyLength, copyLength + 1),
                    AsnEncodingRules.BER,
                    out _,
                    out _));

            Assert.True(
                tryWriteMethod(
                    input.AsSpan(encodedValueOffset, encodedValueLength),
                    input.AsSpan(encodedValueOffset + encodedValueLength, copyLength),
                    AsnEncodingRules.BER,
                    out int bytesConsumed,
                    out int bytesWritten));

            Assert.Equal(encodedValueLength, bytesConsumed);
            Assert.Equal(copyLength, bytesWritten);
            Assert.Equal(
                input[encodedValueOffset + encodedValueLength - copyLength],
                input[encodedValueOffset + encodedValueLength]);

            Assert.True(
                tryWriteMethod(
                    input.AsSpan(encodedValueOffset, encodedValueLength),
                    input.AsSpan(0, copyLength),
                    AsnEncodingRules.BER,
                    out bytesConsumed,
                    out bytesWritten));

            Assert.Equal(encodedValueLength, bytesConsumed);
            Assert.Equal(copyLength, bytesWritten);
            Assert.Equal(input[encodedValueOffset + encodedValueLength - copyLength], input[0]);
        }
    }
}
