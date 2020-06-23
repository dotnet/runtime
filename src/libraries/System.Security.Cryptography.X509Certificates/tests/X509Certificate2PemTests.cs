// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class X509Certificate2PemTests
    {
        [Fact]
        public static void CreateFromPem_CryptographicException_NoCertificate()
        {
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(default, default));
        }

        [Fact]
        public static void CreateFromPem_CryptographicException_MalformedCertificate()
        {
            const string CertContents = @"
-----BEGIN CERTIFICATE-----
MII
-----END CERTIFICATE-----
";
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(CertContents, default));
        }

        [Fact]
        public static void CreateFromPem_CryptographicException_InvalidKeyAlgorithm()
        {
            CryptographicException ce = Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(TestData.Ed25519Certificate, default));

            Assert.Contains("'1.3.101.112'", ce.Message);
        }

        [Fact]
        public static void CreateFromPem_CryptographicException_NoKey()
        {
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(TestData.RsaCertificate, default));
        }

        [Fact]
        public static void CreateFromPem_CryptographicException_MalformedKey()
        {
            const string CertContents = @"
-----BEGIN RSA PRIVATE KEY-----
MII
-----END RSA PRIVATE KEY-----
";
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(TestData.RsaCertificate, CertContents));
        }

        [Fact]
        public static void CreateFromPem_Rsa_Pkcs1_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, TestData.RsaPkcs1Key))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_Pkcs8_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, TestData.RsaPkcs8Key))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs8Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_Aggregate_Pkcs8_Success()
        {
            string pemAggregate = TestData.RsaCertificate + TestData.RsaPkcs8Key;
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(pemAggregate, pemAggregate))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs8Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_Aggregate_Pkcs1_Success()
        {
            string pemAggregate = TestData.RsaCertificate + TestData.RsaPkcs1Key;
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(pemAggregate, pemAggregate))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_LoadsFirstCertificate_Success()
        {
            string certAggregate = TestData.RsaCertificate + TestData.ECDsaCertificate;
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(certAggregate, TestData.RsaPkcs1Key))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_IgnoresNonMatchingAlgorithmKeys_Success()
        {
            string keyAggregate = TestData.ECDsaECPrivateKey + TestData.RsaPkcs1Key;
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, keyAggregate))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_IgnoresPkcs1PublicKey_Success()
        {
            string keyAggregate = TestData.RsaPkcs1PublicKey + TestData.RsaPkcs1Key;
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, keyAggregate))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_IgnoresPkcs8PublicKey_Success()
        {
            string keyAggregate = TestData.RsaPkcs8PublicKey + TestData.RsaPkcs1Key;
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, keyAggregate))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Rsa_KeyMismatch_Fail()
        {
            AssertExtensions.Throws<ArgumentException>("privateKey", () =>
                X509Certificate2.CreateFromPem(TestData.RsaCertificate, TestData.OtherRsaPkcs1Key));
        }

        [Fact]
        public static void CreateFromEncryptedPem_Rsa_Success()
        {
            X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPem(
                TestData.RsaCertificate,
                TestData.RsaEncryptedPkcs8Key, 
                "test");

            using (cert)
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaEncryptedPkcs8Key, cert.GetRSAPrivateKey, "test");
            }
        }

        [Fact]
        public static void CreateFromEncryptedPem_Rsa_InvalidPassword_Fail()
        {
            CryptographicException ce = Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromEncryptedPem(TestData.RsaCertificate, TestData.RsaEncryptedPkcs8Key, "florp"));

            Assert.Contains("password may be incorrect", ce.Message);
        }

        [Fact]
        public static void CreateFromEncryptedPem_Rsa_IgnoresUnencryptedPem_Fail()
        {
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromEncryptedPem(TestData.RsaCertificate, TestData.RsaPkcs8Key, "test"));
        }

        [Fact]
        public static void CreateFromPem_ECDsa_ECPrivateKey_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.ECDsaCertificate, TestData.ECDsaECPrivateKey))
            {
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", cert.Thumbprint);
                AssertKeysMatch(TestData.ECDsaECPrivateKey, cert.GetECDsaPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_ECDsa_Pkcs8_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.ECDsaCertificate, TestData.ECDsaPkcs8Key))
            {
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", cert.Thumbprint);
                AssertKeysMatch(TestData.ECDsaPkcs8Key, cert.GetECDsaPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_ECDsa_EncryptedPkcs8_Success()
        {
            X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPem(
                TestData.ECDsaCertificate,
                TestData.ECDsaEncryptedPkcs8Key,
                "test");

            using (cert)
            {
                Assert.Equal("E844FA74BC8DCE46EF4F8605EA00008F161AB56F", cert.Thumbprint);
                AssertKeysMatch(TestData.ECDsaEncryptedPkcs8Key, cert.GetECDsaPrivateKey, "test");
            }
        }

        [Fact]
        public static void CreateFromPem_Dsa_Pkcs8_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.DsaCertificate, TestData.DsaPkcs8Key))
            {
                Assert.Equal("35052C549E4E7805E4EA204C2BE7F4BC19B88EC8", cert.Thumbprint);
                AssertKeysMatch(TestData.DsaPkcs8Key, cert.GetDSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPem_Dsa_EncryptedPkcs8_Success()
        {
            X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPem(
                TestData.DsaCertificate,
                TestData.DsaEncryptedPkcs8Key,
                "test");

            using (cert)
            {
                Assert.Equal("35052C549E4E7805E4EA204C2BE7F4BC19B88EC8", cert.Thumbprint);
                AssertKeysMatch(TestData.DsaEncryptedPkcs8Key, cert.GetDSAPrivateKey, "test");
            }
        }

        private static void AssertKeysMatch<T>(string keyPem, Func<T> keyLoader, string password = null) where T : AsymmetricAlgorithm
        {
            AsymmetricAlgorithm key = keyLoader();
            Assert.NotNull(key);
            AsymmetricAlgorithm alg = key switch
            {
                RSA => RSA.Create(),
                DSA => DSA.Create(),
                ECDsa => ECDsa.Create(),
                _ => null
            };

            using (key)
            using (alg)
            {
                if (password is null)
                {
                    alg.ImportFromPem(keyPem);
                }
                else
                {
                    alg.ImportFromEncryptedPem(keyPem, password);
                }

                ReadOnlySpan<byte> pemPkcs8 = alg.ExportPkcs8PrivateKey();
                ReadOnlySpan<byte> keyPkcs8 = key.ExportPkcs8PrivateKey();
                Assert.True(pemPkcs8.SequenceEqual(keyPkcs8));
            }
        }
    }
}
