// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
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
        public static void CreateFromPem_CryptographicException_CertIsPfx()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate, TestData.RsaPkcs1Key))
            {
                string content = Convert.ToBase64String(cert.Export(X509ContentType.Pkcs12));
                string certContents = $@"
-----BEGIN CERTIFICATE-----
{content}
-----END CERTIFICATE-----
";
                Assert.Throws<CryptographicException>(() =>
                    X509Certificate2.CreateFromPem(certContents, TestData.RsaPkcs1Key));
            }
        }

        [Fact]
        public static void CreateFromPem_CryptographicException_CertIsPkcs7()
        {
            string content = Convert.ToBase64String(TestData.Pkcs7ChainDerBytes);
            string certContents = $@"
-----BEGIN CERTIFICATE-----
{content}
-----END CERTIFICATE-----
";
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(certContents, TestData.RsaPkcs1Key));
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
            CryptographicException ce = AssertExtensions.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(TestData.RsaCertificate, TestData.OtherRsaPkcs1Key));

            Assert.IsType<ArgumentException>(ce.InnerException);
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
        public static void CreateFromEncryptedPem_Rsa_KeyMismatch_Fail()
        {
            CryptographicException ce = AssertExtensions.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromEncryptedPem(TestData.RsaCertificate, TestData.OtherRsaPkcs8EncryptedKey, "test"));

            Assert.IsType<ArgumentException>(ce.InnerException);
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
        public static void CreateFromPem_ECDH_Pkcs8_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.EcDhCertificate, TestData.EcDhPkcs8Key))
            {
                Assert.Equal("6EAE9D3E34F7672106585583AA4623B6CC5AE2F7", cert.Thumbprint);
                AssertKeysMatch(TestData.EcDhPkcs8Key, cert.GetECDiffieHellmanPrivateKey);
            }
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void CreateFromPem_MLKem_Pkcs8_Success()
        {
            (string CertificatePem, string PrivateKeyPem, string Thumbprint)[] cases =
            [
                (
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512PrivateKeySeedPem,
                    "877316CB8B7E5C389E99B1094DE4F60E3BBFDA12D2E4ACB8C014D84CFC009E6F"
                ),
                (
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512PrivateKeyExpandedKeyPem,
                    "877316CB8B7E5C389E99B1094DE4F60E3BBFDA12D2E4ACB8C014D84CFC009E6F"
                ),
                (
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512PrivateKeyBothPem,
                    "877316CB8B7E5C389E99B1094DE4F60E3BBFDA12D2E4ACB8C014D84CFC009E6F"
                ),
                (
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768PrivateKeySeedPem,
                    "0E9DFBEDB039156B568D8F59953DD4DA6B81E30EC8E071A775DF6BC2E77B2DCE"
                ),
                (
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768PrivateKeyExpandedKeyPem,
                    "0E9DFBEDB039156B568D8F59953DD4DA6B81E30EC8E071A775DF6BC2E77B2DCE"
                ),
                (
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768PrivateKeyBothPem,
                    "0E9DFBEDB039156B568D8F59953DD4DA6B81E30EC8E071A775DF6BC2E77B2DCE"
                ),
                (
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024PrivateKeySeedPem,
                    "32819AD8477F3620619B1E97744AA26AA9617760A8694E1BD8D17DE8B49A8E8A"
                ),
                (
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024PrivateKeyExpandedKeyPem,
                    "32819AD8477F3620619B1E97744AA26AA9617760A8694E1BD8D17DE8B49A8E8A"
                ),
                (
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024PrivateKeyBothPem,
                    "32819AD8477F3620619B1E97744AA26AA9617760A8694E1BD8D17DE8B49A8E8A"
                ),
            ];

            foreach((string CertificatePem, string PrivateKeyPem, string Thumbprint) in cases)
            {
                using (X509Certificate2 cert = X509Certificate2.CreateFromPem(CertificatePem, PrivateKeyPem))
                {
                    Assert.Equal(Thumbprint, cert.GetCertHashString(HashAlgorithmName.SHA256));
                    AssertKeysMatch(PrivateKeyPem, cert.GetMLKemPrivateKey);
                }
            }
        }

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void CreateFromEncryptedPem_MLKem_Pkcs8_Success()
        {
            (string CertificatePem, string EncryptedPrivateKeyPem, string Thumbprint)[] cases =
            [
                (
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512EncryptedPrivateKeySeedPem,
                    "877316CB8B7E5C389E99B1094DE4F60E3BBFDA12D2E4ACB8C014D84CFC009E6F"
                ),
                (
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512EncryptedPrivateKeyExpandedKeyPem,
                    "877316CB8B7E5C389E99B1094DE4F60E3BBFDA12D2E4ACB8C014D84CFC009E6F"
                ),
                (
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512EncryptedPrivateKeyBothPem,
                    "877316CB8B7E5C389E99B1094DE4F60E3BBFDA12D2E4ACB8C014D84CFC009E6F"
                ),
                (
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768EncryptedPrivateKeySeedPem,
                    "0E9DFBEDB039156B568D8F59953DD4DA6B81E30EC8E071A775DF6BC2E77B2DCE"
                ),
                (
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768EncryptedPrivateKeyExpandedKeyPem,
                    "0E9DFBEDB039156B568D8F59953DD4DA6B81E30EC8E071A775DF6BC2E77B2DCE"
                ),
                (
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768EncryptedPrivateKeyBothPem,
                    "0E9DFBEDB039156B568D8F59953DD4DA6B81E30EC8E071A775DF6BC2E77B2DCE"
                ),
                (
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024EncryptedPrivateKeySeedPem,
                    "32819AD8477F3620619B1E97744AA26AA9617760A8694E1BD8D17DE8B49A8E8A"
                ),
                (
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024EncryptedPrivateKeyExpandedKeyPem,
                    "32819AD8477F3620619B1E97744AA26AA9617760A8694E1BD8D17DE8B49A8E8A"
                ),
                (
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024EncryptedPrivateKeyBothPem,
                    "32819AD8477F3620619B1E97744AA26AA9617760A8694E1BD8D17DE8B49A8E8A"
                ),
            ];

            foreach((string CertificatePem, string PrivateKeyPem, string Thumbprint) in cases)
            {
                using (X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPem(
                    CertificatePem,
                    PrivateKeyPem,
                    MLKemTestData.EncryptedPrivateKeyPassword))
                {
                    Assert.Equal(Thumbprint, cert.GetCertHashString(HashAlgorithmName.SHA256));
                    AssertKeysMatch(PrivateKeyPem, cert.GetMLKemPrivateKey, MLKemTestData.EncryptedPrivateKeyPassword);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void CreateFromPem_Dsa_Pkcs8_Success()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.DsaCertificate, TestData.DsaPkcs8Key))
            {
                Assert.Equal("35052C549E4E7805E4EA204C2BE7F4BC19B88EC8", cert.Thumbprint);
                AssertKeysMatch(TestData.DsaPkcs8Key, cert.GetDSAPrivateKey);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
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

        [Fact]
        public static void CreateFromPemFile_NoKeyFile_Rsa_Success()
        {
            string pemAggregate = TestData.RsaCertificate + TestData.RsaPkcs1Key;

            using (TempFileHolder certAndKey = new TempFileHolder(pemAggregate))
            using (X509Certificate2 cert = X509Certificate2.CreateFromPemFile(certAndKey.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPemFile_SameFile_Rsa_Success()
        {
            using (TempFileHolder aggregatePem = new TempFileHolder(TestData.RsaCertificate + TestData.RsaPkcs1Key))
            using (X509Certificate2 cert = X509Certificate2.CreateFromPemFile(aggregatePem.FilePath, aggregatePem.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPemFile_WithKeyFile_Rsa_Success()
        {
            using (TempFileHolder certPem = new TempFileHolder(TestData.RsaCertificate))
            using (TempFileHolder keyPem = new TempFileHolder(TestData.RsaPkcs1Key))
            using (X509Certificate2 cert = X509Certificate2.CreateFromPemFile(certPem.FilePath, keyPem.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPemFile_PrefersKeyFromKeyFile_Success()
        {
            using (TempFileHolder certPem = new TempFileHolder(TestData.RsaCertificate + TestData.OtherRsaPkcs1Key))
            using (TempFileHolder keyPem = new TempFileHolder(TestData.RsaPkcs1Key))
            using (X509Certificate2 cert = X509Certificate2.CreateFromPemFile(certPem.FilePath, keyPem.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaPkcs1Key, cert.GetRSAPrivateKey);
            }
        }

        [Fact]
        public static void CreateFromPemFile_Null_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("certPemFilePath", () =>
                X509Certificate2.CreateFromPemFile(null));
        }

        [Fact]
        public static void CreateFromEncryptedPemFile_NoKeyFile_Rsa_Success()
        {
            string pemAggregate = TestData.RsaCertificate + TestData.RsaEncryptedPkcs8Key;

            using (TempFileHolder certAndKey = new TempFileHolder(pemAggregate))
            using (X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPemFile(certAndKey.FilePath, "test"))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaEncryptedPkcs8Key, cert.GetRSAPrivateKey, "test");
            }
        }

        [Fact]
        public static void CreateFromEncryptedPemFile_SameFile_Rsa_Success()
        {
            using (TempFileHolder aggregatePem = new TempFileHolder(TestData.RsaCertificate + TestData.RsaEncryptedPkcs8Key))
            using (X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPemFile(aggregatePem.FilePath, "test", aggregatePem.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaEncryptedPkcs8Key, cert.GetRSAPrivateKey, "test");
            }
        }

        [Fact]
        public static void CreateFromEncryptedPemFile_WithKeyFile_Rsa_Success()
        {
            using (TempFileHolder certPem = new TempFileHolder(TestData.RsaCertificate))
            using (TempFileHolder keyPem = new TempFileHolder(TestData.RsaEncryptedPkcs8Key))
            using (X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPemFile(certPem.FilePath, "test", keyPem.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaEncryptedPkcs8Key, cert.GetRSAPrivateKey, "test");
            }
        }

        [Fact]
        public static void CreateFromEncryptedPemFile_PrefersKeyFromKeyFile_Success()
        {
            using (TempFileHolder certPem = new TempFileHolder(TestData.RsaCertificate + TestData.OtherRsaPkcs1Key))
            using (TempFileHolder keyPem = new TempFileHolder(TestData.RsaEncryptedPkcs8Key))
            using (X509Certificate2 cert = X509Certificate2.CreateFromEncryptedPemFile(certPem.FilePath, "test", keyPem.FilePath))
            {
                Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
                AssertKeysMatch(TestData.RsaEncryptedPkcs8Key, cert.GetRSAPrivateKey, "test");
            }
        }

        [Fact]
        public static void CreateFromEncryptedPemFile_Null_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("certPemFilePath", () =>
                X509Certificate2.CreateFromEncryptedPemFile(null, default));
        }

        [Fact]
        public static void CreateFromPem_PublicOnly_IgnoresPrivateKey()
        {
            using X509Certificate2 cert = X509Certificate2.CreateFromPem($"{TestData.RsaCertificate}\n{TestData.RsaPkcs1Key}");
            Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
            Assert.False(cert.HasPrivateKey);
        }

        [Fact]
        public static void CreateFromPem_PublicOnly()
        {
            using X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.RsaCertificate);
            Assert.Equal("A33348E44A047A121F44E810E888899781E1FF19", cert.Thumbprint);
            Assert.False(cert.HasPrivateKey);
        }

        [Fact]
        public static void CreateFromPem_PublicOnly_CryptographicException_Empty()
        {
            Assert.Throws<CryptographicException>(() => X509Certificate2.CreateFromPem(default));
        }

        [Fact]
        public static void CreateFromPem_PublicOnly_CryptographicException_MalformedCertificate()
        {
            const string CertContents = @"
-----BEGIN CERTIFICATE-----
MII
-----END CERTIFICATE-----
";
            Assert.Throws<CryptographicException>(() => X509Certificate2.CreateFromPem(CertContents));
        }

        [Fact]
        public static void CreateFromPem_PublicOnly_CryptographicException_CertIsPkcs7()
        {
            string content = Convert.ToBase64String(TestData.Pkcs7ChainDerBytes);
            string certContents = $@"
-----BEGIN CERTIFICATE-----
{content}
-----END CERTIFICATE-----
";
            Assert.Throws<CryptographicException>(() =>
                X509Certificate2.CreateFromPem(certContents));
        }

        private static void AssertKeysMatch<T>(string keyPem, Func<T> keyLoader, string password = null) where T : IDisposable
        {
            IDisposable key = keyLoader();
            Assert.NotNull(key);
            IDisposable alg;

            if (key is AsymmetricAlgorithm)
            {
                AsymmetricAlgorithm asymmetricAlg = key switch
                {
                    RSA => RSA.Create(),
                    DSA => DSA.Create(),
                    ECDsa => ECDsa.Create(),
                    ECDiffieHellman => ECDiffieHellman.Create(),
                    _ => null,
                };

                if (password is null)
                {
                    asymmetricAlg.ImportFromPem(keyPem);
                }
                else
                {
                    asymmetricAlg.ImportFromEncryptedPem(keyPem, password);
                }

                alg = asymmetricAlg;
            }
            else if (key is MLKem)
            {
                alg = password is null ? MLKem.ImportFromPem(keyPem) : MLKem.ImportFromEncryptedPem(keyPem, password);
            }
            else
            {
                Assert.Fail($"Unhandled key type {key.GetType()}.");
                throw new UnreachableException();
            }

            using (key)
            using (alg)
            {
                byte[] data = RandomNumberGenerator.GetBytes(32);

                switch ((alg, key))
                {
                    case (RSA rsa, RSA rsaPem):
                        byte[] rsaSignature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        Assert.True(rsaPem.VerifyData(data, rsaSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                        break;
                    case (ECDsa ecdsa, ECDsa ecdsaPem):
                        byte[] ecdsaSignature = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                        Assert.True(ecdsaPem.VerifyData(data, ecdsaSignature, HashAlgorithmName.SHA256));
                        break;
                    case (DSA dsa, DSA dsaPem):
                        byte[] dsaSignature = dsa.SignData(data, HashAlgorithmName.SHA1);
                        Assert.True(dsaPem.VerifyData(data, dsaSignature, HashAlgorithmName.SHA1));
                        break;
                    case (ECDiffieHellman ecdh, ECDiffieHellman ecdhPem):
                        ECCurve curve = ecdh.KeySize switch {
                            256 => ECCurve.NamedCurves.nistP256,
                            384 => ECCurve.NamedCurves.nistP384,
                            521 => ECCurve.NamedCurves.nistP521,
                            _ => throw new CryptographicException("Unknown key size")
                        };

                        using (ECDiffieHellman otherParty = ECDiffieHellman.Create(curve))
                        {
                            byte[] key1 = ecdh.DeriveKeyFromHash(otherParty.PublicKey, HashAlgorithmName.SHA256);
                            byte[] key2 = ecdhPem.DeriveKeyFromHash(otherParty.PublicKey, HashAlgorithmName.SHA256);
                            Assert.Equal(key1, key2);
                        }
                        break;
                    case (MLKem kem, MLKem kemPem):
                        kem.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret1);
                        byte[] sharedSecret2 = kemPem.Decapsulate(ciphertext);
                        AssertExtensions.SequenceEqual(sharedSecret1, sharedSecret2);

                        kemPem.Encapsulate(out ciphertext, out sharedSecret1);
                        sharedSecret2 = kem.Decapsulate(ciphertext);
                        AssertExtensions.SequenceEqual(sharedSecret1, sharedSecret2);
                        break;
                    default:
                        throw new CryptographicException("Unknown key algorithm");
                }
            }
        }
    }
}
