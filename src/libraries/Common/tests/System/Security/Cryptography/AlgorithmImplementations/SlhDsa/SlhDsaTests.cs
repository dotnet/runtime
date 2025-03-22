// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsaTestsBase), nameof(SupportedOnPlatform))]
    public class SlhDsaTests : SlhDsaTestsBase
    {
        [ConditionalFact(nameof(SupportedOnPlatform))]
        public static void NulArgumentValidation_Create()
        {
            Assert.Throws<ArgumentNullException>(() => SlhDsa.GenerateKey(null));
            Assert.Throws<ArgumentNullException>(() => SlhDsa.ImportSlhDsaPublicKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<ArgumentNullException>(() => SlhDsa.ImportSlhDsaSecretKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<ArgumentNullException>(() => SlhDsa.ImportSlhDsaPrivateSeed(null, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);

            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            Assert.Throws<ArgumentNullException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            Assert.Throws<ArgumentNullException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int privateSeedSize = algorithm.SecretKeySizeInBytes / 4 * 3;

            Assert.Throws<CryptographicException>(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, new byte[publicKeySize - 1]));
            Assert.Throws<CryptographicException>(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, new byte[publicKeySize + 1]));
            Assert.Throws<CryptographicException>(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, new byte[secretKeySize - 1]));
            Assert.Throws<CryptographicException>(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, new byte[secretKeySize + 1]));
            Assert.Throws<CryptographicException>(() => SlhDsa.ImportSlhDsaPrivateSeed(algorithm, new byte[privateSeedSize - 1]));
            Assert.Throws<CryptographicException>(() => SlhDsa.ImportSlhDsaPrivateSeed(algorithm, new byte[privateSeedSize + 1]));

            Assert.Throws<ArgumentException>(() => slhDsa.ExportSlhDsaPublicKey(new byte[publicKeySize - 1]));
            Assert.Throws<ArgumentException>(() => slhDsa.ExportSlhDsaSecretKey(new byte[secretKeySize - 1]));
            Assert.Throws<ArgumentException>(() => slhDsa.ExportSlhDsaPrivateSeed(new byte[privateSeedSize - 1]));

            // Context length must be less than 256
            Assert.Throws<ArgumentOutOfRangeException>(() => slhDsa.SignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
            Assert.Throws<ArgumentOutOfRangeException>(() => slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void AlgorithmMatches(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
            Assert.Equal(algorithm, slhDsa.Algorithm);
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void UseAfterDispose(SlhDsaAlgorithm algorithm)
        {
            SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
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
    }
}
