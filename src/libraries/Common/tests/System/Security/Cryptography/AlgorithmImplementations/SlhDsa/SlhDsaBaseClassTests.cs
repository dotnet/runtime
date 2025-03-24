// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaBaseClassTests : SlhDsaTestsBase
    {
        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void NullArgumentValidation(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            Assert.Throws<ArgumentNullException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int privateSeedSize = algorithm.SecretKeySizeInBytes / 4 * 3;
            int signatureSize = algorithm.SignatureSizeInBytes;

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
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);
            slhDsa.Dispose();

            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportPkcs8PrivateKeyPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPrivateSeed(Span<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaPublicKey(Span<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSlhDsaSecretKey(Span<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfo());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportPkcs8PrivateKey(Span<byte>.Empty, out _));
            Assert.Throws<ObjectDisposedException>(() => slhDsa.TryExportSubjectPublicKeyInfo(Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void CallsExportSlhDsaPublicKeyCore(SlhDsaAlgorithm algorithm)
        {
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            byte[] publicKey = new byte[publicKeySize + 2];

            Action<Span<byte>> exportPublicKeyCoreHook = slhDsa.ExportSlhDsaPublicKeyCoreHook;
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
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

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
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

            int privateSeedSize = algorithm.SecretKeySizeInBytes / 4 * 3;
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
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

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
            SlhDsaTestImplementation slhDsa = SlhDsaTestImplementation.CreateOverriddenMethodsFail(algorithm);

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

            returnValue = true;
            Assert.False(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize - 1), testContext));
            Assert.False(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize + 1), testContext));

            Assert.True(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize), testContext));

            returnValue = false;
            Assert.False(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize), testContext));
        }
    }
}
