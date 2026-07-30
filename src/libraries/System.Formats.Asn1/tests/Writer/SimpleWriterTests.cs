// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public static class SimpleWriterTests
    {
        public static IEnumerable<object[]> VectorBoundaryLengths
        {
            get
            {
                yield return new object[] { Vector<byte>.Count - 1 };
                yield return new object[] { Vector<byte>.Count };
                yield return new object[] { Vector<byte>.Count + 1 };
            }
        }

        [Theory]
        [MemberData(nameof(VectorBoundaryLengths))]
        public static void WriteVisibleString_DoesNotAccessOutsideBounds(int payloadLength)
        {
            using BoundedMemory<char> value = BoundedMemory.Allocate<char>(payloadLength);
            value.Span.Fill('A');
            value.MakeReadonly();

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteCharacterString(UniversalTagNumber.VisibleString, value.Span);
            byte[] encoded = writer.Encode();

            string decoded = AsnDecoder.ReadCharacterString(
                encoded,
                AsnEncodingRules.DER,
                UniversalTagNumber.VisibleString,
                out int bytesConsumed);
            Assert.Equal(encoded.Length, bytesConsumed);
            Assert.Equal(payloadLength, decoded.Length);
            AssertExtensions.FilledWith('A', decoded);
        }

        [Fact]
        public static void WriteVisibleString_VectorSizedRange()
        {
            const int PayloadLength = 128;
            char[] value = new char[PayloadLength];

            for (int i = 0; i < PayloadLength; i++)
            {
                value[i] = i % 2 == 0 ? (char)0x20 : (char)0x7E;
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteCharacterString(UniversalTagNumber.VisibleString, value);
            byte[] encoded = writer.Encode();

            Assert.Equal((byte)UniversalTagNumber.VisibleString, encoded[0]);
            Assert.Equal(0x81, encoded[1]);
            Assert.Equal(PayloadLength, encoded[2]);
            Assert.Equal(new string(value), Encoding.ASCII.GetString(encoded, 3, PayloadLength));
        }

        [Theory]
        [InlineData('\u001F', 10)]
        [InlineData('\u007F', 10)]
        [InlineData('\u001F', 128)]
        [InlineData('\u007F', 128)]
        public static void WriteVisibleString_Invalid(char invalidValue, int invalidIndex)
        {
            char[] value = new string('A', 129).ToCharArray();
            value[invalidIndex] = invalidValue;
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            EncoderFallbackException exception = Assert.Throws<EncoderFallbackException>(
                () => writer.WriteCharacterString(UniversalTagNumber.VisibleString, value));
            Assert.Equal(invalidIndex, exception.Index);
            Assert.Equal(0, writer.GetEncodedLength());
        }

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

#if NET
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

#if NET
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
