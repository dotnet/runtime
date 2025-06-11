// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
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
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;
            int signatureSize = algorithm.SignatureSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                mldsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPublicKey(new byte[publicKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaSecretKey(new byte[secretKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.ExportMLDsaPrivateSeed(new byte[privateSeedSize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => mldsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize - 1], ReadOnlySpan<byte>.Empty));

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.SignData(Array.Empty<byte>(), new byte[signatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[signatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => mldsa.VerifyData(Array.Empty<byte>(), new byte[signatureSize], new byte[256]));
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
            byte[] publicKey = CreatePaddedFilledArray(publicKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = publicKey.AsMemory(PaddingSize, publicKeySize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.ExportMLDsaPublicKey(destination.Span);
            Assert.Equal(1, mldsa.ExportMLDsaPublicKeyCoreCallCount);
            AssertExpectedFill(publicKey, fillElement: 1, paddingElement: 42, PaddingSize, publicKeySize);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public static void ExportMLDsaSecretKey_CallsCore(MLDsaAlgorithm algorithm)
        {
            using MLDsaTestImplementation mldsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            mldsa.ExportMLDsaSecretKeyHook = _ => { };
            mldsa.AddFillDestination(1);

            int secretKeySize = algorithm.SecretKeySizeInBytes;
            byte[] secretKey = CreatePaddedFilledArray(secretKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = secretKey.AsMemory(PaddingSize, secretKeySize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.ExportMLDsaSecretKey(destination.Span);
            Assert.Equal(1, mldsa.ExportMLDsaSecretKeyCoreCallCount);
            AssertExpectedFill(secretKey, fillElement: 1, paddingElement: 42, PaddingSize, secretKeySize);
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
            byte[] signature = CreatePaddedFilledArray(signatureSize, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, signatureSize);
            mldsa.AddDestinationBufferIsSameAssertion(destination);

            mldsa.SignData(testData, destination.Span, testContext);
            Assert.Equal(1, mldsa.SignDataCoreCallCount);
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
        public static void ExportPkcs8PrivateKey_DoesNotCallExportMLDsaSecretKey(MLDsaAlgorithm algorithm)
        {
            byte[] secretKeyBytes = CreateFilledArray(algorithm.SecretKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = null,
                },
                PrivateKey = secretKeyBytes,
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

                // Assert that the PKCS#8 private key is NOT generated with the secret key but from our test callback
                Assert.Equal(0, mldsa.ExportMLDsaSecretKeyCoreCallCount);
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

            byte[] secretKeyBytes = CreateFilledArray(algorithm.SecretKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                    Parameters = null,
                },
                PrivateKey = secretKeyBytes,
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

            byte[] secretKeyBytes = CreateFilledArray(algorithm.SecretKeySizeInBytes, 42);
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = MLDsaTestHelpers.AlgorithmToOid(MLDsaAlgorithm.MLDsa44),
                    Parameters = null,
                },
                PrivateKey = secretKeyBytes,
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
