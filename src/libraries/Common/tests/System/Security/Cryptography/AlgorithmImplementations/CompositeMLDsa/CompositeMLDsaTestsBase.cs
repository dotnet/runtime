// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(CompositeMLDsa), nameof(CompositeMLDsa.IsSupported))]
    public abstract class CompositeMLDsaTestsBase
    {
        protected abstract CompositeMLDsa GenerateKey(CompositeMLDsaAlgorithm algorithm);
        protected abstract CompositeMLDsa ImportPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract CompositeMLDsa ImportPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void GenerateSignVerifyWithPublicKey(CompositeMLDsaAlgorithm algorithm)
        {
            byte[] signature;
            byte[] data = [0, 1, 2, 3];
            byte[] exportedPublicKey;

            using (CompositeMLDsa generatedKey = GenerateKey(algorithm))
            {
                signature = generatedKey.SignData(data);

                ExerciseSuccessfulVerify(generatedKey, data, signature, []);

                exportedPublicKey = generatedKey.ExportCompositeMLDsaPublicKey();
            }

            using (CompositeMLDsa publicKey = ImportPublicKey(algorithm, exportedPublicKey))
            {
                ExerciseSuccessfulVerify(publicKey, data, signature, []);

                Assert.Throws<CryptographicException>(() => publicKey.SignData(data));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void GenerateSignVerifyWithPrivateKey(CompositeMLDsaAlgorithm algorithm)
        {
            byte[] signature;
            byte[] data = [0, 1, 2, 3];
            byte[] exportedPrivateKey;

            using (CompositeMLDsa generatedKey = GenerateKey(algorithm))
            {
                signature = generatedKey.SignData(data);
                exportedPrivateKey = generatedKey.ExportCompositeMLDsaPrivateKey();
            }

            using (CompositeMLDsa privateKey = ImportPrivateKey(algorithm, exportedPrivateKey))
            {
                ExerciseSuccessfulVerify(privateKey, data, signature, []);

                signature = new byte[algorithm.MaxSignatureSizeInBytes];
                Array.Resize(ref signature, privateKey.SignData(data, signature, []));

                ExerciseSuccessfulVerify(privateKey, data, signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void GenerateSignVerifyNoContext(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = dsa.SignData(data);
            ExerciseSuccessfulVerify(dsa, data, signature, []);

            signature = new byte[algorithm.MaxSignatureSizeInBytes];
            Array.Resize(ref signature, dsa.SignData(data, signature, Array.Empty<byte>()));
            ExerciseSuccessfulVerify(dsa, data, signature, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void GenerateSignVerifyWithContext(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] data = [1, 2, 3, 4, 5];

            byte[] signature = dsa.SignData(data, context);
            ExerciseSuccessfulVerify(dsa, data, signature, context);

            signature = new byte[algorithm.MaxSignatureSizeInBytes];
            Array.Resize(ref signature, dsa.SignData(data, signature, context));
            ExerciseSuccessfulVerify(dsa, data, signature, context);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void GenerateSignVerifyEmptyMessageNoContext(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] signature = dsa.SignData([]);
            ExerciseSuccessfulVerify(dsa, [], signature, []);

            signature = new byte[algorithm.MaxSignatureSizeInBytes];
            Array.Resize(ref signature, dsa.SignData(Array.Empty<byte>(), signature, Array.Empty<byte>()));
            ExerciseSuccessfulVerify(dsa, [], signature, []);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void GenerateSignVerifyEmptyMessageWithContext(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsa dsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = dsa.SignData([], context);
            ExerciseSuccessfulVerify(dsa, [], signature, context);

            signature = new byte[algorithm.MaxSignatureSizeInBytes];
            Array.Resize(ref signature, dsa.SignData(Array.Empty<byte>(), signature, context));
            ExerciseSuccessfulVerify(dsa, [], signature, context);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportExportVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using (CompositeMLDsa privateKey = ImportPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                byte[] exportedSecretKey = privateKey.ExportCompositeMLDsaPrivateKey();
                CompositeMLDsaTestHelpers.AssertPrivateKeyEquals(vector.Algorithm, vector.SecretKey, exportedSecretKey);

                byte[] exportedPublicKey = privateKey.ExportCompositeMLDsaPublicKey();
                CompositeMLDsaTestHelpers.AssertPublicKeyEquals(vector.Algorithm, vector.PublicKey, exportedPublicKey);

                ExerciseSuccessfulVerify(privateKey, vector.Message, vector.Signature, []);
            }

            using (CompositeMLDsa publicKey = ImportPublicKey(vector.Algorithm, vector.PublicKey))
            {
                Assert.Throws<CryptographicException>(publicKey.ExportCompositeMLDsaPrivateKey);

                CompositeMLDsaTestHelpers.AssertExportPrivateKey(
                    export => Assert.Throws<CryptographicException>(() => export(publicKey)));

                byte[] exportedPublicKey = publicKey.ExportCompositeMLDsaPublicKey();
                CompositeMLDsaTestHelpers.AssertPublicKeyEquals(vector.Algorithm, vector.PublicKey, exportedPublicKey);

                ExerciseSuccessfulVerify(publicKey, vector.Message, vector.Signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportSignVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] signature;

            using (CompositeMLDsa privateKey = ImportPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                signature = privateKey.SignData(vector.Message, null);

                ExerciseSuccessfulVerify(privateKey, vector.Message, signature, []);
                ExerciseSuccessfulVerify(privateKey, vector.Message, vector.Signature, []);
            }

            using (CompositeMLDsa publicKey = ImportPublicKey(vector.Algorithm, vector.PublicKey))
            {
                ExerciseSuccessfulVerify(publicKey, vector.Message, signature, []);
                ExerciseSuccessfulVerify(publicKey, vector.Message, vector.Signature, []);
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
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void Generate_Export_Import_PublicKey(CompositeMLDsaAlgorithm algorithm)
        {
            byte[] exportedPublicKey;

            using (CompositeMLDsa dsa = GenerateKey(algorithm))
            {
                exportedPublicKey = dsa.ExportCompositeMLDsaPublicKey();

                using (CompositeMLDsa importedDsa = ImportPublicKey(algorithm, exportedPublicKey))
                {
                    Assert.Throws<CryptographicException>(() => importedDsa.ExportCompositeMLDsaPrivateKey());
                    AssertExtensions.SequenceEqual(exportedPublicKey, importedDsa.ExportCompositeMLDsaPublicKey());
                }
            }

            using (CompositeMLDsa importedDsa = ImportPublicKey(algorithm, exportedPublicKey))
            {
                Assert.Throws<CryptographicException>(() => importedDsa.ExportCompositeMLDsaPrivateKey());
                AssertExtensions.SequenceEqual(exportedPublicKey, importedDsa.ExportCompositeMLDsaPublicKey());
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void Generate_Export_Import_PrivateKey(CompositeMLDsaAlgorithm algorithm)
        {
            byte[] exportedPrivateKey;
            byte[] exportedPublicKey;

            using (CompositeMLDsa dsa = GenerateKey(algorithm))
            {
                exportedPrivateKey = dsa.ExportCompositeMLDsaPrivateKey();
                exportedPublicKey = dsa.ExportCompositeMLDsaPublicKey();

                using (CompositeMLDsa importedDsa = ImportPrivateKey(algorithm, exportedPrivateKey))
                {
                    AssertExtensions.SequenceEqual(exportedPrivateKey, importedDsa.ExportCompositeMLDsaPrivateKey());
                    AssertExtensions.SequenceEqual(exportedPublicKey, importedDsa.ExportCompositeMLDsaPublicKey());
                }
            }

            using (CompositeMLDsa importedDsa = ImportPrivateKey(algorithm, exportedPrivateKey))
            {
                AssertExtensions.SequenceEqual(exportedPrivateKey, importedDsa.ExportCompositeMLDsaPrivateKey());
                AssertExtensions.SequenceEqual(exportedPublicKey, importedDsa.ExportCompositeMLDsaPublicKey());
            }
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
        public void TryExportPrivateKey_BufferTooSmall(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPrivateKey(vector.Algorithm, vector.SecretKey);
            byte[] key = dsa.ExportCompositeMLDsaPrivateKey();
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(key.AsSpan(0, key.Length - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void TryExportPublicKey_BufferTooSmall(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsa dsa = ImportPublicKey(vector.Algorithm, vector.PublicKey);
            byte[] key = dsa.ExportCompositeMLDsaPublicKey();
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(key.AsSpan(0, key.Length - 1), out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportPrivateKey_TrailingData(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] secretKeyWithTrailingData = vector.SecretKey;
            Array.Resize(ref secretKeyWithTrailingData, vector.SecretKey.Length + 1);
            Assert.Throws<CryptographicException>(() => ImportPrivateKey(vector.Algorithm, secretKeyWithTrailingData));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public void ImportPublicKey_TrailingData(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] publicKeyWithTrailingData = vector.PublicKey;
            Array.Resize(ref publicKeyWithTrailingData, vector.PublicKey.Length + 1);
            Assert.Throws<CryptographicException>(() => ImportPublicKey(vector.Algorithm, publicKeyWithTrailingData));
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

            // Flip mldsaSig
            signature[0] ^= 1;
            AssertExtensions.FalseExpression(dsa.VerifyData(data, signature, context));
            signature[0] ^= 1;

            // Flip tradSig
            int tradSigOffset = CompositeMLDsaTestHelpers.MLDsaAlgorithms[dsa.Algorithm].SignatureSizeInBytes;
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
