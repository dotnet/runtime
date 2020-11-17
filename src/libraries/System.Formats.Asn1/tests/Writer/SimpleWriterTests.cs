// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public static class SimpleWriterTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public static void ValidateRuleSet(int value)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "ruleSet",
                () => new AsnWriter((AsnEncodingRules)value));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void EncodeEmpty(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            byte[] encoded = writer.Encode();
            Assert.Same(Array.Empty<byte>(), encoded);

            Assert.True(writer.TryEncode(Span<byte>.Empty, out int written));
            Assert.Equal(0, written);
            Assert.True(writer.EncodedValueEquals(ReadOnlySpan<byte>.Empty));

            Span<byte> negativeTest = stackalloc byte[] { 5, 0 };
            Assert.False(writer.EncodedValueEquals(negativeTest));
        }

        [Fact]
        public static void DisposeDefaultScope()
        {
            AsnWriter.Scope scope = default;
            // Assert.NoThrow
            scope.Dispose();
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void CopyTo_Empty(AsnEncodingRules ruleSet)
        {
            AsnWriter empty = new AsnWriter(ruleSet);
            AsnWriter dest = new AsnWriter(AsnEncodingRules.BER);

            Assert.Throws<InvalidOperationException>(() => empty.CopyTo(dest));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void CopyTo_TwoValues(AsnEncodingRules ruleSet)
        {
            AsnWriter source = new AsnWriter(ruleSet);
            AsnWriter dest = new AsnWriter(AsnEncodingRules.BER);

            source.WriteBoolean(false);
            source.WriteBoolean(true);

            Assert.Throws<InvalidOperationException>(() => source.CopyTo(dest));
        }

        [Fact]
        public static void CopyTo_IncompatibleRules()
        {
            AsnWriter cer = new AsnWriter(AsnEncodingRules.CER);
            AsnWriter der = new AsnWriter(AsnEncodingRules.DER);

            using (cer.PushSequence())
            using (der.PushSequence())
            {
                cer.WriteBoolean(false);
                der.WriteBoolean(true);
            }

            Assert.Throws<InvalidOperationException>(() => cer.CopyTo(der));
            Assert.Throws<InvalidOperationException>(() => der.CopyTo(cer));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void CopyTo_Success(AsnEncodingRules ruleSet)
        {
            AsnWriter source = new AsnWriter(ruleSet);
            AsnWriter dest = new AsnWriter(AsnEncodingRules.BER);

            Assert.True(source.EncodedValueEquals(dest));

            source.WriteBoolean(false);
            Assert.False(source.EncodedValueEquals(dest));

            source.CopyTo(dest);
            Assert.True(source.EncodedValueEquals(dest));

            Assert.Equal(source.Encode(), dest.Encode());

            source.CopyTo(dest);
            Assert.False(source.EncodedValueEquals(dest));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void EncodedValueEquals_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentNullException>(
                "other",
                () => writer.EncodedValueEquals((AsnWriter)null));
        }

        [Theory]
        [InlineData(AsnEncodingRules.BER)]
        [InlineData(AsnEncodingRules.CER)]
        [InlineData(AsnEncodingRules.DER)]
        public static void CopyTo_Null(AsnEncodingRules ruleSet)
        {
            AsnWriter writer = new AsnWriter(ruleSet);

            AssertExtensions.Throws<ArgumentNullException>(
                "destination",
                () => writer.CopyTo(null));
        }
    }
}
