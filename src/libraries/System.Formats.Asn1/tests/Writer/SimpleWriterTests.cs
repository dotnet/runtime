// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
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

            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "ruleSet",
                () => new AsnWriter((AsnEncodingRules)value, initialCapacity: 1000));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public static void ValidateInitialCapacity(int initialCapacity)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                "initialCapacity",
                () => new AsnWriter(AsnEncodingRules.DER, initialCapacity));
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

#if NET9_0_OR_GREATER
            writer.Encode<object>(encoded => {
                Assert.Equal(0, encoded.Length);
                return null;
            });

            writer.Encode<object, object>(null, (_, encoded) => {
                Assert.Equal(0, encoded.Length);
                return null;
            });

            writer.Encode<object>(null, (_, encoded) => {
                Assert.Equal(0, encoded.Length);
            });
#endif

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

        [Fact]
        public static void InitialCapacity_ExactCapacity()
        {
            ReadOnlySpan<byte> value = new byte[] { 0x04, 0x06, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity: 8);
            writer.WriteEncodedValue(value);

            byte[]? buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);

            byte[] encoded = writer.Encode();
            AssertExtensions.SequenceEqual(value, encoded);

            writer.Reset();
            buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);

            writer.WriteEncodedValue(value);
            buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);
        }

        [Fact]
        public static void InitialCapacity_ZeroHasNoInitialCapacity()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity: 0);

            byte[]? buffer = PeekRawBuffer(writer);
            Assert.Null(buffer);
        }

        [Fact]
        public static void InitialCapacity_UnderCapacity()
        {
            ReadOnlySpan<byte> value = new byte[] { 0x04, 0x01, 0x01 };
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity: 8);
            writer.WriteEncodedValue(value);

            byte[]? buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);

            byte[] encoded = writer.Encode();
            AssertExtensions.SequenceEqual(value, encoded);

            writer.Reset();
            buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);
        }

        [Fact]
        public static void InitialCapacity_ExceedCapacity()
        {
            ReadOnlySpan<byte> value = new byte[] { 0x04, 0x07, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity: 8);
            writer.WriteEncodedValue(value);

            byte[]? buffer = PeekRawBuffer(writer);
            Assert.Equal(1024, buffer?.Length);
        }

        [Fact]
        public static void InitialCapacity_ResizeBlockAligns()
        {
            ReadOnlySpan<byte> value = new byte[] { 0x04, 0x06, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
            ReadOnlySpan<byte> valueLarge = new byte[] { 0x04, 0x07, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity: 8);
            writer.WriteEncodedValue(value);

            byte[]? buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);

            writer.Reset();
            buffer = PeekRawBuffer(writer);
            Assert.Equal(8, buffer?.Length);

            writer.WriteEncodedValue(valueLarge);
            buffer = PeekRawBuffer(writer);
            Assert.Equal(1024, buffer?.Length);
        }

#if NET9_0_OR_GREATER
        [Fact]
        public static void Encode_Callback_NoModifications()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            writer.Encode(writer, static (writer, encoded) =>
            {
                writer.Encode(writer, static (writer, encoded) =>
                {
                    Assert.Throws<InvalidOperationException>(() => writer.WriteNull());
                    return (object)null;
                });

                Assert.Throws<InvalidOperationException>(() => writer.WriteNull());
                return (object)null;
            });

            writer.Encode(writer, static (writer, encoded) =>
            {
                writer.Encode(writer, static (writer, encoded) =>
                {
                    Assert.Throws<InvalidOperationException>(() => writer.Reset());
                    return (object)null;
                });

                Assert.Throws<InvalidOperationException>(() => writer.Reset());
                return (object)null;
            });

            writer.Encode(writer, static (writer, encoded) =>
            {
                writer.Encode(writer, static (writer, encoded) =>
                {
                    Assert.Throws<InvalidOperationException>(() => writer.Reset());
                });

                Assert.Throws<InvalidOperationException>(() => writer.Reset());
            });
        }
#endif

        private static byte[]? PeekRawBuffer(AsnWriter writer)
        {
            FieldInfo bufField = typeof(AsnWriter).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic);
            return (byte[]?)bufField.GetValue(writer);
        }
    }
}
