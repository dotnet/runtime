// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaBaseClassTests : SlhDsaTestsBase
    {
        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in AlgorithmsRaw
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

#if !NETSTANDARD2_0_OR_GREATER && !NETFRAMEWORK // Remove once PbeParameters is outboxed
        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void NullArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }
            
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            Assert.Throws<ArgumentNullException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
        }
#endif

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;
            int signatureSize = algorithm.SignatureSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            Assert.Throws<ArgumentException>(() => slhDsa.ExportSlhDsaPublicKey(new byte[publicKeySize - 1]));
            Assert.Throws<ArgumentException>(() => slhDsa.ExportSlhDsaSecretKey(new byte[secretKeySize - 1]));
            Assert.Throws<ArgumentException>(() => slhDsa.ExportSlhDsaPrivateSeed(new byte[privateSeedSize - 1]));
            Assert.Throws<ArgumentException>(() => slhDsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize - 1], ReadOnlySpan<byte>.Empty));

            // Context length must be less than 256
            Assert.Throws<ArgumentOutOfRangeException>(() => slhDsa.SignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
            Assert.Throws<ArgumentOutOfRangeException>(() => slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void UseAfterDispose(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            // The private seed and public key sizes are both smaller so this can be used for all three:
            byte[] input = new byte[algorithm.SecretKeySizeInBytes];
#if !NETSTANDARD2_0_OR_GREATER && !NETFRAMEWORK // Remove once PbeParameters is outboxed
            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 32);
#endif

            slhDsa.Dispose();

#if !NETSTANDARD2_0_OR_GREATER && !NETFRAMEWORK // Remove once PbeParameters is outboxed
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, pbeParameters));
#endif
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKeyPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPrivateSeed(input));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPublicKey(input));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaSecretKey(input));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfoPem());
#if !NETSTANDARD2_0_OR_GREATER && !NETFRAMEWORK // Remove once PbeParameters is outboxed
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters, Span<byte>.Empty, out _));
#endif
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportPkcs8PrivateKey(Span<byte>.Empty, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportSubjectPublicKeyInfo(Span<byte>.Empty, out _));
        }

        public static IEnumerable<object[]> ApiWithDestinationSpanTestData =>
            from algorithm in AlgorithmsRaw
            from destinationLargerThanRequired in new[] { true, false }
            select new object[] { algorithm, destinationLargerThanRequired };

        private const int PaddingSize = 10;

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsExportSlhDsaPublicKeyCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            byte[] publicKey = new byte[publicKeySize + 2 * PaddingSize];
            publicKey.AsSpan().Fill(42);

            slhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(publicKeySize, destination.Length);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.ExportSlhDsaPublicKey(publicKey.AsSpan(PaddingSize, publicKeySize + extraBytes));
            AssertExpectedFill(publicKey, fillElement: 1, paddingElement: 42, PaddingSize, publicKeySize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsExportSlhDsaSecretKeyCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int secretKeySize = algorithm.SecretKeySizeInBytes;
            byte[] secretKey = new byte[secretKeySize + 2 * PaddingSize];
            secretKey.AsSpan().Fill(42);

            slhDsa.ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(secretKeySize, destination.Length);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.ExportSlhDsaSecretKey(secretKey.AsSpan(PaddingSize, secretKeySize + extraBytes));
            AssertExpectedFill(secretKey, fillElement: 1, paddingElement: 42, PaddingSize, secretKeySize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsExportSlhDsaPrivateSeedCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;
            byte[] privateSeed = new byte[privateSeedSize + 2 * PaddingSize];
            privateSeed.AsSpan().Fill(42);

            slhDsa.ExportSlhDsaPrivateSeedCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(privateSeedSize, destination.Length);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.ExportSlhDsaPrivateSeed(privateSeed.AsSpan(PaddingSize, privateSeedSize + extraBytes));
            AssertExpectedFill(privateSeed, fillElement: 1, paddingElement: 42, PaddingSize, privateSeedSize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsSignDataCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] signature = new byte[signatureSize + 2 * PaddingSize];
            signature.AsSpan().Fill(42);
            byte[] testData = [2];
            byte[] testContext = [3];

            slhDsa.SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                AssertExtensions.SequenceEqual(testData, data);
                AssertExtensions.SequenceEqual(testContext, context);

                Assert.Equal(destination.Length, signatureSize);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.SignData(testData, signature.AsSpan(PaddingSize, signatureSize + extraBytes), testContext);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        private static void AssertExpectedFill(ReadOnlySpan<byte> source, byte fillElement, byte paddingElement, int startIndex, int length)
        {
            // Ensure that the data was filled correctly
            AssertExtensions.FilledWith(fillElement, source.Slice(startIndex, length));

            // And that the padding was not touched
            AssertExtensions.FilledWith(paddingElement, source.Slice(0, startIndex));
            AssertExtensions.FilledWith(paddingElement, source.Slice(startIndex + length));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsVerifyDataCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = new byte[signatureSize + 1];
            testSignature.AsSpan().Fill(42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                AssertExtensions.SequenceEqual(testData, data);
                AssertExtensions.SequenceEqual(testContext, context);
                AssertExtensions.SequenceEqual(testSignature.AsSpan(0, signatureSize), signature);

                return returnValue;
            };

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            Assert.False(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize - 1), testContext));
            Assert.False(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize + 1), testContext));

            // But does for the right one.
            Assert.True(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize), testContext));

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            Assert.False(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize), testContext));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsVirtualDispose(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);
            bool disposeCalled = false;

            // First Dispose call should invoke overridden Dispose should be called
            slhDsa.DisposeHook = (bool disposing) =>
            {
                Assert.True(disposing);
                disposeCalled = true;
            };

            slhDsa.Dispose();
            Assert.True(disposeCalled);

            // Subsequent Dispose calls should be a no-op
            slhDsa.DisposeHook = _ => Assert.Fail();

            slhDsa.Dispose();
            slhDsa.Dispose();
        }
    }
}
