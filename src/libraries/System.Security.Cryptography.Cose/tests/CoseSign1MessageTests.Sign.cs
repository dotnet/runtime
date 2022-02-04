// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Cbor;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Sign and Verify are not supported in browser")]
    public partial class CoseSign1MessageTests
    {
        [Theory]
        [InlineData(ECDsaAlgorithm.ES256)]
        [InlineData(ECDsaAlgorithm.ES384)]
        [InlineData(ECDsaAlgorithm.ES512)]
        public void SignVerifyECDsa(ECDsaAlgorithm algorithm)
        {
            ECDsa ecdsa = ECDsaKeys[algorithm];
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmNameFromCoseAlgorithm((int)algorithm);

            byte[] coseMessageBytes = CoseSign1Message.Sign(s_sampleContent, ecdsa, hashAlgorithm);
            CoseSign1Message msg = CoseMessage.DecodeSign1(coseMessageBytes);
            Assert.True(msg.Verify(ecdsa));
        }

        [Theory]
        [InlineData(RSAAlgorithm.PS256)]
        [InlineData(RSAAlgorithm.PS384)]
        [InlineData(RSAAlgorithm.PS512)]
        public void SignVerifyRSA(RSAAlgorithm algorithm)
        {
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmNameFromCoseAlgorithm((int)algorithm);

            byte[] coseMessageBytes = CoseSign1Message.Sign(s_sampleContent, RSAKey, hashAlgorithm);
            CoseSign1Message msg = CoseMessage.DecodeSign1(coseMessageBytes);
            Assert.True(msg.Verify(RSAKey));
        }

        [Theory]
        [InlineData(ECDsaAlgorithm.ES256)]
        [InlineData(ECDsaAlgorithm.ES384)]
        [InlineData(ECDsaAlgorithm.ES512)]
        public void SignVerifyWithCustomHeaders(ECDsaAlgorithm algorithm)
        {
            var protectedHeaders = new CoseHeaderMap();
            protectedHeaders.SetValue(CoseHeaderLabel.Algorithm, (int)algorithm);

            CoseHeaderMap unprotectedHeaders = new CoseHeaderMap();
            unprotectedHeaders.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);

            ECDsa key = ECDsaKeys[algorithm];
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmNameFromCoseAlgorithm((int)algorithm);

            byte[] message = CoseSign1Message.Sign(s_sampleContent, protectedHeaders, unprotectedHeaders, key, hashAlgorithm);
            Assert.True(CoseMessage.DecodeSign1(message).Verify(key));
        }

        [Fact]
        public void SignWithNullContent()
        {
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(null!, DefaultKey, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(null!, RSAKey, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(null!, GetHeaderMapWithAlgorithm(), GetEmptyHeaderMap(), DefaultKey, DefaultHash));
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void SignWithValidContent(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);

            ECDsa ecdsa = DefaultKey;
            AssertSign1Message(CoseSign1Message.Sign(content, ecdsa, HashAlgorithmName.SHA256),
                (int)ECDsaAlgorithm.ES256, content, ecdsa);

            AssertSign1Message(CoseSign1Message.Sign(content, RSAKey, HashAlgorithmName.SHA256),
                (int)RSAAlgorithm.PS256, content, RSAKey);

            AssertSign1Message(CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm(), GetEmptyHeaderMap(), ecdsa, GetHashAlgorithmNameFromCoseAlgorithm((int)ECDsaAlgorithm.ES256)),
                (int)ECDsaAlgorithm.ES256, content, ecdsa);
        }

        [Fact]
        public void SignWithNullKey()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(content, (ECDsa)null!, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(content, (RSA)null!, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm(), GetEmptyHeaderMap(), null!, GetHashAlgorithmNameFromCoseAlgorithm((int)ECDsaAlgorithm.ES256)));
        }

        [Theory]
        [InlineData((int)ECDsaAlgorithm.ES256)]
        [InlineData((int)ECDsaAlgorithm.ES384)]
        [InlineData((int)ECDsaAlgorithm.ES512)]
        [InlineData((int)RSAAlgorithm.PS256)]
        [InlineData((int)RSAAlgorithm.PS384)]
        [InlineData((int)RSAAlgorithm.PS512)]
        public void SignWithNonPrivateKey(int algorithm)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmNameFromCoseAlgorithm(algorithm);
            AsymmetricAlgorithm key;

            if (Enum.IsDefined(typeof(ECDsaAlgorithm), algorithm))
            {
                ECDsa ecdsa = ECDsaKeysWithoutPrivateKey[(ECDsaAlgorithm)algorithm];
                Assert.ThrowsAny<CryptographicException>(() => CoseSign1Message.Sign(content, ecdsa, hashAlgorithm));
                key = ecdsa;
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(() => CoseSign1Message.Sign(content, RSAKeyWithoutPrivateKey, hashAlgorithm));
                key = RSAKeyWithoutPrivateKey;
            }

            Assert.ThrowsAny<CryptographicException>(() => CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm(algorithm), GetEmptyHeaderMap(), key, hashAlgorithm));
        }

        [Fact]
        public void SignWithUnsupportedKey()
        {
            AsymmetricAlgorithm key = ECDiffieHellman.Create();
            // Header still says that a supported combination of key-algorithm will be used.
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), GetHeaderMapWithAlgorithm(), GetEmptyHeaderMap(), key, DefaultHash));
        }

        [Theory]
        [InlineData("SHA1")]
        [InlineData("FOO")]
        public void SignWithUnsupportedHashAlgorithm(string hashAlgorithm)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);

            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), DefaultKey, new HashAlgorithmName(hashAlgorithm)));
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), RSAKey, new HashAlgorithmName(hashAlgorithm)));
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), GetHeaderMapWithAlgorithm(-47 /*ES256K*/), GetEmptyHeaderMap(), DefaultKey, DefaultHash));
        }

        [Theory]
        [InlineData((int)ECDsaAlgorithm.ES256)]
        [InlineData((int)ECDsaAlgorithm.ES384)]
        [InlineData((int)ECDsaAlgorithm.ES512)]
        [InlineData((int)RSAAlgorithm.PS256)]
        [InlineData((int)RSAAlgorithm.PS384)]
        [InlineData((int)RSAAlgorithm.PS512)]
        public void SignWithSupportedHashAlgorithm(int algorithm)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            HashAlgorithmName hashAlgorithm = GetHashAlgorithmNameFromCoseAlgorithm(algorithm);
            AsymmetricAlgorithm key;

            if (Enum.IsDefined(typeof(ECDsaAlgorithm), algorithm))
            {
                ECDsa ecdsa = ECDsaKeys[(ECDsaAlgorithm)algorithm];
                key = ecdsa;
                AssertSign1Message(CoseSign1Message.Sign(content, ecdsa, hashAlgorithm),algorithm, content, key);
            }
            else
            {
                key = RSAKey;
                AssertSign1Message(CoseSign1Message.Sign(content, RSAKey, hashAlgorithm), algorithm, content, key);
            }

            AssertSign1Message(CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm(algorithm), GetEmptyHeaderMap(), key, hashAlgorithm), algorithm, content, key);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithIsDetached(bool isDetached)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);

            byte[] messageEncoded = CoseSign1Message.Sign(content, DefaultKey, DefaultHash, isDetached);
            VerifyContentDetached(messageEncoded, content, isDetached);

            messageEncoded = CoseSign1Message.Sign(content, DefaultKey, DefaultHash, isDetached);
            VerifyContentDetached(messageEncoded, content, isDetached);

            static void VerifyContentDetached(byte[] messageEncoded, byte[] expectedContent, bool isDetached)
            {
                CoseMessage messageDecoded = CoseMessage.DecodeSign1(messageEncoded);
                if (isDetached)
                {
                    Assert.Null(messageDecoded.Content);
                }
                else
                {
                    AssertExtensions.SequenceEqual(new ReadOnlySpan<byte>(expectedContent), messageDecoded.Content.GetValueOrDefault().Span);
                }
            }
        }

        [Fact]
        public void SignWithEmptyHeaderMaps()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            int expectedAlgorithm = (int)ECDsaAlgorithm.ES256;

            AssertSign1Message(CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), GetEmptyHeaderMap(), GetEmptyHeaderMap(), DefaultKey, DefaultHash),
                expectedAlgorithm, content, DefaultKey);
        }

        [Fact]
        public void SignWithEmptyProtectedHeaderMap()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            CoseHeaderMap protectedHeaders = GetEmptyHeaderMap();
            CoseHeaderMap unprotectedHeaders = GetHeaderMapWithAlgorithm();

            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash));
        }

        [Fact]
        public void SignWithEmptyUnprotectedHeaderMap()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm(); 
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();

            AssertSign1Message(CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash),
                (int)ECDsaAlgorithm.ES256, content, DefaultKey);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithCustomHeader(bool useProtectedMap)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            CoseHeaderMap protectedHeaders, unprotectedHeaders, mapForCustomHeader;
            int expectedProtectedHeaders = useProtectedMap ? 2 : 1;
            int expectedUnprotectedHeaders = useProtectedMap ? 0 : 1;
            InitializeHeaders();

            mapForCustomHeader.SetValue(label: new CoseHeaderLabel(42), value: 42);
            AssertSign1Message(CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash),
                (int)ECDsaAlgorithm.ES256, content, DefaultKey, expectedProtectedHeaders, expectedUnprotectedHeaders);

            InitializeHeaders();
            mapForCustomHeader.SetValue(label: new CoseHeaderLabel("42"), value: 42);
            AssertSign1Message(CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash),
                (int)ECDsaAlgorithm.ES256, content, DefaultKey, expectedProtectedHeaders, expectedUnprotectedHeaders);

            void InitializeHeaders()
            {
                protectedHeaders = GetHeaderMapWithAlgorithm();
                unprotectedHeaders = GetEmptyHeaderMap();
                mapForCustomHeader = useProtectedMap ? protectedHeaders : unprotectedHeaders;
            }
        }

        [Fact]
        public void SignWithDuplicateHeaderBetweenProtectedAndUnprotected()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            AsymmetricAlgorithm key = DefaultKey;
            CoseHeaderMap protectedHeaders, unprotectedHeaders;

            // Algorithm header is duplicated. It is a special case because it is mandatory that the header exists in the protected map.
            InitializeHeaders();
            // @protected.SetValue(CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256); // We don't need to Set Algorithm header as InitHeader does it for us.
            unprotectedHeaders.SetValue(CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash));

            // other known header is duplicate.
            InitializeHeaders();
            protectedHeaders.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            unprotectedHeaders.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash));

            // not-known int header is duplicate.
            InitializeHeaders();
            var myLabel = new CoseHeaderLabel(42);
            protectedHeaders.SetValue(myLabel, 42);
            unprotectedHeaders.SetValue(myLabel, 42);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash));

            // not-known tstr header is duplicate.
            InitializeHeaders();
            myLabel = new CoseHeaderLabel("42");
            protectedHeaders.SetValue(myLabel, 42);
            unprotectedHeaders.SetValue(myLabel, 42);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash));

            void InitializeHeaders()
            {
                protectedHeaders = GetHeaderMapWithAlgorithm();
                unprotectedHeaders = GetEmptyHeaderMap();
            }
        }

        [Theory]
        [MemberData(nameof(AllCborTypes_TestData))]
        public void SignWithAllCborTypesAsHeaderValue(bool useProtectedMap, byte[] encodedValue)
        {
            CoseHeaderMap protectedHeaders = GetHeaderMapWithAlgorithm();
            CoseHeaderMap unprotectedHeaders = GetEmptyHeaderMap();
            var myLabel = new CoseHeaderLabel(42);
            (useProtectedMap ? protectedHeaders : unprotectedHeaders).SetEncodedValue(myLabel, encodedValue);

            byte[] encodedMessage = CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), protectedHeaders, unprotectedHeaders, DefaultKey, DefaultHash);

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
    }
}
