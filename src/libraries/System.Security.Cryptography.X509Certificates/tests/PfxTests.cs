// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class PfxTests
    {
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
                Assert.IsAssignableFrom(typeof(RSA), alg);
                VerifyPrivateKey((RSA)alg);

                // Currently unable to set PrivateKey
                if (!PlatformDetection.IsFullFramework)
                {
                    Assert.Throws<PlatformNotSupportedException>(() => c.PrivateKey = null);
                    Assert.Throws<PlatformNotSupportedException>(() => c.PrivateKey = alg);
                }
            }
        }

        private static void VerifyPrivateKey(RSA rsa)
        {
            byte[] hash = new byte[20];
            byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            Assert.Equal(TestData.PfxSha1Empty_ExpectedSig, sig);
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ExportWithPrivateKey(X509KeyStorageFlags keyStorageFlags)
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable | keyStorageFlags))
            {
                const string password = "NotVerySecret";

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
            using (var cert = new X509Certificate2(TestData.ECDsaP256_DigitalSignature_Pfx_Windows, "Test", Cert.EphemeralIfPossible))
            using (var pubOnly = new X509Certificate2(cert.RawData))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                Assert.Throws<NotSupportedException>(() => cert.PrivateKey);

                Assert.False(pubOnly.HasPrivateKey, "pubOnly.HasPrivateKey");
                Assert.Null(pubOnly.PrivateKey);

                // Currently unable to set PrivateKey
                if (!PlatformDetection.IsFullFramework)
                {
                    Assert.Throws<PlatformNotSupportedException>(() => cert.PrivateKey = null);
                }

                using (var privKey = cert.GetECDsaPrivateKey())
                {
                    Assert.ThrowsAny<NotSupportedException>(() => cert.PrivateKey = privKey);
                    Assert.ThrowsAny<NotSupportedException>(() => pubOnly.PrivateKey = privKey);
                }
            }
        }

#if !NO_DSA_AVAILABLE
        [Fact]
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
#endif

        private static void Verify_ECDsaPrivateKey_WindowsPfx(ECDsa ecdsa)
        {
            Assert.NotNull(ecdsa);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AssertEccAlgorithm(ecdsa, "ECDSA_P256");
            }
        }

        [Theory, MemberData(nameof(BrainpoolCurvesPfx))]
        public static void ReadECDsaPrivateKey_BrainpoolP160r1_Pfx(byte[] pfxData)
        {
            try
            {
                using (var cert = new X509Certificate2(pfxData, TestData.PfxDataPassword))
                {
                    using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
                    {
                        Assert.NotNull(ecdsa);

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            AssertEccAlgorithm(ecdsa, "ECDH");
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                // Windows 7, Windows 8, Ubuntu 14, CentOS, macOS can fail. Verify known good platforms don't fail.
                Assert.False(PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10, "Is Windows 10");
                Assert.False(PlatformDetection.IsUbuntu && !PlatformDetection.IsUbuntu1404, "Is Ubuntu 16.04 or up");
            }
        }

        [Theory]
        [MemberData(nameof(StorageFlags))]
        public static void ReadECDsaPrivateKey_OpenSslPfx(X509KeyStorageFlags keyStorageFlags)
        {
            using (var cert = new X509Certificate2(TestData.ECDsaP256_DigitalSignature_Pfx_OpenSsl, "Test", keyStorageFlags))
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                Assert.NotNull(ecdsa);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

#if !NO_DSA_AVAILABLE
        [Fact]
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
#endif

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

        // Keep the ECDsaCng-ness contained within this helper method so that it doesn't trigger a
        // FileNotFoundException on Unix.
        private static void AssertEccAlgorithm(ECDsa ecdsa, string algorithmId)
        {
            ECDsaCng cng = ecdsa as ECDsaCng;

            if (cng != null)
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
    }
}
