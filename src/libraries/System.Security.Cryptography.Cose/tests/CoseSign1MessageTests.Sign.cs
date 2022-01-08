// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Cbor;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public partial class CoseSign1MessageTests
    {
        [Theory]
        [InlineData(ECDsaAlgorithm.ES256, CoseTestHelpers.SHA256)]
        [InlineData(ECDsaAlgorithm.ES384, CoseTestHelpers.SHA384)]
        [InlineData(ECDsaAlgorithm.ES512, CoseTestHelpers.SHA512)]
        public void SignVerifyECDsa(ECDsaAlgorithm algorithm, string hashAlgorithm)
        {
            ECDsa ecdsaKey = ECDsaKeys[algorithm];
            byte[] coseMessageBytes = CoseSign1Message.Sign(s_SampleContent, ecdsaKey, new HashAlgorithmName(hashAlgorithm));
            CoseSign1Message msg = CoseMessage.DecodeSign1(coseMessageBytes);
            Assert.True(msg.Verify(ecdsaKey));
        }

        [Theory]
        [InlineData(CoseTestHelpers.SHA256)]
        [InlineData(CoseTestHelpers.SHA384)]
        [InlineData(CoseTestHelpers.SHA512)]
        public void SignVerifyRSA(string hashAlgorithm)
        {
            byte[] coseMessageBytes = CoseSign1Message.Sign(s_SampleContent, RSAKey, new HashAlgorithmName(hashAlgorithm));
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
            byte[] message = CoseSign1Message.Sign(s_SampleContent, protectedHeaders, unprotectedHeaders, key);
            Assert.True(CoseMessage.DecodeSign1(message).Verify(key));
        }

        // Content
        [Fact]
        public void SignWithNullContent()
        {
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(null!, ECDsaKeys[ECDsaAlgorithm.ES256], HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(null!, RSAKey, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(null!, GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256), GetEmptyHeaderMap(), ECDsaKeys[ECDsaAlgorithm.ES256]));
        }

        [Theory]
        [InlineData(ContentTestCase.Empty)]
        [InlineData(ContentTestCase.Small)]
        [InlineData(ContentTestCase.Large)]
        public void SignWithValidContent(ContentTestCase @case)
        {
            byte[] content = GetDummyContent(@case);

            Assert.NotNull(CoseSign1Message.Sign(content, ECDsaKeys[ECDsaAlgorithm.ES256], HashAlgorithmName.SHA256));
            Assert.NotNull(CoseSign1Message.Sign(content, RSAKey, HashAlgorithmName.SHA256));
            Assert.NotNull(CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256), GetEmptyHeaderMap(), ECDsaKeys[ECDsaAlgorithm.ES256]));
        }

        // Key
        [Fact]
        public void SignWithNullKey()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(content, (ECDsa)null!, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(content, (RSA)null!, HashAlgorithmName.SHA256));
            Assert.Throws<ArgumentNullException>(() => CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256), GetEmptyHeaderMap(), null!));
        }

        [Theory]
        [InlineData((int)ECDsaAlgorithm.ES256, CoseTestHelpers.SHA256)]
        [InlineData((int)ECDsaAlgorithm.ES384, CoseTestHelpers.SHA384)]
        [InlineData((int)ECDsaAlgorithm.ES512, CoseTestHelpers.SHA512)]
        [InlineData((int)RSAAlgorithm.PS256, CoseTestHelpers.SHA256)]
        [InlineData((int)RSAAlgorithm.PS384, CoseTestHelpers.SHA384)]
        [InlineData((int)RSAAlgorithm.PS512, CoseTestHelpers.SHA512)]
        public void SignWithNonPrivateKey(int algorithm, string hashAlgorithm)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            AsymmetricAlgorithm key;

            if (Enum.IsDefined(typeof(ECDsaAlgorithm), algorithm))
            {
                ECDsa ecDsaKey = ECDsaKeysWithoutPrivateKey[(ECDsaAlgorithm)algorithm];
                Assert.ThrowsAny<CryptographicException>(() => CoseSign1Message.Sign(content, ecDsaKey, new HashAlgorithmName(hashAlgorithm)));
                key = ecDsaKey;
            }
            else
            {
                Assert.ThrowsAny<CryptographicException>(() => CoseSign1Message.Sign(content, RSAKeyWithoutPrivateKey, new HashAlgorithmName(hashAlgorithm)));
                key = RSAKeyWithoutPrivateKey;
            }

            Assert.ThrowsAny<CryptographicException>(() => CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm(algorithm), GetEmptyHeaderMap(), key));
        }

        // SignWithUnsupportedKey (ECDA?)
        [Fact]
        public void SignWithUnsupportedKey()
        {
            AsymmetricAlgorithm key = DSA.Create();
            // CoseHeader will say that a supported combination of key-algorithm will be used.
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256), GetEmptyHeaderMap(), key));
        }

        // Hash algorithms
        [Theory]
        [InlineData("SHA1")]
        [InlineData("FOO")]
        public void SignWithUnsupportedHashAlgorithm(string hashAlgorithm)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);

            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), ECDsaKeys[ECDsaAlgorithm.ES256], new HashAlgorithmName(hashAlgorithm)));
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), RSAKey, new HashAlgorithmName(hashAlgorithm)));
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), GetHeaderMapWithAlgorithm(-47 /*ES256K*/), GetEmptyHeaderMap(), ECDsaKeys[ECDsaAlgorithm.ES256]));
        }

        [Theory]
        [InlineData((int)ECDsaAlgorithm.ES256, CoseTestHelpers.SHA256)]
        [InlineData((int)ECDsaAlgorithm.ES384, CoseTestHelpers.SHA384)]
        [InlineData((int)ECDsaAlgorithm.ES512, CoseTestHelpers.SHA512)]
        [InlineData((int)RSAAlgorithm.PS256, CoseTestHelpers.SHA256)]
        [InlineData((int)RSAAlgorithm.PS384, CoseTestHelpers.SHA384)]
        [InlineData((int)RSAAlgorithm.PS512, CoseTestHelpers.SHA512)]
        public void SignWithSupportedHashAlgorithm(int algorithm, string hashAlgorithm)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            AsymmetricAlgorithm key;

            if (Enum.IsDefined(typeof(ECDsaAlgorithm), algorithm))
            {
                ECDsa ecDsaKey = ECDsaKeys[(ECDsaAlgorithm)algorithm];
                Assert.NotNull(CoseSign1Message.Sign(content, ecDsaKey, new HashAlgorithmName(hashAlgorithm)));
                key = ecDsaKey;
            }
            else
            {
                Assert.NotNull(CoseSign1Message.Sign(content, RSAKey, new HashAlgorithmName(hashAlgorithm)));
                key = RSAKey;
            }

            Assert.NotNull(() => CoseSign1Message.Sign(content, GetHeaderMapWithAlgorithm(algorithm), GetEmptyHeaderMap(), key));
        }

        // isDetached
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithIsDetached(bool isDetached)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            ECDsa key = ECDsaKeys[ECDsaAlgorithm.ES256];
            HashAlgorithmName algorithmName = new HashAlgorithmName(CoseTestHelpers.SHA256);

            byte[] messageEncoded = CoseSign1Message.Sign(content, key, algorithmName, isDetached);
            VerifyContentDetached(messageEncoded, content, isDetached);

            messageEncoded = CoseSign1Message.Sign(content, key, algorithmName, isDetached);
            VerifyContentDetached(messageEncoded, content, isDetached);

            static void VerifyContentDetached(byte[] messageEncoded, byte[] expecteContent, bool isDetached)
            {
                CoseMessage messageDecoded = CoseMessage.DecodeSign1(messageEncoded);
                if (isDetached)
                {
                    Assert.Null(messageDecoded.Content);
                }
                else
                {
                    Assert.True(new ReadOnlySpan<byte>(expecteContent).SequenceEqual(messageDecoded.Content.GetValueOrDefault().Span));
                }
            }
        }

        [Fact]
        public void SignWithEmptyHeaderMaps()
        {
            // algorithm header is required in either protected or unprotected header.
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), GetEmptyHeaderMap(), GetEmptyHeaderMap(), ECDsaKeys[ECDsaAlgorithm.ES256]));
        }

        [Fact]
        public void SignWithEmptyProtectedHeaderMap()
        {
            CoseHeaderMap @protected = GetEmptyHeaderMap();
            CoseHeaderMap unprotected = GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256);
            Assert.NotNull(CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), @protected, unprotected, ECDsaKeys[ECDsaAlgorithm.ES256]));
        }

        [Fact]
        public void SignWithEmptyUnprotectedHeaderMap()
        {
            CoseHeaderMap @protected = GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256); 
            CoseHeaderMap unprotected = GetEmptyHeaderMap();
            Assert.NotNull(CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), @protected, unprotected, ECDsaKeys[ECDsaAlgorithm.ES256]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SignWithCustomHeader(bool useProtectedMap)
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            AsymmetricAlgorithm key = ECDsaKeys[ECDsaAlgorithm.ES256];
            CoseHeaderMap @protected, unprotected, mapForCustomHeader;
            InitializeHeaders();

            mapForCustomHeader.SetValue(label: new CoseHeaderLabel(42), value: 42);
            Assert.NotNull(CoseSign1Message.Sign(content, @protected, unprotected, key));

            InitializeHeaders();
            mapForCustomHeader.SetValue(label: new CoseHeaderLabel("42"), value: 42);
            Assert.NotNull(CoseSign1Message.Sign(content, @protected, unprotected, key));

            void InitializeHeaders()
            {
                @protected = GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256);
                unprotected = GetEmptyHeaderMap();
                mapForCustomHeader = useProtectedMap ? @protected : unprotected;
            }
        }

        [Fact]
        public void SignWithDuplicateHeaderBetweenProtectedAndUnprotected()
        {
            byte[] content = GetDummyContent(ContentTestCase.Small);
            AsymmetricAlgorithm key = ECDsaKeys[ECDsaAlgorithm.ES256];
            CoseHeaderMap @protected, unprotected;

            // Algorithm header is duplicated. It is a special case because it is mandatory that the header exists in the protected map.
            InitializeHeaders();
            // @protected.SetValue(CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256); // We don't need to Set Algorithm header as InitHeader does it for us.
            unprotected.SetValue(CoseHeaderLabel.Algorithm, (int)ECDsaAlgorithm.ES256);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, @protected, unprotected, key));

            // other known header is duplicate.
            InitializeHeaders();
            @protected.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            unprotected.SetValue(CoseHeaderLabel.ContentType, ContentTypeDummyValue);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, @protected, unprotected, key));

            // not-known int header is duplicate.
            InitializeHeaders();
            var myLabel = new CoseHeaderLabel(42);
            @protected.SetValue(myLabel, 42);
            unprotected.SetValue(myLabel, 42);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, @protected, unprotected, key));

            // not-known tstr header is duplicate.
            InitializeHeaders();
            myLabel = new CoseHeaderLabel("42");
            @protected.SetValue(myLabel, 42);
            unprotected.SetValue(myLabel, 42);
            Assert.Throws<CryptographicException>(() => CoseSign1Message.Sign(content, @protected, unprotected, key));

            void InitializeHeaders()
            {
                @protected = GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256);
                unprotected = GetEmptyHeaderMap();
            }
        }

        [Theory]
        [MemberData(nameof(AllCborTypes_TestData))]
        public void SignWithAllCborTypesAsHeaderValue(bool useProtectedMap, ReadOnlyMemory<byte> encodedValue)
        {
            //Diagnostics.Debugger.Launch();
            CoseHeaderMap @protected = GetHeaderMapWithAlgorithm((int)ECDsaAlgorithm.ES256);
            CoseHeaderMap unprotected = GetEmptyHeaderMap();
            var myLabel = new CoseHeaderLabel(42);
            (useProtectedMap ? @protected : unprotected).SetEncodedValue(myLabel, encodedValue);

            byte[] encodedMessage = CoseSign1Message.Sign(GetDummyContent(ContentTestCase.Small), @protected, unprotected, ECDsaKeys[ECDsaAlgorithm.ES256]);

            // Verify it is transported correctly.
            CoseSign1Message message = CoseMessage.DecodeSign1(encodedMessage);
            ReadOnlyMemory<byte> roundtrippedValue = (useProtectedMap ? message.ProtectedHeader : message.UnprotectedHeader).GetEncodedValue(myLabel);
            Assert.True(encodedValue.Span.SequenceEqual(roundtrippedValue.Span));
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

                w.WriteByteString(s_SampleContent);
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
                w.WriteByteString(s_SampleContent);
                w.WriteEndIndefiniteLengthByteString();
                yield return ReturnDataAndReset(useProtectedMap, w);
            }

            static object[] ReturnDataAndReset(bool useProtectedMap, CborWriter w)
            {
                ReadOnlyMemory<byte> encodedValue = w.Encode();
                w.Reset();
                return new object[] { useProtectedMap, encodedValue };
            }
        }
    }
}
