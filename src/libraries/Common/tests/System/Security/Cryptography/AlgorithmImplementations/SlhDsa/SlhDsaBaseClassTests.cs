// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaBaseClassTests : SlhDsaTestsBase
    {
        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in AlgorithmsRaw
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

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
            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 32);

            slhDsa.Dispose();

            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, pbeParameters));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKeyPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPrivateSeed(input));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPublicKey(input));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaSecretKey(input));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pbeParameters, Span<byte>.Empty, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportPkcs8PrivateKey(Span<byte>.Empty, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportSubjectPublicKeyInfo(Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsExportSlhDsaPublicKeyCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            byte[] publicKey = new byte[publicKeySize + 2];

            slhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(publicKeySize, destination.Length);
                destination.Fill(1);
            };

            Array.Fill(publicKey, (byte)42);
            slhDsa.ExportSlhDsaPublicKey(publicKey.AsSpan(1, publicKeySize));

            Assert.Equal(42, publicKey[0]);
            Assert.Equal(42, publicKey[^1]);
            Assert.All(publicKey.Skip(1).SkipLast(1), b => Assert.Equal(1, b));

            Array.Fill(publicKey, (byte)42);
            slhDsa.ExportSlhDsaPublicKey(publicKey.AsSpan(1, publicKeySize + 1)); // Extra byte should be ignored

            Assert.Equal(42, publicKey[0]);
            Assert.Equal(42, publicKey[^1]);
            Assert.All(publicKey.Skip(1).SkipLast(1), b => Assert.Equal(1, b));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsExportSlhDsaSecretKeyCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int secretKeySize = algorithm.SecretKeySizeInBytes;
            byte[] secretKey = new byte[secretKeySize + 2];

            slhDsa.ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(secretKeySize, destination.Length);
                destination.Fill(1);
            };

            Array.Fill(secretKey, (byte)42);
            slhDsa.ExportSlhDsaSecretKey(secretKey.AsSpan(1, secretKeySize));

            Assert.Equal(42, secretKey[0]);
            Assert.Equal(42, secretKey[^1]);
            Assert.All(secretKey.Skip(1).SkipLast(1), b => Assert.Equal(1, b));

            Array.Fill(secretKey, (byte)42);
            slhDsa.ExportSlhDsaSecretKey(secretKey.AsSpan(1, secretKeySize + 1)); // Extra byte should be ignored

            Assert.Equal(42, secretKey[0]);
            Assert.Equal(42, secretKey[^1]);
            Assert.All(secretKey.Skip(1).SkipLast(1), b => Assert.Equal(1, b));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsExportSlhDsaPrivateSeedCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;
            byte[] privateSeed = new byte[privateSeedSize + 2];

            slhDsa.ExportSlhDsaPrivateSeedCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(privateSeedSize, destination.Length);
                destination.Fill(1);
            };

            Array.Fill(privateSeed, (byte)42);
            slhDsa.ExportSlhDsaPrivateSeed(privateSeed.AsSpan(1, privateSeedSize));

            Assert.Equal(42, privateSeed[0]);
            Assert.Equal(42, privateSeed[^1]);
            Assert.All(privateSeed.Skip(1).SkipLast(1), b => Assert.Equal(1, b));

            Array.Fill(privateSeed, (byte)42);
            slhDsa.ExportSlhDsaPrivateSeed(privateSeed.AsSpan(1, privateSeedSize + 1)); // Extra byte should be ignored

            Assert.Equal(42, privateSeed[0]);
            Assert.Equal(42, privateSeed[^1]);
            Assert.All(privateSeed.Skip(1).SkipLast(1), b => Assert.Equal(1, b));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsSignDataCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] signature = new byte[signatureSize + 2];
            byte[] testData = [2];
            byte[] testContext = [3];

            slhDsa.SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                Assert.Equal(testData, data);
                Assert.Equal(testContext, context);

                Assert.Equal(destination.Length, signatureSize);
                destination.Fill(1);
            };

            Array.Fill(signature, (byte)42);
            slhDsa.SignData(testData, signature.AsSpan(1, signatureSize), testContext);

            Assert.Equal(42, signature[0]);
            Assert.Equal(42, signature[^1]);
            Assert.All(signature.Skip(1).SkipLast(1), b => Assert.Equal(1, b));

            Array.Fill(signature, (byte)42);
            slhDsa.SignData(testData, signature.AsSpan(1, signatureSize), testContext); // Extra byte should be ignored

            Assert.Equal(42, signature[0]);
            Assert.Equal(42, signature[^1]);
            Assert.All(signature.Skip(1).SkipLast(1), b => Assert.Equal(1, b));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsVerifyDataCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = new byte[signatureSize + 1];
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            Array.Fill(testSignature, (byte)42);

            slhDsa.VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                Assert.Equal(testData, data);
                Assert.Equal(testContext, context);
                Assert.Equal(testSignature.AsSpan(0, signatureSize), signature);

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
