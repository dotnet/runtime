// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class CompositeMLKemTestsBase
    {
        protected abstract CompositeMLKem GenerateKey(CompositeMLKemAlgorithm algorithm);
        protected abstract CompositeMLKem ImportDecapsulationKey(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract CompositeMLKem ImportEncapsulationKey(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source);

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void AlgorithmMatches_GenerateKey(CompositeMLKemAlgorithm algorithm)
        {
            using (CompositeMLKem kem = GenerateKey(algorithm))
            {
                Assert.Equal(algorithm, kem.Algorithm);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Generate_Encapsulate_Decapsulate(CompositeMLKemAlgorithm algorithm)
        {
            byte[] ct;
            byte[] expectedSs;

            byte[] encapsKeyBytes;
            byte[] decapsKeyBytes;

            using (CompositeMLKem generatedKem = GenerateKey(algorithm))
            {
                generatedKem.Encapsulate(out ct, out expectedSs);
                byte[] ss = generatedKem.Decapsulate(ct);

                AssertExtensions.SequenceEqual(expectedSs, ss);

                encapsKeyBytes = generatedKem.ExportEncapsulationKey();
                decapsKeyBytes = generatedKem.ExportDecapsulationKey();
            }

            ct = null;
            expectedSs = null;

            using (CompositeMLKem encapsKey = ImportEncapsulationKey(algorithm, encapsKeyBytes))
            {
                encapsKey.Encapsulate(out ct, out expectedSs);
            }

            using (CompositeMLKem decapsKey = ImportDecapsulationKey(algorithm, decapsKeyBytes))
            {
                byte[] ss = decapsKey.Decapsulate(ct);

                AssertExtensions.SequenceEqual(expectedSs, ss);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_Encapsulate_Decapsulate(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem encapsKey = ImportEncapsulationKey(vector.Algorithm, vector.EncapsulationKey))
            using (CompositeMLKem decapsKey = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                encapsKey.Encapsulate(out byte[] ct, out byte[] expectedSs);
                byte[] ss = decapsKey.Decapsulate(ct);
                AssertExtensions.SequenceEqual(expectedSs,ss);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_EncapsulationKey_Encapsulate_Decapsulate(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem encapsKey = ImportEncapsulationKey(vector.Algorithm, vector.EncapsulationKey))
            {
                encapsKey.Encapsulate(out byte[] ct, out byte[] expectedSs);
                AssertExtensions.Throws<CryptographicException>(() => encapsKey.Decapsulate(ct));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_DecapsulationKey_Encapsulate_Decapsulate(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem decapsKey = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                decapsKey.Encapsulate(out byte[] ct, out byte[] expectedSs);
                byte[] ss = decapsKey.Decapsulate(ct);
                AssertExtensions.SequenceEqual(expectedSs, ss);

                decapsKey.Decapsulate(vector.Ciphertext, ss);
                AssertExtensions.SequenceEqual(vector.SharedSecret, ss);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_DecapsulationKey_AlgorithmMatches(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem decapsKey = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                Assert.Equal(vector.Algorithm, decapsKey.Algorithm);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_DecapsulationKey_Export(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem decapsKey = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                    export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(decapsKey)));

                CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                    export => AssertExtensions.SequenceEqual(vector.DecapsulationKey, export(decapsKey)));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_EncapsulationKey_AlgorithmMatches(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem encapsKey = ImportEncapsulationKey(vector.Algorithm, vector.EncapsulationKey))
            {
                Assert.Equal(vector.Algorithm, encapsKey.Algorithm);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_EncapsulationKey_Export(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem encapsKey = ImportEncapsulationKey(vector.Algorithm, vector.EncapsulationKey))
            {
                CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                    export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(encapsKey)));

                CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                    export => Assert.Throws<CryptographicException>(() => export(encapsKey)));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_EncapsulationKey_Export_Spki(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem encapsKey = ImportEncapsulationKey(vector.Algorithm, vector.EncapsulationKey))
            {
                // SPKI is DER so the exported SPKI must be identical to the reference SPKI.
                CompositeMLKemTestHelpers.AssertExportSubjectPublicKeyInfo(
                    export => AssertExtensions.SequenceEqual(vector.Spki, export(encapsKey)));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void DifferentKey_DecapsulationRejection(CompositeMLKemAlgorithm algorithm)
        {
            using (CompositeMLKem one = GenerateKey(algorithm))
            using (CompositeMLKem two = GenerateKey(algorithm))
            {
                one.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                AssertDecapsulationRejects(two, ciphertext, sharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void DifferentTradKeyComponents_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem orig = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                byte[] otherDecapsKeyBytes;
                byte[] otherEncapsKeyBytes;

                using (CompositeMLKem other = GenerateKey(vector.Algorithm))
                {
                    otherDecapsKeyBytes = other.ExportDecapsulationKey();
                    otherEncapsKeyBytes = other.ExportEncapsulationKey();
                }

                // Make decaps key with original ML-KEM component and other traditional component.
                byte[] origKeyBytes = vector.DecapsulationKey.ToArray();
                int tradKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
                origKeyBytes.AsSpan().Slice(0, tradKeyOffset).CopyTo(otherDecapsKeyBytes);

                using (CompositeMLKem other = ImportDecapsulationKey(vector.Algorithm, otherDecapsKeyBytes))
                {
                    orig.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                    AssertDecapsulationRejects(other, ciphertext, sharedSecret);

                    other.Encapsulate(out ciphertext, out sharedSecret);
                    AssertDecapsulationRejects(orig, ciphertext, sharedSecret);
                }

                // Now the encaps key
                origKeyBytes = vector.EncapsulationKey.ToArray();
                tradKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
                origKeyBytes.AsSpan().Slice(0, tradKeyOffset).CopyTo(otherEncapsKeyBytes);

                using (CompositeMLKem other = ImportEncapsulationKey(vector.Algorithm, otherEncapsKeyBytes))
                {
                    other.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                    AssertDecapsulationRejects(orig, ciphertext, sharedSecret);
                }
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void DifferentMLKemComponents_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            using (CompositeMLKem orig = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                byte[] otherDecapsKeyBytes;
                byte[] otherEncapsKeyBytes;

                using (CompositeMLKem other = GenerateKey(vector.Algorithm))
                {
                    otherDecapsKeyBytes = other.ExportDecapsulationKey();
                    otherEncapsKeyBytes = other.ExportEncapsulationKey();
                }

                // Make decaps key with original TradKey component and other ML-KEM component.
                byte[] testKeyBytes = vector.DecapsulationKey.ToArray();
                int tradKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
                otherDecapsKeyBytes.AsSpan().Slice(0, tradKeyOffset).CopyTo(testKeyBytes);

                using (CompositeMLKem other = ImportDecapsulationKey(vector.Algorithm, testKeyBytes))
                {
                    orig.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                    AssertDecapsulationRejects(other, ciphertext, sharedSecret);

                    other.Encapsulate(out ciphertext, out sharedSecret);
                    AssertDecapsulationRejects(orig, ciphertext, sharedSecret);
                }

                // Now the encaps key
                testKeyBytes = vector.EncapsulationKey.ToArray();
                tradKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
                otherEncapsKeyBytes.AsSpan().Slice(0, tradKeyOffset).CopyTo(testKeyBytes);

                using (CompositeMLKem other = ImportEncapsulationKey(vector.Algorithm, testKeyBytes))
                {
                    other.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                    AssertDecapsulationRejects(orig, ciphertext, sharedSecret);
                }
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void ModifiedMLKemCiphertext_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            byte[] ciphertext = vector.Ciphertext.ToArray();
            ciphertext[0] ^= 1;

            using (CompositeMLKem kem = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                AssertDecapsulationRejects(kem, ciphertext, vector.SharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void ModifiedTraditionalCiphertext_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            byte[] ciphertext = vector.Ciphertext.ToArray();
            ciphertext[^1] ^= 1;

            using (CompositeMLKem kem = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                AssertDecapsulationRejects(kem, ciphertext, vector.SharedSecret);
            }
        }

        public static IEnumerable<object[]> SupportedRsaIetfVectorsTestData =>
            CompositeMLKemTestData.AllIetfVectors
                .Where(vector =>
                    CompositeMLKem.IsAlgorithmSupported(vector.Algorithm) &&
                    CompositeMLKemTestData.ExecuteComponentFunc(
                        vector.Algorithm,
                        rsa => true,
                        ecdh => false,
                        xdh => false))
                .Select(vector => new object[] { vector });

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaUndersizedSharedSecret_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            byte[] ciphertext = CreateRsaCiphertextWithSharedSecretSize(vector, sharedSecretSizeInBytes: 31);

            using (CompositeMLKem kem = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                AssertDecapsulationRejects(kem, ciphertext, vector.SharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaOversizedSharedSecret_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            byte[] ciphertext = CreateRsaCiphertextWithSharedSecretSize(vector, sharedSecretSizeInBytes: 33);

            using (CompositeMLKem kem = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                AssertDecapsulationRejects(kem, ciphertext, vector.SharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void ModifiedMLKemDecapsulationKey_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            byte[] decapsulationKey = vector.DecapsulationKey.ToArray();
            decapsulationKey[0] ^= 1;

            using (CompositeMLKem kem = ImportDecapsulationKey(vector.Algorithm, decapsulationKey))
            {
                AssertDecapsulationRejects(kem, vector.Ciphertext, vector.SharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedXDiffieHellmanIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void ModifiedXDiffieHellmanEncapsulationKey_Failure(CompositeMLKemTestVector vector)
        {
            byte[] encapsulationKeyBytes = vector.EncapsulationKey.ToArray();
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
            encapsulationKeyBytes[traditionalKeyOffset] ^= 1;

            CompositeMLKem tamperedEncapsulationKey;

            try
            {
                tamperedEncapsulationKey = ImportEncapsulationKey(vector.Algorithm, encapsulationKeyBytes);
            }
            catch (CryptographicException)
            {
                // Some providers notice the tampering and throw immediately.
                return;
            }

            // Other providers allow import, but decapsulation must result in rejection.

            using (tamperedEncapsulationKey)
            using (CompositeMLKem decapsulationKey = ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey))
            {
                tamperedEncapsulationKey.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);

                AssertDecapsulationRejects(decapsulationKey, ciphertext, sharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedXDiffieHellmanIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void ModifiedXDiffieHellmanDecapsulationKey_DecapsulationRejection(CompositeMLKemTestVector vector)
        {
            byte[] decapsulationKey = vector.DecapsulationKey.ToArray();
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
            decapsulationKey[traditionalKeyOffset + 1] ^= 1;

            // Tampering doesn't cause import failures, but should cause decapsulation rejection.
            using (CompositeMLKem kem = ImportDecapsulationKey(vector.Algorithm, decapsulationKey))
            {
                AssertDecapsulationRejects(kem, vector.Ciphertext, vector.SharedSecret);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void InvalidRsaEncoding_ImportEncapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] encapsulationKey = vector.EncapsulationKey.ToArray();
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
            encapsulationKey.AsSpan(traditionalKeyOffset).Clear();

            AssertCorrectlySizedEncapsulationKeyImportFails(vector.Algorithm, encapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaWrongKeySize_ImportEncapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] encapsulationKey = CreateCompositeRsaKeyWithWrongSize(vector, includePrivateKey: false);

            AssertCorrectlySizedEncapsulationKeyImportFails(vector.Algorithm, encapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaTrailingData_ImportEncapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] encapsulationKey = new byte[vector.EncapsulationKey.Length + 1];
            vector.EncapsulationKey.CopyTo(encapsulationKey);

            AssertCorrectlySizedEncapsulationKeyImportFails(vector.Algorithm, encapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaBerEncoding_ImportEncapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
            byte[] rsaPublicKey = AsnUtils.ConvertDerToNonDerBer(vector.EncapsulationKey.Slice(traditionalKeyOffset));
            byte[] encapsulationKey = ChangeTraditionalEncapsulationKeyComponent(vector, rsaPublicKey);

            AssertCorrectlySizedEncapsulationKeyImportFails(vector.Algorithm, encapsulationKey);
        }

        public static IEnumerable<object[]> SupportedECDiffieHellmanIetfVectorsTestData =>
            CompositeMLKemTestData.AllIetfVectors
                .Where(vector =>
                    CompositeMLKem.IsAlgorithmSupported(vector.Algorithm) &&
                    CompositeMLKemTestData.ExecuteComponentFunc(
                        vector.Algorithm,
                        rsa => false,
                        ecdh => true,
                        xdh => false))
                .Select(vector => new object[] { vector });

        [Theory]
        [MemberData(nameof(SupportedECDiffieHellmanIetfVectorsTestData))]
        public void ECDiffieHellmanInvalidPoint_ImportEncapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] encapsulationKey = vector.EncapsulationKey.ToArray();
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
            encapsulationKey[traditionalKeyOffset] = 0;

            AssertCorrectlySizedEncapsulationKeyImportFails(vector.Algorithm, encapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaInvalidEncoding_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] decapsulationKey = vector.DecapsulationKey.ToArray();
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
            decapsulationKey.AsSpan(traditionalKeyOffset).Clear();

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaWrongKeySize_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] decapsulationKey = CreateCompositeRsaKeyWithWrongSize(vector, includePrivateKey: true);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaInvalidParameterP_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
            byte[] invalidRsaPrivateKey = CreateRsaPrivateKeyWithTamperedParameterP(vector.DecapsulationKey.Slice(traditionalKeyOffset));
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, invalidRsaPrivateKey);

            // Make sure it's correctly sized so it won't trigger early validation failure
            Assert.InRange(
                decapsulationKey.Length,
                CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(vector.Algorithm),
                CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(vector.Algorithm));

            Action test = () => ImportDecapsulationKey(vector.Algorithm, decapsulationKey);

            if (PlatformDetection.IsOpenSslSupported)
            {
                // OpenSSL has a custom message for this.
                CryptographicException ex = Assert.ThrowsAny<CryptographicException>(test);
                Assert.Contains("n does not equal p q", ex.Message);
            }
            else
            {
                Assert.Throws<CryptographicException>(test);
            }
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaTrailingData_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] decapsulationKey = new byte[vector.DecapsulationKey.Length + 1];
            vector.DecapsulationKey.CopyTo(decapsulationKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedRsaIetfVectorsTestData))]
        public void RsaBerEncoding_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
            byte[] rsaPrivateKey = AsnUtils.ConvertDerToNonDerBer(vector.DecapsulationKey.Slice(traditionalKeyOffset));
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, rsaPrivateKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedECDiffieHellmanIetfVectorsTestData))]
        public void ECDiffieHellmanInvalidVersion_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] ecPrivateKey = EncodeCorrectlySizedEcPrivateKey(vector.Algorithm, version: 0, GetECDiffieHellmanCurveOid(vector.Algorithm));
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, ecPrivateKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedECDiffieHellmanIetfVectorsTestData))]
        public void ECDiffieHellmanPublicKeyField_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] ecPrivateKey = EncodeCorrectlySizedEcPrivateKey(vector.Algorithm, version: 1, curveOid: null, publicKey: [0x04]);
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, ecPrivateKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedECDiffieHellmanIetfVectorsTestData))]
        public void ECDiffieHellmanMissingParameters_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] ecPrivateKey = EncodeCorrectlySizedEcPrivateKey(vector.Algorithm, version: 1, curveOid: null);
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, ecPrivateKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedECDiffieHellmanIetfVectorsTestData))]
        public void ECDiffieHellmanWrongCurve_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            string curveOid = GetECDiffieHellmanCurveOid(vector.Algorithm);
            string wrongCurveOid =
                curveOid == ECCurve.NamedCurves.nistP256.Oid.Value ?
                    ECCurve.NamedCurves.nistP384.Oid.Value! :
                    ECCurve.NamedCurves.nistP256.Oid.Value!;
            byte[] ecPrivateKey = EncodeCorrectlySizedEcPrivateKey(vector.Algorithm, version: 1, wrongCurveOid);
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, ecPrivateKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        [Theory]
        [MemberData(nameof(SupportedECDiffieHellmanIetfVectorsTestData))]
        public void ECDiffieHellmanBerEncoding_ImportDecapsulationKeyFailure(CompositeMLKemTestVector vector)
        {
            byte[] ecPrivateKey = EncodeBerEcPrivateKey(vector.Algorithm);
            byte[] decapsulationKey = ChangeTraditionalDecapsulationKeyComponent(vector, ecPrivateKey);

            AssertCorrectlySizedDecapsulationKeyImportFails(vector.Algorithm, decapsulationKey);
        }

        private static void AssertDecapsulationRejects(
            CompositeMLKem kem,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> expectedSharedSecret)
        {
            byte[] sharedSecret = new byte[kem.Algorithm.SharedSecretSizeInBytes];
            sharedSecret.AsSpan().Fill(0xA5);

            try
            {
                kem.Decapsulate(ciphertext, sharedSecret);

                // Implicit rejection
                AssertExtensions.FalseExpression(sharedSecret.AsSpan().SequenceEqual(expectedSharedSecret));
            }
            catch (CryptographicException)
            {
                // Explicit rejection, must clear the shared secret
                AssertExtensions.SequenceEqual(new byte[sharedSecret.Length], sharedSecret);
            }
        }

        private static byte[] CreateRsaCiphertextWithSharedSecretSize(CompositeMLKemTestVector vector, int sharedSecretSizeInBytes)
        {
            MLKemAlgorithm mlKemAlgorithm = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm);
            ReadOnlySpan<byte> rsaPublicKey = vector.EncapsulationKey.Slice(mlKemAlgorithm.EncapsulationKeySizeInBytes);

            byte[] rsaCiphertext;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(rsaPublicKey, out int bytesRead);
                Assert.Equal(rsaPublicKey.Length, bytesRead);
                rsaCiphertext = rsa.Encrypt(new byte[sharedSecretSizeInBytes], RSAEncryptionPadding.OaepSHA256);
            }

            byte[] ciphertext = new byte[mlKemAlgorithm.CiphertextSizeInBytes + rsaCiphertext.Length];
            vector.Ciphertext.Slice(0, mlKemAlgorithm.CiphertextSizeInBytes).CopyTo(ciphertext);
            rsaCiphertext.CopyTo(ciphertext.AsSpan(mlKemAlgorithm.CiphertextSizeInBytes));
            return ciphertext;
        }

        private static byte[] CreateCompositeRsaKeyWithWrongSize(CompositeMLKemTestVector vector, bool includePrivateKey)
        {
            const int KeySizeDifferenceInBits = 64;

            int expectedKeySizeInBits =
                CompositeMLKemTestData.ExecuteComponentFunc(
                    vector.Algorithm,
                    rsa => rsa.KeySizeInBits,
                    ecdh => throw new Xunit.Sdk.XunitException("Expected an RSA algorithm."),
                    xdh => throw new Xunit.Sdk.XunitException("Expected an RSA algorithm."));
            int wrongKeySizeInBits = expectedKeySizeInBits - KeySizeDifferenceInBits;

            byte[] traditionalKey;

            using (RSA rsa = RSA.Create(wrongKeySizeInBits))
            {
                traditionalKey = includePrivateKey ? rsa.ExportRSAPrivateKey() : rsa.ExportRSAPublicKey();
            }

            MLKemAlgorithm mlKemAlgorithm = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm);
            ReadOnlySpan<byte> mlKemKey =
                includePrivateKey ?
                    vector.DecapsulationKey.Slice(0, mlKemAlgorithm.PrivateSeedSizeInBytes) :
                    vector.EncapsulationKey.Slice(0, mlKemAlgorithm.EncapsulationKeySizeInBytes);
            byte[] compositeKey = new byte[mlKemKey.Length + traditionalKey.Length];
            mlKemKey.CopyTo(compositeKey);
            traditionalKey.CopyTo(compositeKey.AsSpan(mlKemKey.Length));

            return compositeKey;
        }

        private static byte[] CreateRsaPrivateKeyWithTamperedParameterP(ReadOnlySpan<byte> rsaPrivateKey)
        {
            RSAParameters parameters;

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(rsaPrivateKey, out int bytesRead);
                Assert.Equal(rsaPrivateKey.Length, bytesRead);
                parameters = rsa.ExportParameters(includePrivateParameters: true);
            }

            parameters.P[0] ^= 1;

            AsnWriter writer = new(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                writer.WriteInteger(0);
                writer.WriteIntegerUnsigned(parameters.Modulus);
                writer.WriteIntegerUnsigned(parameters.Exponent);
                writer.WriteIntegerUnsigned(parameters.D);
                writer.WriteIntegerUnsigned(parameters.P);
                writer.WriteIntegerUnsigned(parameters.Q);
                writer.WriteIntegerUnsigned(parameters.DP);
                writer.WriteIntegerUnsigned(parameters.DQ);
                writer.WriteIntegerUnsigned(parameters.InverseQ);
            }

            return writer.Encode();
        }

        private void AssertCorrectlySizedEncapsulationKeyImportFails(CompositeMLKemAlgorithm algorithm, byte[] encapsulationKey)
        {
            // Make sure it's correctly sized so it won't trigger early validation failure
            Assert.InRange(
                encapsulationKey.Length,
                CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm),
                CompositeMLKemTestData.ExpectedEncapsulationKeySizeUpperBound(algorithm));

            Assert.Throws<CryptographicException>(() => ImportEncapsulationKey(algorithm, encapsulationKey));
        }

        private void AssertCorrectlySizedDecapsulationKeyImportFails(CompositeMLKemAlgorithm algorithm, byte[] decapsulationKey)
        {
            // Make sure it's correctly sized so it won't trigger early validation failure
            Assert.InRange(
                decapsulationKey.Length,
                CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm),
                CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm));

            Assert.Throws<CryptographicException>(() => ImportDecapsulationKey(algorithm, decapsulationKey));
        }

        private byte[] ChangeTraditionalEncapsulationKeyComponent(CompositeMLKemTestVector vector, byte[] tradKey)
        {
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).EncapsulationKeySizeInBytes;
            byte[] encapsulationKey = new byte[traditionalKeyOffset + tradKey.Length];
            vector.EncapsulationKey.Slice(0, traditionalKeyOffset).CopyTo(encapsulationKey);
            tradKey.CopyTo(encapsulationKey.AsSpan(traditionalKeyOffset));
            return encapsulationKey;
        }

        private byte[] ChangeTraditionalDecapsulationKeyComponent(CompositeMLKemTestVector vector, byte[] tradKey)
        {
            int traditionalKeyOffset = CompositeMLKemTestData.GetMLKemAlgorithm(vector.Algorithm).PrivateSeedSizeInBytes;
            byte[] decapsulationKey = new byte[traditionalKeyOffset + tradKey.Length];
            vector.DecapsulationKey.Slice(0, traditionalKeyOffset).CopyTo(decapsulationKey);
            tradKey.CopyTo(decapsulationKey.AsSpan(traditionalKeyOffset));
            return decapsulationKey;
        }

        private static byte[] EncodeCorrectlySizedEcPrivateKey(
            CompositeMLKemAlgorithm algorithm,
            int version,
            string? curveOid,
            byte[]? publicKey = null)
        {
            int targetSize = GetECDiffieHellmanPrivateKeyEncodingSize(algorithm);
            byte[] emptyPrivateKey = EncodeEcPrivateKey(version, privateKeySize: 0, curveOid, publicKey);
            int privateKeySize = targetSize - emptyPrivateKey.Length;
            AssertExtensions.GreaterThanOrEqualTo(privateKeySize, 0);

            byte[] key = EncodeEcPrivateKey(version, privateKeySize, curveOid, publicKey);
            Assert.Equal(targetSize, key.Length);
            return key;
        }

        private static byte[] EncodeEcPrivateKey(int version, int privateKeySize, string? curveOid, byte[]? publicKey = null)
        {
            AsnWriter writer = new(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                writer.WriteInteger(version);
                writer.WriteOctetString(new byte[privateKeySize]);

                if (curveOid is not null)
                {
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
                    {
                        writer.WriteObjectIdentifier(curveOid);
                    }
                }

                if (publicKey is not null)
                {
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true)))
                    {
                        writer.WriteBitString(publicKey);
                    }
                }
            }

            return writer.Encode();
        }

        private static byte[] EncodeBerEcPrivateKey(CompositeMLKemAlgorithm algorithm)
        {
            byte[] shortPrivateKey = EncodeEcPrivateKey(
                version: 1,
                privateKeySize: GetECDiffieHellmanPrivateKeySize(algorithm) - 1,
                GetECDiffieHellmanCurveOid(algorithm));

            Assert.Equal(GetECDiffieHellmanPrivateKeyEncodingSize(algorithm) - 1, shortPrivateKey.Length);
            Assert.Equal(0x30, shortPrivateKey[0]);
            Assert.Equal(0x02, shortPrivateKey[2]);
            Assert.Equal(0x04, shortPrivateKey[5]);

            byte[] ecPrivateKey = new byte[shortPrivateKey.Length + 1];
            ecPrivateKey[0] = shortPrivateKey[0];
            ecPrivateKey[1] = checked((byte)(shortPrivateKey[1] + 1));
            shortPrivateKey.AsSpan(2, 4).CopyTo(ecPrivateKey.AsSpan(2));
            ecPrivateKey[6] = 0x81;
            shortPrivateKey.AsSpan(6).CopyTo(ecPrivateKey.AsSpan(7));
            return ecPrivateKey;
        }

        private static int GetECDiffieHellmanPrivateKeySize(CompositeMLKemAlgorithm algorithm) =>
            CompositeMLKemTestData.ExecuteComponentFunc(
                algorithm,
                rsa => throw new Xunit.Sdk.XunitException("Expected an ECDH algorithm."),
                ecdh => (ecdh.KeySizeInBits + 7) / 8,
                xdh => throw new Xunit.Sdk.XunitException("Expected an ECDH algorithm."));

        private static int GetECDiffieHellmanPrivateKeyEncodingSize(CompositeMLKemAlgorithm algorithm) =>
            CompositeMLKemTestData.ExecuteComponentFunc(
                algorithm,
                rsa => throw new Xunit.Sdk.XunitException("Expected an ECDH algorithm."),
                ecdh => ecdh.MaxPrivateKeySizeInBytes,
                xdh => throw new Xunit.Sdk.XunitException("Expected an ECDH algorithm."));

        private static string GetECDiffieHellmanCurveOid(CompositeMLKemAlgorithm algorithm) =>
            algorithm.Name switch
            {
                "MLKEM768-ECDH-P256-SHA3-256" => ECCurve.NamedCurves.nistP256.Oid.Value!,
                "MLKEM768-ECDH-P384-SHA3-256" or
                "MLKEM1024-ECDH-P384-SHA3-256" => ECCurve.NamedCurves.nistP384.Oid.Value!,
                "MLKEM768-ECDH-brainpoolP256r1-SHA3-256" => "1.3.36.3.3.2.8.1.1.7",
                "MLKEM1024-ECDH-brainpoolP384r1-SHA3-256" => "1.3.36.3.3.2.8.1.1.11",
                "MLKEM1024-ECDH-P521-SHA3-256" => ECCurve.NamedCurves.nistP521.Oid.Value!,
                _ => throw new Xunit.Sdk.XunitException($"Expected an ECDH algorithm, got '{algorithm.Name}'."),
            };
    }
}
