// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaContractTests
    {
        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void NullArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42);

            AssertExtensions.Throws<ArgumentNullException>("data", () => slhDsa.SignData(null));
            AssertExtensions.Throws<ArgumentNullException>("data", () => slhDsa.VerifyData(null, null));

            AssertExtensions.Throws<ArgumentNullException>("hash", () => slhDsa.SignPreHash(null, "", Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("hash", () => slhDsa.VerifyPreHash(null, Array.Empty<byte>(), "", Array.Empty<byte>()));

            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithmOid", () => slhDsa.SignPreHash(ReadOnlySpan<byte>.Empty, [], null));
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithmOid", () => slhDsa.VerifyPreHash(ReadOnlySpan<byte>.Empty, [], null));
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithmOid", () => slhDsa.SignPreHash(Array.Empty<byte>(), null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithmOid", () => slhDsa.VerifyPreHash(Array.Empty<byte>(), Array.Empty<byte>(), null, Array.Empty<byte>()));

            AssertExtensions.Throws<ArgumentNullException>("signature", () => slhDsa.VerifyData(Array.Empty<byte>(), null));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => slhDsa.VerifyPreHash(Array.Empty<byte>(), null, "", Array.Empty<byte>()));

            AssertExtensions.Throws<ArgumentNullException>("password", () => slhDsa.ExportEncryptedPkcs8PrivateKey((string)null, pbeParameters));
            AssertExtensions.Throws<ArgumentNullException>("password", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem((string)null, pbeParameters));
            AssertExtensions.Throws<ArgumentNullException>("password", () => slhDsa.TryExportEncryptedPkcs8PrivateKey((string)null, pbeParameters, Span<byte>.Empty, out _));

            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(string.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(string.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(string.Empty, null, Span<byte>.Empty, out _));
        }

        [Fact]
        public static void ArgumentValidation_Ctor_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => new SlhDsaMockImplementation(null));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int signatureSize = algorithm.SignatureSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaPublicKey(new byte[publicKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaPublicKey(new byte[publicKeySize + 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaSecretKey(new byte[secretKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaSecretKey(new byte[secretKeySize + 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize - 1], ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize + 1], ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.SignPreHash(ReadOnlySpan<byte>.Empty, new byte[signatureSize - 1], "", ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.SignPreHash(ReadOnlySpan<byte>.Empty, new byte[signatureSize + 1], "", ReadOnlySpan<byte>.Empty));

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.SignData(Array.Empty<byte>(), new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[signatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.VerifyData(Array.Empty<byte>(), new byte[signatureSize], new byte[256]));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.SignPreHash(ReadOnlySpan<byte>.Empty, new byte[signatureSize], "", new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.SignPreHash(Array.Empty<byte>(), "", new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.VerifyPreHash(ReadOnlySpan<byte>.Empty, new byte[signatureSize], "", new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.VerifyPreHash(Array.Empty<byte>(), new byte[signatureSize], "", new byte[256]));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation_PbeParameters(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                // Unknown algorithm
                AssertExtensions.Throws<CryptographicException>(() =>
                    export(slhDsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.Unknown, HashAlgorithmName.SHA1, 42)));

                // TripleDes3KeyPkcs12 only works with SHA1
                AssertExtensions.Throws<CryptographicException>(() =>
                    export(slhDsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA512, 42)));
            });

            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                // Bytes not allowed in TripleDes3KeyPkcs12
                AssertExtensions.Throws<CryptographicException>(() =>
                    export(slhDsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42)));
            }, SlhDsaTestHelpers.EncryptionPasswordType.Byte);
        }

        [Theory]
        [InlineData(true, SlhDsaTestHelpers.Md5Oid, 128 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha1Oid, 160 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha256Oid, 256 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha384Oid, 384 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha512Oid, 512 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha3_256Oid, 256 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha3_384Oid, 384 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Sha3_512Oid, 512 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Shake128Oid, 256 / 8)]
        [InlineData(true, SlhDsaTestHelpers.Shake256Oid, 512 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Md5Oid, 128 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha1Oid, 160 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha256Oid, 256 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha384Oid, 384 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha512Oid, 512 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha3_256Oid, 256 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha3_384Oid, 384 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Sha3_512Oid, 512 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Shake128Oid, 256 / 8)]
        [InlineData(false, SlhDsaTestHelpers.Shake256Oid, 512 / 8)]
        public static void ArgumentValidation_Hash_WrongSize(bool shouldDispose, string oid, int hashLength)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128f);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            byte[] signature = new byte[SlhDsaAlgorithm.SlhDsaSha2_128f.SignatureSizeInBytes];

            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash(new byte[hashLength - 1], oid));
            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash(new byte[hashLength + 1], oid));
            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash(new byte[hashLength - 1], signature, oid));
            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash(new byte[hashLength + 1], signature, oid));

            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash(new byte[hashLength - 1], signature, oid));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash(new byte[hashLength + 1], signature, oid));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash(new byte[hashLength - 1], signature.AsSpan(), oid));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash(new byte[hashLength + 1], signature.AsSpan(), oid));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ArgumentValidation_HashAlgorithm_BadOid(bool shouldDispose)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128f);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            byte[] signature = new byte[SlhDsaAlgorithm.SlhDsaSha2_128f.SignatureSizeInBytes];

            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash([], "a"));
            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash([], signature, "a"));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash(Array.Empty<byte>(), signature, "a"));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash([], signature.AsSpan(), "a"));

            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash([1], "a"));
            Assert.Throws<CryptographicException>(() => slhDsa.SignPreHash([1], signature, "a"));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash(new byte[] { 1 }, signature, "a"));
            Assert.Throws<CryptographicException>(() => slhDsa.VerifyPreHash([1], signature.AsSpan(), "a"));
        }

        [Fact]
        public static void ArgumentValidation_HashAlgorithm_UnknownOidCallsCore()
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128f);

            byte[] signature = new byte[SlhDsaAlgorithm.SlhDsaSha2_128f.SignatureSizeInBytes];
            string hashAlgorithmOid = "1.0";

            slhDsa.SignPreHashCoreHook = (_, _, _, _) => { };
            slhDsa.AddSignatureBufferIsSameAssertion(signature.AsMemory());
            slhDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());

            _ = slhDsa.SignPreHash([], hashAlgorithmOid);
            Assert.Equal(1, slhDsa.SignPreHashCoreCallCount);

            slhDsa.SignPreHash([], signature, hashAlgorithmOid);
            Assert.Equal(2, slhDsa.SignPreHashCoreCallCount);

            _ = slhDsa.SignPreHash([1], hashAlgorithmOid);
            Assert.Equal(3, slhDsa.SignPreHashCoreCallCount);

            slhDsa.SignPreHash([1], signature, hashAlgorithmOid);
            Assert.Equal(4, slhDsa.SignPreHashCoreCallCount);

            slhDsa.SignPreHashCoreHook = (_, _, _, _) => Assert.Fail();
            slhDsa.VerifyPreHashCoreHook = (_, _, _, _) => true;
            slhDsa.AddSignatureBufferIsSameAssertion(signature.AsMemory());
            slhDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());

            _ = slhDsa.VerifyPreHash(Array.Empty<byte>(), signature, hashAlgorithmOid);
            Assert.Equal(1, slhDsa.VerifyPreHashCoreCallCount);

            _ = slhDsa.VerifyPreHash([], signature.AsSpan(), hashAlgorithmOid);
            Assert.Equal(2, slhDsa.VerifyPreHashCoreCallCount);

            _ = slhDsa.VerifyPreHash(new byte[] { 1 }, signature, hashAlgorithmOid);
            Assert.Equal(3, slhDsa.VerifyPreHashCoreCallCount);

            _ = slhDsa.VerifyPreHash([1], signature.AsSpan(), hashAlgorithmOid);
            Assert.Equal(4, slhDsa.VerifyPreHashCoreCallCount);
        }

        public static IEnumerable<object[]> ApiWithDestinationSpanTestData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from destinationLargerThanRequired in new[] { true, false }
            select new object[] { algorithm, destinationLargerThanRequired };

        private const int PaddingSize = 10;

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportSlhDsaPublicKey_CallsCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);
            slhDsa.ExportSlhDsaPublicKeyCoreHook = _ => { };
            slhDsa.AddFillDestination(1);

            int publicKeySize = algorithm.PublicKeySizeInBytes;

            // Array overload
            byte[] exported = slhDsa.ExportSlhDsaPublicKey();
            Assert.Equal(1, slhDsa.ExportSlhDsaPublicKeyCoreCallCount);
            Assert.Equal(publicKeySize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] publicKey = CreatePaddedFilledArray(publicKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = publicKey.AsMemory(PaddingSize, publicKeySize);
            slhDsa.AddDestinationBufferIsSameAssertion(destination);

            slhDsa.ExportSlhDsaPublicKey(destination.Span);
            Assert.Equal(2, slhDsa.ExportSlhDsaPublicKeyCoreCallCount);
            AssertExpectedFill(publicKey, fillElement: 1, paddingElement: 42, PaddingSize, publicKeySize);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportSlhDsaSecretKey_CallsCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);
            slhDsa.ExportSlhDsaSecretKeyCoreHook = _ => { };
            slhDsa.AddFillDestination(1);

            int secretKeySize = algorithm.SecretKeySizeInBytes;

            // Array overload
            byte[] exported = slhDsa.ExportSlhDsaSecretKey();
            Assert.Equal(1, slhDsa.ExportSlhDsaSecretKeyCoreCallCount);
            Assert.Equal(secretKeySize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] secretKey = CreatePaddedFilledArray(secretKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = secretKey.AsMemory(PaddingSize, secretKeySize);
            slhDsa.AddDestinationBufferIsSameAssertion(destination);

            slhDsa.ExportSlhDsaSecretKey(destination.Span);
            Assert.Equal(2, slhDsa.ExportSlhDsaSecretKeyCoreCallCount);
            AssertExpectedFill(secretKey, fillElement: 1, paddingElement: 42, PaddingSize, secretKeySize);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void SignData_CallsCore(SlhDsaAlgorithm algorithm)
        {
            byte[] testData = [2];
            byte[] testContext = [3];

            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);
            slhDsa.SignDataCoreHook = (_, _, _) => { };
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddFillDestination(1);

            int signatureSize = algorithm.SignatureSizeInBytes;

            // Array overload
            byte[] exported = slhDsa.SignData(testData, testContext);
            Assert.Equal(1, slhDsa.SignDataCoreCallCount);
            Assert.Equal(signatureSize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] signature = CreatePaddedFilledArray(signatureSize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, signatureSize);
            slhDsa.AddDestinationBufferIsSameAssertion(destination);

            slhDsa.SignData(testData, destination.Span, testContext);
            Assert.Equal(2, slhDsa.SignDataCoreCallCount);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void VerifyData_CallsCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = CreatePaddedFilledArray(signatureSize, 42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyDataCoreHook = (_, _, _) => returnValue;
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddSignatureBufferIsSameAssertion(testSignature.AsMemory(PaddingSize, signatureSize));

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize - 1), testContext));
            Assert.Equal(0, slhDsa.VerifyDataCoreCallCount);

            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize + 1), testContext));
            Assert.Equal(0, slhDsa.VerifyDataCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize), testContext));
            Assert.Equal(1, slhDsa.VerifyDataCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize), testContext));
            Assert.Equal(2, slhDsa.VerifyDataCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void VerifyData_ByteArray_CallsCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = CreateFilledArray(signatureSize, 42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyDataCoreHook = (_, _, _) => returnValue;
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddSignatureBufferIsSameAssertion(testSignature);

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, new byte[signatureSize - 1], testContext));
            Assert.Equal(0, slhDsa.VerifyDataCoreCallCount);

            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, new byte[signatureSize - 1], testContext));
            Assert.Equal(0, slhDsa.VerifyDataCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(slhDsa.VerifyData(testData, testSignature, testContext));
            Assert.Equal(1, slhDsa.VerifyDataCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature, testContext));
            Assert.Equal(2, slhDsa.VerifyDataCoreCallCount);
        }

        [Theory]
        [InlineData(SlhDsaTestHelpers.Md5Oid, 128 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha1Oid, 160 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha256Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha384Oid, 384 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha512Oid, 512 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_256Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_384Oid, 384 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_512Oid, 512 / 8)]
        [InlineData(SlhDsaTestHelpers.Shake128Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Shake256Oid, 512 / 8)]
        [InlineData("1.0", 0)]
        [InlineData("1.0", 1)]
        [InlineData("1.0", 2)]
        public static void SignPreHash_CallsCore(string hashAlgorithmOid, int hashLength)
        {
            SlhDsaAlgorithm algorithm = SlhDsaAlgorithm.SlhDsaSha2_128f;

            byte[] testData = new byte[hashLength];
            byte[] testContext = [3];

            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);
            slhDsa.SignPreHashCoreHook = (_, _, _, _) => { };

            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());
            slhDsa.AddFillDestination(1);

            int signatureSize = algorithm.SignatureSizeInBytes;

            // Array overload
            byte[] exported = slhDsa.SignPreHash(testData, hashAlgorithmOid, testContext);
            Assert.Equal(1, slhDsa.SignPreHashCoreCallCount);
            Assert.Equal(signatureSize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] signature = CreatePaddedFilledArray(signatureSize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, signatureSize);
            slhDsa.AddDestinationBufferIsSameAssertion(destination);

            slhDsa.SignPreHash(testData, destination.Span, hashAlgorithmOid, testContext);
            Assert.Equal(2, slhDsa.SignPreHashCoreCallCount);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        [Theory]
        [InlineData(SlhDsaTestHelpers.Md5Oid, 128 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha1Oid, 160 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha256Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha384Oid, 384 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha512Oid, 512 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_256Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_384Oid, 384 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_512Oid, 512 / 8)]
        [InlineData(SlhDsaTestHelpers.Shake128Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Shake256Oid, 512 / 8)]
        [InlineData("1.0", 0)]
        [InlineData("1.0", 1)]
        [InlineData("1.0", 2)]
        public static void VerifyPreHash_CallsCore(string hashAlgorithmOid, int hashLength)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128f);

            int signatureSize = SlhDsaAlgorithm.SlhDsaSha2_128f.SignatureSizeInBytes;
            byte[] testSignature = CreatePaddedFilledArray(signatureSize, 42);
            byte[] testData = new byte[hashLength];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyPreHashCoreHook = (_, _, _, _) => returnValue;
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddSignatureBufferIsSameAssertion(testSignature.AsMemory(PaddingSize, signatureSize));
            slhDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize - 1), hashAlgorithmOid, testContext));
            Assert.Equal(0, slhDsa.VerifyPreHashCoreCallCount);

            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize + 1), hashAlgorithmOid, testContext));
            Assert.Equal(0, slhDsa.VerifyPreHashCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize), hashAlgorithmOid, testContext));
            Assert.Equal(1, slhDsa.VerifyPreHashCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize), hashAlgorithmOid, testContext));
            Assert.Equal(2, slhDsa.VerifyPreHashCoreCallCount);
        }

        [Theory]
        [InlineData(SlhDsaTestHelpers.Md5Oid, 128 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha1Oid, 160 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha256Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha384Oid, 384 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha512Oid, 512 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_256Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_384Oid, 384 / 8)]
        [InlineData(SlhDsaTestHelpers.Sha3_512Oid, 512 / 8)]
        [InlineData(SlhDsaTestHelpers.Shake128Oid, 256 / 8)]
        [InlineData(SlhDsaTestHelpers.Shake256Oid, 512 / 8)]
        [InlineData("1.0", 0)]
        [InlineData("1.0", 1)]
        [InlineData("1.0", 2)]
        public static void VerifyPreHash_ByteArray_CallsCore(string hashAlgorithmOid, int hashLength)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128f);

            int signatureSize = SlhDsaAlgorithm.SlhDsaSha2_128f.SignatureSizeInBytes;
            byte[] testSignature = CreateFilledArray(signatureSize, 42);
            byte[] testData = new byte[hashLength];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyPreHashCoreHook = (_, _, _, _) => returnValue;
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddSignatureBufferIsSameAssertion(testSignature);
            slhDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(testData, new byte[signatureSize - 1], hashAlgorithmOid, testContext));
            Assert.Equal(0, slhDsa.VerifyPreHashCoreCallCount);

            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(testData, new byte[signatureSize - 1], hashAlgorithmOid, testContext));
            Assert.Equal(0, slhDsa.VerifyPreHashCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(testData, testSignature, hashAlgorithmOid, testContext));
            Assert.Equal(1, slhDsa.VerifyPreHashCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(testData, testSignature, hashAlgorithmOid, testContext));
            Assert.Equal(2, slhDsa.VerifyPreHashCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void Dispose_CallsVirtual(SlhDsaAlgorithm algorithm)
        {
            SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);
            bool disposeCalled = false;

            // First Dispose call should invoke overridden Dispose should be called
            slhDsa.DisposeHook = (bool disposing) =>
            {
                AssertExtensions.TrueExpression(disposing);
                disposeCalled = true;
            };

            slhDsa.Dispose();
            AssertExtensions.TrueExpression(disposeCalled);

            // Subsequent Dispose calls should be a no-op
            slhDsa.DisposeHook = _ => Assert.Fail();

            slhDsa.Dispose();
            slhDsa.Dispose(); // no throw

            SlhDsaTestHelpers.VerifyDisposed(slhDsa);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_CallsExportSlhDsaSecretKey(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
            {
                using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

                slhDsa.ExportSlhDsaSecretKeyCoreHook = _ => { };
                slhDsa.AddLengthAssertion();
                slhDsa.AddFillDestination(1);

                // SlhDsaMockImplementation overrides TryExportPkcs8PrivateKeyCore with a stub. In order to replicate the
                // non-overridden behavior, we will replace the stub with a call to base.TryExportPkcs8PrivateKeyCore.
                // We can then assert that base.TryExportPkcs8PrivateKeyCore calls ExportSlhDsaSecretKeyCore as expected.
                slhDsa.TryExportPkcs8PrivateKeyCoreHook = slhDsa.BaseTryExportPkcs8PrivateKeyCore;

                // Invoke the export
                byte[] exported = export(slhDsa);

                // Assert that the core methods were called
                AssertExtensions.GreaterThan(slhDsa.ExportSlhDsaSecretKeyCoreCallCount, 0);
                AssertExtensions.GreaterThan(slhDsa.TryExportPkcs8PrivateKeyCoreCallCount, 0);

                // And check the returned data
                PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);
                AssertExtensions.SequenceEqual(CreateFilledArray(algorithm.SecretKeySizeInBytes, 1), exportedPkcs8.PrivateKey.Span);
                Assert.Equal(0, exportedPkcs8.Version);
                Assert.Equal(SlhDsaTestHelpers.AlgorithmToOid(algorithm), exportedPkcs8.PrivateKeyAlgorithm.Algorithm);
                AssertExtensions.FalseExpression(exportedPkcs8.PrivateKeyAlgorithm.Parameters.HasValue);
                Assert.Null(exportedPkcs8.Attributes);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_DoesNotCallExportSlhDsaSecretKey(SlhDsaAlgorithm algorithm)
        {
            byte[] secretKeyBytes = CreateFilledArray(algorithm.SecretKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = null,
                },
                PrivateKey = secretKeyBytes,
            };
            byte[] minimalEncoding = pkcs8.Encode();

            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
            {
                using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

                // Override the TryExport method to return our test data
                slhDsa.TryExportPkcs8PrivateKeyCoreHook = (dest, out bytesWritten) =>
                {
                    if (dest.Length >= minimalEncoding.Length)
                    {
                        minimalEncoding.CopyTo(dest);
                        bytesWritten = minimalEncoding.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                };

                slhDsa.AddLengthAssertion();

                byte[] exported = export(slhDsa);

                // Assert that the PKCS#8 private key is NOT generated with the secret key but from our test callback
                Assert.Equal(0, slhDsa.ExportSlhDsaSecretKeyCoreCallCount);
                AssertExtensions.GreaterThan(slhDsa.TryExportPkcs8PrivateKeyCoreCallCount, 0);

                PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);
                AssertExtensions.SequenceEqual(pkcs8.PrivateKey.Span, exportedPkcs8.PrivateKey.Span);
                Assert.Equal(pkcs8.Version, exportedPkcs8.Version);
                Assert.Equal(pkcs8.PrivateKeyAlgorithm.Algorithm, exportedPkcs8.PrivateKeyAlgorithm.Algorithm);
                Assert.Equal(pkcs8.PrivateKeyAlgorithm.Parameters, exportedPkcs8.PrivateKeyAlgorithm.Parameters);
                Assert.Equal(pkcs8.Attributes, exportedPkcs8.Attributes); // Null
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportSubjectPublicKeyInfo_CallsExportSlhDsaPublicKey(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestHelpers.AssertExportSubjectPublicKeyInfo(export =>
            {
                using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

                slhDsa.ExportSlhDsaPublicKeyCoreHook = _ => { };
                slhDsa.AddLengthAssertion();
                slhDsa.AddFillDestination(1);

                byte[] exported = export(slhDsa);
                AssertExtensions.GreaterThan(slhDsa.ExportSlhDsaPublicKeyCoreCallCount, 0);

                SubjectPublicKeyInfoAsn exportedPkcs8 = SubjectPublicKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);
                AssertExtensions.SequenceEqual(CreateFilledArray(algorithm.PublicKeySizeInBytes, 1), exportedPkcs8.SubjectPublicKey.Span);
                Assert.Equal(SlhDsaTestHelpers.AlgorithmToOid(algorithm), exportedPkcs8.Algorithm.Algorithm);
                AssertExtensions.FalseExpression(exportedPkcs8.Algorithm.Parameters.HasValue);
            });
        }

        public static IEnumerable<object[]> AlgorithmWithPbeParametersData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from pbeParameters in new[]
            {
                new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42),
                new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 1),
                new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, HashAlgorithmName.SHA384, 5),
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA512, 10),
            }
            select new object[] { algorithm, pbeParameters };

        public static bool HasSymmetricEncryption
#if NETFRAMEWORK
            => true;
#else
            => !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
#endif

        [ConditionalTheory(nameof(HasSymmetricEncryption))]
        [MemberData(nameof(AlgorithmWithPbeParametersData))]
        public static void ExportEncryptedPkcs8PrivateKey_CallsExportSlhDsaPrivateKey(SlhDsaAlgorithm algorithm, PbeParameters pbeParameters)
        {
            Action<SlhDsaTestHelpers.ExportEncryptedPkcs8PrivateKeyCallback> test = export =>
            {
                using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

                slhDsa.ExportSlhDsaSecretKeyCoreHook = _ => { };
                slhDsa.AddLengthAssertion();
                slhDsa.AddFillDestination(1);

                // SlhDsaMockImplementation overrides TryExportPkcs8PrivateKeyCore with a stub. In order to replicate the
                // non-overridden behavior, we will replace the stub with a call to base.TryExportPkcs8PrivateKeyCore.
                // We can then assert that base.TryExportPkcs8PrivateKeyCore calls ExportSlhDsaSecretKeyCore as expected.
                slhDsa.TryExportPkcs8PrivateKeyCoreHook = slhDsa.BaseTryExportPkcs8PrivateKeyCore;

                byte[] exported = export(slhDsa, "PLACEHOLDER", pbeParameters);

                AssertExtensions.GreaterThan(slhDsa.ExportSlhDsaSecretKeyCoreCallCount, 0);
                AssertExtensions.GreaterThan(slhDsa.TryExportPkcs8PrivateKeyCoreCallCount, 0);

                EncryptedPrivateKeyInfoAsn epki = EncryptedPrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.BER);
                AsnUtils.AssertEncryptedPkcs8PrivateKeyContents(epki, pbeParameters);
            };

            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(test, SlhDsaTestHelpers.GetValidPasswordTypes(pbeParameters));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void TryExportPkcs8PrivateKey_DestinationTooSmall(SlhDsaAlgorithm algorithm)
        {
            const int MinimumOverhead = 12;
            int lengthCutoff = algorithm.SecretKeySizeInBytes + MinimumOverhead;

            // First check that the length cutoff is enforced
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            byte[] secretKey = new byte[lengthCutoff];

            // Early heuristic based bailout so no core methods are called
            AssertExtensions.FalseExpression(
                slhDsa.TryExportPkcs8PrivateKey(secretKey.AsSpan(0, lengthCutoff - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            // No bailout case: set up the core method
            slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bytesWritten = destination.Length;
                return true;
            };

            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(secretKey, out bytesWritten));
            Assert.Equal(secretKey.Length, bytesWritten);

            // Now check that the length cutoff permits a minimal encoding
            // Build the minimal encoding:
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteInteger(0); // Version

                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(SlhDsaTestHelpers.AlgorithmToOid(algorithm));
                }

                writer.WriteOctetString(new byte[algorithm.SecretKeySizeInBytes]);
            }

            byte[] encodedMetadata = writer.Encode();

            // Verify that a buffer of this size meets the length cutoff
            AssertExtensions.LessThanOrEqualTo(lengthCutoff, encodedMetadata.Length);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_DestinationInitialSize(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            byte[] secretKeyBytes = CreateFilledArray(algorithm.SecretKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
                    Parameters = null,
                },
                PrivateKey = secretKeyBytes,
            };

            byte[] minimalEncoding = pkcs8.Encode();
            slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                // The first call should at least be the size of the minimal encoding
                bool ret = true;
                AssertExtensions.TrueExpression(destination.Length >= minimalEncoding.Length);
                minimalEncoding.CopyTo(destination);
                bytesWritten = minimalEncoding.Length;

                // Before we return, update the next callback so subsequent calls fail the test
                slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    Assert.Fail();
                    bytesWritten = 0;
                    return true;
                };

                return ret;
            };

            byte[] exported = slhDsa.ExportPkcs8PrivateKey();
            PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);

            Assert.Equal(1, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);
            AssertExtensions.SequenceEqual(pkcs8.PrivateKey.Span, exportedPkcs8.PrivateKey.Span);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_Resizes(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            byte[] secretKeyBytes = CreateFilledArray(algorithm.SecretKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = SlhDsaTestHelpers.AlgorithmToOid(SlhDsaAlgorithm.SlhDsaSha2_128s),
                    Parameters = null,
                },
                PrivateKey = secretKeyBytes,
            };

            byte[] minimalEncoding = pkcs8.Encode();
            int originalSize = -1;
            slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                // Return false to force a resize
                bool ret = false;
                originalSize = destination.Length;
                bytesWritten = 0;

                // Before we return false, update the callback so the next call will succeed
                slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    // New buffer must be larger than the original
                    bool ret = true;
                    AssertExtensions.GreaterThan(destination.Length, originalSize);
                    minimalEncoding.CopyTo(destination);
                    bytesWritten = minimalEncoding.Length;

                    // Before we return, update the next callback so subsequent calls fail the test
                    slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
                    {
                        Assert.Fail();
                        bytesWritten = 0;
                        return true;
                    };

                    return ret;
                };

                return ret;
            };

            byte[] exported = slhDsa.ExportPkcs8PrivateKey();
            PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);

            Assert.Equal(2, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);
            AssertExtensions.SequenceEqual(pkcs8.PrivateKey.Span, exportedPkcs8.PrivateKey.Span);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_IgnoreReturnValue(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int[] valuesToWrite = [-1, 0, int.MaxValue];
            int index = 0;

            int finalDestinationSize = -1;
            slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
            {
                // Go through all the values we want to test, and once we reach the last one,
                // return true with a valid value
                if (index >= valuesToWrite.Length)
                {
                    finalDestinationSize = bytesWritten = 1;
                    return true;
                }

                // This returned value should should be ignored. There's no way to check
                // what happens with it, but at the very least we should expect no exceptions
                // and the correct number of calls.
                bytesWritten = valuesToWrite[index];
                index++;
                return false;
            };

            int actualSize = slhDsa.ExportPkcs8PrivateKey().Length;
            Assert.Equal(finalDestinationSize, actualSize);
            Assert.Equal(valuesToWrite.Length + 1, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_HandleBadReturnValue(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            Func<int, int> getBadReturnValue = (int destinationLength) => destinationLength + 1;
            SlhDsaMockImplementation.TryExportPkcs8PrivateKeyCoreFunc hook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = true;

                bytesWritten = getBadReturnValue(destination.Length);

                // Before we return, update the next callback so subsequent calls fail the test
                slhDsa.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    Assert.Fail();
                    bytesWritten = 0;
                    return true;
                };

                return ret;
            };

            slhDsa.TryExportPkcs8PrivateKeyCoreHook = hook;
            Assert.Throws<CryptographicException>(slhDsa.ExportPkcs8PrivateKey);
            Assert.Equal(1, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);

            slhDsa.TryExportPkcs8PrivateKeyCoreHook = hook;
            getBadReturnValue = (int destinationLength) => int.MaxValue;
            Assert.Throws<CryptographicException>(slhDsa.ExportPkcs8PrivateKey);
            Assert.Equal(2, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);

            slhDsa.TryExportPkcs8PrivateKeyCoreHook = hook;
            getBadReturnValue = (int destinationLength) => -1;
            Assert.Throws<CryptographicException>(slhDsa.ExportPkcs8PrivateKey);
            Assert.Equal(3, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ExportPkcs8PrivateKey_HandleBadReturnBuffer(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(exportEncrypted =>
            {
                using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

                // Create a bad encoding
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                writer.WriteBitString("some string"u8);
                byte[] validEncoding = writer.Encode();
                Memory<byte> badEncoding = validEncoding.AsMemory(0, validEncoding.Length - 1); // Chop off the last byte

                SlhDsaMockImplementation.TryExportPkcs8PrivateKeyCoreFunc hook = (Span<byte> destination, out int bytesWritten) =>
                {
                    bool ret = badEncoding.Span.TryCopyTo(destination);
                    bytesWritten = ret ? badEncoding.Length : 0;
                    return ret;
                };

                slhDsa.TryExportPkcs8PrivateKeyCoreHook = hook;

                // Exporting the key should work without any issues because there's no validation
                AssertExtensions.SequenceEqual(badEncoding.Span, slhDsa.ExportPkcs8PrivateKey().AsSpan());

                int numberOfCalls = slhDsa.TryExportPkcs8PrivateKeyCoreCallCount;
                slhDsa.TryExportPkcs8PrivateKeyCoreCallCount = 0;

                // However, exporting the encrypted key should fail because it validates the PKCS#8 private key encoding first
                AssertExtensions.Throws<CryptographicException>(() =>
                        exportEncrypted(slhDsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1)));

                // Sanity check that the code to export the private key was called
                Assert.Equal(numberOfCalls, slhDsa.TryExportPkcs8PrivateKeyCoreCallCount);
            });
        }

        private static void AssertExpectedFill(ReadOnlySpan<byte> source, byte fillElement) =>
            AssertExpectedFill(source, fillElement, 255, 0, source.Length);

        private static void AssertExpectedFill(ReadOnlySpan<byte> source, byte fillElement, byte paddingElement, int startIndex, int length)
        {
            // Ensure that the data was filled correctly
            AssertExtensions.FilledWith(fillElement, source.Slice(startIndex, length));

            // And that the padding was not touched
            AssertExtensions.FilledWith(paddingElement, source.Slice(0, startIndex));
            AssertExtensions.FilledWith(paddingElement, source.Slice(startIndex + length));
        }

        private static byte[] CreatePaddedFilledArray(int size, byte filling)
        {
            byte[] publicKey = new byte[size + 2 * PaddingSize];
            publicKey.AsSpan().Fill(filling);
            return publicKey;
        }

        private static byte[] CreateFilledArray(int size, byte filling)
        {
            byte[] publicKey = new byte[size];
            publicKey.AsSpan().Fill(filling);
            return publicKey;
        }
    }
}
