// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using Microsoft.DotNet.RemoteExecutor;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class MLDsaTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void IsSupported_InitializesCrypto()
        {
            string arg = MLDsa.IsSupported ? "1" : "0";

            // This ensures that ML-DSA is the first cryptographic algorithm touched in the process, which kicks off
            // the initialization of the crypto layer on some platforms. Running in a remote executor ensures no other
            // test has pre-initialized anything.
            RemoteExecutor.Invoke(static (string isSupportedStr) =>
            {
                bool isSupported = isSupportedStr == "1";
                return MLDsa.IsSupported == isSupported ? RemoteExecutor.SuccessExitCode : 0;
            }, arg).Dispose();
        }

        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(PlatformSupportsMLDsa(), MLDsa.IsSupported);
        }

        private static bool PlatformSupportsMLDsa() =>
            PlatformDetection.IsOpenSsl3_5 || PlatformDetection.IsWindows10Version27858OrGreater;

        [Fact]
        public static void DisposeIsCalledOnImplementation()
        {
            MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa44);
            int numberOfTimesDisposeCalled = 0;
            mldsa.DisposeHook = (disposing) =>
            {
                numberOfTimesDisposeCalled++;
            };

            Assert.Equal(0, numberOfTimesDisposeCalled);
            mldsa.Dispose();
            Assert.Equal(1, numberOfTimesDisposeCalled);
            mldsa.Dispose();
            Assert.Equal(1, numberOfTimesDisposeCalled);
        }

        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in MLDsaTestsData.AllMLDsaAlgorithms().Select(args => args.Single())
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void NullArgumentValidation(MLDsaAlgorithm algorithm, bool shouldDispose)
        {
            using MLDsa mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                mldsa.Dispose();
            }

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42);

            byte[] hash = [1, 2, 3, 4];
            byte[] signature = [5, 6, 7, 8];
            AssertExtensions.Throws<ArgumentNullException>("data", () => mldsa.SignData(null));
            AssertExtensions.Throws<ArgumentNullException>("data", () => mldsa.VerifyData(null, null));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => mldsa.VerifyData(Array.Empty<byte>(), null));

            AssertExtensions.Throws<ArgumentNullException>("hash", () => mldsa.SignPreHash(null, null));
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithmOid", () => mldsa.SignPreHash([1, 2, 3, 4], null));
            AssertExtensions.Throws<ArgumentNullException>("hash", () => mldsa.VerifyPreHash(null, null, null));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => mldsa.VerifyPreHash(hash, null, null));
            AssertExtensions.Throws<ArgumentNullException>("hashAlgorithmOid", () => mldsa.VerifyPreHash(hash, signature, null));

            AssertExtensions.Throws<ArgumentNullException>("externalMu", () => mldsa.SignMu(null));
            AssertExtensions.Throws<ArgumentNullException>("externalMu", () => mldsa.VerifyMu(null, null));
            AssertExtensions.Throws<ArgumentNullException>("signature", () => mldsa.VerifyMu(Array.Empty<byte>(), null));

            AssertExtensions.Throws<ArgumentNullException>("password", () => mldsa.ExportEncryptedPkcs8PrivateKey((string)null, pbeParameters));
            AssertExtensions.Throws<ArgumentNullException>("password", () => mldsa.ExportEncryptedPkcs8PrivateKeyPem((string)null, pbeParameters));
            AssertExtensions.Throws<ArgumentNullException>("password", () => mldsa.TryExportEncryptedPkcs8PrivateKey((string)null, pbeParameters, Span<byte>.Empty, out _));

            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.ExportEncryptedPkcs8PrivateKey(string.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.ExportEncryptedPkcs8PrivateKeyPem(string.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => mldsa.TryExportEncryptedPkcs8PrivateKey(string.Empty, null, Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation(MLDsaAlgorithm algorithm, bool shouldDispose)
        {
            using MLDsa mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int privateKeySize = algorithm.PrivateKeySizeInBytes;
            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;
            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] signature = new byte[signatureSize];
            byte[] hash = new byte[HashInfo.Sha256.OutputSize];
            byte[] mu = new byte[64];
            byte[] shortMu = new byte[mu.Length - 1];
            byte[] longMu = new byte[mu.Length + 1];
            byte[] shortSignature = new byte[signatureSize - 1];
            byte[] longSignature = new byte[signatureSize + 1];

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                mldsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPublicKey(new byte[publicKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPublicKey(new byte[publicKeySize + 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPrivateKey(new byte[privateKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPrivateKey(new byte[privateKeySize + 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPrivateSeed(new byte[privateSeedSize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPrivateSeed(new byte[privateSeedSize + 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignData(ReadOnlySpan<byte>.Empty, shortSignature, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignData(ReadOnlySpan<byte>.Empty, longSignature, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignMu(mu, shortSignature));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignMu(mu, longSignature));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignPreHash(new byte[HashInfo.Sha512.OutputSize], shortSignature, HashInfo.Sha512.Oid, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignPreHash(new byte[HashInfo.Sha512.OutputSize], longSignature, HashInfo.Sha512.Oid, ReadOnlySpan<byte>.Empty));

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.SignData(ReadOnlySpan<byte>.Empty, signature, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.SignData(Array.Empty<byte>(), signature, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.VerifyData(Array.Empty<byte>(), signature, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.SignPreHash(hash.AsSpan(), signature, HashInfo.Sha256.Oid, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.SignPreHash(hash, HashInfo.Sha256.Oid, new byte[256]));

            // Mu must be the correct size
            AssertExtensions.Throws<ArgumentException>("externalMu", () => mldsa.SignMu(shortMu));
            AssertExtensions.Throws<ArgumentException>("externalMu", () => mldsa.SignMu(longMu));
            AssertExtensions.Throws<ArgumentException>("externalMu", () => mldsa.SignMu(shortMu, signature));
            AssertExtensions.Throws<ArgumentException>("externalMu", () => mldsa.SignMu(longMu, signature));

            // Hash length of known OID hash algorithms must be correct
            AssertExtensions.Throws<CryptographicException>(() => mldsa.SignPreHash(new byte[HashInfo.Sha512.OutputSize - 1], new byte[signatureSize], HashInfo.Sha512.Oid, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<CryptographicException>(() => mldsa.SignPreHash(new byte[HashInfo.Sha512.OutputSize + 1], new byte[signatureSize], HashInfo.Sha512.Oid, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<CryptographicException>(() => mldsa.VerifyPreHash(new byte[HashInfo.Sha512.OutputSize - 1], new byte[signatureSize], HashInfo.Sha512.Oid, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<CryptographicException>(() => mldsa.VerifyPreHash(new byte[HashInfo.Sha512.OutputSize + 1], new byte[signatureSize], HashInfo.Sha512.Oid, ReadOnlySpan<byte>.Empty));

            // Must be valid OID
            Assert.Throws<CryptographicException>(() => mldsa.SignPreHash([], "not.an.oid"));
            Assert.Throws<CryptographicException>(() => mldsa.SignPreHash([], signature, "not-an-oid"));

            Assert.Throws<CryptographicException>(() => mldsa.SignPreHash([1], string.Empty));
            Assert.Throws<CryptographicException>(() => mldsa.SignPreHash([1], signature, "-1.0.0"));
        }

        public static IEnumerable<object[]> ArgumentValidation_Hash_WrongSizeInputs()
        {
            foreach (bool shouldDispose in new[] { true, false })
            {
                foreach (HashInfo hashInfo in HashInfo.AllHashInfos())
                {
                    yield return new object[] { shouldDispose, hashInfo };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentValidation_Hash_WrongSizeInputs))]
        public static void ArgumentValidation_Hash_WrongSize(bool shouldDispose, HashInfo hashInfo)
        {
            using MLDsa mlDsa = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa44);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                mlDsa.Dispose();
            }

            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];

            Assert.Throws<CryptographicException>(() => mlDsa.SignPreHash(new byte[hashInfo.OutputSize - 1], hashInfo.Oid));
            Assert.Throws<CryptographicException>(() => mlDsa.SignPreHash(new byte[hashInfo.OutputSize + 1], hashInfo.Oid));
            Assert.Throws<CryptographicException>(() => mlDsa.SignPreHash(new byte[hashInfo.OutputSize - 1], signature, hashInfo.Oid));
            Assert.Throws<CryptographicException>(() => mlDsa.SignPreHash(new byte[hashInfo.OutputSize + 1], signature, hashInfo.Oid));

            Assert.Throws<CryptographicException>(() => mlDsa.VerifyPreHash(new byte[hashInfo.OutputSize - 1], signature, hashInfo.Oid));
            Assert.Throws<CryptographicException>(() => mlDsa.VerifyPreHash(new byte[hashInfo.OutputSize + 1], signature, hashInfo.Oid));
            Assert.Throws<CryptographicException>(() => mlDsa.VerifyPreHash(new byte[hashInfo.OutputSize - 1], signature.AsSpan(), hashInfo.Oid));
            Assert.Throws<CryptographicException>(() => mlDsa.VerifyPreHash(new byte[hashInfo.OutputSize + 1], signature.AsSpan(), hashInfo.Oid));
        }

        [Fact]
        public static void ArgumentValidation_HashAlgorithm_UnknownOidDoesNotCallCore()
        {
            using MLDsaTestImplementation mlDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44);

            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];
            string hashAlgorithmOid = "1.0";
            CryptographicException ce = Assert.Throws<CryptographicException>(() => mlDsa.SignPreHash([], hashAlgorithmOid));
            Assert.Contains(hashAlgorithmOid, ce.Message);
        }

        public static IEnumerable<object[]> AllHashesAndLengths()
        {
            foreach (HashInfo hashInfo in HashInfo.AllHashInfos())
            {
                yield return new object[] { hashInfo.Oid, hashInfo.OutputSize };
            }
        }

        [Theory]
        [MemberData(nameof(AllHashesAndLengths))]
        public static void SignPreHash_CallsCore(string hashAlgorithmOid, int hashLength)
        {
            MLDsaAlgorithm algorithm = MLDsaAlgorithm.MLDsa44;

            byte[] testData = new byte[hashLength];
            byte[] testContext = [3];

            using MLDsaTestImplementation mlDsa = MLDsaTestImplementation.CreateNoOp(algorithm);
            mlDsa.SignPreHashHook = (_, _, _, _) => { };

            mlDsa.AddDataBufferIsSameAssertion(testData);
            mlDsa.AddContextBufferIsSameAssertion(testContext);
            mlDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());
            mlDsa.AddFillDestination(1);

            int signatureSize = algorithm.SignatureSizeInBytes;

            // Array overload
            byte[] exported = mlDsa.SignPreHash(testData, hashAlgorithmOid, testContext);
            Assert.Equal(1, mlDsa.SignPreHashCoreCallCount);
            Assert.Equal(signatureSize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] signature = CreatePaddedFilledArray(signatureSize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, signatureSize);
            mlDsa.AddDestinationBufferIsSameAssertion(destination);

            mlDsa.SignPreHash(testData, destination.Span, hashAlgorithmOid, testContext);
            Assert.Equal(2, mlDsa.SignPreHashCoreCallCount);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        [Theory]
        [MemberData(nameof(AllHashesAndLengths))]
        public static void VerifyPreHash_CallsCore(string hashAlgorithmOid, int hashLength)
        {
            using MLDsaTestImplementation mlDsa = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa44);

            int signatureSize = MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes;
            byte[] testSignature = CreatePaddedFilledArray(signatureSize, 42);
            byte[] testData = new byte[hashLength];
            byte[] testContext = [3];
            bool returnValue = false;

            mlDsa.VerifyPreHashHook = (_, _, _, _) => returnValue;
            mlDsa.AddDataBufferIsSameAssertion(testData);
            mlDsa.AddContextBufferIsSameAssertion(testContext);
            mlDsa.AddSignatureBufferIsSameAssertion(testSignature.AsMemory(PaddingSize, signatureSize));
            mlDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(mlDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize - 1), hashAlgorithmOid, testContext));
            Assert.Equal(0, mlDsa.VerifyPreHashCoreCallCount);

            AssertExtensions.FalseExpression(mlDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize + 1), hashAlgorithmOid, testContext));
            Assert.Equal(0, mlDsa.VerifyPreHashCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(mlDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize), hashAlgorithmOid, testContext));
            Assert.Equal(1, mlDsa.VerifyPreHashCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(mlDsa.VerifyPreHash(testData, testSignature.AsSpan(PaddingSize, signatureSize), hashAlgorithmOid, testContext));
            Assert.Equal(2, mlDsa.VerifyPreHashCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(AllHashesAndLengths))]
        public static void VerifyPreHash_ByteArray_CallsCore(string hashAlgorithmOid, int hashLength)
        {
            using MLDsaTestImplementation mlDsa = MLDsaTestImplementation.CreateNoOp(MLDsaAlgorithm.MLDsa44);

            int signatureSize = MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes;
            byte[] testSignature = CreateFilledArray(signatureSize, 42);
            byte[] testData = new byte[hashLength];
            byte[] testContext = [3];
            bool returnValue = false;

            mlDsa.VerifyPreHashHook = (_, _, _, _) => returnValue;
            mlDsa.AddDataBufferIsSameAssertion(testData);
            mlDsa.AddContextBufferIsSameAssertion(testContext);
            mlDsa.AddSignatureBufferIsSameAssertion(testSignature);
            mlDsa.AddHashAlgorithmIsSameAssertion(hashAlgorithmOid.AsMemory());

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(mlDsa.VerifyPreHash(testData, new byte[signatureSize - 1], hashAlgorithmOid, testContext));
            Assert.Equal(0, mlDsa.VerifyPreHashCoreCallCount);

            AssertExtensions.FalseExpression(mlDsa.VerifyPreHash(testData, new byte[signatureSize - 1], hashAlgorithmOid, testContext));
            Assert.Equal(0, mlDsa.VerifyPreHashCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(mlDsa.VerifyPreHash(testData, testSignature, hashAlgorithmOid, testContext));
            Assert.Equal(1, mlDsa.VerifyPreHashCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(mlDsa.VerifyPreHash(testData, testSignature, hashAlgorithmOid, testContext));
            Assert.Equal(2, mlDsa.VerifyPreHashCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation_PbeParameters(MLDsaAlgorithm algorithm, bool shouldDispose)
        {
            using MLDsa mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                mldsa.Dispose();
            }

            MLDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                // Unknown algorithm
                AssertExtensions.Throws<CryptographicException>(() =>
                    export(mldsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.Unknown, HashAlgorithmName.SHA1, 42)));

                // TripleDes3KeyPkcs12 only works with SHA1
                AssertExtensions.Throws<CryptographicException>(() =>
                    export(mldsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA512, 42)));
            });

            MLDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                // Bytes not allowed in TripleDes3KeyPkcs12
                AssertExtensions.Throws<CryptographicException>(() =>
                    export(mldsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42)));
            }, MLDsaTestHelpers.EncryptionPasswordType.Byte);
        }

        public static IEnumerable<object[]> ApiWithDestinationSpanTestData =>
            from algorithm in MLDsaTestsData.AllMLDsaAlgorithms().Select(args => args.Single())
            from destinationLargerThanRequired in new[] { true, false }
            select new object[] { algorithm, destinationLargerThanRequired };

        private const int PaddingSize = 10;

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportMLDsaPublicKey_CallsCore(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            mldsa.ExportMLDsaPublicKeyHook = _ => { };
            mldsa.AddFillDestination(1);

            int publicKeySize = algorithm.PublicKeySizeInBytes;

            // Array overload
            byte[] exported = mldsa.ExportMLDsaPublicKey();
            Assert.Equal(1, mldsa.ExportMLDsaPublicKeyCoreCallCount);
            Assert.Equal(publicKeySize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] publicKey = CreatePaddedFilledArray(publicKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = publicKey.AsMemory(PaddingSize, publicKeySize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.ExportMLDsaPublicKey(destination.Span);
            Assert.Equal(2, mldsa.ExportMLDsaPublicKeyCoreCallCount);
            AssertExpectedFill(publicKey, fillElement: 1, paddingElement: 42, PaddingSize, publicKeySize);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportMLDsaPrivateKey_CallsCore(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            mldsa.ExportMLDsaPrivateKeyHook = _ => { };
            mldsa.AddFillDestination(1);

            int privateKeySize = algorithm.PrivateKeySizeInBytes;

            // Array overload
            byte[] exported = mldsa.ExportMLDsaPrivateKey();
            Assert.Equal(1, mldsa.ExportMLDsaPrivateKeyCoreCallCount);
            Assert.Equal(privateKeySize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] privateKey = CreatePaddedFilledArray(privateKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = privateKey.AsMemory(PaddingSize, privateKeySize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.ExportMLDsaPrivateKey(destination.Span);
            Assert.Equal(2, mldsa.ExportMLDsaPrivateKeyCoreCallCount);
            AssertExpectedFill(privateKey, fillElement: 1, paddingElement: 42, PaddingSize, privateKeySize);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportMLDsaPrivateSeed_CallsCore(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            mldsa.ExportMLDsaPrivateSeedHook = _ => { };
            mldsa.AddFillDestination(1);

            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;

            // Array overload
            byte[] exported = mldsa.ExportMLDsaPrivateSeed();
            Assert.Equal(1, mldsa.ExportMLDsaPrivateSeedCoreCallCount);
            Assert.Equal(privateSeedSize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] privateSeed = CreatePaddedFilledArray(privateSeedSize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = privateSeed.AsMemory(PaddingSize, privateSeedSize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.ExportMLDsaPrivateSeed(destination.Span);
            Assert.Equal(2, mldsa.ExportMLDsaPrivateSeedCoreCallCount);
            AssertExpectedFill(privateSeed, fillElement: 1, paddingElement: 42, PaddingSize, privateSeedSize);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void SignData_CallsCore(MLDsaAlgorithm algorithm)
        {
            byte[] testData = [2];
            byte[] testContext = [3];

            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            mldsa.SignDataHook = (_, _, _) => { };
            mldsa.AddDataBufferIsSameAssertion(testData);
            mldsa.AddContextBufferIsSameAssertion(testContext);
            mldsa.AddFillDestination(1);

            int signatureSize = algorithm.SignatureSizeInBytes;

            // Array overload
            byte[] exported = mldsa.SignData(testData, testContext);
            Assert.Equal(1, mldsa.SignDataCoreCallCount);
            Assert.Equal(signatureSize, exported.Length);
            AssertExpectedFill(exported, fillElement: 1);

            // Span overload
            byte[] signature = CreatePaddedFilledArray(signatureSize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, signatureSize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.SignData(testData, destination.Span, testContext);
            Assert.Equal(2, mldsa.SignDataCoreCallCount);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void VerifyData_CallsCore(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = CreatePaddedFilledArray(signatureSize, 42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            mldsa.VerifyDataHook = (_, _, _) => returnValue;
            mldsa.AddDataBufferIsSameAssertion(testData);
            mldsa.AddContextBufferIsSameAssertion(testContext);
            mldsa.AddSignatureBufferIsSameAssertion(testSignature.AsMemory(PaddingSize, signatureSize));

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(mldsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize - 1), testContext));
            Assert.Equal(0, mldsa.VerifyDataCoreCallCount);

            AssertExtensions.FalseExpression(mldsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize + 1), testContext));
            Assert.Equal(0, mldsa.VerifyDataCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(mldsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize), testContext));
            Assert.Equal(1, mldsa.VerifyDataCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(mldsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize), testContext));
            Assert.Equal(2, mldsa.VerifyDataCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void VerifyData_ByteArray_CallsCore(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = CreateFilledArray(signatureSize, 42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            mldsa.VerifyDataHook = (_, _, _) => returnValue;
            mldsa.AddDataBufferIsSameAssertion(testData);
            mldsa.AddContextBufferIsSameAssertion(testContext);
            mldsa.AddSignatureBufferIsSameAssertion(testSignature);

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(mldsa.VerifyData(testData, new byte[signatureSize - 1], testContext));
            Assert.Equal(0, mldsa.VerifyDataCoreCallCount);

            AssertExtensions.FalseExpression(mldsa.VerifyData(testData, new byte[signatureSize - 1], testContext));
            Assert.Equal(0, mldsa.VerifyDataCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(mldsa.VerifyData(testData, testSignature, testContext));
            Assert.Equal(1, mldsa.VerifyDataCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(mldsa.VerifyData(testData, testSignature, testContext));
            Assert.Equal(2, mldsa.VerifyDataCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void Dispose_CallsVirtual(MLDsaAlgorithm algorithm)
        {
            MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            bool disposeCalled = false;

            // First Dispose call should invoke overridden Dispose should be called
            mldsa.DisposeHook = (bool disposing) =>
            {
                AssertExtensions.TrueExpression(disposing);
                disposeCalled = true;
            };

            mldsa.Dispose();
            AssertExtensions.TrueExpression(disposeCalled);

            // Subsequent Dispose calls should be a no-op
            mldsa.DisposeHook = _ => Assert.Fail();

            mldsa.Dispose();
            mldsa.Dispose(); // no throw

            MLDsaTestHelpers.VerifyDisposed(mldsa);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportPkcs8PrivateKey_DoesNotCallExportMLDsaPrivateKey(MLDsaAlgorithm algorithm)
        {
            byte[] privateKeyBytes = CreateFilledArray(algorithm.PrivateKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = null,
                },
                PrivateKey = privateKeyBytes,
            };
            byte[] minimalEncoding = pkcs8.Encode();

            MLDsaTestHelpers.AssertExportPkcs8PrivateKey(export =>
            {
                using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

                // Override the TryExport method to return our test data
                mldsa.TryExportPkcs8PrivateKeyHook = (dest, out bytesWritten) =>
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

                mldsa.AddLengthAssertion();

                byte[] exported = export(mldsa);

                // Assert that the PKCS#8 private key is NOT generated with the private key but from our test callback
                Assert.Equal(0, mldsa.ExportMLDsaPrivateKeyCoreCallCount);
                AssertExtensions.GreaterThan(mldsa.TryExportPkcs8PrivateKeyCoreCallCount, 0);

                PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);
                AssertExtensions.SequenceEqual(pkcs8.PrivateKey.Span, exportedPkcs8.PrivateKey.Span);
                Assert.Equal(pkcs8.Version, exportedPkcs8.Version);
                Assert.Equal(pkcs8.PrivateKeyAlgorithm.Algorithm, exportedPkcs8.PrivateKeyAlgorithm.Algorithm);
                Assert.Equal(pkcs8.PrivateKeyAlgorithm.Parameters, exportedPkcs8.PrivateKeyAlgorithm.Parameters);
                Assert.Equal(pkcs8.Attributes, exportedPkcs8.Attributes); // Null
            });
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportSubjectPublicKeyInfo_CallsExportMLDsaPublicKey(MLDsaAlgorithm algorithm)
        {
            MLDsaTestHelpers.AssertExportSubjectPublicKeyInfo(export =>
            {
                using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

                mldsa.ExportMLDsaPublicKeyHook = _ => { };
                mldsa.AddLengthAssertion();
                mldsa.AddFillDestination(1);

                byte[] exported = export(mldsa);
                AssertExtensions.GreaterThan(mldsa.ExportMLDsaPublicKeyCoreCallCount, 0);

                SubjectPublicKeyInfoAsn exportedPkcs8 = SubjectPublicKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);
                AssertExtensions.SequenceEqual(CreateFilledArray(algorithm.PublicKeySizeInBytes, 1), exportedPkcs8.SubjectPublicKey.Span);
                Assert.Equal(MLDsaTestHelpers.AlgorithmToOid(algorithm), exportedPkcs8.Algorithm.Algorithm);
                AssertExtensions.FalseExpression(exportedPkcs8.Algorithm.Parameters.HasValue);
            });
        }

        public static IEnumerable<object[]> AlgorithmWithPbeParametersData =>
            from algorithm in MLDsaTestsData.AllMLDsaAlgorithms().Select(args => args.Single())
            from pbeParameters in new[]
            {
                new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42),
                new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 1),
                new PbeParameters(PbeEncryptionAlgorithm.Aes192Cbc, HashAlgorithmName.SHA384, 5),
                new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA512, 10),
            }
            select new object[] { algorithm, pbeParameters };

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void TryExportPkcs8PrivateKey_DestinationTooSmall(MLDsaAlgorithm algorithm)
        {
            const int MinimumOverhead = 12;
            int lengthCutoff = algorithm.PrivateSeedSizeInBytes + MinimumOverhead;

            // First check that the length cutoff is enforced
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            byte[] privateKey = new byte[lengthCutoff];

            // Early heuristic based bailout so no core methods are called
            AssertExtensions.FalseExpression(
                mldsa.TryExportPkcs8PrivateKey(privateKey.AsSpan(0, lengthCutoff - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            // No bailout case: set up the core method
            mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
            {
                bytesWritten = destination.Length;
                return true;
            };

            AssertExtensions.TrueExpression(mldsa.TryExportPkcs8PrivateKey(privateKey, out bytesWritten));
            Assert.Equal(privateKey.Length, bytesWritten);

            // Now check that the length cutoff permits a minimal encoding
            // Build the minimal encoding:
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteInteger(0); // Version

                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(MLDsaTestHelpers.AlgorithmToOid(algorithm));
                }

                using (writer.PushOctetString())
                {
                    AsnWriter privateKeyWriter = new AsnWriter(AsnEncodingRules.DER);
                    privateKeyWriter.WriteOctetString(new byte[algorithm.PrivateSeedSizeInBytes]);
                    privateKeyWriter.CopyTo(writer);
                }
            }

            byte[] encodedMetadata = writer.Encode();

            // Verify that a buffer of this size meets the length cutoff
            AssertExtensions.LessThanOrEqualTo(lengthCutoff, encodedMetadata.Length);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportPkcs8PrivateKey_DestinationInitialSize(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            byte[] privateKeyBytes = CreateFilledArray(algorithm.PrivateKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                    Parameters = null,
                },
                PrivateKey = privateKeyBytes,
            };

            byte[] minimalEncoding = pkcs8.Encode();
            mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
            {
                // The first call should at least be the size of the minimal encoding
                bool ret = true;
                AssertExtensions.TrueExpression(destination.Length >= minimalEncoding.Length);
                minimalEncoding.CopyTo(destination);
                bytesWritten = minimalEncoding.Length;

                // Before we return, update the next callback so subsequent calls fail the test
                mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    Assert.Fail();
                    bytesWritten = 0;
                    return true;
                };

                return ret;
            };

            byte[] exported = mldsa.ExportPkcs8PrivateKey();
            PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);

            Assert.Equal(1, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);
            AssertExtensions.SequenceEqual(pkcs8.PrivateKey.Span, exportedPkcs8.PrivateKey.Span);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportPkcs8PrivateKey_Resizes(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            byte[] privateKeyBytes = CreateFilledArray(algorithm.PrivateKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                    Parameters = null,
                },
                PrivateKey = privateKeyBytes,
            };

            byte[] minimalEncoding = pkcs8.Encode();
            int originalSize = -1;
            mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
            {
                // Return false to force a resize
                bool ret = false;
                originalSize = destination.Length;
                bytesWritten = 0;

                // Before we return false, update the callback so the next call will succeed
                mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    // New buffer must be larger than the original
                    bool ret = true;
                    AssertExtensions.GreaterThan(destination.Length, originalSize);
                    minimalEncoding.CopyTo(destination);
                    bytesWritten = minimalEncoding.Length;

                    // Before we return, update the next callback so subsequent calls fail the test
                    mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
                    {
                        Assert.Fail();
                        bytesWritten = 0;
                        return true;
                    };

                    return ret;
                };

                return ret;
            };

            byte[] exported = mldsa.ExportPkcs8PrivateKey();
            PrivateKeyInfoAsn exportedPkcs8 = PrivateKeyInfoAsn.Decode(exported, AsnEncodingRules.DER);

            Assert.Equal(2, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);
            AssertExtensions.SequenceEqual(pkcs8.PrivateKey.Span, exportedPkcs8.PrivateKey.Span);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportPkcs8PrivateKey_IgnoreReturnValue(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int[] valuesToWrite = [-1, 0, int.MaxValue];
            int index = 0;

            int finalDestinationSize = -1;
            mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
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

            int actualSize = mldsa.ExportPkcs8PrivateKey().Length;
            Assert.Equal(finalDestinationSize, actualSize);
            Assert.Equal(valuesToWrite.Length + 1, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportPkcs8PrivateKey_HandleBadReturnValue(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            Func<int, int> getBadReturnValue = (int destinationLength) => destinationLength + 1;
            MLDsaTestImplementation.TryExportFunc hook = (Span<byte> destination, out int bytesWritten) =>
            {
                bool ret = true;

                bytesWritten = getBadReturnValue(destination.Length);

                // Before we return, update the next callback so subsequent calls fail the test
                mldsa.TryExportPkcs8PrivateKeyHook = (Span<byte> destination, out int bytesWritten) =>
                {
                    Assert.Fail();
                    bytesWritten = 0;
                    return true;
                };

                return ret;
            };

            mldsa.TryExportPkcs8PrivateKeyHook = hook;
            Assert.Throws<CryptographicException>(mldsa.ExportPkcs8PrivateKey);
            Assert.Equal(1, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);

            mldsa.TryExportPkcs8PrivateKeyHook = hook;
            getBadReturnValue = (int destinationLength) => int.MaxValue;
            Assert.Throws<CryptographicException>(mldsa.ExportPkcs8PrivateKey);
            Assert.Equal(2, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);

            mldsa.TryExportPkcs8PrivateKeyHook = hook;
            getBadReturnValue = (int destinationLength) => -1;
            Assert.Throws<CryptographicException>(mldsa.ExportPkcs8PrivateKey);
            Assert.Equal(3, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportPkcs8PrivateKey_HandleBadReturnBuffer(MLDsaAlgorithm algorithm)
        {
            MLDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(exportEncrypted =>
            {
                using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

                // Create a bad encoding
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                writer.WriteBitString("some string"u8);
                byte[] validEncoding = writer.Encode();
                Memory<byte> badEncoding = validEncoding.AsMemory(0, validEncoding.Length - 1); // Chop off the last byte

                MLDsaTestImplementation.TryExportFunc hook = (Span<byte> destination, out int bytesWritten) =>
                {
                    bool ret = badEncoding.Span.TryCopyTo(destination);
                    bytesWritten = ret ? badEncoding.Length : 0;
                    return ret;
                };

                mldsa.TryExportPkcs8PrivateKeyHook = hook;

                // Exporting the key should work without any issues because there's no validation
                AssertExtensions.SequenceEqual(badEncoding.Span, mldsa.ExportPkcs8PrivateKey().AsSpan());

                int numberOfCalls = mldsa.TryExportPkcs8PrivateKeyCoreCallCount;
                mldsa.TryExportPkcs8PrivateKeyCoreCallCount = 0;

                // However, exporting the encrypted key should fail because it validates the PKCS#8 private key encoding first
                AssertExtensions.Throws<CryptographicException>(() =>
                        exportEncrypted(mldsa, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 1)));

                // Sanity check that the code to export the private key was called
                Assert.Equal(numberOfCalls, mldsa.TryExportPkcs8PrivateKeyCoreCallCount);
            });
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void SignExternalMu_GetsMuAndDestination(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] buffer = CreatePaddedFilledArray(signatureSize, 0x42);
            Memory<byte> signature = buffer.AsMemory(PaddingSize, signatureSize);
            byte[] mu = new byte[64];

            mldsa.SignMuHook = (mu, destination) => destination.Fill(0xAB);
            mldsa.AddLengthAssertion();
            mldsa.AddDataBufferIsSameAssertion(mu);
            mldsa.AddDestinationBufferIsSameAssertion(signature);

            mldsa.SignMu(mu, signature.Span);
            Assert.Equal(1, mldsa.SignMuCoreCallCount);

            AssertExpectedFill(buffer, fillElement: 0xAB, paddingElement: 0x42, PaddingSize, signatureSize);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void VerifyExternalMu_GetsMuAndSignature(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            byte[] buffer = CreatePaddedFilledArray(algorithm.SignatureSizeInBytes, 0x42);
            Memory<byte> signature = buffer.AsMemory(PaddingSize, algorithm.SignatureSizeInBytes);
            byte[] mu = new byte[64];

            mldsa.VerifyMuHook = (mu, signature) => true;
            mldsa.AddLengthAssertion();
            mldsa.AddDataBufferIsSameAssertion(mu);
            mldsa.AddDestinationBufferIsSameAssertion(signature);

            mldsa.VerifyMu(mu, signature.Span);
            Assert.Equal(1, mldsa.VerifyMuCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void VerifyExternalMu_EarlyFalseForWrongSizeMu(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            byte[] mu = new byte[100];
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes];
            const int CorrectMuLength = 64;

            for (int i = 0; i < mu.Length; i++)
            {
                // Don't check with the correct length, since the callback is Assert.Fail.
                if (i == CorrectMuLength)
                {
                    continue;
                }

                AssertExtensions.FalseExpression(mldsa.VerifyMu(mu.AsSpan(0, i), signature));
            }

            Assert.Equal(0, mldsa.VerifyMuCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void VerifyExternalMu_EarlyFalseForWrongSizeSignature(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            byte[] mu = new byte[64];
            byte[] signature = new byte[mldsa.Algorithm.SignatureSizeInBytes + 1];

            AssertExtensions.FalseExpression(mldsa.VerifyMu(mu, signature));
            AssertExtensions.FalseExpression(mldsa.VerifyMu(mu, signature.AsSpan(2)));

            Assert.Equal(0, mldsa.VerifyMuCoreCallCount);
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
