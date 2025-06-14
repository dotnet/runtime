// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;
using System.Security.Cryptography.SLHDsa.Tests;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static partial class PrivateKeyAssociationTests
    {
        private static partial Func<X509Certificate2, MLKem, X509Certificate2> CopyWithPrivateKey_MLKem { get; }
        private static partial Func<X509Certificate2, MLKem> GetMLKemPublicKey { get; }
        private static partial Func<X509Certificate2, MLKem> GetMLKemPrivateKey { get; }
        private static partial Func<X509Certificate2, SlhDsa, X509Certificate2> CopyWithPrivateKey_SlhDsa { get; }
        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPublicKey { get; }
        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPrivateKey { get; }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void GetSlhDsaPublicKeyTest()
        {
            // Cert without private key
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(SlhDsaTestData.IetfSlhDsaSha2_128sCertificate))
            using (SlhDsa? certKey = GetSlhDsaPublicKey(cert))
            {
                Assert.NotNull(certKey);
                AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, certKey.ExportSlhDsaPublicKey());

                // Verify the key is not actually private
                Assert.ThrowsAny<CryptographicException>(() => certKey.SignData([1, 2, 3]));
            }

            // Cert with private key
            using (X509Certificate2 cert = LoadShlDsaIetfCertificateWithPrivateKey())
            using (SlhDsa? certKey = GetSlhDsaPublicKey(cert))
            {
                Assert.NotNull(certKey);
                AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, certKey.ExportSlhDsaPublicKey());

                // Verify the key is not actually private
                Assert.ThrowsAny<CryptographicException>(() => certKey.SignData([1, 2, 3]));
            }
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void GetSlhDsaPrivateKeyTest()
        {
            // Cert without private key
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(SlhDsaTestData.IetfSlhDsaSha2_128sCertificate))
            {
                using (SlhDsa? certKey = GetSlhDsaPrivateKey(cert))
                {
                    Assert.Null(certKey);
                }
            }

            // Cert with private key
            using (X509Certificate2 certWithPrivateKey = LoadShlDsaIetfCertificateWithPrivateKey())
            {
                using (SlhDsa? certKey = GetSlhDsaPrivateKey(certWithPrivateKey))
                {
                    Assert.NotNull(certKey);

                    // Verify the key is actually private
                    AssertExtensions.SequenceEqual(
                        SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue,
                        certKey.ExportSlhDsaSecretKey());
                }
            }
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_SlhDsa()
        {
            Random rng = new Random();

            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(SlhDsaTestData.IetfSlhDsaSha2_128sCertificate))
            using (SlhDsa privKey = SlhDsa.ImportPkcs8PrivateKey(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8))
            using (X509Certificate2 wrongAlg = X509CertificateLoader.LoadCertificate(TestData.CertWithEnhancedKeyUsage))
            {
                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => SlhDsa.GenerateKey(SlhDsaAlgorithm.SlhDsaSha2_128s),
                        () => SlhDsa.GenerateKey(SlhDsaAlgorithm.SlhDsaSha2_192f),
                        () => SlhDsa.GenerateKey(SlhDsaAlgorithm.SlhDsaShake256f),
                    ],
                    CopyWithPrivateKey_SlhDsa,
                    GetSlhDsaPublicKey,
                    GetSlhDsaPrivateKey,
                    (priv, pub) =>
                    {
                        byte[] data = new byte[rng.Next(97)];
                        rng.NextBytes(data);

                        byte[] signature = priv.SignData(data);
                        Assert.True(pub.VerifyData(data, signature));
                    });
            }
        }

        [ConditionalFact(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_SlhDsa_OtherSlhDsa()
        {
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(SlhDsaTestData.IetfSlhDsaSha2_128sCertificate))
            {
                using (SlhDsaMockImplementation publicSlhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128s))
                {
                    Exception e = new Exception("no secret key");
                    publicSlhDsa.ExportSlhDsaSecretKeyCoreHook = _ => throw e;
                    publicSlhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
                        SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue.CopyTo(destination);

                    Assert.Same(e, AssertExtensions.Throws<Exception>(() => CopyWithPrivateKey_SlhDsa(pubOnly, publicSlhDsa)));
                }

                SlhDsaMockImplementation privateSlhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128s);
                privateSlhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
                    SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue.CopyTo(destination);
                privateSlhDsa.ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
                    SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue.CopyTo(destination);

                using (X509Certificate2 privCert = CopyWithPrivateKey_SlhDsa(pubOnly, privateSlhDsa))
                {
                    AssertExtensions.TrueExpression(privCert.HasPrivateKey);

                    using (SlhDsa certPrivateSlhDsa = GetSlhDsaPrivateKey(privCert))
                    {
                        AssertExtensions.SequenceEqual(
                            SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue,
                            certPrivateSlhDsa.ExportSlhDsaSecretKey());

                        privateSlhDsa.Dispose();
                        privateSlhDsa.ExportSlhDsaPublicKeyCoreHook = _ => Assert.Fail();
                        privateSlhDsa.ExportSlhDsaSecretKeyCoreHook = _ => Assert.Fail();

                        // Ensure the key is actual a clone
                        AssertExtensions.SequenceEqual(
                            SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue,
                            certPrivateSlhDsa.ExportSlhDsaSecretKey());
                    }
                }
            }
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void GetMLKemPublicKey_WithoutPrivateKey()
        {
            using (X509Certificate2 cert = MLKemCertTests.LoadCertificateFromPem(MLKemTestData.IetfMlKem512CertificatePem))
            using (MLKem certKey = GetMLKemPublicKey(cert))
            {
                Assert.NotNull(certKey);
                AssertExtensions.SequenceEqual(MLKemTestData.IetfMlKem512Spki, certKey.ExportSubjectPublicKeyInfo());

                certKey.Encapsulate(out byte[] ciphertext, out _);
                Assert.ThrowsAny<CryptographicException>(() => certKey.Decapsulate(ciphertext));
            }
        }

        [ConditionalFact(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        public static void GetMLKemPublicKey_WithPrivateKey()
        {
            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(
                MLKemTestData.IetfMlKem512PrivateKeySeedPfx,
                MLKemTestData.EncryptedPrivateKeyPassword))
            using (MLKem certKey = GetMLKemPublicKey(cert))
            {
                AssertExtensions.TrueExpression(cert.HasPrivateKey);
                Assert.NotNull(certKey);
                AssertExtensions.SequenceEqual(MLKemTestData.IetfMlKem512Spki, certKey.ExportSubjectPublicKeyInfo());

                certKey.Encapsulate(out byte[] ciphertext, out _);
                Assert.ThrowsAny<CryptographicException>(() => certKey.Decapsulate(ciphertext));
            }
        }

        [ConditionalFact(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        public static void GetMLKemPrivateKey_NoPrivateKey()
        {
            using (X509Certificate2 cert = MLKemCertTests.LoadCertificateFromPem(MLKemTestData.IetfMlKem512CertificatePem))
            using (MLKem certKey = GetMLKemPrivateKey(cert))
            {
                Assert.Null(certKey);
            }
        }

        [ConditionalFact(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        public static void GetMLKemPrivateKey_WithPrivateKey()
        {
            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(
                MLKemTestData.IetfMlKem512PrivateKeySeedPfx,
                MLKemTestData.EncryptedPrivateKeyPassword))
            using (MLKem certKey = GetMLKemPrivateKey(cert))
            {
                AssertExtensions.TrueExpression(cert.HasPrivateKey);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, certKey.ExportPrivateSeed());
            }
        }

        [ConditionalFact(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        public static void CheckCopyWithPrivateKey_MLKem()
        {
            using (X509Certificate2 pubOnly = MLKemCertTests.LoadCertificateFromPem(MLKemTestData.IetfMlKem512CertificatePem))
            using (MLKem privKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem512PrivateKeySeed))
            using (X509Certificate2 wrongAlg = X509CertificateLoader.LoadCertificate(TestData.CertWithEnhancedKeyUsage))
            {
                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => MLKem.GenerateKey(MLKemAlgorithm.MLKem512),
                        () => MLKem.GenerateKey(MLKemAlgorithm.MLKem768),
                        () => MLKem.GenerateKey(MLKemAlgorithm.MLKem1024),
                    ],
                    CopyWithPrivateKey_MLKem,
                    GetMLKemPublicKey,
                    GetMLKemPrivateKey,
                    (priv, pub) =>
                    {
                        pub.Encapsulate(out byte[] ciphertext, out byte[] pubSharedSecret);
                        byte[] privSharedSecret = priv.Decapsulate(ciphertext);
                        AssertExtensions.SequenceEqual(pubSharedSecret, privSharedSecret);
                    });
            }
        }

        [ConditionalFact(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        public static void CheckCopyWithPrivateKey_MLKem_OtherMLKem_Seed()
        {
            using (X509Certificate2 pubOnly = MLKemCertTests.LoadCertificateFromPem(MLKemTestData.IetfMlKem512CertificatePem))
            using (MLKemContract contract = new(MLKemAlgorithm.MLKem512))
            {
                contract.OnExportPrivateSeedCore = (Span<byte> destination) =>
                {
                    MLKemTestData.IncrementalSeed.CopyTo(destination);
                };

                contract.OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                {
                    using MLKem publicKem = MLKem.ImportSubjectPublicKeyInfo(MLKemTestData.IetfMlKem512Spki);
                    publicKem.ExportEncapsulationKey(destination);
                };

                using (X509Certificate2 cert = CopyWithPrivateKey_MLKem(pubOnly, contract))
                {
                    AssertExtensions.TrueExpression(cert.HasPrivateKey);

                    using (MLKem kem = GetMLKemPrivateKey(cert))
                    {
                        AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
                    }
                }
            }
        }

        [ConditionalFact(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        public static void CheckCopyWithPrivateKey_MLKem_OtherMLKem_DecapsulationKey()
        {
            using (X509Certificate2 pubOnly = MLKemCertTests.LoadCertificateFromPem(MLKemTestData.IetfMlKem512CertificatePem))
            using (MLKemContract contract = new(MLKemAlgorithm.MLKem512))
            {
                contract.OnExportPrivateSeedCore = (Span<byte> destination) =>
                {
                    throw new CryptographicException("Should signal to try decaps key");
                };

                contract.OnExportDecapsulationKeyCore = (Span<byte> destination) =>
                {
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey.AsSpan().CopyTo(destination);
                };

                contract.OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                {
                    using MLKem publicKem = MLKem.ImportSubjectPublicKeyInfo(MLKemTestData.IetfMlKem512Spki);
                    publicKem.ExportEncapsulationKey(destination);
                };

                using (X509Certificate2 cert = CopyWithPrivateKey_MLKem(pubOnly, contract))
                {
                    AssertExtensions.TrueExpression(cert.HasPrivateKey);

                    using (MLKem kem = GetMLKemPrivateKey(cert))
                    {
                        AssertExtensions.SequenceEqual(
                            MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey,
                            kem.ExportDecapsulationKey());
                    }
                }
            }
        }

        private static partial void CheckCopyWithPrivateKey<TKey>(
            X509Certificate2 cert,
            X509Certificate2 wrongAlgorithmCert,
            TKey correctPrivateKey,
            IEnumerable<Func<TKey>> incorrectKeys,
            Func<X509Certificate2, TKey, X509Certificate2> copyWithPrivateKey,
            Func<X509Certificate2, TKey> getPublicKey,
            Func<X509Certificate2, TKey> getPrivateKey,
            Action<TKey, TKey> keyProver)
            where TKey : class, IDisposable
        {
            Exception e = AssertExtensions.Throws<ArgumentException>(
                null,
                () => copyWithPrivateKey(wrongAlgorithmCert, correctPrivateKey));

            Assert.Contains("algorithm", e.Message);

            List<TKey> generatedKeys = new();

            foreach (Func<TKey> func in incorrectKeys)
            {
                TKey incorrectKey = func();
                generatedKeys.Add(incorrectKey);

                e = AssertExtensions.Throws<ArgumentException>(
                    "privateKey",
                    () => copyWithPrivateKey(cert, incorrectKey));

                Assert.Contains("key does not match the public key for this certificate", e.Message);
            }

            using (X509Certificate2 withKey = copyWithPrivateKey(cert, correctPrivateKey))
            {
                e = AssertExtensions.Throws<InvalidOperationException>(
                    () => copyWithPrivateKey(withKey, correctPrivateKey));

                Assert.Contains("already has an associated private key", e.Message);

                foreach (TKey incorrectKey in generatedKeys)
                {
                    e = AssertExtensions.Throws<InvalidOperationException>(
                        () => copyWithPrivateKey(withKey, incorrectKey));

                    Assert.Contains("already has an associated private key", e.Message);
                }

                using (TKey pub = getPublicKey(withKey))
                using (TKey pub2 = getPublicKey(withKey))
                using (TKey pubOnly = getPublicKey(cert))
                using (TKey priv = getPrivateKey(withKey))
                using (TKey priv2 = getPrivateKey(withKey))
                {
                    Assert.NotSame(pub, pub2);
                    Assert.NotSame(pub, pubOnly);
                    Assert.NotSame(pub2, pubOnly);
                    Assert.NotSame(priv, priv2);

                    keyProver(priv, pub2);
                    keyProver(priv2, pub);
                    keyProver(priv, pubOnly);

                    priv.Dispose();
                    pub2.Dispose();

                    keyProver(priv2, pub);
                    keyProver(priv2, pubOnly);
                }
            }

            foreach (TKey incorrectKey in generatedKeys)
            {
                incorrectKey.Dispose();
            }
        }

        private static X509Certificate2 LoadShlDsaIetfCertificateWithPrivateKey()
        {
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(SlhDsaTestData.IetfSlhDsaSha2_128sCertificate))
            using (SlhDsa? privateKey = SlhDsa.ImportSlhDsaSecretKey(SlhDsaAlgorithm.SlhDsaSha2_128s, SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue))
                return cert.CopyWithPrivateKey(privateKey);
        }
    }
}
