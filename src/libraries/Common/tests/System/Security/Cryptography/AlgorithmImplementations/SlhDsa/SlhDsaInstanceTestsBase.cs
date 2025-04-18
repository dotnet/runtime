// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Formats.Asn1;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.Asn1;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaInstanceTestsBase : SlhDsaTestsBase
    {
        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public void NullArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }
            
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public void ArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int signatureSize = algorithm.SignatureSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaPublicKey(new byte[publicKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaSecretKey(new byte[secretKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize - 1], ReadOnlySpan<byte>.Empty));

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public void ArgumentValidation_PbeParameters(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            AssertEncryptedExportPkcs8PrivateKey(
                new PbeParameters(PbeEncryptionAlgorithm.Unknown, HashAlgorithmName.SHA1, 42),
                exportAction => AssertExtensions.Throws<CryptographicException>(exportAction));

            AssertEncryptedExportPkcs8PrivateKey(
                new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 42),
                exportAction => AssertExtensions.Throws<CryptographicException>(exportAction));

            // Chars not allowed in TripleDes3KeyPkcs12
            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42);
            AssertExtensions.Throws<CryptographicException>(() => slhDsa.ExportEncryptedPkcs8PrivateKey("password"u8, pbeParameters));
            AssertExtensions.Throws<CryptographicException>(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem("password"u8, pbeParameters));
            AssertExtensions.Throws<CryptographicException>(() => slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, pbeParameters, Span<byte>.Empty, out _));

            void AssertEncryptedExportPkcs8PrivateKey(PbeParameters pbeParameters, Action<Action> exportAction)
            {
                exportAction (() => slhDsa.ExportEncryptedPkcs8PrivateKey("password"u8, pbeParameters));
                exportAction (() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem("password"u8, pbeParameters));
                exportAction(() => slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, pbeParameters, Span<byte>.Empty, out _));

                exportAction (() => slhDsa.ExportEncryptedPkcs8PrivateKey("password", pbeParameters));
                exportAction(() => slhDsa.ExportEncryptedPkcs8PrivateKeyPem("password", pbeParameters));
                exportAction(() => slhDsa.TryExportEncryptedPkcs8PrivateKey("password", pbeParameters, Span<byte>.Empty, out _));
            }
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void UseAfterDispose(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            slhDsa.Dispose();
            slhDsa.Dispose(); // no throw

            VerifyDisposed(slhDsa);
        }
    }
}
