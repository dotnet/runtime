// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Reader
{
    public sealed class ReaderStateAsnReaderTests : ReaderStateBase
    {
        internal override AsnReaderWrapper CreateWrapper(
            ReadOnlyMemory<byte> data,
            AsnEncodingRules ruleSet,
            AsnReaderOptions options = default)
        {
            return AsnReaderWrapper.CreateClassReader(data, ruleSet, options);
        }
    }

    public sealed class ReaderStateValueAsnReaderTests : ReaderStateBase
    {
        internal override AsnReaderWrapper CreateWrapper(
            ReadOnlyMemory<byte> data,
            AsnEncodingRules ruleSet,
            AsnReaderOptions options = default)
        {
            return AsnReaderWrapper.CreateValueReader(data, ruleSet, options);
        }
    }

    public abstract class ReaderStateBase
    {
        internal abstract AsnReaderWrapper CreateWrapper(
            ReadOnlyMemory<byte> data,
            AsnEncodingRules ruleSet,
            AsnReaderOptions options = default);

        [Fact]
        public void HasDataAndThrowIfNotEmpty()
        {
            AsnReaderWrapper reader = CreateWrapper(new byte[] { 0x01, 0x01, 0x00 }, AsnEncodingRules.BER);
            Assert.True(reader.HasData);
            Assert.Throws<AsnContentException>(ref reader, static (ref reader) => reader.ThrowIfNotEmpty());

            // Consume the current value and move on.
            reader.ReadEncodedValue();

            Assert.False(reader.HasData);
            // Assert.NoThrow
            reader.ThrowIfNotEmpty();
        }

        [Fact]
        public void HasDataAndThrowIfNotEmpty_StartsEmpty()
        {
            AsnReaderWrapper reader = CreateWrapper(ReadOnlyMemory<byte>.Empty, AsnEncodingRules.BER);
            Assert.False(reader.HasData);
            // Assert.NoThrow
            reader.ThrowIfNotEmpty();
        }

        [Fact]
        public void Clone_CopiesCurrentState()
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

            AsnReaderWrapper sequence = CreateWrapper(asn, AsnEncodingRules.DER, options);
            AsnReaderWrapper reader = sequence.ReadSequence();
            sequence.ThrowIfNotEmpty();

            AsnReaderWrapper clone = reader.Clone();
            Assert.Equal(reader.RuleSet, clone.RuleSet);

            AssertReader(ref reader);
            Assert.False(reader.HasData, "reader.HasData");
            Assert.True(clone.HasData, "clone.HasData");

            AssertReader(ref clone);
            Assert.False(clone.HasData, "clone.HasData");

            static void AssertReader(ref AsnReaderWrapper reader)
            {
                AsnReaderWrapper setOf = reader.ReadSetOf();
                reader.ThrowIfNotEmpty();

                DateTimeOffset dateTime = setOf.ReadUtcTime();
                Assert.Equal(2050, dateTime.Year);
                setOf.ReadNull();
                setOf.ThrowIfNotEmpty();
            }
        }

        [Fact]
        public void Clone_Empty()
        {
            AsnReaderWrapper reader = CreateWrapper(ReadOnlyMemory<byte>.Empty, AsnEncodingRules.DER);
            AsnReaderWrapper clone = reader.Clone();
            Assert.False(reader.HasData, "reader.HasData");
            Assert.False(clone.HasData, "clone.HasData");
        }

        [Fact]
        public void Clone_SameUnderlyingData()
        {
            ReadOnlyMemory<byte> data = "04050102030405".HexToByteArray();
            AsnReaderWrapper reader = CreateWrapper(data, AsnEncodingRules.DER);
            AsnReaderWrapper clone = reader.Clone();

            Assert.True(reader.TryReadPrimitiveOctetString(out ReadOnlySpan<byte> readerData));
            Assert.True(clone.TryReadPrimitiveOctetString(out ReadOnlySpan<byte> cloneData));
            Assert.True(readerData == cloneData, "readerData == cloneData");
        }
    }
}
