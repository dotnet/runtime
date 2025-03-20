// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaTestsBase
    {
        public static bool SupportedOnPlatform => PlatformDetection.OpenSslPresentOnSystem;
        public static bool NotSupportedOnPlatform => !SupportedOnPlatform;

        public static IEnumerable<object[]> AlgorithmsData => AlgorithmsRaw.Select(a => new[] { a });

        public static SlhDsaAlgorithm[] AlgorithmsRaw =
        [
            SlhDsaAlgorithm.SlhDsaSha2_128s,
            SlhDsaAlgorithm.SlhDsaShake128s,
            SlhDsaAlgorithm.SlhDsaSha2_128f,
            SlhDsaAlgorithm.SlhDsaShake128f,
            SlhDsaAlgorithm.SlhDsaSha2_192s,
            SlhDsaAlgorithm.SlhDsaShake192s,
            SlhDsaAlgorithm.SlhDsaSha2_192f,
            SlhDsaAlgorithm.SlhDsaShake192f,
            SlhDsaAlgorithm.SlhDsaSha2_256s,
            SlhDsaAlgorithm.SlhDsaShake256s,
            SlhDsaAlgorithm.SlhDsaSha2_256f,
            SlhDsaAlgorithm.SlhDsaShake256f,
        ];
    }

    public class SlhDsaPlatformTests : SlhDsaTestsBase
    {
        [Fact]
        public void IsSupportedOnPlatform()
        {
            Assert.Equal(SupportedOnPlatform, SlhDsa.IsSupported);
        }

        [ConditionalFact(nameof(NotSupportedOnPlatform))]
        public void ThrowIfNotSupportedOnPlatform()
        {
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.GenerateKey(null));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromPem(ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportPkcs8PrivateKey(ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPrivateSeed(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPublicKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaSecretKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSubjectPublicKeyInfo(ReadOnlySpan<byte>.Empty));
        }
    }

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

    // TODO validate PBE parameters
    // TODO validate Import* that does stuff before creating key (like reading asn1)

    public class SlhDsaAlgorithmTests
    {
        [Fact]
        public static void AlgorithmsHaveExpectedParameters()
        {
            SlhDsaAlgorithm algorithm;

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_128s;
            Assert.Equal("SLH-DSA-SHA2-128s", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(7856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake128s;
            Assert.Equal("SLH-DSA-SHAKE-128s", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(7856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_128f;
            Assert.Equal("SLH-DSA-SHA2-128f", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(17088, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake128f;
            Assert.Equal("SLH-DSA-SHAKE-128f", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(17088, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_192s;
            Assert.Equal("SLH-DSA-SHA2-192s", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(16224, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake192s;
            Assert.Equal("SLH-DSA-SHAKE-192s", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(16224, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_192f;
            Assert.Equal("SLH-DSA-SHA2-192f", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(35664, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake192f;
            Assert.Equal("SLH-DSA-SHAKE-192f", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(35664, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_256s;
            Assert.Equal("SLH-DSA-SHA2-256s", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(29792, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake256s;
            Assert.Equal("SLH-DSA-SHAKE-256s", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(29792, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_256f;
            Assert.Equal("SLH-DSA-SHA2-256f", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(49856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake256f;
            Assert.Equal("SLH-DSA-SHAKE-256f", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(49856, algorithm.SignatureSizeInBytes);
        }
    }
}
