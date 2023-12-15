// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public static class ReaderStateTests
    {
        [Fact]
        public static void HasDataAndThrowIfNotEmpty()
        {
            AsnReader reader = new AsnReader(new byte[] { 0x01, 0x01, 0x00 }, AsnEncodingRules.BER);
            Assert.True(reader.HasData);
            Assert.Throws<AsnContentException>(() => reader.ThrowIfNotEmpty());

            // Consume the current value and move on.
            reader.ReadEncodedValue();

            Assert.False(reader.HasData);
            // Assert.NoThrow
            reader.ThrowIfNotEmpty();
        }

        [Fact]
        public static void HasDataAndThrowIfNotEmpty_StartsEmpty()
        {
            AsnReader reader = new AsnReader(ReadOnlyMemory<byte>.Empty, AsnEncodingRules.BER);
            Assert.False(reader.HasData);
            // Assert.NoThrow
            reader.ThrowIfNotEmpty();
        }

        [Fact]
        public static void Clone_CopiesCurrentState()
        {
            // Sequence {
            //   SetOf {
            //     UtcTime 500405000012Z
            //     Null
            //   }
            // }
            // Verify the options are preserved in the clone by observing them:
            // this is an incorrectly sorted SET OF with a date of 50/04/05 that should be 2050, not 1950.
            ReadOnlyMemory<byte> asn = "30133111170D3530303430353030303031325A0500".HexToByteArray();

            AsnReaderOptions options = new AsnReaderOptions
            {
                UtcTimeTwoDigitYearMax = 2050,
                SkipSetSortOrderVerification = true,
            };

            AsnReader sequence = new AsnReader(asn, AsnEncodingRules.DER, options);
            AsnReader reader = sequence.ReadSequence();
            sequence.ThrowIfNotEmpty();

            AsnReader clone = reader.Clone();
            Assert.Equal(reader.RuleSet, clone.RuleSet);

            AssertReader(reader);
            Assert.False(reader.HasData, "reader.HasData");
            Assert.True(clone.HasData, "clone.HasData");

            AssertReader(clone);
            Assert.False(clone.HasData, "clone.HasData");

            static void AssertReader(AsnReader reader)
            {
                AsnReader setOf = reader.ReadSetOf();
                reader.ThrowIfNotEmpty();

                DateTimeOffset dateTime = setOf.ReadUtcTime();
                Assert.Equal(2050, dateTime.Year);
                setOf.ReadNull();
                setOf.ThrowIfNotEmpty();
            }
        }

        [Fact]
        public static void Clone_Empty()
        {
            AsnReader reader = new AsnReader(ReadOnlyMemory<byte>.Empty, AsnEncodingRules.DER);
            AsnReader clone = reader.Clone();
            Assert.False(reader.HasData, "reader.HasData");
            Assert.False(clone.HasData, "clone.HasData");
        }

        [Fact]
        public static void Clone_SameUnderlyingData()
        {
            ReadOnlyMemory<byte> data = "04050102030405".HexToByteArray();
            AsnReader reader = new AsnReader(data, AsnEncodingRules.DER);
            AsnReader clone = reader.Clone();

            Assert.True(reader.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> readerData));
            Assert.True(clone.TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> cloneData));
            Assert.True(readerData.Span == cloneData.Span, "readerData == cloneData");
        }
    }
}
