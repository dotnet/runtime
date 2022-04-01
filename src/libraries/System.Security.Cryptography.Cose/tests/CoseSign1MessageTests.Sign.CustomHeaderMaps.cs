// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    // Tests that apply only to custom header scenarios.
    public abstract class CoseSign1MessageTests_Sign_CustomHeaderMaps : CoseSign1MessageTests_Sign<AsymmetricAlgorithm>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => Enum.GetValues(typeof(CoseAlgorithm)).Cast<CoseAlgorithm>().ToList();

        internal override byte[] Sign(byte[] content, AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, bool isDetached = false)
            => Sign(content, key, hashAlgorithm, null, null, isDetached);
        internal abstract byte[] Sign(
            byte[] content,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            bool isDetached = false);
        internal override bool Verify(CoseSign1Message msg, AsymmetricAlgorithm key)
            => key is ECDsa ecdsa ? msg.Verify(ecdsa) : msg.Verify((RSA)key);
        internal override bool Verify(CoseSign1Message msg, AsymmetricAlgorithm key, ReadOnlySpan<byte> content)
            => key is ECDsa ecdsa ? msg.Verify(ecdsa, content) : msg.Verify((RSA)key);

        [Fact]
        public void SignVerifyWithCustomCoseHeaderMaps()
        {
            foreach ((AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm) in GetKeyHashAlgorithmTriplet())
            {
                var protectedHeaders = new CoseHeaderMap();
                protectedHeaders.SetValue(CoseHeaderLabel.Algorithm, (int)algorithm);

                CoseHeaderMap unprotectedHeaders = new CoseHeaderMap();
                unprotectedHeaders.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);

                ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, key, hashAlgorithm, protectedHeaders, unprotectedHeaders);

                List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedProtectedHeaders = GetExpectedProtectedHeaders(algorithm);
                List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedUnprotectedHeaders = GetEmptyExpectedHeaders();
                AddEncoded(expectedUnprotectedHeaders, CoseHeaderLabel.ContentType, ContentTypeDummyValue);

                AssertSign1Message(encodedMsg, s_sampleContent, key, algorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

                CoseSign1Message decodedMsg = CoseMessage.DecodeSign1(encodedMsg);
                Assert.True(Verify(decodedMsg, key));
            }
        }

        [Fact]
        public void SignWithUnsupportedKey()
        {
            AsymmetricAlgorithm key = ECDiffieHellman.Create();
            // Header still says that a supported combination of key-algorithm will be used.
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, key, DefaultHash, GetHeaderMapWithAlgorithm(DefaultAlgorithm)));
        }

        [Fact]
        public void SignWithUnsupportedCoseAlgorithm()
        {
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, GetHeaderMapWithAlgorithm((CoseAlgorithm)(-47) /*ES256K*/)));
        }

        [Fact]
        public void SignWithEmptyHeaderMaps()
        {
            CoseHeaderMap protectedHeaders = GetEmptyHeaderMap();
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);

            Assert.Equal(0, protectedHeaders.Count());
            Assert.Equal(0, unprotectedHeaders.Count());
        }

        [Fact]
        public void SignWith_EmptyProtectedHeaderMap_UnprotectedHeaderMapWithAlgorithm()
        {
            CoseHeaderMap protectedHeaders = GetEmptyHeaderMap();
            CoseHeaderMap unprotectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));
        }

        [Fact]
        public void SignWith_ProtectedHeaderMapWithAlgorithm_EmptyUnprotectedHeaderMap()
        {
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullHeaderMaps()
        {
            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, null, null);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullProtectedHeaderMap()
        {
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, null, unprotectedHeaders);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullUnprotectedHeaderMap()
        {
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, null);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithNotKnownHeader(bool useProtectedMap)
        {
            CoseHeaderMap protectedHeaders, unprotectedHeaders, mapForCustomHeader;
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedProtectedHeaders, expectedUnprotectedHeaders, listForCustomHeader;

            Initialize();
            CoseHeaderLabel myLabel = new CoseHeaderLabel(42);
            int myValue = 42;

            mapForCustomHeader.SetValue(myLabel, myValue);
            AddEncoded(listForCustomHeader, myLabel, myValue);

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

            Initialize();
            myLabel = new CoseHeaderLabel("42");

            mapForCustomHeader.SetValue(myLabel, myValue);
            AddEncoded(listForCustomHeader, myLabel, myValue);

            encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertSign1Message(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

            void Initialize()
            {
                protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
                unprotectedHeaders = GetEmptyHeaderMap();
                mapForCustomHeader = useProtectedMap ? protectedHeaders : unprotectedHeaders;

                expectedProtectedHeaders = GetExpectedProtectedHeaders(DefaultAlgorithm);
                expectedUnprotectedHeaders = new List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>();
                listForCustomHeader = useProtectedMap ? expectedProtectedHeaders : expectedUnprotectedHeaders;
            }
        }

        [Fact]
        public void SignWithDuplicateHeaderBetweenProtectedAndUnprotected()
        {
            CoseHeaderMap protectedHeaders, unprotectedHeaders;

            // Algorithm header is duplicated. It is a special case because it is mandatory that the header exists in the protected map.
            Initialize(DefaultAlgorithm);
            unprotectedHeaders.SetValue(CoseHeaderLabel.Algorithm, (int)DefaultAlgorithm);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            // other known header is duplicate.
            Initialize(DefaultAlgorithm);
            protectedHeaders.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            unprotectedHeaders.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            // not-known int header is duplicate.
            Initialize(DefaultAlgorithm);
            var myLabel = new CoseHeaderLabel(42);
            protectedHeaders.SetValue(myLabel, 42);
            unprotectedHeaders.SetValue(myLabel, 42);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            // not-known tstr header is duplicate.
            Initialize(DefaultAlgorithm);
            myLabel = new CoseHeaderLabel("42");
            protectedHeaders.SetValue(myLabel, 42);
            unprotectedHeaders.SetValue(myLabel, 42);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            void Initialize(CoseAlgorithm algorithm)
            {
                protectedHeaders = GetHeaderMapWithAlgorithm(algorithm);
                unprotectedHeaders = GetEmptyHeaderMap();
            }
        }

        [Theory]
        [MemberData(nameof(AllCborTypes_TestData))]
        public void SignWithAllCborTypesAsHeaderValue(bool useProtectedMap, byte[] encodedValue)
        {
            var myLabel = new CoseHeaderLabel(42);

            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();
            (useProtectedMap ? protectedHeaders : unprotectedHeaders).SetEncodedValue(myLabel, encodedValue);

            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedProtectedHeaders = GetExpectedProtectedHeaders(DefaultAlgorithm);
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedUnprotectedHeaders = GetEmptyExpectedHeaders();
            (useProtectedMap ? expectedProtectedHeaders : expectedUnprotectedHeaders).Add((myLabel, encodedValue));

            ReadOnlySpan<byte> encodedMessage = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertSign1Message(encodedMessage, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

            // Verify it is transported correctly.
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            ReadOnlyMemory<byte> roundtrippedValue = (useProtectedMap ? message.ProtectedHeaders : message.UnprotectedHeaders).GetEncodedValue(myLabel);
            AssertExtensions.SequenceEqual(encodedValue, roundtrippedValue.Span);
        }

        public static IEnumerable<object[]> AllCborTypes_TestData()
        {
            foreach (bool useProtectedMap in new[] { false, true })
            {
                var w = new CborWriter();

                w.WriteBigInteger(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteBoolean(true);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteByteString(s_sampleContent);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteCborNegativeIntegerRepresentation(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteDateTimeOffset(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteDecimal(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteDecimal(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteDouble(default);
                yield return ReturnDataAndReset(useProtectedMap, w);
#if NETCOREAPP
                w.WriteHalf(default);
                yield return ReturnDataAndReset(useProtectedMap, w);
#endif
                w.WriteInt32(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteInt64(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteNull();
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteSimpleValue(CborSimpleValue.Undefined);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteSingle(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteTag(CborTag.UnsignedBigNum);
                w.WriteInt32(42);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteTextString(string.Empty);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteUInt32(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteUInt64(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                w.WriteUnixTimeSeconds(default);
                yield return ReturnDataAndReset(useProtectedMap, w);

                // Array
                w.WriteStartArray(2);
                w.WriteInt32(42);
                w.WriteTextString("foo");
                w.WriteEndArray();
                yield return ReturnDataAndReset(useProtectedMap, w);

                // Map
                w.WriteStartMap(2);
                // first label-value pair.
                w.WriteInt32(42);
                w.WriteTextString("4242");
                // second label-value pair.
                w.WriteTextString("42");
                w.WriteInt32(4242);
                w.WriteEndMap();
                yield return ReturnDataAndReset(useProtectedMap, w);

                // Indefinite length array
                w.WriteStartArray(null);
                w.WriteInt32(42);
                w.WriteTextString("foo");
                w.WriteEndArray();
                yield return ReturnDataAndReset(useProtectedMap, w);

                // Indefinite length map
                w.WriteStartMap(null);
                // first label-value pair.
                w.WriteInt32(42);
                w.WriteTextString("4242");
                // second label-value pair.
                w.WriteTextString("42");
                w.WriteInt32(4242);
                w.WriteEndMap();
                yield return ReturnDataAndReset(useProtectedMap, w);

                // Indefinite length tstr
                w.WriteStartIndefiniteLengthTextString();
                w.WriteTextString("foo");
                w.WriteEndIndefiniteLengthTextString();
                yield return ReturnDataAndReset(useProtectedMap, w);

                // Indefinite length bstr
                w.WriteStartIndefiniteLengthByteString();
                w.WriteByteString(s_sampleContent);
                w.WriteEndIndefiniteLengthByteString();
                yield return ReturnDataAndReset(useProtectedMap, w);
            }

            static object[] ReturnDataAndReset(bool useProtectedMap, CborWriter w)
            {
                byte[] encodedValue = w.Encode();
                w.Reset();
                return new object[] { useProtectedMap, encodedValue };
            }
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(int.MinValue - 1L)]
        [InlineData(int.MaxValue + 1L)]
        public void SignWithInt64AlgorithmHeaderValue(long value)
        {
            var writer = new CborWriter();
            writer.WriteInt64(value);
            ReadOnlySpan<byte> encodedValue = writer.Encode();

            CoseHeaderMap protectedHeaders = new CoseHeaderMap();
            protectedHeaders.SetEncodedValue(CoseHeaderLabel.Algorithm, encodedValue);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders));
        }

        [Theory]
        [InlineData(0UL)]
        [InlineData(ulong.MaxValue)]
        [InlineData(long.MaxValue + 1UL)]
        [InlineData(long.MaxValue - 1UL)]
        public void SignWithUInt64AlgorithmHeaderValue(ulong value)
        {
            var writer = new CborWriter();
            writer.WriteUInt64(value);
            ReadOnlySpan<byte> encodedValue = writer.Encode();

            CoseHeaderMap protectedHeaders = new CoseHeaderMap();
            protectedHeaders.SetEncodedValue(CoseHeaderLabel.Algorithm, encodedValue);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders));
        }

        [Theory]
        [InlineData(0UL)]
        [InlineData(ulong.MaxValue)]
        [InlineData(long.MaxValue + 1UL)]
        [InlineData(long.MaxValue - 1UL)]
        public void SignWithCborNegativeIntegerRepresentationAlgorithmHeaderValue(ulong value)
        {
            var writer = new CborWriter();
            writer.WriteCborNegativeIntegerRepresentation(value);
            ReadOnlySpan<byte> encodedValue = writer.Encode();

            CoseHeaderMap protectedHeaders = new CoseHeaderMap();
            protectedHeaders.SetEncodedValue(CoseHeaderLabel.Algorithm, encodedValue);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders));
        }
    }

    public class CoseSign1MessageTests_Sign_ByteArray : CoseSign1MessageTests_Sign_CustomHeaderMaps
    {
        internal override byte[] Sign(
            byte[] content,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            bool isDetached = false)
            => CoseSign1Message.Sign(content, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, isDetached);
    }

    public class CoseSign1MessageTests_TrySign : CoseSign1MessageTests_Sign_CustomHeaderMaps
    {
        internal override byte[] Sign(
            byte[] content!!,
            AsymmetricAlgorithm key!!,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            bool isDetached = false)
        {
            Span<byte> destination;
            int bytesWritten;

            byte[] expectedEncodedMsg = CoseSign1Message.Sign(content, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, isDetached);

            // Assert TrySign returns false when destination buffer is smaller than what we need (size - i).
            for (int i = 1; i < 10; i++)
            {
                destination = expectedEncodedMsg.AsSpan(0, expectedEncodedMsg.Length - i);
                Assert.False(CoseSign1Message.TrySign(content, destination, key, hashAlgorithm, out bytesWritten, protectedHeaders, unprotectedHeaders, isDetached));
                Assert.Equal(0, bytesWritten);
            }

            // Assert TrySign returns true when destination is double the required size (or at least 2k).
            destination = new byte[Math.Max(expectedEncodedMsg.Length * 2, 2048)];
            Assert.True(CoseSign1Message.TrySign(content, destination, key, hashAlgorithm, out bytesWritten, protectedHeaders, unprotectedHeaders, isDetached));
            Assert.Equal(expectedEncodedMsg.Length, bytesWritten);

            // Assert TrySign returns true when destination is the exact size required.
            destination = destination.Slice(0, expectedEncodedMsg.Length);
            destination.Clear();
            Assert.True(CoseSign1Message.TrySign(content, destination, key, hashAlgorithm, out bytesWritten, protectedHeaders, unprotectedHeaders, isDetached));
            Assert.Equal(destination.Length, bytesWritten);

            return destination.ToArray();
        }
    }
}
