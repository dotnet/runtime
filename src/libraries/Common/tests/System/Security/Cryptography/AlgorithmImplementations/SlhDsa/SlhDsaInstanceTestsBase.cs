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
