// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.SLHDsa.Tests;
using System.Security.Cryptography.Tests;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
    public static class PfxTests
    {
        private const long UnspecifiedIterations = -2;
        private const long UnlimitedIterations = -1;
        internal const long DefaultIterations = 600_000;
        private const long DefaultIterationsWindows = 600_000;

        // We don't know for sure this is a correct Windows version when this support was added but
        // we know for a fact lower versions don't support it.
        public static bool Pkcs12PBES2Supported => !PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1703OrGreater;
        public static bool MLKemIsNotSupported => !MLKem.IsSupported;

        public static IEnumerable<object[]> BrainpoolCurvesPfx
        {
            get
            {
                yield return new object[] { TestData.ECDsabrainpoolP160r1_Pfx };
                yield return new object[] { TestData.ECDsabrainpoolP160r1_Explicit_Pfx };
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void TestConstructor(X509KeyStorageFlags keyStorageFlags)
        {
            using (var c = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            {
                byte[] expectedThumbprint = "71cb4e2b02738ad44f8b382c93bd17ba665f9914".HexToByteArray();

                string subject = c.Subject;
                Assert.Equal("CN=MyName", subject);
                byte[] thumbPrint = c.GetCertHash();
                Assert.Equal(expectedThumbprint, thumbPrint);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void TestConstructor_SecureString(X509KeyStorageFlags keyStorageFlags)
        {
            using (SecureString password = TestData.CreatePfxDataPasswordSecureString())
            using (var c = new X509Certificate2(TestData.PfxData, password, keyStorageFlags))
            {
                byte[] expectedThumbprint = "71cb4e2b02738ad44f8b382c93bd17ba665f9914".HexToByteArray();

                string subject = c.Subject;
                Assert.Equal("CN=MyName", subject);
                byte[] thumbPrint = c.GetCertHash();
                Assert.Equal(expectedThumbprint, thumbPrint);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void EnsurePrivateKeyPreferred(X509KeyStorageFlags keyStorageFlags)
        {
            using (var cert = new X509Certificate2(TestData.ChainPfxBytes, TestData.ChainPfxPassword, keyStorageFlags))
            {
                // While checking cert.HasPrivateKey first is most matching of the test description, asserting
                // on the certificate's simple name will provide a more diagnosable failure.
                Assert.Equal("test.local", cert.GetNameInfo(X509NameType.SimpleName, false));
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void TestRawData(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] expectedRawData = (
                "308201e530820152a0030201020210d5b5bc1c458a558845" +
                "bff51cb4dff31c300906052b0e03021d05003011310f300d" +
                "060355040313064d794e616d65301e170d31303034303130" +
                "38303030305a170d3131303430313038303030305a301131" +
                "0f300d060355040313064d794e616d6530819f300d06092a" +
                "864886f70d010101050003818d0030818902818100b11e30" +
                "ea87424a371e30227e933ce6be0e65ff1c189d0d888ec8ff" +
                "13aa7b42b68056128322b21f2b6976609b62b6bc4cf2e55f" +
                "f5ae64e9b68c78a3c2dacc916a1bc7322dd353b32898675c" +
                "fb5b298b176d978b1f12313e3d865bc53465a11cca106870" +
                "a4b5d50a2c410938240e92b64902baea23eb093d9599e9e3" +
                "72e48336730203010001a346304430420603551d01043b30" +
                "39801024859ebf125e76af3f0d7979b4ac7a96a113301131" +
                "0f300d060355040313064d794e616d658210d5b5bc1c458a" +
                "558845bff51cb4dff31c300906052b0e03021d0500038181" +
                "009bf6e2cf830ed485b86d6b9e8dffdcd65efc7ec145cb93" +
                "48923710666791fcfa3ab59d689ffd7234b7872611c5c23e" +
                "5e0714531abadb5de492d2c736e1c929e648a65cc9eb63cd" +
                "84e57b5909dd5ddf5dbbba4a6498b9ca225b6e368b94913b" +
                "fc24de6b2bd9a26b192b957304b89531e902ffc91b54b237" +
                "bb228be8afcda26476").HexToByteArray();

            using (var c = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            {
                byte[] rawData = c.RawData;
                Assert.Equal(expectedRawData, rawData);
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void TestPrivateKey(X509KeyStorageFlags keyStorageFlags)
        {
            using (var c = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, keyStorageFlags))
            {
                bool hasPrivateKey = c.HasPrivateKey;
                Assert.True(hasPrivateKey);

                using (RSA rsa = c.GetRSAPrivateKey())
                {
                    VerifyPrivateKey(rsa);
                }
            }
        }

        [Fact]
        public static void TestPrivateKeyProperty()
        {
            using (var c = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, Cert.EphemeralIfPossible))
            {
                bool hasPrivateKey = c.HasPrivateKey;
                Assert.True(hasPrivateKey);

                AsymmetricAlgorithm alg = c.PrivateKey;
                Assert.NotNull(alg);
                Assert.Same(alg, c.PrivateKey);
                Assert.IsAssignableFrom<RSA>(alg);
                VerifyPrivateKey((RSA)alg);

                // Currently unable to set PrivateKey
                Assert.Throws<PlatformNotSupportedException>(() => c.PrivateKey = null);
                Assert.Throws<PlatformNotSupportedException>(() => c.PrivateKey = alg);
            }
        }

        private static void VerifyPrivateKey(RSA rsa)
        {
            byte[] hash = new byte[SHA256.HashSizeInBytes];
            byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.Equal(TestData.PfxSha256Empty_ExpectedSig, sig);
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ExportWithPrivateKey(X509KeyStorageFlags keyStorageFlags)
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable | keyStorageFlags))
            {
                const string password = "Placeholder";

                byte[] pkcs12 = cert.Export(X509ContentType.Pkcs12, password);

                using (var certFromPfx = new X509Certificate2(pkcs12, password, keyStorageFlags))
                {
                    Assert.True(certFromPfx.HasPrivateKey);
                    Assert.Equal(cert, certFromPfx);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ReadECDsaPrivateKey_WindowsPfx(X509KeyStorageFlags keyStorageFlags)
        {
            // [SuppressMessage("Microsoft.Security", "CSCAN0220.DefaultPasswordContexts", Justification="Legacy Test Data")]
            using (var cert = new X509Certificate2(TestData.ECDsaP256_DigitalSignature_Pfx_Windows, "Test", keyStorageFlags))
            {
                using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
                {
                    Verify_ECDsaPrivateKey_WindowsPfx(ecdsa);
                }
            }
        }

        [Fact]
        public static void ECDsaPrivateKeyProperty_WindowsPfx()
        {
            // [SuppressMessage("Microsoft.Security", "CSCAN0220.DefaultPasswordContexts", Justification="Legacy Test Data")]
            using (var cert = new X509Certificate2(TestData.ECDsaP256_DigitalSignature_Pfx_Windows, "Test", Cert.EphemeralIfPossible))
            using (var pubOnly = new X509Certificate2(cert.RawData))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.Throws<NotSupportedException>(() => cert.PrivateKey);

                Assert.False(pubOnly.HasPrivateKey, "pubOnly.HasPrivateKey");
                Assert.Null(pubOnly.PrivateKey);

                // Currently unable to set PrivateKey
                Assert.Throws<PlatformNotSupportedException>(() => cert.PrivateKey = null);

                using (var privKey = cert.GetECDsaPrivateKey())
                {
                    Assert.ThrowsAny<NotSupportedException>(() => cert.PrivateKey = privKey);
                    Assert.ThrowsAny<NotSupportedException>(() => pubOnly.PrivateKey = privKey);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ReadECDHPrivateKey_WindowsPfx(X509KeyStorageFlags keyStorageFlags)
        {
            // [SuppressMessage("Microsoft.Security", "CSCAN0220.DefaultPasswordContexts", Justification="Legacy Test Data")]
            using (var cert = new X509Certificate2(TestData.EcDhP256_KeyAgree_Pfx_Windows, "test", keyStorageFlags))
            {
                using (ECDiffieHellman ecdh = cert.GetECDiffieHellmanPrivateKey())
                {
                    Verify_ECDHPrivateKey_WindowsPfx(ecdh);
                }
            }
        }

        [Fact]
        public static void ECDHPrivateKeyProperty_WindowsPfx()
        {
            // [SuppressMessage("Microsoft.Security", "CSCAN0220.DefaultPasswordContexts", Justification="Legacy Test Data")]
            using (var cert = new X509Certificate2(TestData.EcDhP256_KeyAgree_Pfx_Windows, "test", Cert.EphemeralIfPossible))
            using (var pubOnly = new X509Certificate2(cert.RawData))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.Throws<NotSupportedException>(() => cert.PrivateKey);

                Assert.False(pubOnly.HasPrivateKey, "pubOnly.HasPrivateKey");
                Assert.Null(pubOnly.PrivateKey);

                // Currently unable to set PrivateKey
                Assert.Throws<PlatformNotSupportedException>(() => cert.PrivateKey = null);

                using (ECDiffieHellman privKey = cert.GetECDiffieHellmanPrivateKey())
                {
                    Assert.NotNull(privKey);
                    Assert.ThrowsAny<NotSupportedException>(() => cert.PrivateKey = privKey);
                    Assert.ThrowsAny<NotSupportedException>(() => pubOnly.PrivateKey = privKey);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Only windows cares about the key usage attribute in the PKCS12
        public static void ECDHPrivateKey_PfxKeyIsEcdsaConstrained()
        {
            // [SuppressMessage("Microsoft.Security", "CSCAN0220.DefaultPasswordContexts", Justification="Legacy Test Data")]
            using (X509Certificate2 cert = new X509Certificate2(TestData.ECDsaP256_DigitalSignature_Pfx_Windows, "Test"))
            {
                    Assert.Null(cert.GetECDiffieHellmanPrivateKey());
                    Assert.NotNull(cert.GetECDiffieHellmanPublicKey());
                    Assert.NotNull(cert.GetECDsaPrivateKey());
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void DsaPrivateKeyProperty()
        {
            using (var cert = new X509Certificate2(TestData.Dsa1024Pfx, TestData.Dsa1024PfxPassword, Cert.EphemeralIfPossible))
            {
                AsymmetricAlgorithm alg = cert.PrivateKey;
                Assert.NotNull(alg);
                Assert.Same(alg, cert.PrivateKey);
                Assert.IsAssignableFrom<DSA>(alg);

                DSA dsa = (DSA)alg;
                byte[] data = { 1, 2, 3, 4, 5 };
                byte[] sig = dsa.SignData(data, HashAlgorithmName.SHA1);

                Assert.True(dsa.VerifyData(data, sig, HashAlgorithmName.SHA1), "Key verifies signature");

                data[0] ^= 0xFF;

                Assert.False(dsa.VerifyData(data, sig, HashAlgorithmName.SHA1), "Key verifies tampered data signature");
            }
        }

        private static void Verify_ECDsaPrivateKey_WindowsPfx(ECDsa ecdsa)
        {
            Assert.NotNull(ecdsa);

            if (OperatingSystem.IsWindows())
            {
                AssertEccAlgorithm(ecdsa, "ECDSA_P256");
            }
        }

        private static void Verify_ECDHPrivateKey_WindowsPfx(ECDiffieHellman ecdh)
        {
            Assert.NotNull(ecdh);

            if (OperatingSystem.IsWindows())
            {
                AssertEccAlgorithm(ecdh, "ECDH_P256");
            }
        }

        [Theory, MemberData(nameof(BrainpoolCurvesPfx))]
        public static void ReadECDsaPrivateKey_BrainpoolP160r1_Pfx(byte[] pfxData)
        {
            static bool IsKnownGoodPlatform() => PlatformDetection.IsWindows10OrLater || PlatformDetection.IsUbuntu;

            try
            {
                using (var cert = new X509Certificate2(pfxData, TestData.PfxDataPassword))
                {
                    using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
                    {
                        Assert.NotNull(ecdsa);

                        if (OperatingSystem.IsWindows())
                        {
                            AssertEccAlgorithm(ecdsa, "ECDH");
                        }
                    }
                }
            }
            catch (CryptographicException) when (!IsKnownGoodPlatform())
            {
                // Windows 7, Windows 8, CentOS, macOS can fail. If the platform is a known good, let the exception
                // through since it should not fail.
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ReadECDsaPrivateKey_OpenSslPfx(X509KeyStorageFlags keyStorageFlags)
        {
            // [SuppressMessage("Microsoft.Security", "CSCAN0220.DefaultPasswordContexts", Justification="Legacy Test Data")]
            using (var cert = new X509Certificate2(TestData.ECDsaP256_DigitalSignature_Pfx_OpenSsl, "Test", keyStorageFlags))
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                Assert.NotNull(ecdsa);

                if (OperatingSystem.IsWindows())
                {
                    // If Windows were to start detecting this case as ECDSA that wouldn't be bad,
                    // but this assert is the only proof that this certificate was made with OpenSSL.
                    //
                    // Windows ECDSA PFX files contain metadata in the private key keybag which identify it
                    // to Windows as ECDSA.  OpenSSL doesn't have anywhere to persist that data when
                    // extracting it to the key PEM file, and so no longer has it when putting the PFX
                    // together.  But, it also wouldn't have had the Windows-specific metadata when the
                    // key was generated on the OpenSSL side in the first place.
                    //
                    // So, again, it's not important that Windows "mis-detects" this as ECDH.  What's
                    // important is that we were able to create an ECDsa object from it.
                    AssertEccAlgorithm(ecdsa, "ECDH_P256");
                }
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void ReadDSAPrivateKey()
        {
            byte[] data = { 1, 2, 3, 4, 5 };

            using (var cert = new X509Certificate2(TestData.Dsa1024Pfx, TestData.Dsa1024PfxPassword, Cert.EphemeralIfPossible))
            using (DSA privKey = cert.GetDSAPrivateKey())
            using (DSA pubKey = cert.GetDSAPublicKey())
            {
                // Stick to FIPS 186-2 (DSS-SHA1)
                byte[] signature = privKey.SignData(data, HashAlgorithmName.SHA1);

                Assert.True(pubKey.VerifyData(data, signature, HashAlgorithmName.SHA1), "pubKey verifies signed data");

                data[0] ^= 0xFF;
                Assert.False(pubKey.VerifyData(data, signature, HashAlgorithmName.SHA1), "pubKey verifies tampered data");

                // And verify that the public key isn't accidentally a private key.
                Assert.ThrowsAny<CryptographicException>(() => pubKey.SignData(data, HashAlgorithmName.SHA1));
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem512PrivateKey_Seed_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem512PrivateKeySeedPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem512, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem512PrivateKey_ExpandedKey_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem512PrivateKeyExpandedKeyPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem512, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                Assert.Throws<CryptographicException>(() => kem.ExportPrivateSeed());
                AssertExtensions.SequenceEqual(
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey,
                    kem.ExportDecapsulationKey());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem512PrivateKey_Both_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem512PrivateKeyBothPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem512, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
                AssertExtensions.SequenceEqual(
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey,
                    kem.ExportDecapsulationKey());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem768PrivateKey_Seed_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem768PrivateKeySeedPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem768, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem768PrivateKey_ExpandedKey_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem768PrivateKeyExpandedKeyPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem768, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                Assert.Throws<CryptographicException>(() => kem.ExportPrivateSeed());
                AssertExtensions.SequenceEqual(
                    MLKemTestData.IetfMlKem768PrivateKeyDecapsulationKey,
                    kem.ExportDecapsulationKey());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem768PrivateKey_Both_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem768PrivateKeyBothPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem768, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
                AssertExtensions.SequenceEqual(
                    MLKemTestData.IetfMlKem768PrivateKeyDecapsulationKey,
                    kem.ExportDecapsulationKey());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem1024PrivateKey_Seed_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem1024PrivateKeySeedPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem1024, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem1024PrivateKey_ExpandedKey_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem1024PrivateKeyExpandedKeyPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem1024, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                Assert.Throws<CryptographicException>(() => kem.ExportPrivateSeed());
                AssertExtensions.SequenceEqual(
                    MLKemTestData.IetfMlKem1024PrivateKeyDecapsulationKey,
                    kem.ExportDecapsulationKey());
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsPqcMLKemX509Supported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem1024PrivateKey_Both_Pfx(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLKemTestData.IetfMlKem1024PrivateKeyBothPfx;
            string pfxPassword = MLKemTestData.EncryptedPrivateKeyPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLKem kem = cert.GetMLKemPrivateKey())
            {
                Assert.NotNull(kem);
                Assert.Equal(MLKemAlgorithm.MLKem1024, kem.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
                AssertExtensions.SequenceEqual(
                    MLKemTestData.IetfMlKem1024PrivateKeyDecapsulationKey,
                    kem.ExportDecapsulationKey());
            }
        }

        [ConditionalTheory(nameof(MLKemIsNotSupported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLKem512PrivateKey_NotSupported(X509KeyStorageFlags keyStorageFlags)
        {
            const string PfxPassword = "PLACEHOLDER";
            // [SuppressMessage("Microsoft.Security", "CSCAN-GENERAL0060", Justification="False positive, this is a certificate for unit testing.")]
            byte[] pfxBytes = Convert.FromBase64String(@"
                MIIPbgIBAzCCDzQGCSqGSIb3DQEHAaCCDyUEgg8hMIIPHTCCDk8GCSqGSIb3DQEHBqCCDkAwgg48
                AgEAMIIONQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQMwDgQIuOL/cp44/ycCAgfQgIIOCBb45pj6
                GZvu+xvAdBLNywjAGc9qIpToR79uA07thLEZIhYvldyE13JgtT1qwL++wQHbYgGHmwKqHjjIbFLw
                yhaeoZRkwcALEw1o0t5eVM/k+GN5/uTgzMtaiSgQN+LZ/GqGAu9uJqfP1L75Js+rddY65Bf0hrIQ
                KEZckjIoFJj4gRX590YMyR+mVcxzMJ/IQ9Na9UunliKjGkdJeXUm+4eTyYV4vGI9Uzfb08dn+Vlx
                lyASLG0h6yZTjc9pl9HWQ3gjqwpGUJlRzwpSe5PjV5K1ZCk+QyWgwePwQErKu7/Y5QySIqTZcpAF
                gvFgjaUhWJES+/1KfH8EpQAlj+I+O2T00NhF0eS4nBF5Yk3w7UD8dII6Ubh9qM7t2YBhAmIh2c90
                ioqmouOexrnQzIlc3nEKnGzsH0XOK2fna/fqsVHTDl7+2YO9VWG7zzOZCcpNnoJzCD7yAz5RmwMD
                xoP7Lfzg8wZxMMo88deTR+ZJOMBS7nqElmXoQofQmRGPRQBg5IloFXml5Jyny13uYXu20eQb2Qi/
                ygxyPTPZWQr3dFzns8c7Ef9fIjfbxio0qjVHCH77eswR7J12Ys7ypc5gmfgIl3KGEEA5ht/fSkSv
                cUsUWOTldBYpT5rcJ3GYR2Tsvoq2l315ZlDuUTmYZIJLGVWNpNxs199UI6mzopMLY1rmF69jl53v
                XLufEDfl3jRaSSGyhuTpmUnJODdEPJyeLpWOOrIFbGAN8SF9kjwmKHgjTFM+vFzLtvt8NQ/W1i0Y
                G8DUU3KFVHNJ4qsp8tFo3X9+PPPxWOb0epUpXLm3HgQlmrzy18z1P2mJSDVCoo/4OBNZZlJ3cAru
                6/umR7cbvzGU2m/01m/QPeKIP9QHELPGO0ynGProHQDhQMXJ8l7XSEQzq6ccS6EeAA5R44b4olCk
                FRGln5+RPtA0/EuP1gq5dh70jHowwVBKMykXPZuCpuhqCt0/dynQhX9ar/c9Au9v9KiSM4/NDdkm
                SZcEjbUqJw5Y95Btr+YpxgOgeZPypRny8ptZM2pNww5VmnFFNCT+hmPsTBTSokPE0BMwjgoVEimP
                VETsSimUT721ZL8RxBFYWF3RPA7sUzZ/WADF+BWv0u9uhkTFQREoMfPy14gsnpBVHT1n8ANuUKGI
                W3JDjoG2h0wb8LjtKbRHdC/xwhlM8SMZyk7gVimZxPKnrWaQq00WhyfMyChtwulm5vM3ByZK2cqV
                YBz//A3/O5INoLiM+9ZFE/UCzXxr3iA2oX5goobGDvui8C+VqCCkgchAhNBNVZvt24eaMGes33hb
                ZhrPyuyR0OtEQFuGIICmH6Py8UdXukNrZvO/jA1RZtEcsU8Hz5WWVpJG5nv4+4+IAYIIlmsUaZPG
                HqVOv/0S6ByTENieVxjdGZSdQnvddvjYJ6cntWokmsGK311v8ml7634CQgzviIGPOSkOmHlW2UTR
                QoroGBLAuGffem//K+ahnnvst086X+otHCauvcJyTD/O2hxOh1omlBUKs1Da25G9soquwwD3cnnH
                yfQMUZ7ZohbDeDRiyuAHTxs/42N+Wcg2xzoaefHb/LouGEzl2lPgncl7cRiZSi/mq2MNp6S4g2bd
                /o3ITbvhLECte2EESTF20IjaGf85eeXW0oBc9fsmitIF7N5nLCikBYBwTynEyQVc4qEUU8w2hgGz
                0QJdF6+V0n2qpwaX6FxSe37cE4snhigD1OJ0aogO8THeCSXCE3G23/fZTAwVf5X5SSezAK6z/mn0
                vkUyeRUcCXorcoI7KO3hy5rZ7QJjqYKsHQTh6MI2r7gMhmyAa0/nb7KV6Bo1zOMfm4SWMMqrwCze
                18TQVF6S2eRiD8CVJjE1saqgLDSXnQqtbybl57mp4t/4nkicyIsnSuv+cMLfRqMx0FaH/bW+UxqO
                Aa9QlIM1VyCFMH/RlTwa8WxKwFzS9lEBQCwfwnx6az8AKc1+EmK4JhBh2UFsvLhmgDUxr+3MOccD
                803aP37fBSOGi+E4zqdTQTmLJNRylwqNi3Hcnliz/RS6lcDLms4alsYL8+8ZsOIQynOkQTIJi6kc
                x+MpCM4wlLmQLr6fUctcgAml61eYDAWYDgkQkioKsbLOSSWXHpeFGoXAQT4LCh5WY6IGWjTbUMRt
                jR6fyHvIEIvV7E2gSMo2qdDYsK6Rp8Y4/319E1IF1tcCSLnvuiZUHtqoMy4wUDhbnGBD7CeM9etQ
                rDNcqEXmOHX7exKxsd2+QqtH7fovGqA678EDAP9LTePYpEZy3BGTEFATIib4KXmTfJ+tAYpFCMU3
                /b8B6kiVlutc2BUZFnK8bfQB6FcjFEyq5L0cv+GUJzj+kWNkc2Y+VtpjIg/bFZQMovyoMIJbEWXV
                srEH83EJ85mX3NikeCfKYtDN4bQ3VY01YMPS7H1Npl9zpnRPyBXwzomeaxGMPgNSoqfbzNjNN+ZM
                Q22Q0LkVzjLjCft00lAl54VvyXY+GGCKLeVCGsB6sLC/UpNnNuJtSGAHNRefFsDTlJGIaeBCKII9
                w25a/JTIWnpmP/Hm88yJLDCCUI9srHM70TptllViRDDI1vHfhlDHJaQJoccLJnfTiqDqTVho+0fO
                +U/oN2udwaVnKA732w1UnNaOuS2nok3+1QZ9fAmqXoVJyx6u8UnQyugY93TlqTQx5Hbgtqy4QX6a
                yxa5bnpXgvZR93QCYfXUcRDuJub6lH3wsPEAME7UlAE+BkCFKJ4KxbaU1ZVtSeUmFi4t8Q0iKIYi
                K+HhENH0mVqBHV11ovI/KcWNR1OPY13NGLXOyp9k2J0GCt5M/Tocm5pyeqQK769nrEmD9w89C4jI
                SCT6aoMHlsM0lua3VmDeS1FsD+QeiwSa2shjEiW0809IwI/5J4LdXjjs6saVxgIZMtEGOdD5XtGZ
                m26wEaQ1toYKgW88YTaWJLWbiGGF1hYmDiR9OPrTMvNiDjRPgs6wANJP29t25KuEAqeYxyUHItM+
                omlki7yrB1OgZSy2+SNT8CmLO0XAey9wVUaynK7jqtyMjwW4T33AhjdLERDXaqd+2tqUILOiDspw
                85RTZfPTjVBQrvRZ0dSCE7YqbWutUQFiB7WFGDk5C1XguytQRmdB+50qoFZ+UFYz7+RDteupZNFe
                oA/3dZXXCm7XovcS0EZDuZB9svQOcOIyz1FsuPF06hViBYKFORPbWMV18h/sb9ocjWmKf8e8VoHD
                MKySvGg0nrCa/eDYDAivfj9bOT/XBfJLPfV4Awj6h5WjcH9Y50Y0z/9u1/ZX8lIzpgOykNmntzpX
                mnGmPiC9SzhkUfGm21hwQuIZLk+bJ6PL+jjnM7jRyRpttlyBQLNoSzV1l0TUlggOkX+OaNW5oyoG
                FyWnh2/6Je+aU9fB7qPyeZNnDp3kPtVoy3yq3Fr6Ja8xcXu1FbrosuQE59iMeoMd7NVZhLqjDjW+
                +4LYzcJrAFb8t8RlC1R8Rhgxs3l+uQLwUB7Jms/vGxTBvKVpjCWx4YZktBY/dFBsQRk3ZrsuhMLK
                Oxrav6wyZM+QSMdBZmIaEE9R3z+1QUt/89amR2JrVKbl/GqgtD7/lQh1RrzFcRiByCtVhPW+rrfx
                LHHCTPMrz3SzYX6N8HvT1NXCQXHbtA4Ia4jnY2M57C3iuh51Q8VWMizRACaqRQaO6gFjUEXRIu3G
                S7DO83P1jgsfu/4Dmsfz7TJ5nOqsOcKzaZiqqP30OCa2P3RJ61Fzd2gFMatT1xbdGDyK+B00xbiD
                AScB36JaKjOCFbh4ogwGLQy/lKljgUAaCp/bGbryINeJLTDUqoTXJCIP0j6sXZhWF2Bnv9Et2FE9
                I/zb0gznyM5YJU5wRXiRe5zQvWUlJLa71P7oQm0ryLvLrL4pRXjSpoD0d3xqX8RxRVgXWntmoI2A
                X5xKn5i5to5i/86J5fgAmTN44Vf9MBpuoGL3cYUzx2i6BO2Zz4qvZwK06roLo1HN4EM8paRSXc71
                Ln35pKxEO286r9gHO2f3UMF5kCYC8EW8D9OUvGedZb/7B9aVhN9bc1jHENwu/ANxvd+2rr2r4LOs
                JCiI64ILY8Uxh8szDCoIAU777f54B0+8m/CiZsLLpgqW91XvDh3XYGuSZ/HUUFCzXqbBE/hH4DBk
                obJg0GmU9u7WOZuE5WuTxlUUnpiaawfGdWfVSG5VIv4VhI2X3H88P4GA4FoBH5fbarMd65NVTHdV
                d2RYxORqGfWpY8QAtIT9+pH+S1aL/uDLo2mRECh4fSicu+YdRRjfcrBWwosr72JRNNLBPeLJ9U1W
                DlOFZGaZmI2/hXjIgJeGXMqVN0PmUFrxQibJtb7QApzVEbpLGsW6IJG7ECuPtTxJ5nJkNH0itBiL
                X/QW3MOyYG/0R7TzP2WZpn4vHSYIhS/imficz0qc9f25FhKI74aYF/ITbVJJADdjCssLFgYL2V6A
                VpQMT/4taNc4UQEurY0s82hLdca0Sv6Sj7WumVQJJzGTB1udxGc432uii6rmydut/1Feqkj3FMWI
                EzIIIaT5udGFCUybMJOPChj3sn2oIjSmW88mJltkROMX9+K0oYyXZnFRzQESz2Ng1tAxJP2od6XN
                QIAKDyx71bg2e2EdTpd6liTYq8x2O+7sqfHPyXXAkIbZuYf94NcArPnflcr2GfQRnoiJGcfv6Grd
                hU4LB+J3A7PvIpdcTQip3SaomwwN1XzlTp9uqWi4YOriEsCFf4X+s77lI9qZUBCcXZ0xIx6GpnPf
                DYow3xoSXP/dwZd+tzTjWzB0cLGhf0CC5Hl+4brZ3mRhy2FDGqlvNEiD213LsPJ76rdis0wwgccG
                CSqGSIb3DQEHAaCBuQSBtjCBszCBsAYLKoZIhvcNAQwKAQKgejB4MBwGCiqGSIb3DQEMAQMwDgQI
                Kf2rZ5CMXzECAgfQBFieWmsNihgo16StxXMs21eEnLenWStP2ZBGweIz8nOrqDnmX9TDaN8Scaf9
                zVqXffa8Oj54MVG/ciqCAoK59kg4pfryovpiXXFbuMLAZlcXaUPRDjNyeb4tMSUwIwYJKoZIhvcN
                AQkVMRYEFAoeq6T+1m3SxQcSi9MLIHgD+izRMDEwITAJBgUrDgMCGgUABBSkzDq8UAiqd5YK7p1i
                YwIgZfxAsAQIG3eE/Gomu/ECAgfQ");

            // Windows when using non-ephemeral delays throwing no private key and instead acts as it the
            // keyset does not exist. Exporting it again to PFX forces Windows to reconcile the fact the key
            // didn't actually load.
            if (PlatformDetection.IsWindows && keyStorageFlags != X509KeyStorageFlags.EphemeralKeySet)
            {
                using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, PfxPassword, keyStorageFlags))
                {
                    Assert.Throws<CryptographicException>(
                        () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, PfxPassword));
                }

                using (X509Certificate2 cert = new(pfxBytes, PfxPassword, keyStorageFlags))
                {
                    Assert.Throws<CryptographicException>(
                        () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, PfxPassword));
                }
            }
            else
            {
                Assert.Throws<CryptographicException>(
                    () => X509CertificateLoader.LoadPkcs12(pfxBytes, PfxPassword, keyStorageFlags));

                Assert.Throws<CryptographicException>(
                    () => new X509Certificate2(pfxBytes, PfxPassword, keyStorageFlags));
            }
        }

        public static IEnumerable<object[]> ReadMLDsa_Pfx_Ietf_Data =>
            from storageFlagWrapped in StorageFlags
            from storageFlag in storageFlagWrapped
            from ietfVectorWrapped in MLDsaTestsData.IetfMLDsaAlgorithms
            from ietfVector in ietfVectorWrapped
            select new object[] { storageFlag, ietfVector };

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SupportsDraft10Pkcs8))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/116463", TestPlatforms.Windows)]
        [MemberData(nameof(ReadMLDsa_Pfx_Ietf_Data))]
        public static void ReadMLDsa512PrivateKey_Seed_Pfx(X509KeyStorageFlags keyStorageFlags, MLDsaKeyInfo info)
        {
            byte[] pfxBytes = info.Pfx_Seed;
            string pfxPassword = info.EncryptionPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
            {
                Assert.NotNull(mldsa);
                Assert.Equal(info.Algorithm, mldsa.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);

                byte[] seed = new byte[info.PrivateSeed.Length];
                Assert.Equal(seed.Length, mldsa.ExportMLDsaPrivateSeed(seed));
                AssertExtensions.SequenceEqual(info.PrivateSeed, seed);
            }
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SupportsDraft10Pkcs8))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/116463", TestPlatforms.Windows)]
        [MemberData(nameof(ReadMLDsa_Pfx_Ietf_Data))]
        public static void ReadMLDsa512PrivateKey_ExpandedKey_Pfx(X509KeyStorageFlags keyStorageFlags, MLDsaKeyInfo info)
        {
            byte[] pfxBytes = info.Pfx_Expanded;
            string pfxPassword = info.EncryptionPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
            {
                Assert.NotNull(mldsa);
                Assert.Equal(info.Algorithm, mldsa.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);

                byte[] secretKey = new byte[info.SecretKey.Length];
                Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed(secretKey));
                Assert.Equal(secretKey.Length, mldsa.ExportMLDsaSecretKey(secretKey));
                AssertExtensions.SequenceEqual(info.SecretKey, secretKey);
            }
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SupportsDraft10Pkcs8))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/116463", TestPlatforms.Windows)]
        [MemberData(nameof(ReadMLDsa_Pfx_Ietf_Data))]
        public static void ReadMLDsa512PrivateKey_Both_Pfx(X509KeyStorageFlags keyStorageFlags, MLDsaKeyInfo info)
        {
            byte[] pfxBytes = info.Pfx_Both;
            string pfxPassword = info.EncryptionPassword;

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
            {
                Assert.NotNull(mldsa);
                Assert.Equal(info.Algorithm, mldsa.Algorithm);
                Assert.Equal("CN=LAMPS WG, O=IETF", cert.Subject);

                byte[] buffer = new byte[info.SecretKey.Length];
                Assert.Equal(info.PrivateSeed.Length, mldsa.ExportMLDsaPrivateSeed(buffer));
                AssertExtensions.SequenceEqual(info.PrivateSeed.AsSpan(), buffer.AsSpan(0, info.PrivateSeed.Length));

                Assert.Equal(info.SecretKey.Length, mldsa.ExportMLDsaSecretKey(buffer));
                AssertExtensions.SequenceEqual(
                    info.SecretKey,
                    buffer);
            }
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.MLDsaIsNotSupported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadMLDsa_Pfx_Ietf_NotSupported(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = MLDsaTestsData.IetfMLDsa_Pfx_Pbes1;
            string pfxPassword = "PLACEHOLDER";

            // Windows when using non-ephemeral delays throwing no private key and instead acts as it the
            // keyset does not exist. Exporting it again to PFX forces Windows to reconcile the fact the key
            // didn't actually load.
            if (PlatformDetection.IsWindows && keyStorageFlags != X509KeyStorageFlags.EphemeralKeySet)
            {
                using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
                {
                    Assert.Throws<CryptographicException>(
                        () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, pfxPassword));
                }

                using (X509Certificate2 cert = new(pfxBytes, pfxPassword, keyStorageFlags))
                {
                    Assert.Throws<CryptographicException>(
                        () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, pfxPassword));
                }
            }
            else
            {
                Assert.Throws<CryptographicException>(
                    () => X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags));

                Assert.Throws<CryptographicException>(
                    () => new X509Certificate2(pfxBytes, pfxPassword, keyStorageFlags));
            }
        }

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadSlhDsa_Pfx_Ietf(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = SlhDsaTestData.IetfSlhDsaSha2_128sCertificatePfx;
            string pfxPassword = "PLACEHOLDER";

            using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
            using (SlhDsa slhDsa = cert.GetSlhDsaPrivateKey())
            {
                Assert.NotNull(slhDsa);
                Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);
                // Note this display string is reversed from the one in the IETF example but equivalent.
                Assert.Equal("O=Bogus SLH-DSA-SHA2-128s CA, L=Paris, C=FR", cert.Subject);
                AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, slhDsa.ExportSlhDsaSecretKey());
            }
        }

        [ConditionalTheory(typeof(SlhDsaTestHelpers), nameof(SlhDsaTestHelpers.SlhDsaIsNotSupported))]
        [MemberData(nameof(StorageFlags))]
        public static void ReadSlhDsa_Pfx_Ietf_NotSupported(X509KeyStorageFlags keyStorageFlags)
        {
            byte[] pfxBytes = SlhDsaTestData.IetfSlhDsaSha2_128sCertificatePfx_Pbes1;
            string pfxPassword = "PLACEHOLDER";

            // Windows when using non-ephemeral delays throwing no private key and instead acts as it the
            // keyset does not exist. Exporting it again to PFX forces Windows to reconcile the fact the key
            // didn't actually load.
            if (PlatformDetection.IsWindows && keyStorageFlags != X509KeyStorageFlags.EphemeralKeySet)
            {
                using (X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags))
                {
                    Assert.Throws<CryptographicException>(
                        () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, pfxPassword));
                }

                using (X509Certificate2 cert = new(pfxBytes, pfxPassword, keyStorageFlags))
                {
                    Assert.Throws<CryptographicException>(
                        () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, pfxPassword));
                }
            }
            else
            {
                Assert.Throws<CryptographicException>(
                    () => X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword, keyStorageFlags));

                Assert.Throws<CryptographicException>(
                    () => new X509Certificate2(pfxBytes, pfxPassword, keyStorageFlags));
            }
        }

#if !NO_EPHEMERALKEYSET_AVAILABLE
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void EphemeralImport_HasNoKeyName()
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.EphemeralKeySet))
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                Assert.NotNull(rsa);

                // While RSACng is not a guaranteed answer, it's currently the answer and we'd have to
                // rewrite the rest of this test if it changed.
                RSACng rsaCng = rsa as RSACng;
                Assert.NotNull(rsaCng);

                CngKey key = rsaCng.Key;
                Assert.NotNull(key);

                Assert.True(key.IsEphemeral, "key.IsEphemeral");
                Assert.Null(key.KeyName);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void CollectionEphemeralImport_HasNoKeyName()
        {
            using (var importedCollection = Cert.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.EphemeralKeySet))
            {
                X509Certificate2 cert = importedCollection.Collection[0];

                using (RSA rsa = cert.GetRSAPrivateKey())
                {
                    Assert.NotNull(rsa);

                    // While RSACng is not a guaranteed answer, it's currently the answer and we'd have to
                    // rewrite the rest of this test if it changed.
                    RSACng rsaCng = rsa as RSACng;
                    Assert.NotNull(rsaCng);

                    CngKey key = rsaCng.Key;
                    Assert.NotNull(key);

                    Assert.True(key.IsEphemeral, "key.IsEphemeral");
                    Assert.Null(key.KeyName);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void PerphemeralImport_HasKeyName()
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                Assert.NotNull(rsa);

                // While RSACng is not a guaranteed answer, it's currently the answer and we'd have to
                // rewrite the rest of this test if it changed.
                RSACng rsaCng = rsa as RSACng;
                Assert.NotNull(rsaCng);

                CngKey key = rsaCng.Key;
                Assert.NotNull(key);

                Assert.False(key.IsEphemeral, "key.IsEphemeral");
                Assert.NotNull(key.KeyName);
            }
        }
#endif

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void CollectionPerphemeralImport_HasKeyName()
        {
            using (var importedCollection = Cert.Import(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.DefaultKeySet))
            {
                X509Certificate2 cert = importedCollection.Collection[0];

                using (RSA rsa = cert.GetRSAPrivateKey())
                {
                    Assert.NotNull(rsa);

                    // While RSACng is not a guaranteed answer, it's currently the answer and we'd have to
                    // rewrite the rest of this test if it changed.
                    RSACng rsaCng = rsa as RSACng;
                    Assert.NotNull(rsaCng);

                    CngKey key = rsaCng.Key;
                    Assert.NotNull(key);

                    Assert.False(key.IsEphemeral, "key.IsEphemeral");
                    Assert.NotNull(key.KeyName);
                }
            }
        }

        [Fact]
        public static void VerifyNamesWithDuplicateAttributes()
        {
            // This is the same as the Windows Attributes test for X509CertificateLoaderPkcs12Tests,
            // but using the legacy X509Certificate2 ctor, to test the settings for that set of
            // loader limits with respect to duplicates.

            X509Certificate2 cert = new X509Certificate2(TestData.DuplicateAttributesPfx, TestData.PlaceholderPw);

            using (cert)
            {
                Assert.Equal("Certificate 1", cert.GetNameInfo(X509NameType.SimpleName, false));
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");

                if (OperatingSystem.IsWindows())
                {
                    Assert.Equal("Microsoft Enhanced RSA and AES Cryptographic Provider", cert.FriendlyName);

                    using (RSA key = cert.GetRSAPrivateKey())
                    using (CngKey cngKey = Assert.IsType<RSACng>(key).Key)
                    {
                        Assert.Equal("Microsoft Enhanced RSA and AES Cryptographic Provider", cngKey.Provider.Provider);
                        Assert.True(cngKey.IsMachineKey, "cngKey.IsMachineKey");

                        // If keyname000 gets taken, we'll get a random key name on import.  What's important is that we
                        // don't pick the second entry: keyname001.
                        Assert.NotEqual("keyname001", cngKey.KeyName);
                    }
                }
            }
        }

        internal static bool IsPkcs12IterationCountAllowed(long iterationCount, long allowedIterations)
        {
            if (allowedIterations == UnlimitedIterations)
            {
                return true;
            }

            if (allowedIterations == UnspecifiedIterations)
            {
                allowedIterations = DefaultIterations;
            }

            Assert.True(allowedIterations >= 0);

            return iterationCount <= allowedIterations;
        }

        // This is a horrible way to produce SecureString. SecureString is deprecated and should not be used.
        // This is only reasonable because it is a test driver.
        internal static SecureString GetSecureString(string password)
        {
            if (password == null)
                return null;

            SecureString secureString = new SecureString();
            foreach (char c in password)
            {
                secureString.AppendChar(c);
            }

            return secureString;
        }

        // Keep the ECDsaCng-ness contained within this helper method so that it doesn't trigger a
        // FileNotFoundException on Unix.
        private static void AssertEccAlgorithm(ECDsa ecdsa, string algorithmId)
        {
            if (ecdsa is ECDsaCng cng)
            {
                Assert.Equal(algorithmId, cng.Key.Algorithm.Algorithm);
            }
        }

        // Keep the ECDiffieHellmanCng-ness contained within this helper method so that it doesn't trigger a
        // FileNotFoundException on Unix.
        private static void AssertEccAlgorithm(ECDiffieHellman ecdh, string algorithmId)
        {
            if (ecdh is ECDiffieHellmanCng cng)
            {
                Assert.Equal(algorithmId, cng.Key.Algorithm.Algorithm);
            }
        }

        public static IEnumerable<object[]> StorageFlags => CollectionImportTests.StorageFlags;

        private static X509Certificate2 Rewrap(this X509Certificate2 c)
        {
            X509Certificate2 newC = new X509Certificate2(c.Handle);
            c.Dispose();
            return newC;
        }

        internal delegate ulong GetIterationCountDelegate(ReadOnlySpan<byte> pkcs12, out int bytesConsumed);
    }
}
