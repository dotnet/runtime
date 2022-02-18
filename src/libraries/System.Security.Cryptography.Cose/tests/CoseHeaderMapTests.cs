// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Formats.Cbor;
using System.Collections.Generic;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseHeaderMapTests
    {
        [Fact]
        public void SetValue_GetValue_KnownCoseHeaderLabel()
        {
            var map = new CoseHeaderMap();
            map.SetValue(CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256);
            map.SetValue(CoseHeaderLabel.Critical, GetDummyCritHeaderValue());
            map.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            map.SetValue(CoseHeaderLabel.KeyIdentifier, s_sampleContent);
            map.SetValue(CoseHeaderLabel.IV, ReadOnlySpan<byte>.Empty);
            map.SetValue(CoseHeaderLabel.PartialIV, ReadOnlySpan<byte>.Empty);
            map.SetValue(CoseHeaderLabel.CounterSignature, ReadOnlySpan<byte>.Empty);

            Assert.Equal((int)ECDsaAlgorithm.ES256, map.GetValueAsInt32(CoseHeaderLabel.Algorithm));
            AssertExtensions.SequenceEqual(GetDummyCritHeaderValue(), map.GetValueAsBytes(CoseHeaderLabel.Critical));
            Assert.Equal(ContentTypeDummyValue, map.GetValueAsString(CoseHeaderLabel.ContentType));
            AssertExtensions.SequenceEqual(s_sampleContent, map.GetValueAsBytes(CoseHeaderLabel.KeyIdentifier));
            AssertExtensions.SequenceEqual(ReadOnlySpan<byte>.Empty, map.GetValueAsBytes(CoseHeaderLabel.IV));
            AssertExtensions.SequenceEqual(ReadOnlySpan<byte>.Empty, map.GetValueAsBytes(CoseHeaderLabel.PartialIV));
            AssertExtensions.SequenceEqual(ReadOnlySpan<byte>.Empty, map.GetValueAsBytes(CoseHeaderLabel.CounterSignature));
        }

        [Theory]
        [MemberData(nameof(KnownHeadersEncodedValues_TestData))]
        public void SetEncodedValue_GetEncodedValue_KnownCoseHeaderLabel(int knownHeader, byte[] encodedValue)
        {
            var map = new CoseHeaderMap();
            var label = new CoseHeaderLabel(knownHeader);

            map.SetEncodedValue(label, encodedValue);

            ReadOnlyMemory<byte> returnedEncocedValue = map.GetEncodedValue(label);
            AssertExtensions.SequenceEqual(encodedValue, returnedEncocedValue.Span);

            map.TryGetEncodedValue(label, out returnedEncocedValue);
            AssertExtensions.SequenceEqual(encodedValue, returnedEncocedValue.Span);
        }

        [Fact]
        public void SetValue_KnownHeaders_ThrowIf_IncorrectValue()
        {
            var map = new CoseHeaderMap();
            // only accepts int or tstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(CoseHeaderLabel.Algorithm, ReadOnlySpan<byte>.Empty));
            // [ +label ] (non-empty array) Not yet properly supported.
            //Assert.Throws<NotSupportedException>(() => map.SetValue(CoseHeaderLabel.Critical, ReadOnlySpan<byte>.Empty));
            // tstr / uint
            Assert.Throws<InvalidOperationException>(() => map.SetValue(CoseHeaderLabel.ContentType, -1));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(CoseHeaderLabel.KeyIdentifier, "foo"));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(CoseHeaderLabel.IV, "foo"));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(CoseHeaderLabel.PartialIV, "foo"));
        }

        [Fact]
        public void SetEncodedValue_KnownHeaders_ThrowIf_IncorrectValue()
        {
            var writer = new CborWriter();
            writer.WriteNull();
            byte[] encodedNullValue = writer.Encode();

            var map = new CoseHeaderMap();
            // only accepts int or tstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(CoseHeaderLabel.Algorithm, encodedNullValue));
            // [ +label ] (non-empty array) Not yet properly supported.
            //Assert.Throws<NotSupportedException>(() => map.SetEncodedValue(CoseHeaderLabel.Critical, encodedNullValue));
            // tstr / uint
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(CoseHeaderLabel.ContentType, encodedNullValue));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(CoseHeaderLabel.KeyIdentifier, encodedNullValue));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(CoseHeaderLabel.IV, encodedNullValue));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(CoseHeaderLabel.PartialIV, encodedNullValue));
        }

        [Fact]
        public void Enumerate()
        {
            var map = new CoseHeaderMap();
            map.SetValue(CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256);
            map.SetEncodedValue(CoseHeaderLabel.Critical, GetDummyCritHeaderValue());
            map.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            map.SetValue(CoseHeaderLabel.KeyIdentifier, s_sampleContent);
            map.SetValue(CoseHeaderLabel.IV, ReadOnlySpan<byte>.Empty);
            map.SetValue(CoseHeaderLabel.PartialIV, ReadOnlySpan<byte>.Empty);
            map.SetValue(CoseHeaderLabel.CounterSignature, ReadOnlySpan<byte>.Empty);

            var writer = new CborWriter();
            int currentHeader = KnownHeaderAlg;
            foreach ((CoseHeaderLabel label, ReadOnlyMemory<byte> encodedValue) in map)
            {
                Assert.Equal(new CoseHeaderLabel(currentHeader), label);
                ReadOnlyMemory<byte> expectedValue = currentHeader switch
                {
                    KnownHeaderAlg => EncodeInt32((int)ECDsaAlgorithm.ES256, writer),
                    KnownHeaderCrit => GetDummyCritHeaderValue(),
                    KnownHeaderContentType => EncodeString(ContentTypeDummyValue, writer),
                    KnownHeaderKid => EncodeBytes(s_sampleContent, writer),
                    KnownHeaderIV or KnownHeaderPartialIV or KnownHeaderCounterSignature => EncodeBytes(ReadOnlySpan<byte>.Empty, writer),
                    _ => throw new InvalidOperationException()
                };
                AssertExtensions.SequenceEqual(expectedValue.Span, encodedValue.Span);
                currentHeader++;
            }
            Assert.Equal(KnownHeaderCounterSignature + 1, currentHeader);

            static ReadOnlyMemory<byte> EncodeInt32(int value, CborWriter writer)
            {
                writer.WriteInt32(value);
                return EncodeAndReset(writer);
            }

            static ReadOnlyMemory<byte> EncodeString(string value, CborWriter writer)
            {
                writer.WriteTextString(value);
                return EncodeAndReset(writer);
            }

            static ReadOnlyMemory<byte> EncodeBytes(ReadOnlySpan<byte> value, CborWriter writer)
            {
                writer.WriteByteString(value);
                return EncodeAndReset(writer);
            }

            static ReadOnlyMemory<byte> EncodeAndReset(CborWriter writer)
            {
                ReadOnlyMemory<byte> encodedValue = writer.Encode();
                writer.Reset();
                return encodedValue;
            }
        }

        [Fact]
        public void DecodedProtectedMapShouldBeReadOnly()
        {
            byte[] encodedMessage = CoseSign1Message.Sign(s_sampleContent, DefaultKey, HashAlgorithmName.SHA256);
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeaders.IsReadOnly, "message.ProtectedHeaders.IsReadOnly");
        }

        [Fact]
        public void GetValueFromReadOnlyProtectedMap()
        {
            byte[] encodedMessage = CoseSign1Message.Sign(s_sampleContent, DefaultKey, HashAlgorithmName.SHA256);
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeaders.IsReadOnly, "message.ProtectedHeaders.IsReadOnly");

            int expectedAlgorithm = (int)ECDsaAlgorithm.ES256;

            int algorithm = message.ProtectedHeaders.GetValueAsInt32(CoseHeaderLabel.Algorithm);
            Assert.Equal(expectedAlgorithm, algorithm);

            ReadOnlyMemory<byte> encodedAlgorithm = message.ProtectedHeaders.GetEncodedValue(CoseHeaderLabel.Algorithm);
            Assert.Equal(expectedAlgorithm, new CborReader(encodedAlgorithm).ReadInt32());

            message.ProtectedHeaders.TryGetEncodedValue(CoseHeaderLabel.Algorithm, out encodedAlgorithm);
            Assert.Equal(expectedAlgorithm, new CborReader(encodedAlgorithm).ReadInt32());
        }

        [Fact]
        public void SetValueAndRemoveThrowIfProtectedMapIsReadOnly()
        {
            byte[] encodedMessage = CoseSign1Message.Sign(s_sampleContent, DefaultKey, HashAlgorithmName.SHA256);
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeaders.IsReadOnly, "message.ProtectedHeaders.IsReadOnly");

            // New value.
            var barLabel = new CoseHeaderLabel("bar");
            Assert.Throws<InvalidOperationException>(() => message.ProtectedHeaders.SetValue(barLabel, 42));
            Assert.Throws<InvalidOperationException>(() => message.ProtectedHeaders.Remove(barLabel));

            // Existing value.
            Assert.Throws<InvalidOperationException>(() => message.ProtectedHeaders.SetValue(CoseHeaderLabel.Algorithm, 42));
            Assert.Throws<InvalidOperationException>(() => message.ProtectedHeaders.Remove(CoseHeaderLabel.Algorithm));

            // Verify existing value was not overwritten even after throwing.
            Assert.Equal((int)ECDsaAlgorithm.ES256, message.ProtectedHeaders.GetValueAsInt32(CoseHeaderLabel.Algorithm));

            // Non-readonly header works correctly.
            var fooLabel = new CoseHeaderLabel("foo");
            message.UnprotectedHeaders.SetValue(fooLabel, 42);
            message.UnprotectedHeaders.Remove(fooLabel);
        }

        public static IEnumerable<object[]> KnownHeadersEncodedValues_TestData()
        {
            var writer = new CborWriter();

            writer.WriteInt32((int)ECDsaAlgorithm.ES256);
            yield return ReturnDataAndReset(KnownHeaderAlg, writer);

            WriteDummyCritHeaderValue(writer);
            yield return ReturnDataAndReset(KnownHeaderCrit, writer);

            writer.WriteTextString(ContentTypeDummyValue);
            yield return ReturnDataAndReset(KnownHeaderContentType, writer);

            writer.WriteByteString(new byte[] { 0x42, 0x31, 0x31 });
            yield return ReturnDataAndReset(KnownHeaderKid, writer);

            writer.WriteByteString(ReadOnlySpan<byte>.Empty);
            yield return ReturnDataAndReset(KnownHeaderIV, writer);

            writer.WriteByteString(ReadOnlySpan<byte>.Empty);
            yield return ReturnDataAndReset(KnownHeaderPartialIV, writer);

            writer.WriteByteString(ReadOnlySpan<byte>.Empty);
            yield return ReturnDataAndReset(KnownHeaderCounterSignature, writer);

            static object[] ReturnDataAndReset(int knownHeader, CborWriter w)
            {
                byte[] encodedValue = w.Encode();
                w.Reset();
                return new object[] { knownHeader, encodedValue };
            }
        }

        private static void WriteDummyCritHeaderValue(CborWriter writer)
        {
            writer.WriteStartArray(1);
            writer.WriteInt32(42);
            writer.WriteEndArray();
        }

        private static byte[] GetDummyCritHeaderValue()
        {
            var writer = new CborWriter();
            WriteDummyCritHeaderValue(writer);
            return writer.Encode();
        }
    }
}
