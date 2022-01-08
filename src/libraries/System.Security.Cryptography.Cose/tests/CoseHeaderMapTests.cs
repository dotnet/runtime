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
        public void SetValue_GetValue_KnownHeader()
        {
            var alg = new CoseHeaderLabel(KnownHeaders.Alg);
            var contentType = new CoseHeaderLabel(KnownHeaders.ContentType);
            var kid = new CoseHeaderLabel(KnownHeaders.Kid);
            var iV = new CoseHeaderLabel(KnownHeaders.IV);
            var partialIV = new CoseHeaderLabel(KnownHeaders.PartialIV);

            var map = new CoseHeaderMap();
            map.SetValue(alg, (int)ECDsaAlgorithm.ES256);
            map.SetValue(contentType, ContentTypeDummyValue);
            map.SetValue(kid, s_SampleContent);
            map.SetValue(iV, ReadOnlySpan<byte>.Empty);
            map.SetValue(partialIV, ReadOnlySpan<byte>.Empty);

            Assert.Equal((int)ECDsaAlgorithm.ES256, map.GetValueAsInt32(alg));
            Assert.Equal(ContentTypeDummyValue, map.GetValueAsString(contentType));
            Assert.True(map.GetValueAsBytes(kid).SequenceEqual(s_SampleContent));
            Assert.True(map.GetValueAsBytes(iV).SequenceEqual(ReadOnlySpan<byte>.Empty));
            Assert.True(map.GetValueAsBytes(partialIV).SequenceEqual(ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(KnownHeadersEncodedValues_TestData))]
        public void SetEncodedValue_GetEncodedValue_KnownHeader(int knownHeader, ReadOnlyMemory<byte> encodedValue)
        {
            var map = new CoseHeaderMap();
            var label = new CoseHeaderLabel(knownHeader);

            map.SetEncodedValue(label, encodedValue);

            ReadOnlyMemory<byte> returnedEncocedValue = map.GetEncodedValue(label);
            Assert.True(returnedEncocedValue.Span.SequenceEqual(encodedValue.Span));

            map.TryGetEncodedValue(label, out returnedEncocedValue);
            Assert.True(returnedEncocedValue.Span.SequenceEqual(encodedValue.Span));
        }

        [Fact]
        public void SetValue_KnownHeaders_ThrowIf_IncorrectValue()
        {
            var map = new CoseHeaderMap();
            // only accepts int or tstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(new CoseHeaderLabel(KnownHeaders.Alg), ReadOnlySpan<byte>.Empty));
            // [ +label ] (non-empty array)
            Assert.Throws<NotSupportedException>(() => map.SetValue(new CoseHeaderLabel(KnownHeaders.Crit), ReadOnlySpan<byte>.Empty));
            // tstr / uint
            Assert.Throws<InvalidOperationException>(() => map.SetValue(new CoseHeaderLabel(KnownHeaders.ContentType), -1));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(new CoseHeaderLabel(KnownHeaders.Kid), "foo"));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(new CoseHeaderLabel(KnownHeaders.IV), "foo"));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetValue(new CoseHeaderLabel(KnownHeaders.PartialIV), "foo"));
        }

        [Fact]
        public void SetEncodedValue_KnownHeaders_ThrowIf_IncorrectValue()
        {
            var writer = new CborWriter();
            writer.WriteNull();
            ReadOnlyMemory<byte> encodedNullValue = writer.Encode();

            var map = new CoseHeaderMap();
            // only accepts int or tstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(new CoseHeaderLabel(KnownHeaders.Alg), encodedNullValue));
            // [ +label ] (non-empty array)
            Assert.Throws<NotSupportedException>(() => map.SetEncodedValue(new CoseHeaderLabel(KnownHeaders.Crit), encodedNullValue));
            // tstr / uint
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(new CoseHeaderLabel(KnownHeaders.ContentType), encodedNullValue));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(new CoseHeaderLabel(KnownHeaders.Kid), encodedNullValue));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(new CoseHeaderLabel(KnownHeaders.IV), encodedNullValue));
            // bstr
            Assert.Throws<InvalidOperationException>(() => map.SetEncodedValue(new CoseHeaderLabel(KnownHeaders.PartialIV), encodedNullValue));
        }

        [Fact]
        public void Enumerate()
        {
            var alg = new CoseHeaderLabel(KnownHeaders.Alg);
            var crit = new CoseHeaderLabel(KnownHeaders.Crit);
            var contentType = new CoseHeaderLabel(KnownHeaders.ContentType);
            var kid = new CoseHeaderLabel(KnownHeaders.Kid);
            var iV = new CoseHeaderLabel(KnownHeaders.IV);
            var partialIV = new CoseHeaderLabel(KnownHeaders.PartialIV);

            var map = new CoseHeaderMap();
            map.SetValue(alg, (int)ECDsaAlgorithm.ES256);
            //map.SetEncodedValue(crit, GetDummyCritHeaderValue());
            map.SetValue(contentType, ContentTypeDummyValue);
            map.SetValue(kid, s_SampleContent);
            map.SetValue(iV, ReadOnlySpan<byte>.Empty);
            map.SetValue(partialIV, ReadOnlySpan<byte>.Empty);

            var writer = new CborWriter();
            int currentHeader = KnownHeaders.Alg;
            foreach ((CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue) in map)
            {
                if (currentHeader == KnownHeaders.Crit) // TODO remove when Crit gets supported
                {
                    currentHeader++;
                }

                Assert.Equal(new CoseHeaderLabel(currentHeader), Label);
                ReadOnlyMemory<byte> ExpectedValue = currentHeader switch
                {
                    KnownHeaders.Alg => EncodeInt32((int)ECDsaAlgorithm.ES256, writer),
                    KnownHeaders.Crit => GetDummyCritHeaderValue(),
                    KnownHeaders.ContentType => EncodeString(ContentTypeDummyValue, writer),
                    KnownHeaders.Kid => EncodeBytes(s_SampleContent, writer),
                    KnownHeaders.IV or KnownHeaders.PartialIV => EncodeBytes(ReadOnlySpan<byte>.Empty, writer),
                    _ => throw new InvalidOperationException()
                };
                Assert.True(ExpectedValue.Span.SequenceEqual(EncodedValue.Span));
                currentHeader++;
            }
            Assert.Equal(KnownHeaders.PartialIV + 1, currentHeader);

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
            byte[] encodedMessage = CoseSign1Message.Sign(s_SampleContent, ECDsaKeys[ECDsaAlgorithm.ES256], HashAlgorithmName.SHA256);
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeader.IsReadOnly);
        }

        [Fact]
        public void GetValueFromReadOnlyProtectedMap()
        {
            byte[] encodedMessage = CoseSign1Message.Sign(s_SampleContent, ECDsaKeys[ECDsaAlgorithm.ES256], HashAlgorithmName.SHA256);
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeader.IsReadOnly);

            int expectedAlgorithm = (int)ECDsaAlgorithm.ES256;

            int algorithm = message.ProtectedHeader.GetValueAsInt32(CoseHeaderLabel.Algorithm);
            Assert.Equal(expectedAlgorithm, algorithm);

            ReadOnlyMemory<byte> encodedAlgorithm = message.ProtectedHeader.GetEncodedValue(CoseHeaderLabel.Algorithm);
            Assert.Equal(expectedAlgorithm, new CborReader(encodedAlgorithm).ReadInt32());

            message.ProtectedHeader.TryGetEncodedValue(CoseHeaderLabel.Algorithm, out encodedAlgorithm);
            Assert.Equal(expectedAlgorithm, new CborReader(encodedAlgorithm).ReadInt32());
        }

        [Fact]
        public void SetValueAndRemoveThrowIfProtectedMapIsReadOnly()
        {
            byte[] encodedMessage = CoseSign1Message.Sign(s_SampleContent, ECDsaKeys[ECDsaAlgorithm.ES256], HashAlgorithmName.SHA256);
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            Assert.True(message.ProtectedHeader.IsReadOnly);

            var barLabel = new CoseHeaderLabel("bar");
            Assert.Throws<InvalidOperationException>(() => message.ProtectedHeader.SetValue(barLabel, 42));
            Assert.Throws<InvalidOperationException>(() => message.ProtectedHeader.Remove(barLabel));

            var fooLabel = new CoseHeaderLabel("foo");
            message.UnprotectedHeader.SetValue(fooLabel, 42);
            message.UnprotectedHeader.Remove(fooLabel);
        }

        public static IEnumerable<object[]> KnownHeadersEncodedValues_TestData()
        {
            var writer = new CborWriter();

            writer.WriteInt32((int)ECDsaAlgorithm.ES256);
            yield return ReturnDataAndReset(KnownHeaders.Alg, writer);

            //WriteDummyCritHeaderValue(writer);
            //yield return ReturnDataAndReset(KnownHeaders.Crit, writer);

            writer.WriteTextString(ContentTypeDummyValue);
            yield return ReturnDataAndReset(KnownHeaders.ContentType, writer);

            writer.WriteByteString(new byte[] { 0x42, 0x31, 0x31 });
            yield return ReturnDataAndReset(KnownHeaders.Kid, writer);

            writer.WriteByteString(ReadOnlySpan<byte>.Empty);
            yield return ReturnDataAndReset(KnownHeaders.IV, writer);

            writer.WriteByteString(ReadOnlySpan<byte>.Empty);
            yield return ReturnDataAndReset(KnownHeaders.PartialIV, writer);

            static object[] ReturnDataAndReset(int knownHeader, CborWriter w)
            {
                ReadOnlyMemory<byte> encodedValue = w.Encode();
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

        private static ReadOnlyMemory<byte> GetDummyCritHeaderValue()
        {
            var writer = new CborWriter();
            WriteDummyCritHeaderValue(writer);
            return writer.Encode();
        }
    }
}
