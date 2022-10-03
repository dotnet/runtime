// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Cbor;
using System.Linq;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    // Tests that apply only to custom header scenarios.
    public abstract class CoseMessageTests_Sign_CustomHeaderMaps : CoseMessageTests_Sign<AsymmetricAlgorithm>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => Enum.GetValues(typeof(CoseAlgorithm)).Cast<CoseAlgorithm>().ToList();

        // This method always uses the specified headers for signing.
        // That is, sign headers for MultiSign and body headers for Sign1.
        internal byte[] Sign(
            byte[] content,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            RSASignaturePadding? padding = null,
            bool isDetached = false)
            => Sign(content, GetCoseSigner(key, hashAlgorithm, protectedHeaders, unprotectedHeaders, padding), null, null, null, isDetached);

        // Returns the map that is set in CoseSigner and used for Signing.
        // For sign1, it returns one of the body header maps.
        // For multisign, it returns one of the sign header maps.
        internal abstract CoseHeaderMap GetSigningHeaderMap(CoseMessage msg, bool getProtectedMap);

        [Fact]
        public void SignVerifyWithCustomCoseHeaderMaps()
        {
            foreach ((AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm, RSASignaturePadding? padding)
                in GetKeyHashAlgorithmPaddingQuadruplet())
            {
                var protectedHeaders = GetEmptyHeaderMap();
                protectedHeaders.Add(CoseHeaderLabel.Algorithm, (int)algorithm);

                CoseHeaderMap unprotectedHeaders = new CoseHeaderMap();
                unprotectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);

                ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, key, hashAlgorithm, protectedHeaders, unprotectedHeaders, padding);

                List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedProtectedHeaders = GetExpectedProtectedHeaders(algorithm);
                List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedUnprotectedHeaders = GetEmptyExpectedHeaders();
                AddEncoded(expectedUnprotectedHeaders, CoseHeaderLabel.ContentType, ContentTypeDummyValue);

                AssertCoseSignMessage(encodedMsg, s_sampleContent, key, algorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

                CoseMessage decodedMsg = Decode(encodedMsg);
                Assert.True(Verify(decodedMsg, key, s_sampleContent));
            }
        }

        [Fact]
        public void SignVerifyWithStringAlgorithm()
        {
            foreach ((AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm, RSASignaturePadding? padding)
                in GetKeyHashAlgorithmPaddingQuadruplet())
            {
                string algString = algorithm.ToString();
                var protectedHeaders = GetEmptyHeaderMap();
                protectedHeaders.Add(CoseHeaderLabel.Algorithm, algString);

                ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, key, hashAlgorithm, protectedHeaders, padding: padding);

                List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedProtectedHeaders = GetEmptyExpectedHeaders();
                AddEncoded(expectedProtectedHeaders, CoseHeaderLabel.Algorithm, algString);
                List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedUnprotectedHeaders = GetEmptyExpectedHeaders();

                AssertCoseSignMessage(encodedMsg, s_sampleContent, key, algorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

                CoseMessage decodedMsg = Decode(encodedMsg);
                Assert.True(Verify(decodedMsg, key, s_sampleContent));
            }
        }

        [Fact]
        public void SignWithIncorrectAlgorithm()
        {
            foreach ((AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm, RSASignaturePadding? padding)
                in GetKeyHashAlgorithmPaddingQuadruplet())
            {
                // Values out of the ranges of CoseAlgorithm.
                foreach (int edgeValue in new[] { -6, -8, -34, -40, -256, -260 })
                {
                    var protectedHeaders = GetEmptyHeaderMap();
                    protectedHeaders.Add(CoseHeaderLabel.Algorithm, edgeValue);
                    Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, key, hashAlgorithm, protectedHeaders, padding: padding));
                }
            }
        }

        [Fact]
        public void SignWithIncorrectStringAlgorithm()
        {
            foreach ((AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseAlgorithm algorithm, RSASignaturePadding? padding)
                in GetKeyHashAlgorithmPaddingQuadruplet())
            {
                var protectedHeaders = GetEmptyHeaderMap();
                protectedHeaders.Add(CoseHeaderLabel.Algorithm, "FOO");
                Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, key, hashAlgorithm, protectedHeaders, padding: padding));
            }
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
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);

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
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullHeaderMaps()
        {
            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, null, null);
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullProtectedHeaderMap()
        {
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, null, unprotectedHeaders);
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
        }

        [Fact]
        public void SignWithNullUnprotectedHeaderMap()
        {
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, null);
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm);
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

            mapForCustomHeader.Add(myLabel, myValue);
            AddEncoded(listForCustomHeader, myLabel, myValue);

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

            Initialize();
            myLabel = new CoseHeaderLabel("42");

            mapForCustomHeader.Add(myLabel, myValue);
            AddEncoded(listForCustomHeader, myLabel, myValue);

            encodedMsg = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertCoseSignMessage(encodedMsg, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

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
            unprotectedHeaders.Add(CoseHeaderLabel.Algorithm, (int)DefaultAlgorithm);
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            // other known header is duplicate.
            Initialize(DefaultAlgorithm);
            protectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            unprotectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            // not-known int header is duplicate.
            Initialize(DefaultAlgorithm);
            var myLabel = new CoseHeaderLabel(42);
            protectedHeaders.Add(myLabel, 42);
            unprotectedHeaders.Add(myLabel, 42);
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            // not-known tstr header is duplicate.
            Initialize(DefaultAlgorithm);
            myLabel = new CoseHeaderLabel("42");
            protectedHeaders.Add(myLabel, 42);
            unprotectedHeaders.Add(myLabel, 42);
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders));

            void Initialize(CoseAlgorithm algorithm)
            {
                protectedHeaders = GetHeaderMapWithAlgorithm(algorithm);
                unprotectedHeaders = GetEmptyHeaderMap();
            }
        }

        [Fact]
        public void MultiSign_AddSignatureWithDuplicateHeaderBetweenProtectedAndUnprotected()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseHeaderMap protectedHeaders, unprotectedHeaders;
            Initialize(DefaultAlgorithm);
            CoseMultiSignMessage msg = Assert.IsType<CoseMultiSignMessage>(Decode(Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders))));

            // Algorithm header is duplicated. It is a special case because it is mandatory that the header exists in the protected map.
            unprotectedHeaders.Add(CoseHeaderLabel.Algorithm, (int)DefaultAlgorithm);
            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            Assert.Throws<CryptographicException>(() => AddSignature(msg, s_sampleContent, signer));

            // other known header is duplicate.
            Initialize(DefaultAlgorithm);
            protectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            unprotectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            Assert.Throws<CryptographicException>(() => AddSignature(msg, s_sampleContent, signer));

            // not-known int header is duplicate.
            Initialize(DefaultAlgorithm);
            var myLabel = new CoseHeaderLabel(42);
            protectedHeaders.Add(myLabel, 42);
            unprotectedHeaders.Add(myLabel, 42);
            signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            Assert.Throws<CryptographicException>(() => AddSignature(msg, s_sampleContent, signer));

            // not-known tstr header is duplicate.
            Initialize(DefaultAlgorithm);
            myLabel = new CoseHeaderLabel("42");
            protectedHeaders.Add(myLabel, 42);
            unprotectedHeaders.Add(myLabel, 42);
            signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            Assert.Throws<CryptographicException>(() => AddSignature(msg, s_sampleContent, signer));

            void Initialize(CoseAlgorithm algorithm)
            {
                protectedHeaders = GetHeaderMapWithAlgorithm(algorithm);
                unprotectedHeaders = GetEmptyHeaderMap();
            }
        }

        [Fact]
        public void ReEncodeWithDuplicateHeaderBetweenProtectedAndUnprotected()
        {
            // Algorithm header is duplicated. It is a special case because it is mandatory that the header exists in the protected map.
            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            CoseMessage msg = Decode(Sign(s_sampleContent, signer));

            GetSigningHeaderMap(msg, getProtectedMap: false).Add(CoseHeaderLabel.Algorithm, (int)DefaultAlgorithm);
            AllEncodeOverloadsShouldThrow(msg);

            // other known header is duplicate.
            CoseHeaderMap protectedHeaders = GetEmptyHeaderMap();
            protectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);

            signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders);
            msg = Decode(Sign(s_sampleContent, signer));

            GetSigningHeaderMap(msg, getProtectedMap: false).Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            AllEncodeOverloadsShouldThrow(msg);

            // not-known int header is duplicate.
            var myLabel = new CoseHeaderLabel(42);
            protectedHeaders = GetEmptyHeaderMap();
            protectedHeaders.Add(myLabel, 42);

            signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders);
            msg = Decode(Sign(s_sampleContent, signer));

            GetSigningHeaderMap(msg, getProtectedMap: false).Add(myLabel, 42);
            AllEncodeOverloadsShouldThrow(msg);

            // not-known tstr header is duplicate.
            myLabel = new CoseHeaderLabel("42");
            protectedHeaders = GetEmptyHeaderMap();
            protectedHeaders.Add(myLabel, 42);

            signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders);
            msg = Decode(Sign(s_sampleContent, signer));

            GetSigningHeaderMap(msg, getProtectedMap: false).Add(myLabel, 42);
            AllEncodeOverloadsShouldThrow(msg);
        }

        [Fact]
        public void MultiSign_ReEncodeWithDuplicateHeaderBetweenProtectedAndUnprotected_BodyProtected()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            // known header is duplicate.
            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            CoseHeaderMap protectedHeaders = GetEmptyHeaderMap();
            protectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);

            CoseMessage msg = Decode(Sign(s_sampleContent, signer, protectedHeaders));

            msg.UnprotectedHeaders.Add(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            AllEncodeOverloadsShouldThrow(msg);

            // not-known int header is duplicate.
            var myLabel = new CoseHeaderLabel(42);
            protectedHeaders = GetEmptyHeaderMap();
            protectedHeaders.Add(myLabel, 42);

            signer = GetCoseSigner(DefaultKey, DefaultHash);
            msg = Decode(Sign(s_sampleContent, signer, protectedHeaders));

            msg.UnprotectedHeaders.Add(myLabel, 42);
            AllEncodeOverloadsShouldThrow(msg);

            // not-known tstr header is duplicate.
            myLabel = new CoseHeaderLabel("42");
            protectedHeaders = GetEmptyHeaderMap();
            protectedHeaders.Add(myLabel, 42);

            signer = GetCoseSigner(DefaultKey, DefaultHash);
            msg = Decode(Sign(s_sampleContent, signer, protectedHeaders));

            msg.UnprotectedHeaders.Add(myLabel, 42);
            AllEncodeOverloadsShouldThrow(msg);
        }

        private void AllEncodeOverloadsShouldThrow(CoseMessage msg)
        {
            Assert.Throws<CryptographicException>(msg.Encode);
            byte[] destination = new byte[msg.GetEncodedLength()];
            Assert.Throws<CryptographicException>(() => msg.Encode(destination));
            Assert.Throws<CryptographicException>(() => msg.TryEncode(destination, out _));
        }

        [Theory]
        [MemberData(nameof(AllCborTypesTestDataHeaderMaps))]
        public void SignWithAllCborTypesAsHeaderValue(bool useProtectedMap, byte[] encodedValue)
        {
            var myLabel = new CoseHeaderLabel(42);

            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();
            (useProtectedMap ? protectedHeaders : unprotectedHeaders)[myLabel] = CoseHeaderValue.FromEncodedValue(encodedValue);

            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedProtectedHeaders = GetExpectedProtectedHeaders(DefaultAlgorithm);
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedUnprotectedHeaders = GetEmptyExpectedHeaders();
            (useProtectedMap ? expectedProtectedHeaders : expectedUnprotectedHeaders).Add((myLabel, encodedValue));

            ReadOnlySpan<byte> encodedMessage = Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders, unprotectedHeaders);
            AssertCoseSignMessage(encodedMessage, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders, expectedUnprotectedHeaders);

            // Verify it is transported correctly.
            CoseMessage message = Decode(encodedMessage);
            ReadOnlyMemory<byte> roundtrippedValue = GetSigningHeaderMap(message, useProtectedMap)[myLabel].EncodedValue;
            AssertExtensions.SequenceEqual(encodedValue, roundtrippedValue.Span);
        }

        [Theory]
        [MemberData(nameof(AllCborTypesTestDataHeaderMaps))]
        public void MultiSign_SignWithAllCborTypesAsHeaderValue_BodyHeaders(bool useProtectedMap, byte[] encodedValue)
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            var myLabel = new CoseHeaderLabel(42);

            CoseHeaderMap signProtectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            CoseHeaderMap signUnprotectedHeaders = GetEmptyHeaderMap();

            CoseHeaderMap bodyProtectedHeaders = GetEmptyHeaderMap();
            CoseHeaderMap bodyUnprotectedHeaders = GetEmptyHeaderMap();
            (useProtectedMap ? bodyProtectedHeaders : bodyUnprotectedHeaders)[myLabel] = CoseHeaderValue.FromEncodedValue(encodedValue);

            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedSignProtectedHeaders = GetExpectedProtectedHeaders(DefaultAlgorithm);
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedSignUnprotectedHeaders = GetEmptyExpectedHeaders();

            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedProtectedHeaders = GetEmptyExpectedHeaders();
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedUnprotectedHeaders = GetEmptyExpectedHeaders();
            (useProtectedMap ? expectedProtectedHeaders : expectedUnprotectedHeaders).Add((myLabel, encodedValue));

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash, signProtectedHeaders, signUnprotectedHeaders);
            ReadOnlySpan<byte> encodedMessage = Sign(s_sampleContent, signer, bodyProtectedHeaders, bodyUnprotectedHeaders);
            AssertCoseSignMessage(
                encodedMessage,
                s_sampleContent,
                DefaultKey,
                DefaultAlgorithm,
                expectedSignProtectedHeaders,
                expectedSignUnprotectedHeaders,
                null,
                expectedProtectedHeaders,
                expectedUnprotectedHeaders);

            // Verify it is transported correctly.
            CoseMessage message = Decode(encodedMessage);
            ReadOnlyMemory<byte> roundtrippedValue = (useProtectedMap ? message.ProtectedHeaders : message.UnprotectedHeaders)[myLabel].EncodedValue;
            AssertExtensions.SequenceEqual(encodedValue, roundtrippedValue.Span);
        }

        public static IEnumerable<object[]> AllCborTypesTestDataHeaderMaps()
        {
            foreach (bool useProtectedMap in new[] { false, true })
            {
                foreach (byte[] encodedValue in AllCborTypes())
                {
                    yield return new object[] { useProtectedMap, encodedValue };
                }
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
            protectedHeaders[CoseHeaderLabel.Algorithm] =  CoseHeaderValue.FromEncodedValue(encodedValue);

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
            protectedHeaders[CoseHeaderLabel.Algorithm] = CoseHeaderValue.FromEncodedValue(encodedValue);

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
            protectedHeaders[CoseHeaderLabel.Algorithm] = CoseHeaderValue.FromEncodedValue(encodedValue);

            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, DefaultKey, DefaultHash, protectedHeaders));
        }

        [Fact]
        public void SignWithCriticalHeaders()
        {
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedProtectedHeaders = GetExpectedProtectedHeaders(DefaultAlgorithm);
            AddCriticalHeaders(protectedHeaders, expectedProtectedHeaders, includeSpecifiedCritHeader: true);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders);
            ReadOnlySpan<byte> encodedMessage = Sign(s_sampleContent, signer);

            AssertCoseSignMessage(encodedMessage, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders);
        }

        [Fact]
        public void SignWithCriticalHeaders_NotTransportingTheSpecifiedCriticalHeaderThrows()
        {
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            AddCriticalHeaders(protectedHeaders, null, includeSpecifiedCritHeader: false);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash, protectedHeaders);
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, signer));
        }

        [Fact]
        public void MultiSign_SignWithCriticalHeaders_BodyHeaders()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseHeaderMap bodyProtectedHeaders = GetEmptyHeaderMap();
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedBodyProtected = GetEmptyExpectedHeaders();
            AddCriticalHeaders(bodyProtectedHeaders, expectedBodyProtected, includeSpecifiedCritHeader: true);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            ReadOnlySpan<byte> encodedMessage = Sign(s_sampleContent, signer, bodyProtectedHeaders);

            AssertCoseSignMessage(encodedMessage, s_sampleContent, DefaultKey, DefaultAlgorithm, expectedMultiSignBodyProtectedHeaders: expectedBodyProtected);
        }

        [Fact]
        public void MultiSign_SignWithCriticalHeaders_NotTransportingTheSpecifiedCriticalHeaderThrows_BodyHeaders()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            CoseHeaderMap bodyProtectedHeaders = GetEmptyHeaderMap();
            AddCriticalHeaders(bodyProtectedHeaders, null, includeSpecifiedCritHeader: false);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash);
            Assert.Throws<CryptographicException>(() => Sign(s_sampleContent, signer, bodyProtectedHeaders));
        }

        [Fact]
        public void MultiSign_SignWithCriticalHeaders_AddSignature()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(Decode(encodedMsg));

            multiSignMsg.RemoveSignature(0);

            CoseHeaderMap signProtectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            List<(CoseHeaderLabel, ReadOnlyMemory<byte>)> expectedSignProtected = GetExpectedProtectedHeaders(DefaultAlgorithm);
            AddCriticalHeaders(signProtectedHeaders, expectedSignProtected, includeSpecifiedCritHeader: true);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash, signProtectedHeaders);
            AddSignature(multiSignMsg, s_sampleContent, signer);

            AssertCoseSignMessage(multiSignMsg.Encode(), s_sampleContent, DefaultKey, DefaultAlgorithm, expectedProtectedHeaders: expectedSignProtected);
        }

        [Fact]
        public void MultiSign_SignWithCriticalHeaders_NotTransportingTheSpecifiedCriticalHeaderThrows_AddSignature()
        {
            if (MessageKind != CoseMessageKind.MultiSign)
            {
                return;
            }

            ReadOnlySpan<byte> encodedMsg = Sign(s_sampleContent, GetCoseSigner(DefaultKey, DefaultHash));
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(Decode(encodedMsg));

            multiSignMsg.RemoveSignature(0);

            CoseHeaderMap signProtectedHeaders = GetHeaderMapWithAlgorithm(DefaultAlgorithm);
            AddCriticalHeaders(signProtectedHeaders, null, includeSpecifiedCritHeader: false);

            CoseSigner signer = GetCoseSigner(DefaultKey, DefaultHash, signProtectedHeaders);
            Assert.Throws<CryptographicException>(() => AddSignature(multiSignMsg, s_sampleContent, signer));
        }

        private static void AddCriticalHeaders(
            CoseHeaderMap protectedHeaders, List<(CoseHeaderLabel, ReadOnlyMemory<byte>)>? expectedHeaders, bool includeSpecifiedCritHeader)
        {
            Assert.Equal(expectedHeaders != null, includeSpecifiedCritHeader);

            CoseHeaderValue critValue = CoseHeaderValue.FromEncodedValue(GetDummyCritHeaderValue());
            protectedHeaders[CoseHeaderLabel.CriticalHeaders] = critValue;
            expectedHeaders?.Add((CoseHeaderLabel.CriticalHeaders, critValue.EncodedValue));

            if (includeSpecifiedCritHeader)
            {
                var myCritHeaderLabel = new CoseHeaderLabel(42);
                var myCritHeaderValue = CoseHeaderValue.FromString("My custom critical header value.");
                protectedHeaders.Add(myCritHeaderLabel, myCritHeaderValue);
                expectedHeaders?.Add((myCritHeaderLabel, myCritHeaderValue.EncodedValue));
            }
        }
    }
}
