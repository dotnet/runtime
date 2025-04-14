// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class MLKemCertTests
    {
        public static bool MLKemIsNotSupported => !MLKem.IsSupported;

        [ConditionalFact(typeof(MLKem), nameof(MLKem.IsSupported))]
        public static void GetMLKemPublicKey_NotMLKem()
        {
            using X509Certificate2 cert = LoadCertificateFromPem(TestData.RsaCertificate);
            Assert.Null(cert.GetMLKemPublicKey());
        }

        [ConditionalTheory(typeof(MLKem), nameof(MLKem.IsSupported))]
        [MemberData(nameof(MLKemCertificatePublicKeys))]
        public static void GetMLKemPublicKey_IetfMLKemKeys(string certificatePem, byte[] expectedSpki)
        {
            MLKem kem;

            using (X509Certificate2 cert = LoadCertificateFromPem(certificatePem))
            {
                kem = cert.GetMLKemPublicKey();
            }

            using (kem)
            {
                Assert.NotNull(kem);
                AssertExtensions.SequenceEqual(expectedSpki, kem.ExportSubjectPublicKeyInfo());
            }
        }

        [ConditionalTheory(nameof(MLKemIsNotSupported))]
        [InlineData(MLKemTestData.IetfMlKem512CertificatePem)]
        [InlineData(MLKemTestData.IetfMlKem768CertificatePem)]
        [InlineData(MLKemTestData.IetfMlKem1024CertificatePem)]
        public static void GetMLKemPublicKey_PlatformNotSupported(string certificatePem)
        {
            using (X509Certificate2 cert = LoadCertificateFromPem(certificatePem))
            {
                Assert.Throws<PlatformNotSupportedException>(() => cert.GetMLKemPublicKey());
            }
        }

        [Fact]
        public static void GetMLKemPublicKey_BadSubjectPublicKeyInfo_AlgorithmParameters()
        {
            Oid mlKem512Oid = new("2.16.840.1.101.3.4.4.1");
            PublicKey key = new(
                mlKem512Oid,
                new AsnEncodedData(mlKem512Oid, new byte[] { 0x05, 0x00 }),
                new AsnEncodedData(mlKem512Oid, MLKemTestData.MLKem512EncapsulationKey));
            using X509Certificate2 cert = CreateCertificateForPublicKey(key);
            Assert.Throws<CryptographicException>(() => cert.GetMLKemPublicKey());
        }

        [Fact]
        public static void GetMLKemPublicKey_BadSubjectPublicKeyInfo_KeySize()
        {
            Oid mlKem512Oid = new("2.16.840.1.101.3.4.4.1");
            PublicKey key = new(
                mlKem512Oid,
                null,
                new AsnEncodedData(mlKem512Oid, MLKemTestData.MLKem512EncapsulationKey[1..]));
            using X509Certificate2 cert = CreateCertificateForPublicKey(key);
            Assert.Throws<CryptographicException>(() => cert.GetMLKemPublicKey());
        }

        private static X509Certificate2 CreateCertificateForPublicKey(PublicKey key)
        {
            using ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest rootReq = new("CN=Root", rootKey, HashAlgorithmName.SHA256);
            rootReq.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());

            using X509Certificate2 root = rootReq.CreateSelfSigned(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(1));

            X509SignatureGenerator generator = X509SignatureGenerator.CreateForECDsa(rootKey);

            CertificateRequest req = new(
                new X500DistinguishedName("CN=Test"),
                key,
                HashAlgorithmName.SHA256);
            req.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForEndEntity());
            byte[] serial = RandomNumberGenerator.GetBytes(8);
            return req.Create(root.SubjectName, generator, root.NotBefore, root.NotAfter.AddDays(-1), serial);
        }

        public static IEnumerable<object[]> MLKemCertificatePublicKeys
        {
            get
            {
                yield return [MLKemTestData.IetfMlKem512CertificatePem, MLKemTestData.IetfMlKem512Spki];
                yield return [MLKemTestData.IetfMlKem768CertificatePem, MLKemTestData.IetfMlKem768Spki];
                yield return [MLKemTestData.IetfMlKem1024CertificatePem, MLKemTestData.IetfMlKem1024Spki];
            }
        }

        private static X509Certificate2 LoadCertificateFromPem(string pem)
        {
#if NET
            return X509Certificate2.CreateFromPem(pem);
#else
            return new X509Certificate2(System.Text.Encoding.ASCII.GetBytes(pem));
#endif
        }
    }
}
