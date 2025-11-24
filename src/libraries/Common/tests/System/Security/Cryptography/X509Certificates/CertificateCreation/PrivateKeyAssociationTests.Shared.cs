// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;
using System.Security.Cryptography.SLHDsa.Tests;
using Test.Cryptography;
using Xunit;

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
        private static partial Func<X509Certificate2, MLDsa, X509Certificate2> CopyWithPrivateKey_MLDsa { get; }
        private static partial Func<X509Certificate2, MLDsa> GetMLDsaPublicKey { get; }
        private static partial Func<X509Certificate2, MLDsa> GetMLDsaPrivateKey { get; }

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
                        certKey.ExportSlhDsaPrivateKey());
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
                    Exception e = new Exception("no private key");
                    publicSlhDsa.ExportSlhDsaPrivateKeyCoreHook = _ => throw e;
                    publicSlhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
                        SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue.CopyTo(destination);

                    Assert.Same(e, AssertExtensions.Throws<Exception>(() => CopyWithPrivateKey_SlhDsa(pubOnly, publicSlhDsa)));
                }

                SlhDsaMockImplementation privateSlhDsa = SlhDsaMockImplementation.Create(SlhDsaAlgorithm.SlhDsaSha2_128s);
                privateSlhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
                    SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue.CopyTo(destination);
                privateSlhDsa.ExportSlhDsaPrivateKeyCoreHook = (Span<byte> destination) =>
                    SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue.CopyTo(destination);

                using (X509Certificate2 privCert = CopyWithPrivateKey_SlhDsa(pubOnly, privateSlhDsa))
                {
                    AssertExtensions.TrueExpression(privCert.HasPrivateKey);

                    using (SlhDsa certPrivateSlhDsa = GetSlhDsaPrivateKey(privCert))
                    {
                        AssertExtensions.SequenceEqual(
                            SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue,
                            certPrivateSlhDsa.ExportSlhDsaPrivateKey());

                        privateSlhDsa.Dispose();
                        privateSlhDsa.ExportSlhDsaPublicKeyCoreHook = _ => Assert.Fail();
                        privateSlhDsa.ExportSlhDsaPrivateKeyCoreHook = _ => Assert.Fail();

                        // Ensure the key is actual a clone
                        AssertExtensions.SequenceEqual(
                            SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue,
                            certPrivateSlhDsa.ExportSlhDsaPrivateKey());
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

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void GetMLDsaPublicKeyTest()
        {
            // Cert without private key
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsa? certKey = GetMLDsaPublicKey(cert))
            {
                Assert.NotNull(certKey);
                byte[] publicKey = certKey.ExportMLDsaPublicKey();
                AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PublicKey, publicKey);

                // Verify the key is not actually private
                Assert.ThrowsAny<CryptographicException>(() => certKey.SignData([1, 2, 3]));
            }

            // Cert with private key
            using (X509Certificate2 cert = LoadMLDsaIetfCertificateWithPrivateKey())
            using (MLDsa? certKey = GetMLDsaPublicKey(cert))
            {
                Assert.NotNull(certKey);
                byte[] publicKey = certKey.ExportMLDsaPublicKey();
                AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PublicKey, publicKey);

                // Verify the key is not actually private
                Assert.ThrowsAny<CryptographicException>(() => certKey.SignData([1, 2, 3]));
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void GetMLDsaPrivateKeyTest()
        {
            // Cert without private key
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            {
                using (MLDsa? certKey = GetMLDsaPrivateKey(cert))
                {
                    Assert.Null(certKey);
                }
            }

            // Cert with private key
            using (X509Certificate2 certWithPrivateKey = LoadMLDsaIetfCertificateWithPrivateKey())
            {
                using (MLDsa? certKey = GetMLDsaPrivateKey(certWithPrivateKey))
                {
                    Assert.NotNull(certKey);

                    // Verify the key is actually private
                    byte[] privateSeed = certKey.ExportMLDsaPrivateSeed();
                    AssertExtensions.SequenceEqual(
                        MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                        privateSeed);
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDSA()
        {
            Random rng = new Random();

            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa65.Certificate))
            using (MLDsa privKey = MLDsa.ImportMLDsaPrivateSeed(MLDsaAlgorithm.MLDsa65, MLDsaTestsData.IetfMLDsa65.PrivateSeed))
            using (X509Certificate2 wrongAlg = X509CertificateLoader.LoadCertificate(TestData.CertWithEnhancedKeyUsage))
            {
                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44),
                        () => MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65),
                        () => MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa87),
                    ],
                    (cert, key) => cert.CopyWithPrivateKey(key),
                    cert => cert.GetMLDsaPublicKey(),
                    cert => cert.GetMLDsaPrivateKey(),
                    (priv, pub) =>
                    {
                        byte[] data = new byte[rng.Next(97)];
                        rng.NextBytes(data);

                        byte[] signature = priv.SignData(data);
                        Assert.True(pub.VerifyData(data, signature));
                    });
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_PrivateKey()
        {
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            {
                using (MLDsaTestImplementation publicMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
                {
                    Exception e = new Exception("no private key");

                    // The private key can be retrieved directly or from PKCS#8. If the seed is not available,
                    // it should fall back to the private key.
                    publicMLDsa.TryExportPkcs8PrivateKeyHook = (_, out _) => throw e;
                    publicMLDsa.ExportMLDsaPrivateKeyHook = _ => throw e;
                    publicMLDsa.ExportMLDsaPrivateSeedHook = _ => throw new CryptographicException("Should signal to try private key");
                    publicMLDsa.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;

                    Assert.Same(e, AssertExtensions.Throws<Exception>(() => CopyWithPrivateKey_MLDsa(pubOnly, publicMLDsa)));
                }

                MLDsaTestImplementation privateMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44);
                privateMLDsa.ExportMLDsaPrivateSeedHook = _ => throw new CryptographicException("Should signal to try private key"); ;
                privateMLDsa.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;
                privateMLDsa.ExportMLDsaPrivateKeyHook = MLDsaTestsData.IetfMLDsa44.PrivateKey.CopyTo;

                privateMLDsa.TryExportPkcs8PrivateKeyHook = (dest, out written) =>
                {
                    if (MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.AsSpan().TryCopyTo(dest))
                    {
                        written = MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.Length;
                        return true;
                    }

                    written = 0;
                    return false;
                };

                using (X509Certificate2 privCert = CopyWithPrivateKey_MLDsa(pubOnly, privateMLDsa))
                {
                    AssertExtensions.TrueExpression(privCert.HasPrivateKey);

                    using (MLDsa certPrivateMLDsa = GetMLDsaPrivateKey(privCert))
                    {
                        byte[] privateKey = certPrivateMLDsa.ExportMLDsaPrivateKey();
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.PrivateKey,
                            privateKey);

                        privateMLDsa.Dispose();
                        privateMLDsa.ExportMLDsaPrivateSeedHook = _ => Assert.Fail();
                        privateMLDsa.ExportMLDsaPublicKeyHook = _ => Assert.Fail();
                        privateMLDsa.ExportMLDsaPrivateKeyHook = _ => Assert.Fail();
                        privateMLDsa.TryExportPkcs8PrivateKeyHook = (_, out w) => { Assert.Fail(); w = 0; return false; };

                        // Ensure the key is actual a clone
                        privateKey = certPrivateMLDsa.ExportMLDsaPrivateKey();
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.PrivateKey,
                            privateKey);
                    }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_PrivateSeed()
        {
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            {
                using (MLDsaTestImplementation publicMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
                {
                    Exception e = new Exception("no private key");

                    // The private seed can be retrieved directly or from PKCS#8
                    publicMLDsa.TryExportPkcs8PrivateKeyHook = (_, out _) => throw e;
                    publicMLDsa.ExportMLDsaPrivateSeedHook = _ => throw e;
                    publicMLDsa.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;

                    Assert.Same(e, AssertExtensions.Throws<Exception>(() => CopyWithPrivateKey_MLDsa(pubOnly, publicMLDsa)));
                }

                MLDsaTestImplementation privateMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44);
                privateMLDsa.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;
                privateMLDsa.ExportMLDsaPrivateSeedHook = MLDsaTestsData.IetfMLDsa44.PrivateSeed.CopyTo;

                privateMLDsa.TryExportPkcs8PrivateKeyHook = (dest, out written) =>
                {
                    if (MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.AsSpan().TryCopyTo(dest))
                    {
                        written = MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.Length;
                        return true;
                    }

                    written = 0;
                    return false;
                };

                using (X509Certificate2 privCert = CopyWithPrivateKey_MLDsa(pubOnly, privateMLDsa))
                {
                    AssertExtensions.TrueExpression(privCert.HasPrivateKey);

                    using (MLDsa certPrivateMLDsa = GetMLDsaPrivateKey(privCert))
                    {
                        byte[] privateKey = certPrivateMLDsa.ExportMLDsaPrivateSeed();
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                            privateKey);

                        privateMLDsa.Dispose();
                        privateMLDsa.ExportMLDsaPublicKeyHook = _ => Assert.Fail();
                        privateMLDsa.ExportMLDsaPrivateSeedHook = _ => Assert.Fail();
                        privateMLDsa.TryExportPkcs8PrivateKeyHook = (_, out w) => { Assert.Fail(); w = 0; return false; };

                        // Ensure the key is actual a clone
                        privateKey = certPrivateMLDsa.ExportMLDsaPrivateSeed();
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                            privateKey);
                    }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_WrongPkcs8()
        {
            const string rsaPkcs8Base64 = """
                MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCnDBnNKDxAQOPeb8M+OvGurjRi
                2lB+a2VdTspZXxvRt2wXwtyyqWcV+wGGvLE74hGE21UFPW4Jd2iefuKlkn9U/dvcPEGQjuClO1EY
                FlFcZ1Y9LwNuXuS1IA4doiuzEyYgxXJje8w5CIisqXmwZEJX/OY4yEYE4OV/3xoI5FIGv9Kp2AsU
                ttGaD2uzgsrCUe1Cj7L6IhBBiqvcp55icXlWXb0oSjd/ovhFgrn9+IPW7ln8wGRRRHKwIi+TBuXK
                k7WOVcUECaPZIR7n8AuHHJ90sZHieSnIiMKzah8ZnFc1yG1Y9EnP3jWT00SZ2at84j/vkwfK87lj
                gf9Gx9Gg5kVZAgMBAAECggEARofWcQf3AI4laCq6PhE3MDD/j2lsKSSBRPdaeoeswEx4yEOPWaQr
                EV3M1C3hi041ZWoSKMc6KacQNjOO0KfdOW6CISgT6sxYz4sO/2OU8LX09JpgEX7hhBRHwX1ShCam
                p5mWZajEnqQayQQ5jB+Y33u5XOo6nh6y5920KWL1u0Ed3aYHVa/+rfCIfctsEx+n2CBsiAX4fTaB
                ZtTpZaQlDrDnOPtPDcJ1NOq7L/JwBYn6euBwkOZIl9VQ0q0mZ5YkXr9WB0BwNlRSvqa06b7y16qS
                1Y1M4jRzoYEl4hh7mKzVDrkyAVH2oFEsplMIufIQpt3rFvj+vUQciCY2bz8VrQKBgQDcffvWyKnz
                Twr/nReoGXvkkYt/RBEZsNVZko4BJK0RYXQLUrPCFy8kxzIidgVPYlVvym3hYYpRXMnydTR0pEHn
                UWv9fFp6EISFdssEvP4PvQq1T0EPH6yTo1DflUe82YDtYA/l/nqg24IqYaa7nF2O1RESLYFixh7y
                oM1vsn42TwKBgQDB8swph+ei+1v+V64I3s/rQUhl/6FKwxVF4UbmBKlbd/YtAr1ccljNefeXRmWc
                KmsVGO/Py5eD+VGNk9EUzZLRFqRnbbhTxPYufCGd4cImvlPceN6U/QS5x/52FJPUvIKVNWw7rgxd
                8Fr5YZDNi28ChvVdJBozjIgthElGQ82H1wKBgFikQVmAxGovbcGDex42WIt0Q7t/Nsy4PZ1MANDO
                2NDy978RmXi+71H+ztXx0oKuiqBtpi0ElKHPBtT1b4gw/Nms7xgyJQGLoGszbbzS6eST4Dkxynr1
                BeE4t+uazQNMAbvscZfJ7ay7cqHtLiWgYDBq0fkX2DtIYOqz4MM14+2bAoGAJ5Qisb74ODxPU6IU
                8950U6/o1FfMVHNnHfGRBFOjM/VRGXJbrkfvc08WhZpqFepaG94Q4jjL3LS+PcQSgMpK0bxrJGgx
                m3awPmA6g/uUIU/p0S4hTgosMrVrajFc0ab+hvB1+9/SykDIb+fHIwr3Rm7AF5fMeQSOras3QM2J
                XdUCgYEAtsHg+5g8UmIivixcowQ0jd4xoFV54oxJqIywtwWbHKkkiTEC87Y/bM+yB/FOz9CY6zsj
                czacDBoMMwiiYWhY4fgfwOpsw+B+ZOX95bBd99iGip5Rv+QeOUCoDibCo0thYuF3ZeRCa+A02xVe
                urOzLZcZZcL09b35iX6IaxosmNM=
                """;

            byte[] rsaPkcs8 = Convert.FromBase64String(rsaPkcs8Base64);

            using (X509Certificate2 ietfCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsaTestImplementation keyThatExportsRsaPkcs8 = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
            {
                keyThatExportsRsaPkcs8.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;
                keyThatExportsRsaPkcs8.ExportMLDsaPrivateSeedHook = MLDsaTestsData.IetfMLDsa44.PrivateSeed.CopyTo;

                // Export RSA PKCS#8
                keyThatExportsRsaPkcs8.TryExportPkcs8PrivateKeyHook = (dest, out written) =>
                {
                    if (rsaPkcs8.AsSpan().TryCopyTo(dest))
                    {
                        written = rsaPkcs8.Length;
                        return true;
                    }

                    written = 0;
                    return false;
                };

                if (PlatformDetection.IsWindows)
                {
                    // Only Windows uses PKCS#8 for pairing key to cert.
                    AssertExtensions.Throws<CryptographicException>(() => ietfCert.CopyWithPrivateKey(keyThatExportsRsaPkcs8));
                }
                else
                {
                    // Assert.NoThrow
                    using (ietfCert.CopyWithPrivateKey(keyThatExportsRsaPkcs8)) { }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_MalformedPkcs8()
        {
            using (X509Certificate2 ietfCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsaTestImplementation keyThatExportsMalformedPkcs8 = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
            {
                keyThatExportsMalformedPkcs8.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;
                keyThatExportsMalformedPkcs8.ExportMLDsaPrivateSeedHook = MLDsaTestsData.IetfMLDsa44.PrivateSeed.CopyTo;

                // Export malformed PKCS#8
                keyThatExportsMalformedPkcs8.TryExportPkcs8PrivateKeyHook =
                    (dest, out written) =>
                    {
                        written = 0;
                        return true;
                    };

                if (PlatformDetection.IsWindows)
                {
                    // Only Windows uses PKCS#8 for pairing key to cert.
                    AssertExtensions.Throws<CryptographicException>(() => ietfCert.CopyWithPrivateKey(keyThatExportsMalformedPkcs8));
                }
                else
                {
                    // Assert.NoThrow
                    using (ietfCert.CopyWithPrivateKey(keyThatExportsMalformedPkcs8)) { }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociatePersistedKey_CNG_MLDsa()
        {
            const string KeyName = $"{nameof(PrivateKeyAssociationTests)}_{nameof(AssociatePersistedKey_CNG_MLDsa)}";

            CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
            {
                ExportPolicy = CngExportPolicies.None,
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
            };

            CngProperty parameterSet = MLDsaTestHelpers.GetCngProperty(MLDsaAlgorithm.MLDsa44);
            creationParameters.Parameters.Add(parameterSet);

            // Blob for IETF ML-DSA-44 seed
            byte[] pqDsaSeedBlob = Convert.FromBase64String("RFNTUwYAAAAgAAAANAA0AAAAAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
            CngProperty mldsaBlob = new CngProperty(
                CngKeyBlobFormat.PQDsaPrivateSeedBlob.ToString(),
                pqDsaSeedBlob,
                CngPropertyOptions.None);
            creationParameters.Parameters.Add(mldsaBlob);

            CngKey cngKey = null;
            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];

            try
            {
                cngKey = CngKey.Create(CngAlgorithm.MLDsa, KeyName, creationParameters);

                using (MLDsaCng mldsaCng = new MLDsaCng(cngKey))
                using (X509Certificate2 unpairedCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
                using (X509Certificate2 cert = unpairedCert.CopyWithPrivateKey(mldsaCng))
                using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
                {
                    mldsa.SignData("test"u8, signature);
                    Assert.True(mldsaCng.VerifyData("test"u8, signature));
                }

                // Some certs have disposed, did they delete the key?
                using (CngKey stillPersistedKey = CngKey.Open(KeyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                using (MLDsaCng mldsa = new MLDsaCng(stillPersistedKey))
                {
                    mldsa.SignData("test"u8, signature);
                }
            }
            finally
            {
                cngKey?.Delete();
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociateEphemeralKey_CNG_MLDsa()
        {
            CngKeyCreationParameters creationParameters = new CngKeyCreationParameters
            {
                ExportPolicy = CngExportPolicies.AllowPlaintextExport,
            };

            CngProperty parameterSet = MLDsaTestHelpers.GetCngProperty(MLDsaAlgorithm.MLDsa44);
            creationParameters.Parameters.Add(parameterSet);

            // Blob for IETF ML-DSA-44 seed
            byte[] pqDsaSeedBlob = Convert.FromBase64String("RFNTUwYAAAAgAAAANAA0AAAAAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
            CngProperty mldsaBlob = new CngProperty(
                CngKeyBlobFormat.PQDsaPrivateSeedBlob.ToString(),
                pqDsaSeedBlob,
                CngPropertyOptions.None);
            creationParameters.Parameters.Add(mldsaBlob);

            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];

            using (CngKey ephemeralCngKey = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParameters))
            {
                using (MLDsaCng mldsaCng = new MLDsaCng(ephemeralCngKey))
                {
                    using (X509Certificate2 unpairedCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
                    using (X509Certificate2 cert = unpairedCert.CopyWithPrivateKey(mldsaCng))
                    using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
                    {
                        mldsa.SignData("test"u8, signature);
                        Assert.True(mldsaCng.VerifyData("test"u8, signature));
                    }

                    // Run a few iterations to catch nondeterministic use-after-dispose issues
                    for (int i = 0; i < 5; i++)
                    {
                        using (X509Certificate2 unpairedCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
                        using (X509Certificate2 cert = unpairedCert.CopyWithPrivateKey(mldsaCng))
                        {
                        }
                    }

                    mldsaCng.SignData("test"u8, signature);
                }

                // Some certs have disposed, did they delete the key?
                using (MLDsaCng mldsa = new MLDsaCng(ephemeralCngKey))
                {
                    mldsa.SignData("test"u8, signature);
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
            using (SlhDsa? privateKey = SlhDsa.ImportSlhDsaPrivateKey(SlhDsaAlgorithm.SlhDsaSha2_128s, SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue))
                return cert.CopyWithPrivateKey(privateKey);
        }

        private static X509Certificate2 LoadMLDsaIetfCertificateWithPrivateKey()
        {
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsa? privateKey = MLDsa.ImportMLDsaPrivateSeed(MLDsaAlgorithm.MLDsa44, MLDsaTestsData.IetfMLDsa44.PrivateSeed))
                return cert.CopyWithPrivateKey(privateKey);
        }
    }
}
