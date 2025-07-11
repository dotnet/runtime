// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
    public abstract class CompositeMLDsaTestsBase
    {
        protected abstract CompositeMLDsa ImportPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract CompositeMLDsa ImportPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportExportVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using (CompositeMLDsa key = ImportPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                byte[] exportedSecretKey = key.ExportCompositeMLDsaPrivateKey();
                CompositeMLDsaTestHelpers.AssertPrivateKeyEquals(vector.Algorithm, vector.SecretKey, exportedSecretKey);

                byte[] exportedPublicKey = key.ExportCompositeMLDsaPublicKey();
                CompositeMLDsaTestHelpers.AssertPublicKeyEquals(vector.Algorithm, vector.PublicKey, exportedPublicKey);

                ExerciseSuccessfulVerify(key, vector.Message, vector.Signature, []);
            }

            using (CompositeMLDsa key = ImportPublicKey(vector.Algorithm, vector.PublicKey))
            {
                Assert.Throws<CryptographicException>(key.ExportCompositeMLDsaPrivateKey);

                byte[] exportedPublicKey = key.ExportCompositeMLDsaPublicKey();
                CompositeMLDsaTestHelpers.AssertPublicKeyEquals(vector.Algorithm, vector.PublicKey, exportedPublicKey);

                ExerciseSuccessfulVerify(key, vector.Message, vector.Signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportSignVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] signature;

            using (CompositeMLDsa key = ImportPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                signature = key.SignData(vector.Message, null);

                Assert.Equal(vector.Signature.Length, signature.Length);

                ExerciseSuccessfulVerify(key, vector.Message, signature, []);
            }

            using (CompositeMLDsa key = ImportPublicKey(vector.Algorithm, vector.PublicKey))
            {
                ExerciseSuccessfulVerify(key, vector.Message, signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportPublicKey_Export(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPublicKey(vector.Algorithm, vector.PublicKey);

            CompositeMLDsaTestHelpers.AssertExportPublicKey(
                export => CompositeMLDsaTestHelpers.AssertPublicKeyEquals(vector.Algorithm, vector.PublicKey, export(dsa)));

            CompositeMLDsaTestHelpers.AssertExportPrivateKey(
                export => Assert.Throws<CryptographicException>(() => export(dsa)));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportPrivateKey_Export(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPrivateKey(vector.Algorithm, vector.SecretKey);

            CompositeMLDsaTestHelpers.AssertExportPublicKey(
                export => CompositeMLDsaTestHelpers.AssertPublicKeyEquals(vector.Algorithm, vector.PublicKey, export(dsa)));

            CompositeMLDsaTestHelpers.AssertExportPrivateKey(
                export => CompositeMLDsaTestHelpers.AssertPrivateKeyEquals(vector.Algorithm, vector.SecretKey, export(dsa)));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void SignData_PublicKeyOnlyThrows(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPublicKey(vector.Algorithm, vector.PublicKey);

            CryptographicException ce =
                Assert.ThrowsAny<CryptographicException>(() => dsa.SignData("hello"u8.ToArray()));

            Assert.DoesNotContain("unknown", ce.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void TrySignData_BufferTooSmall(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPrivateKey(vector.Algorithm, vector.SecretKey);
            byte[] signature = new byte[32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[vector.Algorithm].SignatureSizeInBytes];
            AssertExtensions.FalseExpression(dsa.TrySignData("hello"u8.ToArray(), signature, out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void TryExportPrivateKey_BufferTooSmall(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPrivateKey(vector.Algorithm, vector.SecretKey);
            byte[] key = dsa.ExportCompositeMLDsaPrivateKey();
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(key.AsSpan(0, key.Length - 1), out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void TryExportPublicKey_BufferTooSmall(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPublicKey(vector.Algorithm, vector.PublicKey);
            byte[] key = dsa.ExportCompositeMLDsaPublicKey();
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(key.AsSpan(0, key.Length - 1), out _));       
        }

        protected static void ExerciseSuccessfulVerify(CompositeMLDsa dsa, byte[] data, byte[] signature, byte[] context)
        {
            ReadOnlySpan<byte> buffer = [0, 1, 2, 3];

            AssertExtensions.TrueExpression(dsa.VerifyData(data, signature, context));

            if (data.Length > 0)
            {
                AssertExtensions.FalseExpression(dsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.FalseExpression(dsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                data[0] ^= 1;
                AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, context));
                data[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(dsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.TrueExpression(dsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                AssertExtensions.FalseExpression(dsa.VerifyData(buffer.Slice(0, 1), signature, context));
                AssertExtensions.FalseExpression(dsa.VerifyData(buffer.Slice(1, 3), signature, context));
            }

            // Flip randomizer
            signature[0] ^= 1;
            AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, context));
            signature[0] ^= 1;

            // Flip mldsaSig
            signature[32] ^= 1;
            AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, context));
            signature[32] ^= 1;

            // Flip tradSig
            int tradSigOffset = 32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[dsa.Algorithm].SignatureSizeInBytes;
            signature[tradSigOffset] ^= 1;
            AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, context));
            signature[tradSigOffset] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(dsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.TrueExpression(dsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, buffer.Slice(0, 1)));
                AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, buffer.Slice(1, 3)));
            }

            AssertExtensions.TrueExpression(dsa.VerifyData(data, signature, context));
        }
    }
}
