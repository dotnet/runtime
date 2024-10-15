// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static partial class SignedCmsTests
    {
        [Fact]
        public static void CmsSignerKeyIsNullByDefault()
        {
            CmsSigner cmsSigner = new CmsSigner();
            Assert.Null(cmsSigner.PrivateKey);
        }

        [Fact]
        public static void CmsSignerKeyIsNullByDefaultWhenCertificateIsPassed()
        {
            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert);
                Assert.Null(cmsSigner.PrivateKey);
            }
        }

        [Fact]
        public static void CmsSignerConstructorWithKeySetsProperty()
        {
            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (RSA key = cert.GetRSAPrivateKey())
            {
                CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                Assert.Same(key, cmsSigner.PrivateKey);
            }
        }

        [Fact]
        public static void SingUsingExplicitKeySetWithProperty()
        {
            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 pubCert = new X509Certificate2(cert.RawData))
            using (RSA key = cert.GetRSAPrivateKey())
            {
                byte[] content = { 1, 2, 3, 4, 19 };
                ContentInfo contentInfo = new ContentInfo(content);
                SignedCms cms = new SignedCms(contentInfo);
                CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, pubCert);
                cmsSigner.PrivateKey = key;

                cms.ComputeSignature(cmsSigner);
                cms.CheckSignature(true);
                Assert.Equal(1, cms.SignerInfos.Count);
                Assert.Equal(pubCert, cms.SignerInfos[0].Certificate);
            }
        }

        [Fact]
        public static void SignCmsUsingExplicitRSAKey()
        {
            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (RSA key = cert.GetRSAPrivateKey())
            {
                VerifyWithExplicitPrivateKey(cert, key);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void SignCmsUsingExplicitDSAKey()
        {
            using (X509Certificate2 cert = Certificates.Dsa1024.TryGetCertificateWithPrivateKey())
            using (DSA key = cert.GetDSAPrivateKey())
            {
                VerifyWithExplicitPrivateKey(cert, key);
            }
        }

        [Fact]
        public static void SignCmsUsingExplicitECDsaKey()
        {
            using (X509Certificate2 cert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            using (ECDsa key = cert.GetECDsaPrivateKey())
            {
                VerifyWithExplicitPrivateKey(cert, key);
            }
        }

        [Fact]
        public static void SignCmsUsingExplicitECDsaP521Key()
        {
            using (X509Certificate2 cert = Certificates.ECDsaP521Win.TryGetCertificateWithPrivateKey())
            using (ECDsa key = cert.GetECDsaPrivateKey())
            {
                VerifyWithExplicitPrivateKey(cert, key);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void CounterSignCmsUsingExplicitRSAKeyForFirstSignerAndDSAForCounterSignature()
        {
            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (RSA key = cert.GetRSAPrivateKey())
            using (X509Certificate2 counterSignerCert = Certificates.Dsa1024.TryGetCertificateWithPrivateKey())
            using (DSA counterSignerKey = counterSignerCert.GetDSAPrivateKey())
            {
                VerifyCounterSignatureWithExplicitPrivateKey(cert, key, counterSignerCert, counterSignerKey);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void CounterSignCmsUsingExplicitDSAKeyForFirstSignerAndECDsaForCounterSignature()
        {
            using (X509Certificate2 cert = Certificates.Dsa1024.TryGetCertificateWithPrivateKey())
            using (DSA key = cert.GetDSAPrivateKey())
            using (X509Certificate2 counterSignerCert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            using (ECDsa counterSignerKey = counterSignerCert.GetECDsaPrivateKey())
            {
                VerifyCounterSignatureWithExplicitPrivateKey(cert, key, counterSignerCert, counterSignerKey);
            }
        }

        [Fact]
        public static void CounterSignCmsUsingExplicitECDsaKeyForFirstSignerAndRSAForCounterSignature()
        {
            using (X509Certificate2 cert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            using (ECDsa key = cert.GetECDsaPrivateKey())
            using (X509Certificate2 counterSignerCert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (RSA counterSignerKey = counterSignerCert.GetRSAPrivateKey())
            {
                VerifyCounterSignatureWithExplicitPrivateKey(cert, key, counterSignerCert, counterSignerKey);
            }
        }

        [Fact]
        public static void SignCmsUsingRSACertAndECDsaKeyThrows()
        {
            byte[] content = { 9, 8, 7, 6, 5 };

            ContentInfo contentInfo = new ContentInfo(content);
            SignedCms cms = new SignedCms(contentInfo, detached: false);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.GetCertificate())
            using (ECDsa key = ECDsa.Create())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void SignCmsUsingDSACertAndECDsaKeyThrows()
        {
            byte[] content = { 9, 8, 7, 6, 5 };

            ContentInfo contentInfo = new ContentInfo(content);
            SignedCms cms = new SignedCms(contentInfo, detached: false);

            using (X509Certificate2 cert = Certificates.Dsa1024.GetCertificate())
            using (ECDsa key = ECDsa.Create())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;
                signer.DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1);
                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void SignCmsUsingEDCSaCertAndRSAaKeyThrows()
        {
            byte[] content = { 9, 8, 7, 6, 5 };

            ContentInfo contentInfo = new ContentInfo(content);
            SignedCms cms = new SignedCms(contentInfo, detached: false);

            using (X509Certificate2 cert = Certificates.ECDsaP256Win.GetCertificate())
            using (RSA key = RSA.Create())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void SignCmsUsingRSACertWithNotMatchingKeyThrows()
        {
            byte[] content = { 9, 8, 7, 6, 5 };

            ContentInfo contentInfo = new ContentInfo(content);
            SignedCms cms = new SignedCms(contentInfo, detached: false);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.GetCertificate())
            using (RSA key = RSA.Create())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.AppleCrypto, "Creating DSA keys is not supported on OSX")]
        public static void SignCmsUsingDSACertWithNotMatchingKeyThrows()
        {
            byte[] content = { 9, 8, 7, 6, 5 };

            ContentInfo contentInfo = new ContentInfo(content);
            SignedCms cms = new SignedCms(contentInfo, detached: false);

            using (X509Certificate2 cert = Certificates.Dsa1024.GetCertificate())
            using (DSA key = DSA.Create())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                signer.IncludeOption = X509IncludeOption.EndCertOnly;
                signer.DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1);
                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void SignCmsUsingECDsaCertWithNotMatchingKeyThrows()
        {
            byte[] content = { 9, 8, 7, 6, 5 };

            ContentInfo contentInfo = new ContentInfo(content);
            SignedCms cms = new SignedCms(contentInfo, detached: false);

            using (X509Certificate2 cert = Certificates.ECDsaP256Win.GetCertificate())
            using (ECDsa key = ECDsa.Create())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, cert, key);
                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void AddCertificate()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            int numOfCerts = cms.Certificates.Count;

            using (X509Certificate2 newCert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                cms.AddCertificate(newCert);

                Assert.Equal(numOfCerts + 1, cms.Certificates.Count);
                Assert.True(cms.Certificates.Contains(newCert));

                cms.CheckSignature(true);
            }
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void AddCertificateWithPrivateKey()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            int numOfCerts = cms.Certificates.Count;

            using (X509Certificate2 newCert = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                Assert.True(newCert.HasPrivateKey);
                cms.AddCertificate(newCert);

                Assert.Equal(numOfCerts + 1, cms.Certificates.Count);

                X509Certificate2 addedCert = cms.Certificates.OfType<X509Certificate2>().Where((cert) => cert.Equals(newCert)).Single();
                Assert.False(addedCert.HasPrivateKey);

                Assert.Equal(newCert, addedCert);

                cms.CheckSignature(true);
            }
        }

        [Fact]
        public static void RemoveCertificate()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            var expectedCerts = new HashSet<X509Certificate2>(cms.Certificates.OfType<X509Certificate2>());

            using (X509Certificate2 cert1 = Certificates.RSAKeyTransfer1.GetCertificate())
            using (X509Certificate2 cert2 = Certificates.RSAKeyTransfer2.GetCertificate())
            {
                Assert.NotEqual(cert1, cert2);

                cms.AddCertificate(cert1);
                cms.AddCertificate(cert2);

                expectedCerts.Add(cert2);

                cms.RemoveCertificate(cert1);

                Assert.Equal(expectedCerts.Count, cms.Certificates.Count);

                foreach (X509Certificate2 documentCert in cms.Certificates)
                {
                    Assert.Contains(documentCert, expectedCerts);
                }
            }
        }

        [Fact]
        public static void RemoveNonExistingCertificate()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            using (X509Certificate2 certToRemove = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                Assert.Throws<CryptographicException>(() => cms.RemoveCertificate(certToRemove));
            }
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void RemoveAllCertsAddBackSignerCert()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            SignerInfo signerInfoBeforeRemoval = cms.SignerInfos[0];
            X509Certificate2 signerCert = signerInfoBeforeRemoval.Certificate;

            while (cms.Certificates.Count > 0)
            {
                cms.RemoveCertificate(cms.Certificates[0]);
            }

            // Signer info should be gone
            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));

            Assert.Null(cms.SignerInfos[0].Certificate);
            Assert.NotNull(signerInfoBeforeRemoval.Certificate);

            cms.AddCertificate(signerCert);
            cms.CheckSignature(true);

            Assert.Equal(1, cms.Certificates.Count);
        }

        [Fact]
        public static void AddExistingCertificate()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            using (X509Certificate2 newCert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                cms.AddCertificate(newCert);
                Assert.Throws<CryptographicException>(() => cms.AddCertificate(newCert));
            }
        }

        [Fact]
        public static void AddAttributeToIndefiniteLengthContent()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.IndefiniteLengthContentDocument);
            cms.SignerInfos[0].AddUnsignedAttribute(new Pkcs9DocumentDescription("Indefinite length test"));
            byte[] encoded = cms.Encode();

            cms = new SignedCms();
            cms.Decode(encoded);
            // It should sort first, because it's smaller.
            Assert.Equal(Oids.DocumentDescription, cms.SignerInfos[0].UnsignedAttributes[0].Oid.Value);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void AddSigner_RSA_EphemeralKey()
        {
            using (RSA rsa = RSA.Create())
            using (X509Certificate2 publicCertificate = Certificates.RSA2048SignatureOnly.GetCertificate())
            using (X509Certificate2 certificateWithKey = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey(exportable: true))
            {
                if (certificateWithKey == null)
                {
                    return;
                }

                using (RSA privateKey = certificateWithKey.GetRSAPrivateKey())
                using (RSA exportableKey = privateKey.MakeExportable())
                {
                    rsa.ImportParameters(exportableKey.ExportParameters(true));
                }
                using (X509Certificate2 certWithEphemeralKey = publicCertificate.CopyWithPrivateKey(rsa))
                {
                    ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
                    SignedCms cms = new SignedCms(content, false);
                    CmsSigner signer = new CmsSigner(certWithEphemeralKey)
                    {
                        IncludeOption = X509IncludeOption.EndCertOnly
                    };
                    cms.ComputeSignature(signer);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void AddSigner_DSA_EphemeralKey()
        {
            using (DSA dsa = DSA.Create())
            using (X509Certificate2 publicCertificate = Certificates.Dsa1024.GetCertificate())
            using (X509Certificate2 certificateWithKey = Certificates.Dsa1024.TryGetCertificateWithPrivateKey(exportable: true))
            {
                if (certificateWithKey == null)
                {
                    return;
                }

                using (DSA privateKey = certificateWithKey.GetDSAPrivateKey())
                using (DSA exportableKey = privateKey.MakeExportable())
                {
                    dsa.ImportParameters(exportableKey.ExportParameters(true));
                }
                using (X509Certificate2 certWithEphemeralKey = publicCertificate.CopyWithPrivateKey(dsa))
                {
                    ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
                    SignedCms cms = new SignedCms(content, false);
                    CmsSigner signer = new CmsSigner(certWithEphemeralKey)
                    {
                        DigestAlgorithm = new Oid(Oids.Sha1, Oids.Sha1),
                        IncludeOption = X509IncludeOption.EndCertOnly
                    };
                    cms.ComputeSignature(signer);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void AddSigner_ECDSA_EphemeralKey()
        {
            using (ECDsa ecdsa = ECDsa.Create())
            using (X509Certificate2 publicCertificate = Certificates.ECDsaP256Win.GetCertificate())
            using (X509Certificate2 certificateWithKey = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey(exportable: true))
            {
                if (certificateWithKey == null)
                {
                    return;
                }

                using (ECDsa privateKey = certificateWithKey.GetECDsaPrivateKey())
                using (ECDsa exportableKey = privateKey.MakeExportable())
                {
                    ecdsa.ImportParameters(exportableKey.ExportParameters(true));
                }
                using (X509Certificate2 certWithEphemeralKey = publicCertificate.CopyWithPrivateKey(ecdsa))
                {
                    ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
                    SignedCms cms = new SignedCms(content, false);
                    CmsSigner signer = new CmsSigner(certWithEphemeralKey)
                    {
                        IncludeOption = X509IncludeOption.EndCertOnly
                    };
                    cms.ComputeSignature(signer);
                }
            }
        }

        [Fact]
        public static void CreateSignature_DigestAlgorithmWithSignatureOid_Prohibited()
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);
            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(cert);
                signer.DigestAlgorithm = new Oid(Oids.RsaPkcs1Sha256);

                Assert.Throws<CryptographicException>(() => {
                    cms.ComputeSignature(signer);
                });
            }
        }

        [Theory]
        [InlineData(Oids.Sha256, true)]
        [InlineData(Oids.Sha384, true)]
        [InlineData(Oids.Sha512, true)]
        [InlineData(Oids.Sha1, true)]
        [InlineData(Oids.Sha256, false)]
        [InlineData(Oids.Sha384, false)]
        [InlineData(Oids.Sha512, false)]
        [InlineData(Oids.Sha1, false)]
        public static void CreateSignature_RsaPss(string digestOid, bool assignByConstructor)
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);
            byte[] cmsBytes;

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer;

                if (assignByConstructor)
                {
                    signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert, null, RSASignaturePadding.Pss);
                }
                else
                {
                    signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                    signer.SignaturePadding = RSASignaturePadding.Pss;
                }

                signer.DigestAlgorithm = new Oid(digestOid, null);
                if (!SignatureSupport.SupportsRsaSha1Signatures &&
                    digestOid == Oids.Sha1)
                {
                    Assert.ThrowsAny<CryptographicException>(() => cms.ComputeSignature(signer));
                }
                else
                {
                    cms.ComputeSignature(signer);
                    cmsBytes = cms.Encode();
                    cms = new SignedCms();
                    cms.Decode(cmsBytes);
                    cms.CheckSignature(true); // Assert.NoThrow
                    Assert.Single(cms.SignerInfos);

                    SignerInfo signerInfo = cms.SignerInfos[0];
                    Assert.Equal(Oids.RsaPss, signerInfo.SignatureAlgorithm.Value);
                }
            }
        }

        [Fact]
        public static void CreateSignature_RsaPss_SeparateKey()
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);
            byte[] cmsBytes;

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 pubOnly = new X509Certificate2(cert.RawDataMemory.Span))
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, pubOnly, rsa, RSASignaturePadding.Pss);
                cms.ComputeSignature(signer);
                cmsBytes = cms.Encode();
            }

            cms = new SignedCms();
            cms.Decode(cmsBytes);
            cms.CheckSignature(true); // Assert.NoThrow
            Assert.Single(cms.SignerInfos);

            SignerInfo signerInfo = cms.SignerInfos[0];
            Assert.Equal(Oids.RsaPss, signerInfo.SignatureAlgorithm.Value);
        }

        [Fact]
        public static void CreateSignature_RsaPss_Md5NotSupported()
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert, null, RSASignaturePadding.Pss);
                signer.DigestAlgorithm = new Oid(Oids.Md5, null);
                Assert.ThrowsAny<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void CreateSignature_RsaPadding_DefaultToPkcs1()
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);
            byte[] cmsBytes;

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert, null, null);
                cms.ComputeSignature(signer);
                cmsBytes = cms.Encode();
            }

            cms = new SignedCms();
            cms.Decode(cmsBytes);
            cms.CheckSignature(true); // Assert.NoThrow
            Assert.Single(cms.SignerInfos);

            SignerInfo signerInfo = cms.SignerInfos[0];
            Assert.Equal(Oids.Rsa, signerInfo.SignatureAlgorithm.Value);
        }

        [Fact]
        public static void CreateSignature_Ecdsa_ThrowsWithRsaSignaturePadding()
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);

            using (X509Certificate2 cert = Certificates.ECDsaP256Win.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert, null, RSASignaturePadding.Pss);
                Assert.ThrowsAny<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void AddCertificate_CollectionContainsAttributeCertificate()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.TstWithAttributeCertificate);
            signedCms.CheckSignature(true);

            int countBefore = CountCertificateChoices(SignedDocuments.TstWithAttributeCertificate);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.GetCertificate())
            {
                signedCms.AddCertificate(cert);
                byte[] reEncoded = signedCms.Encode();
                int countAfter = CountCertificateChoices(reEncoded);
                Assert.Equal(countBefore + 1, countAfter);

                signedCms = new SignedCms();
                signedCms.Decode(reEncoded);
                signedCms.CheckSignature(true);
            }
        }

        [Fact]
        public static void RemoveCertificate_Existing_CollectionContainsAttributeCertificate()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.TstWithAttributeCertificate);
            int countBefore = CountCertificateChoices(SignedDocuments.TstWithAttributeCertificate);

            signedCms.RemoveCertificate(signedCms.Certificates[0]);
            byte[] reEncoded = signedCms.Encode();
            int countAfter = CountCertificateChoices(reEncoded);
            Assert.Equal(countBefore - 1, countAfter);
        }

        [Fact]
        public static void RemoveCertificate_NonExisting_CollectionContainsAttributeCertificate()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.TstWithAttributeCertificate);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.GetCertificate())
            {
                // Remove a non-existing certificate so that we are forced to enumerate the entire 'certificates[0]'
                // collection (including attribute certificates) looking for it.
                Assert.Throws<CryptographicException>(() => signedCms.RemoveCertificate(cert));
            }
        }

        [Fact]
        public static void ComputeCounterSignature_PreservesAttributeCertificate()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.TstWithAttributeCertificate);
            int countBefore = CountCertificateChoices(SignedDocuments.TstWithAttributeCertificate);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(cert);
                SignerInfo info = signedCms.SignerInfos[0];
                info.ComputeCounterSignature(signer);
            }

            byte[] encoded = signedCms.Encode();
            int countAfter = CountCertificateChoices(encoded);
            Assert.Equal(countBefore + 1, countAfter);
        }

        [Fact]
        public static void ComputeSignature_PreservesAttributeCertificate()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(SignedDocuments.TstWithAttributeCertificate);
            int countBefore = CountCertificateChoices(SignedDocuments.TstWithAttributeCertificate);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(cert);
                signedCms.ComputeSignature(signer);
            }

            byte[] encoded = signedCms.Encode();
            int countAfter = CountCertificateChoices(encoded);
            Assert.Equal(countBefore + 1, countAfter);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsSha3))]
        [InlineData(Oids.Sha3_256)]
        [InlineData(Oids.Sha3_384)]
        [InlineData(Oids.Sha3_512)]
        public static void ComputeSignature_Rsa_Sha3_Roundtrip(string hashAlgorithm)
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);
            byte[] cmsBytes;

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                signer.DigestAlgorithm = new Oid(hashAlgorithm, null);

                cms.ComputeSignature(signer);
                cmsBytes = cms.Encode();
                cms = new SignedCms();
                cms.Decode(cmsBytes);
                cms.CheckSignature(true); // Assert.NoThrow
                Assert.Single(cms.SignerInfos);

                SignerInfo signerInfo = cms.SignerInfos[0];
                Assert.Equal(hashAlgorithm, signerInfo.DigestAlgorithm.Value);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsSha3))]
        [InlineData(Oids.Sha3_256)]
        [InlineData(Oids.Sha3_384)]
        [InlineData(Oids.Sha3_512)]
        public static void ComputeSignature_Ecdsa_Sha3_Roundtrip(string hashAlgorithm)
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);
            byte[] cmsBytes;

            using (X509Certificate2 cert = Certificates.ECDsaP521Win.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                signer.DigestAlgorithm = new Oid(hashAlgorithm, null);

                cms.ComputeSignature(signer);
                cmsBytes = cms.Encode();
                cms = new SignedCms();
                cms.Decode(cmsBytes);
                cms.CheckSignature(true); // Assert.NoThrow
                Assert.Single(cms.SignerInfos);

                SignerInfo signerInfo = cms.SignerInfos[0];
                Assert.Equal(hashAlgorithm, signerInfo.DigestAlgorithm.Value);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.DoesNotSupportSha3))]
        [InlineData(Oids.Sha3_256)]
        [InlineData(Oids.Sha3_384)]
        [InlineData(Oids.Sha3_512)]
        public static void ComputeSignature_Sha3_NotSupported(string hashAlgorithm)
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3 });
            SignedCms cms = new SignedCms(content);

            using (X509Certificate2 cert = Certificates.RSA2048SignatureOnly.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                signer.DigestAlgorithm = new Oid(hashAlgorithm, null);

                Assert.Throws<CryptographicException>(() => cms.ComputeSignature(signer));
            }
        }

        [Fact]
        public static void ExistingDocument_Rsa_Sha3_256()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.Rsa_Sha3_256_SignedDocument);

            if (PlatformDetection.SupportsSha3)
            {
                cms.CheckSignature(true); // Assert.NoThrow
                Assert.Single(cms.SignerInfos);

                SignerInfo signerInfo = cms.SignerInfos[0];
                Assert.Equal(Oids.Sha3_256, signerInfo.DigestAlgorithm.Value);
            }
            else
            {
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
            }
        }

        [Fact]
        public static void ExistingDocument_Rsa_Sha3_384()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.Rsa_Sha3_384_SignedDocument);

            if (PlatformDetection.SupportsSha3)
            {
                cms.CheckSignature(true); // Assert.NoThrow
                Assert.Single(cms.SignerInfos);

                SignerInfo signerInfo = cms.SignerInfos[0];
                Assert.Equal(Oids.Sha3_384, signerInfo.DigestAlgorithm.Value);
            }
            else
            {
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
            }
        }

        [Fact]
        public static void ExistingDocument_Rsa_Sha3_512()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.Rsa_Sha3_512_SignedDocument);

            if (PlatformDetection.SupportsSha3)
            {
                cms.CheckSignature(true); // Assert.NoThrow
                Assert.Single(cms.SignerInfos);

                SignerInfo signerInfo = cms.SignerInfos[0];
                Assert.Equal(Oids.Sha3_512, signerInfo.DigestAlgorithm.Value);
            }
            else
            {
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
            }
        }

        [Fact]
        public static void ExistingDocument_Ecdsa_Sha256_FromNetFX()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.Ecdsa_Sha256_FromNetFX_SignedDocument);

            cms.CheckSignature(true); // Assert.NoThrow
            Assert.Single(cms.SignerInfos);

            SignerInfo signerInfo = cms.SignerInfos[0];
            Assert.Equal(Oids.Sha256, signerInfo.DigestAlgorithm.Value);
            Assert.Equal(Oids.EcPublicKey, signerInfo.SignatureAlgorithm.Value);
        }

        private static void VerifyWithExplicitPrivateKey(X509Certificate2 cert, AsymmetricAlgorithm key)
        {
            using (var pubCert = new X509Certificate2(cert.RawData))
            {
                Assert.False(pubCert.HasPrivateKey);

                byte[] content = { 9, 8, 7, 6, 5 };
                ContentInfo contentInfo = new ContentInfo(content);

                SignedCms cms = new SignedCms(contentInfo);
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, pubCert, key)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly,
                    DigestAlgorithm = key is DSA ? new Oid(Oids.Sha1, Oids.Sha1) : new Oid(Oids.Sha256, Oids.Sha256)
                };

                cms.ComputeSignature(signer);
                cms.CheckSignature(true);

                Assert.Equal(1, cms.SignerInfos.Count);
                Assert.Equal(pubCert, cms.SignerInfos[0].Certificate);
            }
        }

        private static void VerifyCounterSignatureWithExplicitPrivateKey(X509Certificate2 cert, AsymmetricAlgorithm key, X509Certificate2 counterSignerCert, AsymmetricAlgorithm counterSignerKey)
        {
            Assert.NotNull(key);
            Assert.NotNull(counterSignerKey);
            using (var pubCert = new X509Certificate2(cert.RawData))
            using (var counterSignerPubCert = new X509Certificate2(counterSignerCert.RawData))
            {
                Assert.False(pubCert.HasPrivateKey);

                byte[] content = { 9, 8, 7, 6, 5 };
                ContentInfo contentInfo = new ContentInfo(content);

                SignedCms cms = new SignedCms(contentInfo);
                CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, pubCert, key)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly,
                    DigestAlgorithm = key is DSA ? new Oid(Oids.Sha1, Oids.Sha1) : new Oid(Oids.Sha256, Oids.Sha256)
                };

                CmsSigner cmsCounterSigner = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, counterSignerPubCert, counterSignerKey)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly,
                    DigestAlgorithm = counterSignerKey is DSA ? new Oid(Oids.Sha1, Oids.Sha1) : new Oid(Oids.Sha256, Oids.Sha256)
                };

                cms.ComputeSignature(cmsSigner);
                Assert.Equal(1, cms.SignerInfos.Count);
                Assert.Equal(pubCert, cms.SignerInfos[0].Certificate);

                cms.SignerInfos[0].ComputeCounterSignature(cmsCounterSigner);
                cms.CheckSignature(true);

                Assert.Equal(1, cms.SignerInfos[0].CounterSignerInfos.Count);
                Assert.Equal(counterSignerPubCert, cms.SignerInfos[0].CounterSignerInfos[0].Certificate);
            }
        }

        private static int CountCertificateChoices(byte[] encoded)
        {
            AsnReader reader = new AsnReader(encoded, AsnEncodingRules.BER);
            reader = reader.ReadSequence();
            reader.ReadObjectIdentifier();
            reader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            reader = reader.ReadSequence();

            reader.ReadInteger(); // version
            reader.ReadSetOf(); // digestAlgorithms
            reader.ReadSequence(); // encapsulatedContentInfo

            Asn1Tag expectedTag = new Asn1Tag(TagClass.ContextSpecific, 0, true); // certificates[0]

            if (reader.PeekTag() == expectedTag)
            {
                AsnReader certs = reader.ReadSetOf(expectedTag);
                int count = 0;

                while (certs.HasData)
                {
                    certs.ReadEncodedValue();
                    count++;
                }

                return count;
            }

            return 0;
        }
    }
}
